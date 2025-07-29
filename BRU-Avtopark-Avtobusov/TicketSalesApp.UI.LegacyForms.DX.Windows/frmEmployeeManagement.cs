using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Views.Base;
using NLog;
using TicketSalesApp.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.ObjectModel;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

// ******************************************************************************
// **                    ДОБРО ПОЖАЛОВАТЬ В ПРЕИСПОДНЮЮ                         **
// **                (Спонсор: Windows Forms и .NET Framework 4)               **
// ******************************************************************************
//
// Этот величественный монолит кода (более 1200 строк отборного процедурного спагетти)
// выполняет, казалось бы, простую задачу: отображение списка сотрудников.
// Но не дайте себя обмануть! Под капотом скрывается эпическая сага о борьбе
// с ограничениями древних технологий.
//
// Этапы Великого Пути Данных:
// 1.  **ПОЛУЧЕНИЕ JSON С ТОГО СВЕТА:** Мы обращаемся к `ticketsalesapp.adminserver`, чтобы получить JSON, полный коварных `$id` и `$ref`,
//     порожденных Entity Framework и любовью Newtonsoft.Json к циклическим ссылкам.
// 2.  **ОБРАБОТКА НАПИЛЬНИКОМ:** В методе `ProcessJsonToXml` мы вручную,
//     с помощью регулярных выражений и магии JObject/JArray, вычищаем это JSON-непотребство.
//     Мы избавляемся от `$ref`, удаляем ненужные `$id`, распутываем клубки `$values`,
//     чтобы получить хоть какое-то подобие нормального массива данных.
// 3.  **АЛХИМИЯ: JSON -> XML:** Поскольку Windows Forms в .NET 4
//     не поддерживает прямую привязку к JSON, мы вынуждены прибегнуть
//     к конвертации нашего очищенного JSON в XML. Мы превращаем структурированные данные в еще один текстовый формат.
// 4.  **ПАРСИНГ XML (СНОВА ВРУЧНУЮ):** Теперь, когда у нас есть XML, мы снова
//     парсим его (ведь `DeserializeXmlNode` создает свою, особую структуру),
//     чтобы наконец-то создать наши ViewModel и показать их пользователю.
// 5.  **ПРОФИТ?..** Оно работает. Наверное. До следующего странного бага.
//
// Я просто ОБОЖАЮ Windows Forms. Серьезно. Это вершина инженерной мысли.
//
// P.S. Не пытайтесь это рефакторить. Оно проклято.
namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public class EmployeeViewModel
    {
        public Employee EmployeeData { get; private set; }

        public EmployeeViewModel(Employee employee)
        {
            EmployeeData = employee;
        }

        public long Id { get { return EmployeeData.EmpId; } }
        public string Surname { get { return EmployeeData.Surname; } }
        public string Name { get { return EmployeeData.Name; } }
        public string Patronym { get { return EmployeeData.Patronym; } }
        public string FullName
        {
            get
            {
                var parts = new[] { EmployeeData.Surname, EmployeeData.Name, EmployeeData.Patronym };
                return string.Join(" ", parts.Where(p => !string.IsNullOrEmpty(p)).ToArray()).Trim();
            }
        }
        public DateTime EmployedSince { get { return EmployeeData.EmployedSince; } }
        public string JobTitle { get { return EmployeeData.Job != null ? EmployeeData.Job.JobTitle : "[N/A]"; } }
        public long JobId { get { return EmployeeData.JobId; } }
    }

    public partial class frmEmployeeManagement : DevExpress.XtraEditors.XtraForm
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly ApiClientService _apiClient;
        private readonly string _baseUrl = "http://localhost:5000/api";
        private BindingList<EmployeeViewModel> _employeeViewModels = new BindingList<EmployeeViewModel>();
        private List<Job> _availableJobs = new List<Job>();
        private List<Marshut> _availableRoutes = new List<Marshut>();
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public frmEmployeeManagement()
        {
            InitializeComponent();
            _apiClient = ApiClientService.Instance;
            gridControlEmployees.DataSource = _employeeViewModels;

            gridViewEmployees.CustomUnboundColumnData += gridViewEmployees_CustomUnboundColumnData;
            _apiClient.OnAuthTokenChanged -= HandleAuthTokenChanged;
            _apiClient.OnAuthTokenChanged += HandleAuthTokenChanged;

            this.Load += frmEmployeeManagement_Load;
            this.FormClosing += FrmEmployeeManagement_FormClosing;

            UpdateButtonStates();
        }

        private void HandleAuthTokenChanged(object sender, string token)
        {
            Log.Debug("Auth token changed, triggering synchronous data reload.");
            LoadDataSynchronously();
        }

        private void frmEmployeeManagement_Load(object sender, EventArgs e)
        {
            Log.Debug("frmEmployeeManagement_Load event triggered.");
            LoadDataSynchronously();
        }

        private void FrmEmployeeManagement_FormClosing(object sender, FormClosingEventArgs e)
        {
            Log.Debug("Form closing.");
            _apiClient.OnAuthTokenChanged -= HandleAuthTokenChanged;
        }

        private void SetLoadingState(bool isLoading)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(delegate() { SetLoadingState(isLoading); }));
                return;
            }

            Log.Debug(isLoading ? "Setting UI to loading state." : "Setting UI to normal state.");
            Cursor = isLoading ? Cursors.WaitCursor : Cursors.Default;
            gridControlEmployees.Enabled = !isLoading;
            btnAdd.Enabled = !isLoading;
            btnEdit.Enabled = !isLoading && gridViewEmployees.GetFocusedRow() is EmployeeViewModel;
            btnDelete.Enabled = !isLoading && gridViewEmployees.GetFocusedRow() is EmployeeViewModel;
            btnRefresh.Enabled = !isLoading;
            txtSearch.Enabled = !isLoading;

            if (!isLoading)
            {
                UpdateButtonStates();
            }
            else
            {
                btnEdit.Enabled = false;
                btnDelete.Enabled = false;
            }
        }

        private void LoadDataSynchronously()
        {
            Log.Info("Starting synchronous data load process with manual array handling...");
            SetLoadingState(true);

            // Show a "Please wait" dynamic XtraForm
            var waitMessageBox = new XtraForm
            {
                Text = "Загрузка данных...",
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Size = new Size(320, 120),
                ControlBox = false
            };
            var label = new Label
            {
                Text = "Пожалуйста, подождите, пока данные загружаются...",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            waitMessageBox.Controls.Add(label);
            waitMessageBox.Show();

            HttpClient client = null;
            string jobsJsonRaw = null;
            string routesJsonRaw = null;
            string employeesJsonRaw = null;

            List<Job> loadedJobs = new List<Job>();
            List<Marshut> loadedRoutes = new List<Marshut>();
            List<Employee> loadedEmployees = new List<Employee>();

            XDocument jobsXml = XDocument.Parse("<Root><Jobs></Jobs></Root>"); // Default empty
            XDocument routesXml = XDocument.Parse("<Root><Routes></Routes></Root>"); // Default empty
            XDocument employeesXml = XDocument.Parse("<Root><Employees></Employees></Root>"); // Default empty

            try
            {
                client = _apiClient.CreateClient();

                // --- Fetch Data ---
                // (Fetching logic remains the same)
                try
                {
                    Log.Debug("Fetching Jobs synchronously...");
                    var jobsApiUrl = string.Format("{0}/Jobs", _baseUrl);
                    HttpResponseMessage jobsResponse = client.GetAsync(jobsApiUrl).Result;
                    if (jobsResponse.IsSuccessStatusCode)
                    {
                        byte[] jobsBytes = jobsResponse.Content.ReadAsByteArrayAsync().Result;
                        jobsJsonRaw = Encoding.UTF8.GetString(jobsBytes);
                        Log.Debug("Jobs JSON fetched successfully.");
                    }
                    else { throw new Exception("Failed to load Jobs: " + jobsResponse.ReasonPhrase); }
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Error fetching Jobs. Exception: {0}", ex.ToString());
                    Log.Error(errorMsg); // Log error but continue to allow others to load
                }

                try
                {
                    Log.Debug("Fetching Routes synchronously...");
                    var routesApiUrl = string.Format("{0}/Routes", _baseUrl);
                    HttpResponseMessage routesResponse = client.GetAsync(routesApiUrl).Result;
                    if (routesResponse.IsSuccessStatusCode)
                    {
                        byte[] routesBytes = routesResponse.Content.ReadAsByteArrayAsync().Result;
                        routesJsonRaw = Encoding.UTF8.GetString(routesBytes);
                        Log.Debug("Routes JSON fetched successfully.");
                    }
                    else { throw new Exception("Failed to load Routes: " + routesResponse.ReasonPhrase); }
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Error fetching Routes. Exception: {0}", ex.ToString());
                    Log.Error(errorMsg); // Log error but continue
                }

                try
                {
                    Log.Debug("Fetching Employees synchronously...");
                    var employeesApiUrl = string.Format("{0}/Employees?includeJob=true&includeRoute=true", _baseUrl);
                    HttpResponseMessage employeesResponse = client.GetAsync(employeesApiUrl).Result;
                    if (employeesResponse.IsSuccessStatusCode)
                    {
                        byte[] employeesBytes = employeesResponse.Content.ReadAsByteArrayAsync().Result;
                        employeesJsonRaw = Encoding.UTF8.GetString(employeesBytes);
                        Log.Debug("Employees JSON fetched successfully.");
                    }
                    else { throw new Exception("Failed to load Employees: " + employeesResponse.ReasonPhrase); }
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Error fetching Employees. Exception: {0}", ex.ToString());
                    Log.Error(errorMsg); // Log error but continue
                }

                Log.Debug("Manually handling arrays and converting to XML...");

                // --- Process Jobs ---
                try
                {
                    if (!string.IsNullOrEmpty(jobsJsonRaw))
                    {
                        jobsXml = ProcessJsonToXml(jobsJsonRaw, "Jobs");
                        Log.Debug("Jobs JSON processed for XML conversion.");
                    } else {
                        Log.Warn("jobsJsonRaw was null or empty, using default empty Jobs XML.");
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Error processing Jobs JSON to XML. Exception: {0}", ex.ToString());
                    Log.Error(errorMsg);
                    // jobsXml retains its default empty value
                }

                // --- Process Routes ---
                try
                {
                     if (!string.IsNullOrEmpty(routesJsonRaw))
                    {
                        routesXml = ProcessJsonToXml(routesJsonRaw, "Routes");
                        Log.Debug("Routes JSON processed for XML conversion.");
                    } else {
                        Log.Warn("routesJsonRaw was null or empty, using default empty Routes XML.");
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Error processing Routes JSON to XML. Exception: {0}", ex.ToString());
                    Log.Error(errorMsg);
                    // routesXml retains its default empty value
                }

                // --- Process Employees ---
                try
                {
                    if (!string.IsNullOrEmpty(employeesJsonRaw))
                    {
                        employeesXml = ProcessJsonToXml(employeesJsonRaw, "Employees");
                        Log.Debug("Employees JSON processed for XML conversion.");
                    } else {
                        Log.Warn("employeesJsonRaw was null or empty, using default empty Employees XML.");
                    }
                }
                catch (Exception ex)
                {
                     string errorMsg = string.Format("Error processing Employees JSON to XML. Exception: {0}", ex.ToString());
                     Log.Error(errorMsg);
                    // employeesXml retains its default empty value
                }

                // --- XML Parsing Logic (Remains mostly unchanged) ---
                Log.Debug("Parsing XML data into objects...");
                try
                {
                    // Iterate directly over <Jobs> elements under <Root>
                    foreach (XElement jobNode in jobsXml.Root.Elements("Jobs"))
                    {
                        try
                        {
                            Job job = new Job();
                            long jobId;
                            XElement jobIdElement = jobNode.Element("jobId");
                            if (jobIdElement != null && long.TryParse(jobIdElement.Value, out jobId))
                            {
                                job.JobId = jobId;
                            }
                            else
                            {
                                Log.Warn(string.Format("Could not parse jobId for element: {0}. Skipping job.", jobNode.ToString()));
                                continue;
                            }

                            XElement jobTitleElement = jobNode.Element("jobTitle");
                            job.JobTitle = (jobTitleElement != null) ? jobTitleElement.Value : string.Empty;

                            XElement internshipElement = jobNode.Element("internship");
                            job.Internship = (internshipElement != null) ? internshipElement.Value : string.Empty;

                            job.Employees = new List<Employee>(); // Initialize

                            // --- CORRECTED Nested Employee Parsing ---
                            // Iterate over all <employees> elements under the jobNode
                            foreach (XElement empNodeInJob in jobNode.Elements("employees"))
                            {
                                try
                                {
                                    // Each <employees> element represents one employee here
                                    if (!empNodeInJob.HasElements)
                                    {
                                        Log.Debug("Skipping empty placeholder <employees> element in job {0}.", job.JobId);
                                        continue;
                                    }

                                    long empIdInJob;
                                    XElement empIdElementInJob = empNodeInJob.Element("empId");
                                    if (empIdElementInJob != null && long.TryParse(empIdElementInJob.Value, out empIdInJob))
                                    {
                                        // Create minimal stub
                                        job.Employees.Add(new Employee { EmpId = empIdInJob, JobId = job.JobId });
                                    }
                                    else
                                    {
                                         Log.Warn(string.Format("Could not parse empId from nested <employees> node within job {0}. Node: {1}", job.JobId, empNodeInJob.ToString()));
                                    }
                                }
                                catch (Exception exEmpNode)
                                {
                                    string errorMsgEmp = string.Format("Error parsing nested Employee XML node within Job {0}: {1}. Node: {2}", job.JobId, exEmpNode.ToString(), empNodeInJob.ToString());
                                    Log.Error(errorMsgEmp);
                                }
                            }
                            // --- END CORRECTED Nested Employee Parsing ---

                            loadedJobs.Add(job);
                        }
                        catch (Exception exNode)
                        {
                            string errorMsgNode = string.Format("Error parsing individual Job XML node: {0}. Node: {1}", exNode.ToString(), jobNode.ToString());
                            Log.Error(errorMsgNode);
                        }
                    }
                    Log.Debug("Parsed {0} jobs from XML.", loadedJobs.Count);
                }
                catch (Exception ex)
                {
                    string errorMsgXml = string.Format("Error parsing Jobs XML. Exception: {0}", ex.ToString());
                    Log.Error(errorMsgXml);
                    // Continue with potentially empty loadedJobs
                }

                 try
                 {
                     // Iterate directly over <Routes> elements under <Root>
                     foreach (XElement routeNode in routesXml.Root.Elements("Routes"))
                     {
                         try
                         {
                             // --- ADDED Check for empty elements ---
                             if (!routeNode.HasElements)
                             {
                                 Log.Debug("Skipping empty <Routes> element potentially from cleaned $ref.");
                                 continue;
                             }
                             // --- END Check ---

                             Marshut route = new Marshut();
                             long routeId;
                             XElement routeIdElement = routeNode.Element("routeId");
                             if (routeIdElement != null && long.TryParse(routeIdElement.Value, out routeId))
                             {
                                 route.RouteId = routeId;
                             }
                             else { Log.Warn(string.Format("Could not parse routeId for element: {0}. Skipping route.", routeNode.ToString())); continue; }

                             XElement startElement = routeNode.Element("startPoint");
                             route.StartPoint = (startElement != null) ? startElement.Value : string.Empty;
                             XElement endElement = routeNode.Element("endPoint");
                             route.EndPoint = (endElement != null) ? endElement.Value : string.Empty;
                             // ... parse other route properties ...

                             // Handle potential nested Employee (often replaced by empty {} from $ref)
                             XElement employeeElement = routeNode.Element("employee");
                             if (employeeElement != null) {
                                 XElement driverIdElement = employeeElement.Element("empId"); // Check inside employee
                                 long driverId;
                                 if(driverIdElement != null && long.TryParse(driverIdElement.Value, out driverId)) {
                                     route.DriverId = driverId;
                                 }
                                 // else: could be empty {} from $ref, DriverId remains default
                             }

                             // Handle potential nested Bus (often replaced by empty {} from $ref)
                              XElement busElement = routeNode.Element("avtobus");
                             if (busElement != null) {
                                 XElement busIdElement = busElement.Element("busId"); // Check inside avtobus
                                 long busId;
                                 if(busIdElement != null && long.TryParse(busIdElement.Value, out busId)) {
                                     route.BusId = busId;
                                 }
                                 // else: could be empty {} from $ref, BusId remains default
                             }

                             loadedRoutes.Add(route);
                        }
                         catch (Exception exNode) { Log.Error(string.Format("Error parsing individual Route XML node: {0}. Node: {1}", exNode.ToString(), routeNode.ToString())); }
                     }
                     Log.Debug("Parsed {0} routes from XML.", loadedRoutes.Count);
                 }
                 catch (Exception ex)
                 {
                     string errorMsgXml = string.Format("Error parsing Routes XML. Exception: {0}", ex.ToString());
                     Log.Error(errorMsgXml);
                     // Continue
                 }

                 try
                 {
                     // Iterate directly over <Employees> elements under <Root>
                    foreach (XElement empNode in employeesXml.Root.Elements("Employees"))
                    {
                        try
                        {
                            // --- ADDED Check for empty elements ---
                             if (!empNode.HasElements)
                             {
                                 Log.Debug("Skipping empty <Employees> element potentially from cleaned $ref.");
                                 continue;
                             }
                             // --- END Check ---

                            Employee emp = new Employee();
                            long empId;
                            XElement empIdElement = empNode.Element("empId");
                            if (empIdElement != null && long.TryParse(empIdElement.Value, out empId))
                            {
                                emp.EmpId = empId;
                            }
                            else { Log.Warn(string.Format("Could not parse empId for element: {0}. Skipping employee.", empNode.ToString())); continue; }

                            XElement surnameElement = empNode.Element("surname");
                            emp.Surname = (surnameElement != null) ? surnameElement.Value : string.Empty;
                            XElement nameElement = empNode.Element("name");
                            emp.Name = (nameElement != null) ? nameElement.Value : string.Empty;
                            XElement patronymElement = empNode.Element("patronym");
                            emp.Patronym = (patronymElement != null) ? patronymElement.Value : string.Empty;

                            DateTime employedSince;
                            XElement employedSinceElement = empNode.Element("employedSince");
                            if (employedSinceElement != null &&
                                (DateTime.TryParse(employedSinceElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out employedSince) ||
                                 DateTime.TryParse(employedSinceElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out employedSince) ||
                                 DateTime.TryParseExact(employedSinceElement.Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out employedSince)))
                            {
                                emp.EmployedSince = employedSince.ToLocalTime();
                            }
                            else { emp.EmployedSince = DateTime.MinValue; Log.Warn(string.Format("Could not parse EmployedSince for EmpId {0}", emp.EmpId)); }

                            long jobId = 0; // Default
                            XElement jobIdElement = empNode.Element("jobId"); // Direct jobId from Employee
                            XElement jobContainerElement = empNode.Element("job"); // Check for nested <job>

                            if (jobIdElement != null && long.TryParse(jobIdElement.Value, out jobId))
                            {
                                emp.JobId = jobId;
                            }
                            else if (jobContainerElement != null) // Nested job element exists
                            {
                                XElement nestedJobIdElement = jobContainerElement.Element("jobId");
                                if (nestedJobIdElement != null && long.TryParse(nestedJobIdElement.Value, out jobId))
                                {
                                    emp.JobId = jobId; // Parsed from nested job
                                } else {
                                     // Could be the empty {} placeholder from $ref cleaning
                                     if (!jobContainerElement.HasElements && !jobContainerElement.HasAttributes && string.IsNullOrEmpty(jobContainerElement.Value)) {
                                         Log.Debug("Found empty placeholder for job property for EmpId {0}. JobId remains 0.", emp.EmpId);
                                     } else {
                                          Log.Warn(string.Format("Could not parse jobid from nested job element for EmpId {0}. Node: {1}", emp.EmpId, jobContainerElement.ToString()));
                                     }
                                }
                            }
                            else
                            {
                                Log.Warn(string.Format("Could not determine JobId for EmpId {0}", emp.EmpId));
                            }

                            loadedEmployees.Add(emp);
                        }
                        catch (Exception exNode) { Log.Error(string.Format("Error parsing individual Employee XML node: {0}. Node: {1}", exNode.ToString(), empNode.ToString())); }
                    }
                     Log.Debug("Parsed {0} employees from XML.", loadedEmployees.Count);
                 }
                 catch (Exception ex)
                 {
                     string errorMsgXml = string.Format("Error parsing Employees XML. Exception: {0}", ex.ToString());
                     Log.Error(errorMsgXml);
                     // Continue
                 }


                // --- Link Data and Update UI ---
                Log.Debug("Populating internal collections and UI...");
                _availableJobs = loadedJobs;
                _availableRoutes = loadedRoutes;

                foreach (var emp in loadedEmployees)
                {
                    emp.Job = _availableJobs.FirstOrDefault(j => j.JobId == emp.JobId);
                    // --- ADDED Debug Logging for Job Linking ---
                    if (emp.Job != null)
                    {
                        string logMsg = string.Format("Successfully linked Job ID {0} ('{1}') to Employee ID {2}.", emp.Job.JobId, emp.Job.JobTitle, emp.EmpId);
                         Log.Debug(logMsg);
                    }
                    else if (emp.JobId != 0) // Log warning only if JobId was expected
                    {
                        string logMsg = string.Format("Could not find/link Job with ID {0} for Employee ID {1}.", emp.JobId, emp.EmpId);
                         Log.Warn(logMsg);
                    }
                    else
                    {
                        string logMsg = string.Format("Employee ID {0} has no JobId (JobId is 0), skipping link.", emp.EmpId);
                         Log.Debug(logMsg);
                    }
                    // --- END Debug Logging ---
                }

                var tempViewModelList = loadedEmployees.Select(emp => new EmployeeViewModel(emp)).ToList();

                // Safely update BindingList on UI thread (using BeginInvoke within the try block is fine here)
                Action updateAction = delegate()
                {
                    if (this.IsDisposed) return;
                    _employeeViewModels.RaiseListChangedEvents = false;
                    _employeeViewModels.Clear();
                    foreach (var vm in tempViewModelList) { _employeeViewModels.Add(vm); }
                    _employeeViewModels.RaiseListChangedEvents = true;
                    _employeeViewModels.ResetBindings();
                };
                if (this.InvokeRequired) { this.BeginInvoke(updateAction); }
                else { updateAction(); }

                Log.Info("Synchronous data load completed successfully using manual array handling. Loaded {0} jobs, {1} routes, {2} employees.",
                         _availableJobs.Count, _availableRoutes.Count, _employeeViewModels.Count);
            }
            catch (Exception ex)
            {
                 string criticalErrorMsg = string.Format("Critical error during synchronous data load process. Exception: {0}", ex.ToString());
                 Log.Error(criticalErrorMsg);
                 Action errorAction = delegate()
                 {
                    if (this.IsDisposed) return;
                     _availableJobs.Clear();
                     _availableRoutes.Clear();
                _employeeViewModels.Clear();
                     XtraMessageBox.Show("Произошла критическая ошибка при загрузке данных. См. лог.\n" + ex.Message, "Ошибка Загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 };
                 if (this.InvokeRequired) { this.BeginInvoke(errorAction); } else { errorAction(); }
            }
            finally
            {
                if (client != null) { client.Dispose(); }

                // Close the "Please wait" message box
                waitMessageBox.Close();

                // Marshal final UI state reset back to the UI thread
                Action finalUiAction = delegate()
                {
                    if (this.IsDisposed) { Log.Debug("Form disposed before final UI state could be reset."); return; }
                    SetLoadingState(false);
                    // Refresh/Filter/Update handled by the main updateAction or errorAction
                    Log.Debug("Finished final UI state reset after synchronous load attempt.");
                };
                 if (this.InvokeRequired) { this.BeginInvoke(finalUiAction); } else { finalUiAction(); }
            }
        }

        // --- NEW HELPER METHOD to recursively build a map of all $id objects ---
        private static void BuildGlobalIdMap(JToken token, Dictionary<string, JObject> idMap)
        {
            if (token == null) return;

            JObject obj = token as JObject;
            if (obj != null && obj.Property("$id") != null)
            {
                string idValue = obj.Property("$id").Value.ToString();
                if (!idMap.ContainsKey(idValue))
                {
                    idMap.Add(idValue, obj);
                    // Log.Debug($"Mapped item with $id: {idValue} from nested structure."); // Optional: more verbose logging
                }
                // Do NOT stop traversal here, continue into children
            }

            // Recursively traverse children
            if (token.HasValues)
            {
                foreach (JToken child in token.Children())
                {
                    BuildGlobalIdMap(child, idMap);
                }
            }
        }

        // --- MODIFIED HELPER METHOD for processing JSON to XML with manual array handling ---
        private static XDocument ProcessJsonToXml(string jsonRaw, string rootElementName)
        {
            Log.Debug(string.Format("Processing raw JSON for {0}: {1}", rootElementName, jsonRaw));
            string preCleanedJson = Regex.Replace(jsonRaw, @"[\u0000-\u001F]", ""); 
            JToken rootToken = JToken.Parse(preCleanedJson);
            JObject finalObjectForXml = null;

            // --- Build the GLOBAL ID Map FIRST ---
            Dictionary<string, JObject> globalIdMap = new Dictionary<string, JObject>();
            BuildGlobalIdMap(rootToken, globalIdMap);
            Log.Debug(string.Format("Built GLOBAL ID map with {0} entries for {1} structure.", globalIdMap.Count, rootElementName));
            // -------------------------------------

            JObject initialObj = rootToken as JObject;

            // CASE 1: Root IS the common {$id:"...", $values:[...]} structure
            if (initialObj != null && initialObj.Property("$values") != null && initialObj.Property("$values").Value.Type == JTokenType.Array &&
                (initialObj.Count == 1 || (initialObj.Count == 2 && initialObj.Property("$id") != null)))
            {
                Log.Debug(string.Format("Detected root as object containing $values array for {0}.", rootElementName));
                JArray innerArray = (JArray)initialObj.Property("$values").Value; // The top-level $values
                List<JToken> cleanedItems = new List<JToken>();
                
                // --- START: Resolve top-level $refs using GLOBAL map ---
                //Dictionary<string, JObject> idMap = new Dictionary<string, JObject>(); // No longer needed here
                List<JToken> resolvedItems = new List<JToken>();

                // Iterate through the original top-level $values array to resolve
                foreach (JToken item in innerArray)
                {
                    JObject itemObj = item as JObject;
                    JProperty refProp = itemObj != null ? itemObj.Property("$ref") : null; // C# 4.0 compatible

                    if (itemObj != null && refProp != null && itemObj.Count == 1) // It's a $ref object
                    {
                        string refValue = refProp.Value.ToString();
                        if (globalIdMap.ContainsKey(refValue)) // Use GLOBAL map
                        {
                            Log.Debug(string.Format("Resolving top-level $ref '{0}'...", refValue));
                            resolvedItems.Add(globalIdMap[refValue].DeepClone()); // Add a CLONE of the referenced object
                        }
                        else
                        {
                            // This warning is now more significant if it appears
                            Log.Warn(string.Format("Could not resolve top-level $ref '{0}' for {1}. Reference not found in GLOBAL ID map. Skipping item.", refValue, rootElementName));
                        }
                    }
                    else if (itemObj != null && itemObj.Property("$id") != null) // It's an object with an $id (already in global map)
                    {
                         Log.Debug(string.Format("Adding directly defined top-level item with $id: {0}", itemObj.Property("$id").Value));
                         resolvedItems.Add(item); // Add the original object from the top-level array
                    }
                     else if (itemObj != null && !itemObj.HasValues)
                     {
                          Log.Debug("Skipping empty object item from original top-level array.");
                     }
                     else // It's something else unexpected in the top-level $values
                    {
                        Log.Warn(string.Format("Unexpected item type or structure found in top-level $values array for {0}: {1}. Adding directly, might cause issues.", rootElementName, item.GetType().Name));
                        resolvedItems.Add(item);
                     }
                 }
                 Log.Debug(string.Format("Resolved top-level $values array for {0} contains {1} items.", rootElementName, resolvedItems.Count));
                // --- END: Resolve top-level $refs ---


                // Clean the RESOLVED items
                foreach (JToken item in resolvedItems)
                {
                    JToken cleanedItem = CleanAndTransformJsonToken(item);
                    if (cleanedItem != null && cleanedItem.Type != JTokenType.Null)
                    {
                        cleanedItems.Add(cleanedItem);
                    } else { Log.Warn("Cleaned item resulted in null, skipping add."); }
                }
                
                // Filter out empty objects AFTER cleaning
                var filteredItems = cleanedItems.Where(delegate(JToken t) {
                    JObject jobj = t as JObject;
                    return (jobj == null || jobj.HasValues); 
                }).ToList();
                string filterLogMsg = string.Format("Filtered {0} empty objects from {1} cleaned items for {2}", cleanedItems.Count - filteredItems.Count, cleanedItems.Count, rootElementName);
                Log.Debug(filterLogMsg);
                // ---------------------------------------------------------------

                finalObjectForXml = new JObject(new JProperty(rootElementName, new JArray(filteredItems)));
            }
            // CASE 2: Root is some OTHER structure 
            else
            {
                Log.Debug(string.Format("Root token for {0} is not the typical {{$id,$values}} object (Type: {1}). Cleaning token directly.", rootElementName, rootToken.Type));
                JToken cleanedToken = CleanAndTransformJsonToken(rootToken);
                
                // If cleaning resulted in an array, create the structure {RootName: cleanedArray}
                if (cleanedToken is JArray)
                {
                     finalObjectForXml = new JObject(new JProperty(rootElementName, cleanedToken));
                }
                 // If cleaning resulted in an object or something else, wrap it {RootName: {cleanedToken}} (or empty object if null)
                else
                {
                    finalObjectForXml = new JObject(new JProperty(rootElementName, cleanedToken ?? new JObject()));
                }

                // Fallback check: If the cleaned token *still* looks like {"$values": [...]}, extract the inner array.
                 JObject potentiallyStillWrapped = cleanedToken as JObject;
                 if (potentiallyStillWrapped != null && potentiallyStillWrapped.Count == 1 && potentiallyStillWrapped.Property("$values") != null && potentiallyStillWrapped.Property("$values").Value is JArray)
                 {
                     Log.Warn(string.Format("Cleaned token for {0} still contained $values wrapper. Extracting inner array.", rootElementName));
                     finalObjectForXml = new JObject(new JProperty(rootElementName, potentiallyStillWrapped.Property("$values").Value));
                 }
                 // --- REFACTORED: Filter empty objects using C# 4.0 syntax ---
                 else if (finalObjectForXml != null)
                 {
                     JProperty rootProp = finalObjectForXml.Property(rootElementName);
                     if (rootProp != null && rootProp.Value != null && rootProp.Value.Type == JTokenType.Array)
                     {
                         JArray arrayVal = (JArray)rootProp.Value;
                         // Use anonymous delegate for compatibility
                         var filteredItems = arrayVal.Where(delegate(JToken t) {
                             // Logic: Keep if NOT (it's a JObject AND it has no values)
                             //        Keep if (it's NOT a JObject OR it HAS values)
                             JObject jobj = t as JObject;
                             return (jobj == null || jobj.HasValues);
                         }).ToList();

                         if (filteredItems.Count < arrayVal.Count)
                         {
                             string filterLogMsgCase2 = string.Format("Filtered {0} empty objects from {1} cleaned items in CASE 2 for {2}", arrayVal.Count - filteredItems.Count, arrayVal.Count, rootElementName);
                             Log.Debug(filterLogMsgCase2);
                             finalObjectForXml = new JObject(new JProperty(rootElementName, new JArray(filteredItems))); // Update with filtered array
                         }
                    }
                 }
                 // ----------------------------------------------------------------------------------------
            }

            // Convert the final reconstructed JObject to string for XML conversion
            string finalJsonForXml = finalObjectForXml.ToString(Newtonsoft.Json.Formatting.None);
            Log.Debug(string.Format("Final {0} JSON prepared for XML conversion: {1}", rootElementName, finalJsonForXml));

            // Convert to XML
            try
            {
                XmlDocument xmlDoc = JsonConvert.DeserializeXmlNode(finalJsonForXml, "Root", false);
                return XDocument.Load(new XmlNodeReader(xmlDoc));
            }
            catch (Exception xmlEx)
            {
                string errorMsg = string.Format("Final XML conversion failed for {0}. JSON used: {1}. Exception: {2}", rootElementName, finalJsonForXml, xmlEx.ToString());
                Log.Error(errorMsg);
                throw new Exception(string.Format("Ошибка конвертации {0} JSON в XML.", rootElementName), xmlEx); // Re-throw
            }
        }

        // --- MODIFIED HELPER METHOD for JToken Cleaning ---
        private static JToken CleanAndTransformJsonToken(JToken token)
        {
            if (token == null) return null;

            // REMOVED: Top-level $values check (handled outside now)

            switch (token.Type)
            {
                case JTokenType.Object:
                    {
                        JObject obj = (JObject)token;

                        // Check for {$ref: "..."} object
                        if (obj.Count == 1 && obj.Property("$ref") != null)
                        {
                             string refValue = obj.Property("$ref").Value.ToString();
                             string originalPropertyName = (token.Parent is JProperty) ? ((JProperty)token.Parent).Name : "";
                             string refLogMsg = string.Format("Found $ref '{0}' under property '{1}', replacing with empty object {{}}.", refValue, originalPropertyName);
                             Log.Debug(refLogMsg);
                             return new JObject(); // Replace ALL $refs with an empty object
                         }

                        // Process regular object properties recursively
                        JObject cleanedObj = new JObject();
                        foreach (var property in obj.Properties())
                        {
                            if (property.Name.Equals("$id", StringComparison.OrdinalIgnoreCase))
                            {
                                Log.Debug("Removing $id property.");
                                continue; // Skip $id
                             }

                            // Recursively clean the property's value FIRST
                            JToken cleanedValue = CleanAndTransformJsonToken(property.Value);

                            // Check for nested $values wrapper in the cleaned value
                            JObject valueObj = cleanedValue as JObject;
                            if (valueObj != null && valueObj.Count == 1 && valueObj.Property("$values") != null && valueObj.Property("$values").Value.Type == JTokenType.Array)
                            {
                                string nestedValuesLogMsg = string.Format("Found nested $values wrapper in property '{0}', replacing with inner array content.", property.Name);
                                Log.Debug(nestedValuesLogMsg);
                                // Use the cleaned inner array directly (call CleanAndTransformJsonToken on the value)
                                cleanedValue = CleanAndTransformJsonToken(valueObj.Property("$values").Value); 
                            }

                            // Add the property if the cleaned value is not null
                            if (cleanedValue != null && cleanedValue.Type != JTokenType.Null)
                            {
                                cleanedObj.Add(property.Name, cleanedValue);
                            } else {
                                string skipPropLogMsg = string.Format("Skipping property '{0}' because its cleaned value was null.", property.Name);
                                Log.Debug(skipPropLogMsg);
                            }
                        }
                        return cleanedObj;
                    }

                case JTokenType.Array:
                    {
                        JArray array = (JArray)token;
                        JArray cleanedArray = new JArray();
                        foreach (var item in array)
                        {
                            JToken cleanedItem = CleanAndTransformJsonToken(item); // Recursively clean each item
                            // Add item if it's not null
                            if (cleanedItem != null && cleanedItem.Type != JTokenType.Null)
                            {
                                cleanedArray.Add(cleanedItem);
                            } else {
                                Log.Debug("Skipping array item because its cleaned value was null.");
                            }
                        }
                        // Ensure we return the cleaned array
                        return cleanedArray;
                    }

                default:
                    // For simple types like String, Integer, Boolean, etc., return as is.
                    return token;
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            ShowEditEmployeeForm(null);
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            var selectedViewModel = gridViewEmployees.GetFocusedRow() as EmployeeViewModel;
            if (selectedViewModel == null) return;
            ShowEditEmployeeForm(selectedViewModel.EmployeeData);
        }

        private void ShowEditEmployeeForm(Employee employeeToEdit)
        {
            if (_availableJobs == null || !_availableJobs.Any())
            {
                XtraMessageBox.Show("Данные о должностях не загружены или пусты. Невозможно добавить/редактировать сотрудника.", "Ошибка данных", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var form = new XtraForm())
            {
                bool isAdding = employeeToEdit == null;
                form.Text = isAdding ? "Добавить сотрудника" : "Редактировать сотрудника";
                form.Width = 550;
                form.Height = 450;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MinimizeBox = false;
                form.MaximizeBox = false;

                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
                form.Controls.Add(panel);

                int yPos = 20;
                int labelWidth = 150;
                int controlWidth = 350;
                int spacing = 30;

                var surnameLabel = new LabelControl { Text = "Фамилия:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var surnameBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.Surname : "") };
                panel.Controls.AddRange(new Control[] { surnameLabel, surnameBox });
                yPos += spacing;

                var nameLabel = new LabelControl { Text = "Имя:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var nameBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.Name : "") };
                panel.Controls.AddRange(new Control[] { nameLabel, nameBox });
                yPos += spacing;

                var patronymLabel = new LabelControl { Text = "Отчество:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var patronymBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.Patronym : "") };
                panel.Controls.AddRange(new Control[] { patronymLabel, patronymBox });
                yPos += spacing;

                var employedDateLabel = new LabelControl { Text = "Дата приема:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var employedDateEdit = new DateEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos) };
                employedDateEdit.DateTime = (employeeToEdit != null && employeeToEdit.EmployedSince > DateTime.MinValue) ? employeeToEdit.EmployedSince : DateTime.Today;
                employedDateEdit.Properties.Mask.EditMask = "dd.MM.yyyy";
                employedDateEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
                panel.Controls.AddRange(new Control[] { employedDateLabel, employedDateEdit });
                yPos += spacing;

                var jobLabel = new LabelControl { Text = "Должность:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var jobComboBox = new LookUpEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos) };
                jobComboBox.Properties.DataSource = _availableJobs;
                jobComboBox.Properties.DisplayMember = "JobTitle";
                jobComboBox.Properties.ValueMember = "JobId";
                jobComboBox.Properties.Columns.Clear();
                jobComboBox.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("JobTitle", "Название"));
                jobComboBox.Properties.NullText = "[Не выбрана]";
                jobComboBox.Properties.ShowHeader = false;
                jobComboBox.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.False;
                if (employeeToEdit != null && _availableJobs.Any(j => j.JobId == employeeToEdit.JobId)) {
                    jobComboBox.EditValue = employeeToEdit.JobId;
                } else {
                    jobComboBox.EditValue = null;
                }
                panel.Controls.AddRange(new Control[] { jobLabel, jobComboBox });
                yPos += spacing + 10;

                var saveButton = new SimpleButton { Text = isAdding ? "Добавить" : "Обновить", Width = 100, Location = new System.Drawing.Point(form.ClientSize.Width / 2 - 110, yPos), Anchor = AnchorStyles.Top | AnchorStyles.Left };
                var cancelButton = new SimpleButton { Text = "Отмена", Width = 100, Location = new System.Drawing.Point(form.ClientSize.Width / 2 + 10, yPos), Anchor = AnchorStyles.Top | AnchorStyles.Left };
                panel.Controls.Add(saveButton);
                panel.Controls.Add(cancelButton);

                form.CancelButton = cancelButton;
                cancelButton.Click += (s, args) => form.Close();

                saveButton.Click += async (s, args) =>
                {
                    if (string.IsNullOrWhiteSpace(surnameBox.Text) || string.IsNullOrWhiteSpace(nameBox.Text))
                    {
                        XtraMessageBox.Show("Фамилия и Имя обязательны.", "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (jobComboBox.EditValue == null || jobComboBox.EditValue == DBNull.Value || !(jobComboBox.EditValue is long) || (long)jobComboBox.EditValue <= 0) {
                        XtraMessageBox.Show("Необходимо выбрать действительную должность.", "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (employedDateEdit.DateTime == DateTime.MinValue) {
                         XtraMessageBox.Show("Необходимо указать корректную дату приема.", "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    HttpClient crudClient = null;
                    try
                    {
                        saveButton.Enabled = false;
                        cancelButton.Enabled = false;
                        form.Cursor = Cursors.WaitCursor;

                        long selectedJobId = Convert.ToInt64(jobComboBox.EditValue);

                        var employeeData = new Employee
                        {
                            EmpId = isAdding ? 0 : employeeToEdit.EmpId,
                            Surname = surnameBox.Text.Trim(),
                            Name = nameBox.Text.Trim(),
                            Patronym = patronymBox.Text.Trim(),
                            EmployedSince = employedDateEdit.DateTime,
                            JobId = selectedJobId,
                            Job = null
                        };

                        crudClient = _apiClient.CreateClient();
                        HttpResponseMessage response;
                        string jsonPayload = JsonConvert.SerializeObject(employeeData, _jsonSettings);
                        HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                        string apiUrl;
                        if (isAdding)
                        {
                            apiUrl = string.Format("{0}/Employees", _baseUrl);
                            Log.Debug("Posting new employee to: {0}", apiUrl);
                            response = await crudClient.PostAsync(apiUrl, content).ConfigureAwait(false);
                        }
                        else
                        {
                            apiUrl = string.Format("{0}/Employees/{1}", _baseUrl, employeeData.EmpId);
                            Log.Debug("Putting updated employee to: {0}", apiUrl);
                            response = await crudClient.PutAsync(apiUrl, content).ConfigureAwait(false);
                        }

                        if (!form.IsDisposed)
                        {
                            form.BeginInvoke(new Action(async delegate()
                            {
                                if (form.IsDisposed) return;

                                if (response.IsSuccessStatusCode)
                                {
                                    string infoDetails = string.Format("Employee saved successfully (Target ID: {0})", employeeData.EmpId);
                                    Log.Info(infoDetails);
                            form.DialogResult = DialogResult.OK;
                            form.Close();
                                    LoadDataSynchronously();
                        }
                        else
                        {
                                    var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                                    if (!form.IsDisposed)
                                    {
                                        form.BeginInvoke(new Action(delegate() {
                                            if (form.IsDisposed) return;
                                            string errorDetails = string.Format("Failed to save employee. Status: {0}, Error: {1}", response.StatusCode, error);
                                            Log.Error(errorDetails);
                            XtraMessageBox.Show(string.Format("Не удалось сохранить сотрудника: {0}", error), "Ошибка API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            saveButton.Enabled = true;
                                            cancelButton.Enabled = true;
                                            form.Cursor = Cursors.Default;
                                        }));
                                    }
                                }
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!form.IsDisposed)
                        {
                             form.BeginInvoke(new Action(delegate() {
                                if (form.IsDisposed) return;
                                string errorDetails = string.Format("Error saving employee: {0}", ex.ToString());
                                Log.Error(errorDetails);
                        XtraMessageBox.Show(string.Format("Ошибка при сохранении сотрудника: {0}", ex.Message), "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                saveButton.Enabled = true;
                                cancelButton.Enabled = true;
                                form.Cursor = Cursors.Default;
                             }));
                        }
                    }
                    finally
                    {
                        if (crudClient != null) crudClient.Dispose();
                        if (!form.IsDisposed && form.Cursor == Cursors.WaitCursor) {
                             form.BeginInvoke(new Action(delegate() { if (!form.IsDisposed) form.Cursor = Cursors.Default; }));
                        }
                    }
                };

                form.ShowDialog(this);
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            var selectedViewModel = gridViewEmployees.GetFocusedRow() as EmployeeViewModel;
            if (selectedViewModel == null) return;

            var empData = selectedViewModel.EmployeeData;
            var result = XtraMessageBox.Show(string.Format("Вы уверены, что хотите удалить сотрудника '{0}' (ID: {1})?",
                                                selectedViewModel.FullName,
                                                empData.EmpId),
                                              "Подтверждение удаления",
                                              MessageBoxButtons.YesNo,
                                              MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            HttpClient crudClient = null;
            SetLoadingState(true);
             try
            {
                crudClient = _apiClient.CreateClient();
                var apiUrl = string.Format("{0}/Employees/{1}", _baseUrl, empData.EmpId);
                Log.Debug("Deleting employee from: {0}", apiUrl);

                using (var response = await crudClient.DeleteAsync(apiUrl).ConfigureAwait(false))
                {
                    this.BeginInvoke(new Action(async delegate()
                    {
                        if (this.IsDisposed) return;

                    if (response.IsSuccessStatusCode)
                    {
                            string infoDetails = string.Format("Employee deleted successfully: ID {0}", empData.EmpId);
                            Log.Info(infoDetails);
                        XtraMessageBox.Show("Сотрудник успешно удален.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadDataSynchronously();
                    }
                    else
                    {
                            var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            this.BeginInvoke(new Action(delegate() {
                                string errorDetails = string.Format("Failed to delete employee. Status: {0}, Error: {1}", response.StatusCode, error);
                                Log.Error(errorDetails);
                        XtraMessageBox.Show(string.Format("Не удалось удалить сотрудника: {0}\n{1}", response.ReasonPhrase, error), "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                SetLoadingState(false);
                            }));
                        }
                    }));
                    }
                }
                catch (Exception ex)
                {
                this.BeginInvoke(new Action(delegate() {
                    string errorDetails = string.Format("Exception deleting employee: {0}", ex.ToString());
                    Log.Error(errorDetails);
                    XtraMessageBox.Show(string.Format("Произошла ошибка при удалении сотрудника: {0}", ex.Message), "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetLoadingState(false);
                }));
            }
            finally
            {
                if (crudClient != null) crudClient.Dispose();
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            Log.Debug("Refresh button clicked.");
            txtSearch.Text = string.Empty;
            LoadDataSynchronously();
        }

        private void gridViewEmployees_CustomUnboundColumnData(object sender, CustomColumnDataEventArgs e)
        {
            if (e.Row is EmployeeViewModel)
            {
                EmployeeViewModel vm = (EmployeeViewModel)e.Row;

                // Check for the ACTUAL FieldName from the designer
                if (e.Column.FieldName == "Job.JobTitle" && e.IsGetData) 
                {
                    // --- Added Detailed Logging for JobTitle --- 
                    string jobTitleValue = vm.JobTitle; // Get the value from the view model property
                    bool isJobNull = vm.EmployeeData.Job == null;
                    string logMsg = string.Format("CustomUnboundColumnData: EmpId={0}, FieldName='{1}', IsJobNull={2}, JobTitleValue='{3}'", 
                                                vm.Id, e.Column.FieldName, isJobNull, jobTitleValue ?? "NULL");
                    Log.Debug(logMsg);
                    // --- End Detailed Logging ---

                    e.Value = jobTitleValue; // Assign the retrieved value
                }
                else if (e.Column.FieldName == "UnboundEmpId" && e.IsGetData)
                {
                    e.Value = vm.Id; // Get value from ViewModel's Id property
                }
            }
        }

        private void gridViewEmployees_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(UpdateButtonStates));
                return;
            }

            bool isRowSelected = gridViewEmployees.GetFocusedRow() is EmployeeViewModel;
            bool isLoading = Cursor == Cursors.WaitCursor;

            btnAdd.Enabled = !isLoading;
            btnEdit.Enabled = !isLoading && isRowSelected;
            btnDelete.Enabled = !isLoading && isRowSelected;
            btnRefresh.Enabled = !isLoading;
            txtSearch.Enabled = !isLoading && _employeeViewModels != null;
        }

        private void txtSearch_EditValueChanged(object sender, EventArgs e)
        {
            FilterAndBindEmployees();
        }

        private void FilterAndBindEmployees()
        {
            Log.Debug("Applying client-side filter...");
            var searchText = txtSearch.Text.Trim().ToLowerInvariant();
            var originalSource = _employeeViewModels;

            if (originalSource == null) {
                gridControlEmployees.DataSource = null;
                Log.Warn("FilterAndBindEmployees called but _employeeViewModels is null.");
                return;
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                gridControlEmployees.DataSource = _employeeViewModels;
                gridControlEmployees.RefreshDataSource();
                Log.Debug("Search text empty, showing all {0} employees.", _employeeViewModels.Count);
            }
            else
            {
                var filteredList = originalSource.Where(delegate(EmployeeViewModel vm) {
                    bool nameMatch = vm.FullName != null && vm.FullName.ToLowerInvariant().Contains(searchText);
                    bool jobMatch = vm.JobTitle != null && vm.JobTitle.ToLowerInvariant().Contains(searchText);
                    return nameMatch || jobMatch;
                }).ToList();

                var filteredBindingList = new BindingList<EmployeeViewModel>(filteredList);
                gridControlEmployees.DataSource = filteredBindingList;
                gridControlEmployees.RefreshDataSource();
                string debugDetails = string.Format("Filter applied. Displaying {0} of {1} employees.", filteredList.Count, originalSource.Count);
                Log.Debug(debugDetails);
            }
        }
    }
} 
