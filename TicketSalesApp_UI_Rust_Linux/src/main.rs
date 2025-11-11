// Prevent console window in addition to Slint window in Windows release builds
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod api;
mod models;
mod navigation;

use api::ApiClient;
use std::error::Error;
use std::sync::{Arc, Mutex};
use slint::Model;

slint::include_modules!();

const API_BASE_URL: &str = "http://localhost:5000";

fn main() -> Result<(), Box<dyn Error>> {
    // Initialize API client
    let api_client = Arc::new(Mutex::new(ApiClient::new(API_BASE_URL)));
    
    // Show login window and get username if successful
    if let Some(username) = show_login_window(api_client.clone())? {
        // Login successful, show main window
        show_main_window(api_client, username)?;
    }
    
    Ok(())
}

fn show_login_window(api_client: Arc<Mutex<ApiClient>>) -> Result<Option<String>, Box<dyn Error>> {
    let login_ui = LoginWindow::new()?;
    
    // Store login success state
    let login_result = Arc::new(Mutex::new(None));
    
    // Handle login button click
    let api_client_clone = api_client.clone();
    let login_result_clone = login_result.clone();
    login_ui.on_login_clicked({
        let ui_handle = login_ui.as_weak();
        move || {
            let ui = ui_handle.unwrap();
            let username = ui.get_username().to_string();
            let password = ui.get_password().to_string();
            
            // Disable UI during login
            ui.set_is_loading(true);
            ui.set_error_message(slint::SharedString::from(""));
            
            // Spawn blocking task for API call
            let api_client = api_client_clone.clone();
            let ui_weak = ui_handle.clone();
            let login_result = login_result_clone.clone();
            
            std::thread::spawn(move || {
                // Create tokio runtime just for this API call
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let mut client = api_client.lock().unwrap();
                    client.login(&username, &password).await
                });
                
                // Update UI on main thread
                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_is_loading(false);
                    
                    match result {
                        Ok(auth_response) => {
                            println!("Login successful! Token: {}", &auth_response.token[..20]);
                            
                            // Store username for main window
                            *login_result.lock().unwrap() = Some(username.clone());
                            
                            // Close login window - this will exit the event loop
                            let _ = ui.hide();
                        }
                        Err(e) => {
                            ui.set_error_message(slint::SharedString::from(format!(
                                "Ошибка входа: {}",
                                e
                            )));
                        }
                    }
                });
            });
        }
    });
    
    login_ui.run()?;
    
    // Return the login result (username if successful)
    Ok(login_result.lock().unwrap().clone())
}

fn show_main_window(
    api_client: Arc<Mutex<ApiClient>>,
    username: String,
) -> Result<(), Box<dyn Error>> {
    let main_ui = AppWindow::new()?;
    
    // Set current user
    main_ui.set_current_user(slint::SharedString::from(username));
    
    // Load initial employee data
    load_employees(&main_ui, &api_client);
    
    // Handle refresh button
    let api_client_clone = api_client.clone();
    main_ui.on_refresh_employees({
        let ui_handle = main_ui.as_weak();
        move || {
            let ui = ui_handle.unwrap();
            let api_clone = api_client_clone.clone();
            let ui_weak = ui.as_weak();
            
            load_employees_impl(ui_weak, api_clone);
        }
    });
    
    // Handle add employee button
    let api_add = api_client.clone();
    main_ui.on_add_employee_clicked({
        let ui_handle = main_ui.as_weak();
        move || {
            println!("Add employee clicked");
            let ui = ui_handle.unwrap();
            let api = api_add.clone();
            
            // Clear all fields first
            ui.set_employee_surname(slint::SharedString::from(""));
            ui.set_employee_name(slint::SharedString::from(""));
            ui.set_employee_patronym(slint::SharedString::from(""));
            ui.set_selected_job_id(-1);
            ui.set_selected_department_id(-1);
            ui.set_passport_series(slint::SharedString::from(""));
            ui.set_passport_number(slint::SharedString::from(""));
            ui.set_date_of_birth(slint::SharedString::from(""));
            ui.set_email(slint::SharedString::from(""));
            ui.set_address(slint::SharedString::from(""));
            ui.set_personal_phone(slint::SharedString::from(""));
            ui.set_snils(slint::SharedString::from(""));
            ui.set_driver_license_number(slint::SharedString::from(""));
            ui.set_driver_license_category(slint::SharedString::from(""));
            ui.set_driver_license_issue_date(slint::SharedString::from(""));
            ui.set_medical_cert_number(slint::SharedString::from(""));
            ui.set_medical_cert_issue_date(slint::SharedString::from(""));
            
            ui.set_dialog_mode(slint::SharedString::from("add"));
            ui.set_dialog_title(slint::SharedString::from("Добавить сотрудника"));
            ui.set_show_employee_dialog(true);
            
            // Load jobs and departments dynamically
            slint::spawn_local(async move {
                let rt = tokio::runtime::Runtime::new().unwrap();
                
                // Load jobs
                let jobs_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_jobs().await
                });
                
                // Load departments
                let depts_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_departments().await
                });
                
                match (jobs_result, depts_result) {
                    (Ok(jobs), Ok(departments)) => {
                        // Convert to string arrays for ComboBox display
                        let job_names: Vec<slint::SharedString> = jobs.iter()
                            .map(|j| slint::SharedString::from(&j.job_title))
                            .collect();
                        
                        let dept_names: Vec<slint::SharedString> = departments.iter()
                            .map(|d| slint::SharedString::from(&d.department_name))
                            .collect();
                        
                        // Create job ID mapping (index -> actual job_id)
                        let job_ids: Vec<i32> = jobs.iter().map(|j| j.job_id).collect();
                        let dept_ids: Vec<i64> = departments.iter().map(|d| d.department_id).collect();
                        
                        println!("📋 Loaded {} jobs and {} departments for dialog", jobs.len(), departments.len());
                        
                        // Set in UI
                        let model_jobs = std::rc::Rc::new(slint::VecModel::from(job_names));
                        let model_depts = std::rc::Rc::new(slint::VecModel::from(dept_names));
                        
                        ui.set_job_names(model_jobs.into());
                        ui.set_department_names(model_depts.into());
                        
                        // Store ID mappings for later use (we'll need these when saving)
                        // For now, we'll use index-based selection
                    }
                    (Err(e), _) => eprintln!("Failed to load jobs: {}", e),
                    (_, Err(e)) => eprintln!("Failed to load departments: {}", e),
                }
            }).unwrap();
        }
    });
    
    // Handle edit employee button
    let api_edit = api_client.clone();
    main_ui.on_edit_employee_clicked({
        let ui_handle = main_ui.as_weak();
        move |emp_id| {
            println!("Edit employee {}", emp_id);
            let ui = ui_handle.unwrap();
            let api = api_edit.clone();
            
            // Load employee data and combo data - need Tokio runtime inside spawn_local
            slint::spawn_local(async move {
                let rt = tokio::runtime::Runtime::new().unwrap();
                
                // Load employee
                let emp_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_employee(emp_id as i64).await
                });
                
                // Load jobs and departments
                let jobs_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_jobs().await
                });
                
                let depts_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_departments().await
                });
                
                match (emp_result, jobs_result, depts_result) {
                    (Ok(employee), Ok(jobs), Ok(departments)) => {
                        ui.set_show_employee_dialog(true);
                        ui.set_dialog_mode(slint::SharedString::from("edit"));
                        ui.set_dialog_title(slint::SharedString::from("Редактировать сотрудника"));
                        ui.set_employee_id(employee.emp_id as i32);
                        
                        // Convert to string arrays for ComboBox display
                        let job_names: Vec<slint::SharedString> = jobs.iter()
                            .map(|j| slint::SharedString::from(&j.job_title))
                            .collect();
                        
                        let dept_names: Vec<slint::SharedString> = departments.iter()
                            .map(|d| slint::SharedString::from(&d.department_name))
                            .collect();
                        
                        // Find index of current job and department
                        let job_index = jobs.iter().position(|j| j.job_id == employee.job_id as i32).unwrap_or(0) as i32;
                        let dept_index = employee.department_id
                            .and_then(|dept_id| departments.iter().position(|d| d.department_id == dept_id))
                            .unwrap_or(0) as i32;
                        
                        println!("📋 Loaded {} jobs, {} departments. Employee job index: {}, dept index: {}", 
                                 jobs.len(), departments.len(), job_index, dept_index);
                        
                        // Set combo data
                        let model_jobs = std::rc::Rc::new(slint::VecModel::from(job_names));
                        let model_depts = std::rc::Rc::new(slint::VecModel::from(dept_names));
                        ui.set_job_names(model_jobs.into());
                        ui.set_department_names(model_depts.into());
                        
                        // Basic
                        ui.set_employee_surname(slint::SharedString::from(&employee.surname));
                        ui.set_employee_name(slint::SharedString::from(&employee.name));
                        ui.set_employee_patronym(slint::SharedString::from(employee.patronym.as_deref().unwrap_or("")));
                        ui.set_selected_job_id(job_index);
                        ui.set_selected_department_id(dept_index);
                        // Personal
                        ui.set_passport_series(slint::SharedString::from(employee.passport_series.as_deref().unwrap_or("")));
                        ui.set_passport_number(slint::SharedString::from(employee.passport_number.as_deref().unwrap_or("")));
                        ui.set_email(slint::SharedString::from(employee.email.as_deref().unwrap_or("")));
                        ui.set_address(slint::SharedString::from(employee.address.as_deref().unwrap_or("")));
                        ui.set_personal_phone(slint::SharedString::from(employee.personal_phone.as_deref().unwrap_or("")));
                        ui.set_snils(slint::SharedString::from(employee.snils.as_deref().unwrap_or("")));
                        // Driver
                        ui.set_driver_license_number(slint::SharedString::from(employee.driver_license_number.as_deref().unwrap_or("")));
                        ui.set_driver_license_category(slint::SharedString::from(employee.driver_license_category.as_deref().unwrap_or("")));
                        // Medical
                        ui.set_medical_cert_number(slint::SharedString::from(employee.medical_certificate_number.as_deref().unwrap_or("")));
                    }
                    (Err(e), _, _) => eprintln!("Failed to load employee: {}", e),
                    (_, Err(e), _) => eprintln!("Failed to load jobs: {}", e),
                    (_, _, Err(e)) => eprintln!("Failed to load departments: {}", e),
                }
            }).unwrap();
        }
    });
    
    // Handle delete employee button
    let api_delete = api_client.clone();
    main_ui.on_delete_employee_clicked({
        let ui_handle = main_ui.as_weak();
        move |emp_id| {
            println!("Delete employee {}", emp_id);
            let ui = ui_handle.unwrap();
            let api = api_delete.clone();
            
            // Find employee name for confirmation
            let employees = ui.get_employees();
            let mut employee_name = String::new();
            for i in 0..employees.row_count() {
                let emp = employees.row_data(i).unwrap();
                if emp.id == emp_id {
                    employee_name = format!("{} {}", emp.surname, emp.name);
                    break;
                }
            }
            
            ui.set_show_delete_dialog(true);
            ui.set_delete_employee_id(emp_id);
            ui.set_delete_employee_name(slint::SharedString::from(employee_name));
        }
    });
    
    // Handle save employee dialog
    let api_save = api_client.clone();
    main_ui.on_save_employee({
        let ui_handle = main_ui.as_weak();
        move || {
            let ui = ui_handle.unwrap();
            let api = api_save.clone();
            let ui_weak = ui.as_weak();
            
            let mode = ui.get_dialog_mode().to_string();
            // Basic
            let surname = ui.get_employee_surname().to_string();
            let name = ui.get_employee_name().to_string();
            let patronym = ui.get_employee_patronym().to_string();
            let job_index = ui.get_selected_job_id();
            let dept_index = ui.get_selected_department_id();
            // Personal
            let passport_series = ui.get_passport_series().to_string();
            let passport_number = ui.get_passport_number().to_string();
            let email = ui.get_email().to_string();
            let address = ui.get_address().to_string();
            let personal_phone = ui.get_personal_phone().to_string();
            let snils = ui.get_snils().to_string();
            // Driver
            let driver_license_number = ui.get_driver_license_number().to_string();
            let driver_license_category = ui.get_driver_license_category().to_string();
            // Medical
            let medical_cert_number = ui.get_medical_cert_number().to_string();
            
            slint::spawn_local(async move {
                use crate::models::CreateEmployeeRequest;
                
                let rt = tokio::runtime::Runtime::new().unwrap();
                
                // Load jobs and departments to map indices to IDs
                let jobs_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_jobs().await
                });
                
                let depts_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_departments().await
                });
                
                let (jobs, departments) = match (jobs_result, depts_result) {
                    (Ok(j), Ok(d)) => (j, d),
                    (Err(e), _) => {
                        eprintln!("Failed to load jobs: {}", e);
                        return;
                    }
                    (_, Err(e)) => {
                        eprintln!("Failed to load departments: {}", e);
                        return;
                    }
                };
                
                // Map selected indices to actual IDs
                let job_id = if job_index >= 0 && (job_index as usize) < jobs.len() {
                    jobs[job_index as usize].job_id as i64
                } else {
                    0
                };
                
                let department_id = if dept_index >= 0 && (dept_index as usize) < departments.len() {
                    Some(departments[dept_index as usize].department_id as i32)
                } else {
                    None
                };
                
                println!("💾 Saving: job_index={} -> job_id={}, dept_index={} -> dept_id={:?}", 
                         job_index, job_id, dept_index, department_id);
                
                if mode == "add" {
                    let request = CreateEmployeeRequest {
                        surname,
                        name,
                        patronym: Some(patronym).filter(|s| !s.is_empty()),
                        employed_since: chrono::Local::now().naive_local().date(),
                        job_id: Some(job_id as i32),
                        department_id,
                        date_of_birth: None,
                        personal_phone: Some(personal_phone).filter(|s| !s.is_empty()),
                        work_phone: None,
                        address: Some(address).filter(|s| !s.is_empty()),
                        email: Some(email).filter(|s| !s.is_empty()),
                        passport_series: Some(passport_series).filter(|s| !s.is_empty()),
                        passport_number: Some(passport_number).filter(|s| !s.is_empty()),
                        inn: None,
                        snils: Some(snils).filter(|s| !s.is_empty()),
                        driver_license_number: Some(driver_license_number).filter(|s| !s.is_empty()),
                        driver_license_category: Some(driver_license_category).filter(|s| !s.is_empty()),
                        driver_license_issue_date: None,
                        driver_license_expiry_date: None,
                        medical_certificate_number: Some(medical_cert_number).filter(|s| !s.is_empty()),
                        medical_certificate_issue_date: None,
                        medical_certificate_expiry_date: None,
                    };
                    
                    let result = rt.block_on(async {
                        let client = api.lock().unwrap();
                        client.create_employee(&request).await
                    });
                    
                    match result {
                        Ok(_) => {
                            ui_weak.unwrap().set_show_employee_dialog(false);
                            load_employees_impl(ui_weak.clone(), api.clone());
                            println!("Employee created successfully");
                        }
                        Err(e) => {
                            eprintln!("Failed to create employee: {}", e);
                            ui_weak.unwrap().set_employee_error(slint::SharedString::from(format!("Ошибка: {}", e)));
                        }
                    }
                } else if mode == "edit" {
                    let emp_id = ui_weak.unwrap().get_employee_id();
                    
                    // Get existing employee and update fields
                    let get_result = rt.block_on(async {
                        let client = api.lock().unwrap();
                        client.get_employee(emp_id as i64).await
                    });
                    
                    match get_result {
                        Ok(mut employee) => {
                            // Update basic fields
                            employee.surname = surname;
                            employee.name = name;
                            employee.patronym = Some(patronym).filter(|s| !s.is_empty());
                            employee.job_id = job_id;
                            employee.department_id = department_id.map(|d| d as i64);
                            // Update personal fields
                            employee.passport_series = Some(passport_series).filter(|s| !s.is_empty());
                            employee.passport_number = Some(passport_number).filter(|s| !s.is_empty());
                            employee.email = Some(email).filter(|s| !s.is_empty());
                            employee.address = Some(address).filter(|s| !s.is_empty());
                            employee.personal_phone = Some(personal_phone).filter(|s| !s.is_empty());
                            employee.snils = Some(snils).filter(|s| !s.is_empty());
                            // Update driver fields
                            employee.driver_license_number = Some(driver_license_number).filter(|s| !s.is_empty());
                            employee.driver_license_category = Some(driver_license_category).filter(|s| !s.is_empty());
                            // Update medical fields
                            employee.medical_certificate_number = Some(medical_cert_number).filter(|s| !s.is_empty());
                            
                            let update_result = rt.block_on(async {
                                let client = api.lock().unwrap();
                                client.update_employee(emp_id as i64, &employee).await
                            });
                            
                            match update_result {
                                Ok(_) => {
                                    ui_weak.unwrap().set_show_employee_dialog(false);
                                    load_employees_impl(ui_weak.clone(), api.clone());
                                    println!("Employee updated successfully");
                                }
                                Err(e) => {
                                    eprintln!("Failed to update employee: {}", e);
                                    ui_weak.unwrap().set_employee_error(slint::SharedString::from(format!("Ошибка: {}", e)));
                                }
                            }
                        }
                        Err(e) => {
                            eprintln!("Failed to load employee: {}", e);
                            ui_weak.unwrap().set_employee_error(slint::SharedString::from(format!("Ошибка: {}", e)));
                        }
                    }
                }
            }).unwrap();
        }
    });
    
    // Handle confirm delete
    let api_confirm_delete = api_client.clone();
    main_ui.on_confirm_delete({
        let ui_handle = main_ui.as_weak();
        move || {
            let ui = ui_handle.unwrap();
            let api = api_confirm_delete.clone();
            let ui_weak = ui.as_weak();
            let emp_id = ui.get_delete_employee_id();
            
            slint::spawn_local(async move {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.delete_employee(emp_id as i64).await
                });
                
                match result {
                    Ok(_) => {
                        ui_weak.unwrap().set_show_delete_dialog(false);
                        load_employees_impl(ui_weak.clone(), api.clone());
                        println!("Employee deleted successfully");
                    }
                    Err(e) => {
                        eprintln!("Failed to delete employee: {}", e);
                    }
                }
            }).unwrap();
        }
    });
    
    // Handle logout
    let api_client_clone = api_client.clone();
    main_ui.on_logout_clicked({
        let ui_handle = main_ui.as_weak();
        move || {
            {
                let mut client = api_client_clone.lock().unwrap();
                client.logout();
            }
            
            let ui = ui_handle.unwrap();
            ui.hide().unwrap();
            
            println!("Logged out successfully");
            std::process::exit(0);
        }
    });
    
    // Handle navigation changes
    let api_client_nav = api_client.clone();
    main_ui.on_navigation_changed({
        let ui_weak = main_ui.as_weak();
        move |group, index| {
            use navigation::AppRoute;
            
            if let Some(route) = AppRoute::from_indices(group, index) {
                println!("Navigation: {:?} (Group: {}, Index: {})", route, group, index);
                
                // Load data based on route
                match route {
                    AppRoute::Employees => {
                        println!("Loading employees...");
                        load_employees_impl(ui_weak.clone(), api_client_nav.clone());
                    }
                    _ => {
                        println!("Route {} not yet implemented", route.display_name());
                    }
                }
            }
        }
    });
    
    // Initialize with Employees view (Group 1, Index 0)
    main_ui.set_current_nav_group(1);
    main_ui.set_current_nav_index(0);
    
    main_ui.run()?;
    Ok(())
}

fn load_employees(ui: &AppWindow, api_client: &Arc<Mutex<ApiClient>>) {
    let ui_weak = ui.as_weak();
    let api_clone = api_client.clone();
    
    load_employees_impl(ui_weak, api_clone);
}

fn load_employees_impl(
    ui_weak: slint::Weak<AppWindow>,
    api_client: Arc<Mutex<ApiClient>>,
) {
    // Spawn thread for API call
    std::thread::spawn(move || {
        let rt = tokio::runtime::Runtime::new().unwrap();
        let result = rt.block_on(async {
            let client = api_client.lock().unwrap();
            client.get_employees().await
        });
        
        match result {
            Ok(employees) => {
                // Convert employees to Slint struct format
                let employee_models: Vec<_> = employees
                    .iter()
                    .map(|emp| {
                        EmployeeData {
                            id: emp.emp_id as i32,
                            surname: emp.surname.clone().into(),
                            name: emp.name.clone().into(),
                            department: emp.department_name().into(),
                            position: emp.job_title().into(),
                        }
                    })
                    .collect();
                
                let count = employee_models.len();
                
                // Update UI
                let _ = slint::invoke_from_event_loop(move || {
                    if let Some(ui) = ui_weak.upgrade() {
                        let model = std::rc::Rc::new(slint::VecModel::from(employee_models));
                        ui.set_employees(model.into());
                        println!("Loaded {} employees", count);
                    }
                });
            }
            Err(e) => {
                eprintln!("Error loading employees: {}", e);
                let _ = slint::invoke_from_event_loop(move || {
                    if let Some(_ui) = ui_weak.upgrade() {
                        // Show error in UI
                        // TODO: Add error display mechanism
                    }
                });
            }
        }
    });
}

// Note: EmployeeData struct is defined in app-window.slint UI file
// and is automatically generated by slint::include_modules!()
