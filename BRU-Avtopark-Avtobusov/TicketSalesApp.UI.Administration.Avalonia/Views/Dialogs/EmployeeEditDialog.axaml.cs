using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using TicketSalesApp.Core.Models;

namespace TicketSalesApp.UI.Administration.Avalonia.Views.Dialogs
{
    public partial class EmployeeEditDialog : Window
    {
        public Employee? Employee { get; private set; }
        public bool IsSaved { get; private set; }

        public EmployeeEditDialog()
        {
            InitializeComponent();
        }

        public EmployeeEditDialog(Employee? employee, ObservableCollection<Job> jobs, ObservableCollection<Department> departments)
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            // Get controls
            var surnameBox = this.FindControl<TextBox>("SurnameBox");
            var nameBox = this.FindControl<TextBox>("NameBox");
            var patronymBox = this.FindControl<TextBox>("PatronymBox");
            var employedSincePicker = this.FindControl<DatePicker>("EmployedSincePicker");
            var jobComboBox = this.FindControl<ComboBox>("JobComboBox");
            var departmentComboBox = this.FindControl<ComboBox>("DepartmentComboBox");
            var passportSeriesBox = this.FindControl<TextBox>("PassportSeriesBox");
            var passportNumberBox = this.FindControl<TextBox>("PassportNumberBox");
            var dateOfBirthPicker = this.FindControl<DatePicker>("DateOfBirthPicker");
            var addressBox = this.FindControl<TextBox>("AddressBox");
            var phoneBox = this.FindControl<TextBox>("PhoneBox");
            var emailBox = this.FindControl<TextBox>("EmailBox");
            var snilsBox = this.FindControl<TextBox>("SnilsBox");
            var driverLicenseNumberBox = this.FindControl<TextBox>("DriverLicenseNumberBox");
            var driverLicenseCategoryBox = this.FindControl<TextBox>("DriverLicenseCategoryBox");
            var driverLicenseExpiryPicker = this.FindControl<DatePicker>("DriverLicenseExpiryPicker");
            var medicalCertificateNumberBox = this.FindControl<TextBox>("MedicalCertificateNumberBox");
            var medicalCertificateExpiryPicker = this.FindControl<DatePicker>("MedicalCertificateExpiryPicker");
            var saveButton = this.FindControl<Button>("SaveButton");
            var cancelButton = this.FindControl<Button>("CancelButton");

            // Set up combo boxes
            if (jobComboBox != null)
            {
                jobComboBox.ItemsSource = jobs;
                jobComboBox.DisplayMemberBinding = new global::Avalonia.Data.Binding("JobTitle");
            }

            if (departmentComboBox != null)
            {
                departmentComboBox.ItemsSource = departments;
                departmentComboBox.DisplayMemberBinding = new global::Avalonia.Data.Binding("DepartmentName");
            }

            // Populate fields if editing
            if (employee != null)
            {
                Title = "Редактирование сотрудника";
                if (surnameBox != null) surnameBox.Text = employee.Surname;
                if (nameBox != null) nameBox.Text = employee.Name;
                if (patronymBox != null) patronymBox.Text = employee.Patronym;
                SetDatePickerValue(employedSincePicker, employee.EmployedSince);
                if (jobComboBox != null) jobComboBox.SelectedItem = jobs.FirstOrDefault(j => j.JobId == employee.JobId);
                if (departmentComboBox != null) departmentComboBox.SelectedItem = departments.FirstOrDefault(d => d.DepartmentId == employee.DepartmentId);
                if (passportSeriesBox != null) passportSeriesBox.Text = employee.PassportSeries;
                if (passportNumberBox != null) passportNumberBox.Text = employee.PassportNumber;
                SetDatePickerValue(dateOfBirthPicker, employee.DateOfBirth);
                if (addressBox != null) addressBox.Text = employee.Address;
                if (phoneBox != null) phoneBox.Text = employee.PersonalPhone;
                if (emailBox != null) emailBox.Text = employee.Email;
                if (snilsBox != null) snilsBox.Text = employee.SNILS;
                if (driverLicenseNumberBox != null) driverLicenseNumberBox.Text = employee.DriverLicenseNumber;
                if (driverLicenseCategoryBox != null) driverLicenseCategoryBox.Text = employee.DriverLicenseCategory;
                SetDatePickerValue(driverLicenseExpiryPicker, employee.DriverLicenseExpiryDate);
                if (medicalCertificateNumberBox != null) medicalCertificateNumberBox.Text = employee.MedicalCertificateNumber;
                SetDatePickerValue(medicalCertificateExpiryPicker, employee.MedicalCertificateExpiryDate);

                Employee = new Employee { EmpId = employee.EmpId };
            }
            else
            {
                Title = "Добавление сотрудника";
                Employee = new Employee();
            }

            // Wire up buttons
            if (saveButton != null)
            {
                saveButton.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(surnameBox?.Text) || 
                        string.IsNullOrWhiteSpace(nameBox?.Text) ||
                        jobComboBox?.SelectedItem == null)
                    {
                        return;
                    }

                    var selectedJob = jobComboBox.SelectedItem as Job;
                    var selectedDepartment = departmentComboBox?.SelectedItem as Department;

                    if (Employee != null)
                    {
                        Employee.Surname = surnameBox!.Text;
                        Employee.Name = nameBox!.Text;
                        Employee.Patronym = patronymBox?.Text ?? string.Empty;
                        Employee.EmployedSince = GetDateFromPicker(employedSincePicker) ?? DateTime.Now;
                        Employee.JobId = selectedJob!.JobId;
                        Employee.Job = selectedJob;
                        Employee.DepartmentId = selectedDepartment?.DepartmentId;
                        Employee.Department = selectedDepartment;
                        Employee.PassportSeries = passportSeriesBox?.Text;
                        Employee.PassportNumber = passportNumberBox?.Text;
                        Employee.DateOfBirth = GetDateFromPicker(dateOfBirthPicker);
                        Employee.Address = addressBox?.Text;
                        Employee.PersonalPhone = phoneBox?.Text;
                        Employee.Email = emailBox?.Text;
                        Employee.SNILS = snilsBox?.Text;
                        Employee.DriverLicenseNumber = driverLicenseNumberBox?.Text;
                        Employee.DriverLicenseCategory = driverLicenseCategoryBox?.Text;
                        Employee.DriverLicenseExpiryDate = GetDateFromPicker(driverLicenseExpiryPicker);
                        Employee.MedicalCertificateNumber = medicalCertificateNumberBox?.Text;
                        Employee.MedicalCertificateExpiryDate = GetDateFromPicker(medicalCertificateExpiryPicker);
                    }

                    IsSaved = true;
                    Close();
                };
            }

            if (cancelButton != null)
            {
                cancelButton.Click += (s, e) =>
                {
                    IsSaved = false;
                    Close();
                };
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private static void SetDatePickerValue(DatePicker? picker, DateTime date)
        {
            SetDatePickerValue(picker, (DateTime?)date);
        }

        private static void SetDatePickerValue(DatePicker? picker, DateTime? date)
        {
            if (picker == null)
            {
                return;
            }

            if (date.HasValue && date.Value.Year > 1)
            {
                picker.SelectedDate = date.Value;
            }
            else
            {
                picker.SelectedDate = null;
            }
        }

        private static DateTime? GetDateFromPicker(DatePicker? picker)
        {
            if (picker?.SelectedDate is DateTimeOffset dto)
            {
                return dto.Date;
            }

            return null;
        }
    }
}
