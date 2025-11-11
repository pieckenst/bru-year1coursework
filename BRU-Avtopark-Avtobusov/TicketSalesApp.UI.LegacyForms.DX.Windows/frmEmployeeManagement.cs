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
        
        // New HR fields
        public string DepartmentName { get { return EmployeeData.Department != null ? EmployeeData.Department.DepartmentName : "[N/A]"; } }
        public long? DepartmentId { get { return EmployeeData.DepartmentId; } }
        public string Email { get { return EmployeeData.Email ?? ""; } }
        public string PersonalPhone { get { return EmployeeData.PersonalPhone ?? ""; } }
        public string WorkPhone { get { return EmployeeData.WorkPhone ?? ""; } }
        public string Status { get { return EmployeeData.IsActive ? "Активен" : "Уволен"; } }
        public DateTime? DateOfBirth { get { return EmployeeData.DateOfBirth; } }
        public string Address { get { return EmployeeData.Address ?? ""; } }
        public string DriverLicenseNumber { get { return EmployeeData.DriverLicenseNumber ?? ""; } }
        public string DriverLicenseCategory { get { return EmployeeData.DriverLicenseCategory ?? ""; } }
    }

    public partial class frmEmployeeManagement : DevExpress.XtraEditors.XtraForm
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly ApiClientService _apiClient;
        private readonly string _baseUrl = "http://localhost:5000/api";
        private BindingList<EmployeeViewModel> _employeeViewModels = new BindingList<EmployeeViewModel>();
        private List<Job> _availableJobs = new List<Job>();
        
        // Essential fields to preserve during circular reference cleanup
        private static readonly string[] ESSENTIAL_JOB_FIELDS = new string[] { "jobId", "jobTitle", "salary" };
        private static readonly string[] ESSENTIAL_DEPARTMENT_FIELDS = new string[] { "departmentId", "departmentName" };
        private static readonly string[] ESSENTIAL_EMPLOYEE_FIELDS = new string[] { "empId", "surname", "name", "patronym", "email", "personalPhone", "workPhone", "address", "dateOfBirth", "passportSeries", "passportNumber", "inn", "snils", "driverLicenseNumber", "driverLicenseCategory", "driverLicenseIssueDate", "driverLicenseExpiryDate", "medicalCertificateNumber", "medicalCertificateIssueDate", "medicalCertificateExpiryDate", "lastMedicalCheckDate", "nextMedicalCheckDate", "hasPassengerTransportCertification", "hasDangerousGoodsCertification", "employedSince", "isActive", "jobId", "job", "departmentId", "department" };
        private List<Marshut> _availableRoutes = new List<Marshut>();
        private List<Department> _availableDepartments = new List<Department>();
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
            string departmentsJsonRaw = null;

            List<Job> loadedJobs = new List<Job>();
            List<Marshut> loadedRoutes = new List<Marshut>();
            List<Employee> loadedEmployees = new List<Employee>();
            List<Department> loadedDepartments = new List<Department>();

            XDocument jobsXml = XDocument.Parse("<Root><Jobs></Jobs></Root>"); // Default empty
            XDocument routesXml = XDocument.Parse("<Root><Routes></Routes></Root>"); // Default empty
            XDocument employeesXml = XDocument.Parse("<Root><Employees></Employees></Root>"); // Default empty
            XDocument departmentsXml = XDocument.Parse("<Root><Departments></Departments></Root>"); // Default empty

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
                    Log.Debug("Fetching Departments synchronously...");
                    var departmentsApiUrl = string.Format("{0}/Departments", _baseUrl);
                    HttpResponseMessage departmentsResponse = client.GetAsync(departmentsApiUrl).Result;
                    if (departmentsResponse.IsSuccessStatusCode)
                    {
                        byte[] departmentsBytes = departmentsResponse.Content.ReadAsByteArrayAsync().Result;
                        departmentsJsonRaw = Encoding.UTF8.GetString(departmentsBytes);
                        Log.Debug("Departments JSON fetched successfully.");
                    }
                    else { throw new Exception("Failed to load Departments: " + departmentsResponse.ReasonPhrase); }
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Error fetching Departments. Exception: {0}", ex.ToString());
                    Log.Error(errorMsg); // Log error but continue
                }

                try
                {
                    Log.Debug("Fetching Employees synchronously...");
                    var employeesApiUrl = string.Format("{0}/Employees?includeJob=true&includeRoute=true&includeDepartment=true", _baseUrl);
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

                // --- Process Departments ---
                try
                {
                    if (!string.IsNullOrEmpty(departmentsJsonRaw))
                    {
                        departmentsXml = ProcessJsonToXml(departmentsJsonRaw, "Departments");
                        Log.Debug("Departments JSON processed for XML conversion.");
                    } else {
                        Log.Warn("departmentsJsonRaw was null or empty, using default empty Departments XML.");
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Error processing Departments JSON to XML. Exception: {0}", ex.ToString());
                    Log.Error(errorMsg);
                    // departmentsXml retains its default empty value
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

                 // --- Parse Departments XML ---
                 try
                 {
                     // Iterate directly over <Departments> elements under <Root>
                     foreach (XElement deptNode in departmentsXml.Root.Elements("Departments"))
                     {
                         try
                         {
                             if (!deptNode.HasElements)
                             {
                                 Log.Debug("Skipping empty <Departments> element.");
                                 continue;
                             }

                             Department dept = new Department();
                             long deptId;
                             XElement deptIdElement = deptNode.Element("departmentId");
                             if (deptIdElement != null && long.TryParse(deptIdElement.Value, out deptId))
                             {
                                 dept.DepartmentId = deptId;
                             }
                             else { Log.Warn(string.Format("Could not parse departmentId for element: {0}. Skipping department.", deptNode.ToString())); continue; }

                             XElement nameElement = deptNode.Element("departmentName");
                             dept.DepartmentName = (nameElement != null) ? nameElement.Value : string.Empty;

                             XElement codeElement = deptNode.Element("departmentCode");
                             dept.DepartmentCode = (codeElement != null) ? codeElement.Value : string.Empty;

                             XElement descElement = deptNode.Element("description");
                             dept.Description = (descElement != null) ? descElement.Value : string.Empty;

                             dept.Employees = new List<Employee>(); // Initialize
                             dept.ChildDepartments = new List<Department>();

                             loadedDepartments.Add(dept);
                         }
                         catch (Exception exNode) { Log.Error(string.Format("Error parsing individual Department XML node: {0}. Node: {1}", exNode.ToString(), deptNode.ToString())); }
                     }
                     Log.Debug("Parsed {0} departments from XML.", loadedDepartments.Count);
                 }
                 catch (Exception ex)
                 {
                     string errorMsgXml = string.Format("Error parsing Departments XML. Exception: {0}", ex.ToString());
                     Log.Error(errorMsgXml);
                     // Continue
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

                            // Parse DepartmentId
                            long departmentId = 0;
                            XElement deptIdElement = empNode.Element("departmentId");
                            XElement deptContainerElement = empNode.Element("department");
                            
                            if (deptIdElement != null && long.TryParse(deptIdElement.Value, out departmentId))
                            {
                                emp.DepartmentId = departmentId;
                            }
                            else if (deptContainerElement != null)
                            {
                                XElement nestedDeptIdElement = deptContainerElement.Element("departmentId");
                                if (nestedDeptIdElement != null && long.TryParse(nestedDeptIdElement.Value, out departmentId))
                                {
                                    emp.DepartmentId = departmentId;
                                }
                            }

                            // Parse other HR fields
                            XElement emailElement = empNode.Element("email");
                            emp.Email = (emailElement != null) ? emailElement.Value : null;

                            XElement personalPhoneElement = empNode.Element("personalPhone");
                            emp.PersonalPhone = (personalPhoneElement != null) ? personalPhoneElement.Value : null;

                            XElement workPhoneElement = empNode.Element("workPhone");
                            emp.WorkPhone = (workPhoneElement != null) ? workPhoneElement.Value : null;

                            XElement addressElement = empNode.Element("address");
                            emp.Address = (addressElement != null) ? addressElement.Value : null;

                            // Parse Date of Birth
                            DateTime dateOfBirth;
                            XElement dobElement = empNode.Element("dateOfBirth");
                            if (dobElement != null &&
                                (DateTime.TryParse(dobElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dateOfBirth) ||
                                 DateTime.TryParse(dobElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOfBirth) ||
                                 DateTime.TryParseExact(dobElement.Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOfBirth)))
                            {
                                emp.DateOfBirth = dateOfBirth.ToLocalTime();
                            }

                            // Parse Passport
                            XElement passportSeriesElement = empNode.Element("passportSeries");
                            emp.PassportSeries = (passportSeriesElement != null) ? passportSeriesElement.Value : null;
                            XElement passportNumberElement = empNode.Element("passportNumber");
                            emp.PassportNumber = (passportNumberElement != null) ? passportNumberElement.Value : null;

                            // Parse INN and SNILS
                            XElement innElement = empNode.Element("inn");
                            emp.INN = (innElement != null) ? innElement.Value : null;
                            XElement snilsElement = empNode.Element("snils");
                            emp.SNILS = (snilsElement != null) ? snilsElement.Value : null;

                            // Parse Driver License
                            XElement dlNumberElement = empNode.Element("driverLicenseNumber");
                            emp.DriverLicenseNumber = (dlNumberElement != null) ? dlNumberElement.Value : null;
                            XElement dlCategoryElement = empNode.Element("driverLicenseCategory");
                            emp.DriverLicenseCategory = (dlCategoryElement != null) ? dlCategoryElement.Value : null;

                            DateTime dlIssueDate;
                            XElement dlIssueDateElement = empNode.Element("driverLicenseIssueDate");
                            if (dlIssueDateElement != null &&
                                (DateTime.TryParse(dlIssueDateElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dlIssueDate) ||
                                 DateTime.TryParse(dlIssueDateElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dlIssueDate) ||
                                 DateTime.TryParseExact(dlIssueDateElement.Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dlIssueDate)))
                            {
                                emp.DriverLicenseIssueDate = dlIssueDate.ToLocalTime();
                            }

                            DateTime dlExpiryDate;
                            XElement dlExpiryDateElement = empNode.Element("driverLicenseExpiryDate");
                            if (dlExpiryDateElement != null &&
                                (DateTime.TryParse(dlExpiryDateElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dlExpiryDate) ||
                                 DateTime.TryParse(dlExpiryDateElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dlExpiryDate) ||
                                 DateTime.TryParseExact(dlExpiryDateElement.Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dlExpiryDate)))
                            {
                                emp.DriverLicenseExpiryDate = dlExpiryDate.ToLocalTime();
                            }

                            // Parse Medical Certificate
                            XElement medCertNumberElement = empNode.Element("medicalCertificateNumber");
                            emp.MedicalCertificateNumber = (medCertNumberElement != null) ? medCertNumberElement.Value : null;

                            DateTime medCertIssueDate;
                            XElement medCertIssueDateElement = empNode.Element("medicalCertificateIssueDate");
                            if (medCertIssueDateElement != null &&
                                (DateTime.TryParse(medCertIssueDateElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out medCertIssueDate) ||
                                 DateTime.TryParse(medCertIssueDateElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out medCertIssueDate) ||
                                 DateTime.TryParseExact(medCertIssueDateElement.Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out medCertIssueDate)))
                            {
                                emp.MedicalCertificateIssueDate = medCertIssueDate.ToLocalTime();
                            }

                            DateTime medCertExpiryDate;
                            XElement medCertExpiryDateElement = empNode.Element("medicalCertificateExpiryDate");
                            if (medCertExpiryDateElement != null &&
                                (DateTime.TryParse(medCertExpiryDateElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out medCertExpiryDate) ||
                                 DateTime.TryParse(medCertExpiryDateElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out medCertExpiryDate) ||
                                 DateTime.TryParseExact(medCertExpiryDateElement.Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out medCertExpiryDate)))
                            {
                                emp.MedicalCertificateExpiryDate = medCertExpiryDate.ToLocalTime();
                            }

                            DateTime lastMedCheck;
                            XElement lastMedCheckElement = empNode.Element("lastMedicalCheckDate");
                            if (lastMedCheckElement != null &&
                                (DateTime.TryParse(lastMedCheckElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out lastMedCheck) ||
                                 DateTime.TryParse(lastMedCheckElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out lastMedCheck) ||
                                 DateTime.TryParseExact(lastMedCheckElement.Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out lastMedCheck)))
                            {
                                emp.LastMedicalCheckDate = lastMedCheck.ToLocalTime();
                            }

                            DateTime nextMedCheck;
                            XElement nextMedCheckElement = empNode.Element("nextMedicalCheckDate");
                            if (nextMedCheckElement != null &&
                                (DateTime.TryParse(nextMedCheckElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out nextMedCheck) ||
                                 DateTime.TryParse(nextMedCheckElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out nextMedCheck) ||
                                 DateTime.TryParseExact(nextMedCheckElement.Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out nextMedCheck)))
                            {
                                emp.NextMedicalCheckDate = nextMedCheck.ToLocalTime();
                            }

                            // Parse Certifications
                            XElement hasPassengerCertElement = empNode.Element("hasPassengerTransportCertification");
                            bool hasPassengerCert;
                            if (hasPassengerCertElement != null && bool.TryParse(hasPassengerCertElement.Value, out hasPassengerCert))
                            {
                                emp.HasPassengerTransportCertification = hasPassengerCert;
                            }

                            XElement hasDangerousCertElement = empNode.Element("hasDangerousGoodsCertification");
                            bool hasDangerousCert;
                            if (hasDangerousCertElement != null && bool.TryParse(hasDangerousCertElement.Value, out hasDangerousCert))
                            {
                                emp.HasDangerousGoodsCertification = hasDangerousCert;
                            }

                            XElement isActiveElement = empNode.Element("isActive");
                            bool isActive;
                            if (isActiveElement != null && bool.TryParse(isActiveElement.Value, out isActive))
                            {
                                emp.IsActive = isActive;
                            }
                            else { emp.IsActive = true; } // Default to active

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
                _availableDepartments = loadedDepartments;

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

                    // Link Department
                    if (emp.DepartmentId.HasValue)
                    {
                        emp.Department = _availableDepartments.FirstOrDefault(d => d.DepartmentId == emp.DepartmentId.Value);
                        if (emp.Department != null)
                        {
                            string deptLogMsg = string.Format("Successfully linked Department ID {0} ('{1}') to Employee ID {2}.", emp.Department.DepartmentId, emp.Department.DepartmentName, emp.EmpId);
                            Log.Debug(deptLogMsg);
                        }
                        else
                        {
                            string deptLogMsg = string.Format("Could not find/link Department with ID {0} for Employee ID {1}.", emp.DepartmentId.Value, emp.EmpId);
                            Log.Warn(deptLogMsg);
                        }
                    }
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

                Log.Info("Synchronous data load completed successfully using manual array handling. Loaded {0} jobs, {1} routes, {2} departments, {3} employees.",
                         _availableJobs.Count, _availableRoutes.Count, _availableDepartments.Count, _employeeViewModels.Count);
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
                     _availableDepartments.Clear();
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
                    JToken cleanedItem = CleanAndTransformJsonToken(item, globalIdMap);
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
                JToken cleanedToken = CleanAndTransformJsonToken(rootToken, globalIdMap);
                
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

        // Helper method to extract essential fields from an object, including nested essential objects
        private static JObject ExtractEssentialFields(JObject obj, string[] essentialFields)
        {
            if (obj == null) return new JObject();
            
            JObject result = new JObject();
            foreach (string fieldName in essentialFields)
            {
                JToken value = obj[fieldName];
                if (value == null || value.Type == JTokenType.Null) continue;
                
                // Copy scalar values directly
                if (value.Type != JTokenType.Object && value.Type != JTokenType.Array)
                {
                    result[fieldName] = value.DeepClone();
                }
                // For nested objects, recursively extract their essential fields
                else if (value.Type == JTokenType.Object)
                {
                    JObject nestedObj = (JObject)value;
                    // Determine which essential fields to use based on nested object type
                    if (fieldName == "job" || nestedObj["jobTitle"] != null)
                        result[fieldName] = ExtractEssentialFields(nestedObj, ESSENTIAL_JOB_FIELDS);
                    else if (fieldName == "department" || nestedObj["departmentName"] != null)
                        result[fieldName] = ExtractEssentialFields(nestedObj, ESSENTIAL_DEPARTMENT_FIELDS);
                    // Skip unknown nested objects to avoid infinite recursion
                }
                // Skip arrays to avoid recursion
            }
            return result;
        }
        
        // --- MODIFIED HELPER METHOD for JToken Cleaning ---
        private static JToken CleanAndTransformJsonToken(JToken token, Dictionary<string, JObject> globalIdMap)
        {
            if (token == null) return null;

            // REMOVED: Top-level $values check (handled outside now)

            switch (token.Type)
            {
                case JTokenType.Object:
                    {
                        JObject obj = (JObject)token;

                        // Check for {$ref: "..."} object - resolve it using global map and extract essential fields
                        if (obj.Count == 1 && obj.Property("$ref") != null)
                        {
                             string refValue = obj.Property("$ref").Value.ToString();
                             string originalPropertyName = (token.Parent is JProperty) ? ((JProperty)token.Parent).Name : "";
                             
                             // Try to resolve the reference using global ID map
                             if (globalIdMap.ContainsKey(refValue))
                             {
                                 JObject referencedObj = globalIdMap[refValue];
                                 Log.Debug("Resolving $ref '{0}' under property '{1}' and extracting essential fields.", refValue, originalPropertyName);
                                 
                                 // Extract essential fields based on object type
                                 if (referencedObj["jobTitle"] != null || originalPropertyName == "job")
                                     return ExtractEssentialFields(referencedObj, ESSENTIAL_JOB_FIELDS);
                                 else if (referencedObj["departmentName"] != null || originalPropertyName == "department")
                                     return ExtractEssentialFields(referencedObj, ESSENTIAL_DEPARTMENT_FIELDS);
                                 else
                                 {
                                     Log.Warn("Could not determine type for $ref '{0}'. Returning empty object.", refValue);
                                     return new JObject();
                                 }
                             }
                             else
                             {
                                 Log.Warn("Could not resolve $ref '{0}' (not found in global map). Returning empty object.", refValue);
                                 return new JObject(); // Ref not found
                             }
                         }
                         
                        // Detect circular references by checking if we're processing the same object multiple times
                        // This happens when objects reference each other (Employee -> Job -> Employees)
                        JProperty idProp = obj.Property("$id");
                        if (idProp != null)
                        {
                            // Object has $id - check if it might contain circular refs based on context
                            string originalPropertyName = (token.Parent is JProperty) ? ((JProperty)token.Parent).Name : "";
                            
                            // If this is a nested Job or Department object, extract essential fields only
                            if (obj["jobTitle"] != null || originalPropertyName == "job")
                            {
                                Log.Debug("Extracting essential fields from Job object (ID: {0}) to prevent circular references.", idProp.Value);
                                return ExtractEssentialFields(obj, ESSENTIAL_JOB_FIELDS);
                            }
                            else if (obj["departmentName"] != null || originalPropertyName == "department")
                            {
                                Log.Debug("Extracting essential fields from Department object (ID: {0}) to prevent circular references.", idProp.Value);
                                return ExtractEssentialFields(obj, ESSENTIAL_DEPARTMENT_FIELDS);
                            }
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
                            JToken cleanedValue = CleanAndTransformJsonToken(property.Value, globalIdMap);

                            // Check for nested $values wrapper in the cleaned value
                            JObject valueObj = cleanedValue as JObject;
                            if (valueObj != null && valueObj.Count == 1 && valueObj.Property("$values") != null && valueObj.Property("$values").Value.Type == JTokenType.Array)
                            {
                                string nestedValuesLogMsg = string.Format("Found nested $values wrapper in property '{0}', replacing with inner array content.", property.Name);
                                Log.Debug(nestedValuesLogMsg);
                                // Use the cleaned inner array directly (call CleanAndTransformJsonToken on the value)
                                cleanedValue = CleanAndTransformJsonToken(valueObj.Property("$values").Value, globalIdMap); 
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
                            JToken cleanedItem = CleanAndTransformJsonToken(item, globalIdMap); // Recursively clean each item
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
                form.Width = 900;
                form.Height = 700;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.Sizable;
                form.MinimizeBox = true;
                form.MaximizeBox = true;

                // Scrollable panel for all fields
                var scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
                form.Controls.Add(scrollPanel);
                
                var panel = new Panel { Width = 850, Height = 1500, Location = new System.Drawing.Point(0, 0) };
                scrollPanel.Controls.Add(panel);

                int yPos = 20;
                int labelWidth = 180;
                int controlWidth = 250;
                int spacing = 30;
                int col1X = 10;
                int col2X = col1X + labelWidth + controlWidth + 30;

                // === PERSONAL INFO ===
                var surnameLabel = new LabelControl { Text = "Фамилия:*", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col1X, yPos) };
                var surnameBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(col1X + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.Surname : "") };
                var nameLabel = new LabelControl { Text = "Имя:*", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col2X, yPos) };
                var nameBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(col2X + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.Name : "") };
                panel.Controls.AddRange(new Control[] { surnameLabel, surnameBox, nameLabel, nameBox });
                yPos += spacing;

                var patronymLabel = new LabelControl { Text = "Отчество:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col1X, yPos) };
                var patronymBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(col1X + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.Patronym : "") };
                var emailLabel = new LabelControl { Text = "Email:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col2X, yPos) };
                var emailBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(col2X + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.Email : "") };
                panel.Controls.AddRange(new Control[] { patronymLabel, patronymBox, emailLabel, emailBox });
                yPos += spacing;

                var personalPhoneLabel = new LabelControl { Text = "Личный телефон:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col1X, yPos) };
                var personalPhoneBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(col1X + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.PersonalPhone : "") };
                var workPhoneLabel = new LabelControl { Text = "Рабочий телефон:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col2X, yPos) };
                var workPhoneBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(col2X + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.WorkPhone : "") };
                panel.Controls.AddRange(new Control[] { personalPhoneLabel, personalPhoneBox, workPhoneLabel, workPhoneBox });
                yPos += spacing;

                var addressLabel = new LabelControl { Text = "Адрес:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col1X, yPos) };
                var addressBox = new TextEdit { Width = 600, Location = new System.Drawing.Point(col1X + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.Address : "") };
                panel.Controls.AddRange(new Control[] { addressLabel, addressBox });
                yPos += spacing;

                // Date of Birth
                var dobLabel = new LabelControl { Text = "Дата рождения:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col1X, yPos) };
                var dobEdit = new DateEdit { Width = controlWidth, Location = new System.Drawing.Point(col1X + labelWidth + 10, yPos) };
                dobEdit.Properties.Mask.EditMask = "dd.MM.yyyy";
                dobEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
                dobEdit.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
                if (employeeToEdit != null && employeeToEdit.DateOfBirth.HasValue && employeeToEdit.DateOfBirth.Value.Year > 1900)
                    dobEdit.DateTime = employeeToEdit.DateOfBirth.Value;
                else
                    dobEdit.EditValue = null;
                panel.Controls.AddRange(new Control[] { dobLabel, dobEdit });
                yPos += spacing + 10;

                // === DOCUMENTS SECTION ===
                var docSectionLabel = new LabelControl { Text = "=== ДОКУМЕНТЫ ===", Font = new Font("Tahoma", 9, FontStyle.Bold), AutoSizeMode = LabelAutoSizeMode.None, Width = 800, Location = new System.Drawing.Point(col1X, yPos) };
                panel.Controls.Add(docSectionLabel);
                yPos += spacing;

                // Passport
                var passportSeriesLabel = new LabelControl { Text = "Серия паспорта:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col1X, yPos) };
                var passportSeriesBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(col1X + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.PassportSeries : "") };
                var passportNumberLabel = new LabelControl { Text = "Номер паспорта:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col2X, yPos) };
                var passportNumberBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(col2X + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.PassportNumber : "") };
                panel.Controls.AddRange(new Control[] { passportSeriesLabel, passportSeriesBox, passportNumberLabel, passportNumberBox });
                yPos += spacing;

                // INN / SNILS
                var innLabel = new LabelControl { Text = "ИНН:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col1X, yPos) };
                var innBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(col1X + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.INN : "") };
                var snilsLabel = new LabelControl { Text = "СНИЛС:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col2X, yPos) };
                var snilsBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(col2X + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.SNILS : "") };
                panel.Controls.AddRange(new Control[] { innLabel, innBox, snilsLabel, snilsBox });
                yPos += spacing + 10;

                // === DRIVER LICENSE SECTION ===
                var driverSectionLabel = new LabelControl { Text = "=== ВОДИТЕЛЬСКОЕ УДОСТОВЕРЕНИЕ ===", Font = new Font("Tahoma", 9, FontStyle.Bold), AutoSizeMode = LabelAutoSizeMode.None, Width = 800, Location = new System.Drawing.Point(col1X, yPos) };
                panel.Controls.Add(driverSectionLabel);
                yPos += spacing;

                // DL Number / Category
                var dlNumberLabel = new LabelControl { Text = "Номер ВУ:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col1X, yPos) };
                var dlNumberBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(col1X + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.DriverLicenseNumber : "") };
                var dlCategoryLabel = new LabelControl { Text = "Категория:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col2X, yPos) };
                var dlCategoryBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(col2X + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.DriverLicenseCategory : "") };
                panel.Controls.AddRange(new Control[] { dlNumberLabel, dlNumberBox, dlCategoryLabel, dlCategoryBox });
                yPos += spacing;

                // DL Issue / Expiry
                var dlIssueDateLabel = new LabelControl { Text = "Дата выдачи ВУ:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col1X, yPos) };
                var dlIssueDateEdit = new DateEdit { Width = controlWidth, Location = new System.Drawing.Point(col1X + labelWidth + 10, yPos) };
                dlIssueDateEdit.Properties.Mask.EditMask = "dd.MM.yyyy";
                dlIssueDateEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
                dlIssueDateEdit.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
                if (employeeToEdit != null && employeeToEdit.DriverLicenseIssueDate.HasValue && employeeToEdit.DriverLicenseIssueDate.Value.Year > 1900)
                    dlIssueDateEdit.DateTime = employeeToEdit.DriverLicenseIssueDate.Value;
                else
                    dlIssueDateEdit.EditValue = null;

                var dlExpiryDateLabel = new LabelControl { Text = "Срок действия до:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col2X, yPos) };
                var dlExpiryDateEdit = new DateEdit { Width = controlWidth, Location = new System.Drawing.Point(col2X + labelWidth + 10, yPos) };
                dlExpiryDateEdit.Properties.Mask.EditMask = "dd.MM.yyyy";
                dlExpiryDateEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
                dlExpiryDateEdit.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
                if (employeeToEdit != null && employeeToEdit.DriverLicenseExpiryDate.HasValue && employeeToEdit.DriverLicenseExpiryDate.Value.Year > 1900)
                    dlExpiryDateEdit.DateTime = employeeToEdit.DriverLicenseExpiryDate.Value;
                else
                    dlExpiryDateEdit.EditValue = null;
                panel.Controls.AddRange(new Control[] { dlIssueDateLabel, dlIssueDateEdit, dlExpiryDateLabel, dlExpiryDateEdit });
                yPos += spacing;

                // Certifications
                var passengerCertCheck = new CheckEdit { Width = 400, Location = new System.Drawing.Point(col1X + labelWidth + 10, yPos), Text = "Сертификация на перевозку пассажиров" };
                passengerCertCheck.Checked = (employeeToEdit != null && employeeToEdit.HasPassengerTransportCertification);
                panel.Controls.Add(passengerCertCheck);
                yPos += spacing;

                var dangerousGoodsCertCheck = new CheckEdit { Width = 400, Location = new System.Drawing.Point(col1X + labelWidth + 10, yPos), Text = "Сертификация на перевозку опасных грузов" };
                dangerousGoodsCertCheck.Checked = (employeeToEdit != null && employeeToEdit.HasDangerousGoodsCertification);
                panel.Controls.Add(dangerousGoodsCertCheck);
                yPos += spacing + 10;

                // === MEDICAL SECTION ===
                var medicalSectionLabel = new LabelControl { Text = "=== МЕДИЦИНСКАЯ ИНФОРМАЦИЯ ===", Font = new Font("Tahoma", 9, FontStyle.Bold), AutoSizeMode = LabelAutoSizeMode.None, Width = 800, Location = new System.Drawing.Point(col1X, yPos) };
                panel.Controls.Add(medicalSectionLabel);
                yPos += spacing;

                // Medical Cert Number
                var medCertNumberLabel = new LabelControl { Text = "Номер мед. справки:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col1X, yPos) };
                var medCertNumberBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(col1X + labelWidth + 10, yPos), Text = (employeeToEdit != null ? employeeToEdit.MedicalCertificateNumber : "") };
                panel.Controls.AddRange(new Control[] { medCertNumberLabel, medCertNumberBox });
                yPos += spacing;

                // Medical Cert Issue / Expiry
                var medCertIssueDateLabel = new LabelControl { Text = "Дата выдачи:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col1X, yPos) };
                var medCertIssueDateEdit = new DateEdit { Width = controlWidth, Location = new System.Drawing.Point(col1X + labelWidth + 10, yPos) };
                medCertIssueDateEdit.Properties.Mask.EditMask = "dd.MM.yyyy";
                medCertIssueDateEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
                medCertIssueDateEdit.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
                if (employeeToEdit != null && employeeToEdit.MedicalCertificateIssueDate.HasValue && employeeToEdit.MedicalCertificateIssueDate.Value.Year > 1900)
                    medCertIssueDateEdit.DateTime = employeeToEdit.MedicalCertificateIssueDate.Value;
                else
                    medCertIssueDateEdit.EditValue = null;

                var medCertExpiryDateLabel = new LabelControl { Text = "Срок действия до:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col2X, yPos) };
                var medCertExpiryDateEdit = new DateEdit { Width = controlWidth, Location = new System.Drawing.Point(col2X + labelWidth + 10, yPos) };
                medCertExpiryDateEdit.Properties.Mask.EditMask = "dd.MM.yyyy";
                medCertExpiryDateEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
                medCertExpiryDateEdit.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
                if (employeeToEdit != null && employeeToEdit.MedicalCertificateExpiryDate.HasValue && employeeToEdit.MedicalCertificateExpiryDate.Value.Year > 1900)
                    medCertExpiryDateEdit.DateTime = employeeToEdit.MedicalCertificateExpiryDate.Value;
                else
                    medCertExpiryDateEdit.EditValue = null;
                panel.Controls.AddRange(new Control[] { medCertIssueDateLabel, medCertIssueDateEdit, medCertExpiryDateLabel, medCertExpiryDateEdit });
                yPos += spacing + 10;

                // === EMPLOYMENT INFO ===
                var employedDateLabel = new LabelControl { Text = "Дата приема:*", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col1X, yPos) };
                var employedDateEdit = new DateEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos) };
                employedDateEdit.DateTime = (employeeToEdit != null && employeeToEdit.EmployedSince > DateTime.MinValue) ? employeeToEdit.EmployedSince : DateTime.Today;
                employedDateEdit.Properties.Mask.EditMask = "dd.MM.yyyy";
                employedDateEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
                var isActiveLabel = new LabelControl { Text = "Статус:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col2X, yPos) };
                var isActiveCheck = new CheckEdit { Width = controlWidth, Location = new System.Drawing.Point(col2X + labelWidth + 10, yPos), Text = "Активен" };
                isActiveCheck.Checked = (employeeToEdit == null || employeeToEdit.IsActive);
                panel.Controls.AddRange(new Control[] { employedDateLabel, employedDateEdit, isActiveLabel, isActiveCheck });
                yPos += spacing;

                var jobLabel = new LabelControl { Text = "Должность:*", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col1X, yPos) };
                var jobComboBox = new LookUpEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos) };
                jobComboBox.Properties.DataSource = _availableJobs;
                jobComboBox.Properties.DisplayMember = "JobTitle";
                jobComboBox.Properties.ValueMember = "JobId";
                jobComboBox.Properties.Columns.Clear();
                jobComboBox.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("JobTitle", "Название"));
                jobComboBox.Properties.NullText = "[Не выбрана]";
                jobComboBox.Properties.ShowHeader = false;
                jobComboBox.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.False;
                if (employeeToEdit != null && _availableJobs.Any(j => j.JobId == employeeToEdit.JobId))
                    jobComboBox.EditValue = employeeToEdit.JobId;
                else
                    jobComboBox.EditValue = null;
                
                var deptLabel = new LabelControl { Text = "Отдел:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(col2X, yPos) };
                var deptComboBox = new LookUpEdit { Width = controlWidth, Location = new System.Drawing.Point(col2X + labelWidth + 10, yPos) };
                deptComboBox.Properties.DataSource = _availableDepartments;
                deptComboBox.Properties.DisplayMember = "DepartmentName";
                deptComboBox.Properties.ValueMember = "DepartmentId";
                deptComboBox.Properties.Columns.Clear();
                deptComboBox.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("DepartmentName", "Название"));
                deptComboBox.Properties.NullText = "[Не выбран]";
                deptComboBox.Properties.ShowHeader = false;
                deptComboBox.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
                if (employeeToEdit != null && employeeToEdit.DepartmentId.HasValue && _availableDepartments.Any(d => d.DepartmentId == employeeToEdit.DepartmentId.Value))
                    deptComboBox.EditValue = employeeToEdit.DepartmentId.Value;
                else
                    deptComboBox.EditValue = null;
                panel.Controls.AddRange(new Control[] { jobLabel, jobComboBox, deptLabel, deptComboBox });
                yPos += spacing + 20;

                // === SECTION MANAGEMENT BUTTONS (only for existing employees) ===
                if (!isAdding && employeeToEdit != null)
                {
                    var sectionManagementLabel = new LabelControl { Text = "=== УПРАВЛЕНИЕ ДАННЫМИ ===", Font = new Font("Tahoma", 9, FontStyle.Bold), AutoSizeMode = LabelAutoSizeMode.None, Width = 800, Location = new System.Drawing.Point(col1X, yPos) };
                    panel.Controls.Add(sectionManagementLabel);
                    yPos += spacing;

                    var btnDocuments = new SimpleButton { Text = "Документы", Width = 180, Location = new System.Drawing.Point(col1X, yPos) };
                    var btnTrainings = new SimpleButton { Text = "Обучение", Width = 180, Location = new System.Drawing.Point(col1X + 190, yPos) };
                    var btnContacts = new SimpleButton { Text = "Контакты", Width = 180, Location = new System.Drawing.Point(col1X + 380, yPos) };
                    var btnVacations = new SimpleButton { Text = "Отпуска", Width = 180, Location = new System.Drawing.Point(col1X + 570, yPos) };

                    btnDocuments.Click += (s, args) => ShowDocumentsDialog(employeeToEdit.EmpId);
                    btnTrainings.Click += (s, args) => ShowTrainingsDialog(employeeToEdit.EmpId);
                    btnContacts.Click += (s, args) => ShowContactsDialog(employeeToEdit.EmpId);
                    btnVacations.Click += (s, args) => ShowVacationsDialog(employeeToEdit.EmpId);

                    panel.Controls.AddRange(new Control[] { btnDocuments, btnTrainings, btnContacts, btnVacations });
                    yPos += spacing + 20;
                }

                var saveButton = new SimpleButton { Text = isAdding ? "Добавить" : "Обновить", Width = 100, Location = new System.Drawing.Point(form.ClientSize.Width / 2 - 110, yPos), Anchor = AnchorStyles.Top | AnchorStyles.Left };
                var cancelButton = new SimpleButton { Text = "Отмена", Width = 100, Location = new System.Drawing.Point(form.ClientSize.Width / 2 + 10, yPos), Anchor = AnchorStyles.Top | AnchorStyles.Left };
                panel.Controls.Add(saveButton);
                panel.Controls.Add(cancelButton);

                form.CancelButton = cancelButton;
                cancelButton.Click += delegate(object s, EventArgs args) { form.Close(); };

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

                        long? selectedDeptId = null;
                        if (deptComboBox.EditValue != null && deptComboBox.EditValue != DBNull.Value && deptComboBox.EditValue is long)
                            selectedDeptId = (long)deptComboBox.EditValue;

                        var employeeData = new Employee
                        {
                            EmpId = isAdding ? 0 : employeeToEdit.EmpId,
                            Surname = surnameBox.Text.Trim(),
                            Name = nameBox.Text.Trim(),
                            Patronym = patronymBox.Text.Trim(),
                            EmployedSince = employedDateEdit.DateTime,
                            JobId = selectedJobId,
                            Job = null,
                            DepartmentId = selectedDeptId,
                            Department = null,
                            Email = emailBox.Text.Trim(),
                            PersonalPhone = personalPhoneBox.Text.Trim(),
                            WorkPhone = workPhoneBox.Text.Trim(),
                            Address = addressBox.Text.Trim(),
                            IsActive = isActiveCheck.Checked,
                            DateOfBirth = (dobEdit.EditValue != null && dobEdit.EditValue != DBNull.Value) ? (DateTime?)dobEdit.DateTime : null,
                            PassportSeries = passportSeriesBox.Text.Trim(),
                            PassportNumber = passportNumberBox.Text.Trim(),
                            INN = innBox.Text.Trim(),
                            SNILS = snilsBox.Text.Trim(),
                            DriverLicenseNumber = dlNumberBox.Text.Trim(),
                            DriverLicenseCategory = dlCategoryBox.Text.Trim(),
                            DriverLicenseIssueDate = (dlIssueDateEdit.EditValue != null && dlIssueDateEdit.EditValue != DBNull.Value) ? (DateTime?)dlIssueDateEdit.DateTime : null,
                            DriverLicenseExpiryDate = (dlExpiryDateEdit.EditValue != null && dlExpiryDateEdit.EditValue != DBNull.Value) ? (DateTime?)dlExpiryDateEdit.DateTime : null,
                            HasPassengerTransportCertification = passengerCertCheck.Checked,
                            HasDangerousGoodsCertification = dangerousGoodsCertCheck.Checked,
                            MedicalCertificateNumber = medCertNumberBox.Text.Trim(),
                            MedicalCertificateIssueDate = (medCertIssueDateEdit.EditValue != null && medCertIssueDateEdit.EditValue != DBNull.Value) ? (DateTime?)medCertIssueDateEdit.DateTime : null,
                            MedicalCertificateExpiryDate = (medCertExpiryDateEdit.EditValue != null && medCertExpiryDateEdit.EditValue != DBNull.Value) ? (DateTime?)medCertExpiryDateEdit.DateTime : null
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

        // === SECTION MANAGEMENT DIALOGS ===

        private void ShowDocumentsDialog(long employeeId)
        {
            XtraMessageBox.Show(string.Format("Управление документами для сотрудника ID: {0}\n\nФункция в разработке.", employeeId), "Документы", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // TODO: Implement full documents management with grid and add/edit/delete
        }

        private void ShowTrainingsDialog(long employeeId)
        {
            XtraMessageBox.Show(string.Format("Управление обучением для сотрудника ID: {0}\n\nФункция в разработке.", employeeId), "Обучение", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // TODO: Implement full trainings management with grid and add/edit/delete
        }

        private void ShowContactsDialog(long employeeId)
        {
            XtraMessageBox.Show(string.Format("Управление контактами для сотрудника ID: {0}\n\nФункция в разработке.", employeeId), "Экстренные контакты", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // TODO: Implement full contacts management with grid and add/edit/delete
        }

        private void ShowVacationsDialog(long employeeId)
        {
            XtraMessageBox.Show(string.Format("Управление отпусками для сотрудника ID: {0}\n\nФункция в разработке.", employeeId), "Отпуска", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // TODO: Implement full vacations management with grid and add/edit/delete
        }
    }
} 
