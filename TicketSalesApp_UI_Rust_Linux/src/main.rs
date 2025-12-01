// Prevent console window in addition to Slint window in Windows release builds
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod api;
mod date_utils;
mod models;
mod navigation;

use api::ApiClient;
use slint::Model;
use std::error::Error;
use std::sync::{Arc, Mutex};

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
    let login_ui = AuthWindow::new()?;

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

    // Set admin status (TODO: Get this from user authentication response)
    main_ui.set_is_admin(true);

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
            ui.set_driver_license_expiry(slint::SharedString::from(""));
            ui.set_medical_cert_number(slint::SharedString::from(""));
            ui.set_medical_cert_issue_date(slint::SharedString::from(""));
            ui.set_medical_cert_expiry(slint::SharedString::from(""));

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
                        let job_names: Vec<slint::SharedString> = jobs
                            .iter()
                            .map(|j| slint::SharedString::from(&j.job_title))
                            .collect();

                        let dept_names: Vec<slint::SharedString> = departments
                            .iter()
                            .map(|d| slint::SharedString::from(&d.department_name))
                            .collect();

                        // Create job ID mapping (index -> actual job_id)
                        let job_ids: Vec<i32> = jobs.iter().map(|j| j.job_id).collect();
                        let dept_ids: Vec<i64> =
                            departments.iter().map(|d| d.department_id).collect();

                        println!(
                            "📋 Loaded {} jobs and {} departments for dialog",
                            jobs.len(),
                            departments.len()
                        );

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
            })
            .unwrap();
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
                        ui.set_date_of_birth(slint::SharedString::from(date_utils::format_date_for_ui(employee.date_of_birth)));
                        ui.set_email(slint::SharedString::from(employee.email.as_deref().unwrap_or("")));
                        ui.set_address(slint::SharedString::from(employee.address.as_deref().unwrap_or("")));
                        ui.set_personal_phone(slint::SharedString::from(employee.personal_phone.as_deref().unwrap_or("")));
                        ui.set_snils(slint::SharedString::from(employee.snils.as_deref().unwrap_or("")));
                        // Driver
                        ui.set_driver_license_number(slint::SharedString::from(employee.driver_license_number.as_deref().unwrap_or("")));
                        ui.set_driver_license_category(slint::SharedString::from(employee.driver_license_category.as_deref().unwrap_or("")));
                        ui.set_driver_license_issue_date(slint::SharedString::from(date_utils::format_date_for_ui(employee.driver_license_issue_date)));
                        ui.set_driver_license_expiry(slint::SharedString::from(date_utils::format_date_for_ui(employee.driver_license_expiry_date)));
                        // Medical
                        ui.set_medical_cert_number(slint::SharedString::from(employee.medical_certificate_number.as_deref().unwrap_or("")));
                        ui.set_medical_cert_issue_date(slint::SharedString::from(date_utils::format_date_for_ui(employee.medical_certificate_issue_date)));
                        ui.set_medical_cert_expiry(slint::SharedString::from(date_utils::format_date_for_ui(employee.medical_certificate_expiry_date)));
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
            let date_of_birth = ui.get_date_of_birth().to_string();
            let email = ui.get_email().to_string();
            let address = ui.get_address().to_string();
            let personal_phone = ui.get_personal_phone().to_string();
            let snils = ui.get_snils().to_string();
            // Driver
            let driver_license_number = ui.get_driver_license_number().to_string();
            let driver_license_category = ui.get_driver_license_category().to_string();
            let driver_license_issue_date = ui.get_driver_license_issue_date().to_string();
            let driver_license_expiry = ui.get_driver_license_expiry().to_string();
            // Medical
            let medical_cert_number = ui.get_medical_cert_number().to_string();
            let medical_cert_issue_date = ui.get_medical_cert_issue_date().to_string();
            let medical_cert_expiry = ui.get_medical_cert_expiry().to_string();

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

                let department_id = if dept_index >= 0 && (dept_index as usize) < departments.len()
                {
                    Some(departments[dept_index as usize].department_id as i32)
                } else {
                    None
                };

                println!(
                    "💾 Saving: job_index={} -> job_id={}, dept_index={} -> dept_id={:?}",
                    job_index, job_id, dept_index, department_id
                );

                if mode == "add" {
                    let request = CreateEmployeeRequest {
                        surname,
                        name,
                        patronym: Some(patronym).filter(|s| !s.is_empty()),
                        employed_since: chrono::Local::now().naive_local().date(),
                        job_id: Some(job_id as i32),
                        department_id,
                        date_of_birth: date_utils::parse_date_from_ui(&date_of_birth),
                        personal_phone: Some(personal_phone).filter(|s| !s.is_empty()),
                        work_phone: None,
                        address: Some(address).filter(|s| !s.is_empty()),
                        email: Some(email).filter(|s| !s.is_empty()),
                        passport_series: Some(passport_series).filter(|s| !s.is_empty()),
                        passport_number: Some(passport_number).filter(|s| !s.is_empty()),
                        inn: None,
                        snils: Some(snils).filter(|s| !s.is_empty()),
                        driver_license_number: Some(driver_license_number)
                            .filter(|s| !s.is_empty()),
                        driver_license_category: Some(driver_license_category)
                            .filter(|s| !s.is_empty()),
                        driver_license_issue_date: date_utils::parse_date_from_ui(
                            &driver_license_issue_date,
                        ),
                        driver_license_expiry_date: date_utils::parse_date_from_ui(
                            &driver_license_expiry,
                        ),
                        medical_certificate_number: Some(medical_cert_number)
                            .filter(|s| !s.is_empty()),
                        medical_certificate_issue_date: date_utils::parse_date_from_ui(
                            &medical_cert_issue_date,
                        ),
                        medical_certificate_expiry_date: date_utils::parse_date_from_ui(
                            &medical_cert_expiry,
                        ),
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
                            ui_weak
                                .unwrap()
                                .set_employee_error(slint::SharedString::from(format!(
                                    "Ошибка: {}",
                                    e
                                )));
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
                            employee.passport_series =
                                Some(passport_series).filter(|s| !s.is_empty());
                            employee.passport_number =
                                Some(passport_number).filter(|s| !s.is_empty());
                            employee.date_of_birth = date_utils::parse_date_from_ui(&date_of_birth);
                            employee.email = Some(email).filter(|s| !s.is_empty());
                            employee.address = Some(address).filter(|s| !s.is_empty());
                            employee.personal_phone =
                                Some(personal_phone).filter(|s| !s.is_empty());
                            employee.snils = Some(snils).filter(|s| !s.is_empty());
                            // Update driver fields
                            employee.driver_license_number =
                                Some(driver_license_number).filter(|s| !s.is_empty());
                            employee.driver_license_category =
                                Some(driver_license_category).filter(|s| !s.is_empty());
                            employee.driver_license_issue_date =
                                date_utils::parse_date_from_ui(&driver_license_issue_date);
                            employee.driver_license_expiry_date =
                                date_utils::parse_date_from_ui(&driver_license_expiry);
                            // Update medical fields
                            employee.medical_certificate_number =
                                Some(medical_cert_number).filter(|s| !s.is_empty());
                            employee.medical_certificate_issue_date =
                                date_utils::parse_date_from_ui(&medical_cert_issue_date);
                            employee.medical_certificate_expiry_date =
                                date_utils::parse_date_from_ui(&medical_cert_expiry);

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
                                    ui_weak
                                        .unwrap()
                                        .set_employee_error(slint::SharedString::from(format!(
                                            "Ошибка: {}",
                                            e
                                        )));
                                }
                            }
                        }
                        Err(e) => {
                            eprintln!("Failed to load employee: {}", e);
                            ui_weak
                                .unwrap()
                                .set_employee_error(slint::SharedString::from(format!(
                                    "Ошибка: {}",
                                    e
                                )));
                        }
                    }
                }
            })
            .unwrap();
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
            })
            .unwrap();
        }
    });

    // ========== BUS MANAGEMENT CALLBACKS ==========

    // Handle load buses
    let api_load_buses = api_client.clone();
    main_ui.on_load_buses({
        let ui_handle = main_ui.as_weak();
        move || {
            let ui = ui_handle.unwrap();
            let api = api_load_buses.clone();
            let ui_weak = ui.as_weak();

            // Set loading state
            ui.set_buses_loading(true);
            ui.set_buses_error(slint::SharedString::from(""));
            ui.set_buses_has_error(false);

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_buses().await
                });

                // Convert result to Send-able types
                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_buses_loading(false);

                    match result_send {
                        Ok(buses) => {
                            let bus_data: Vec<_> = buses
                                .iter()
                                .map(|bus| BusData {
                                    bus_id: bus.bus_id as i32,
                                    model: slint::SharedString::from(&bus.model),
                                    route_count: bus.route_count() as i32,
                                })
                                .collect();

                            let count = bus_data.len();
                            let model = std::rc::Rc::new(slint::VecModel::from(bus_data));
                            ui.set_buses(model.into());
                            println!("✅ Loaded {} buses", count);
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to load buses: {}", e);
                            ui.set_buses_error(slint::SharedString::from(format!(
                                "Ошибка загрузки: {}",
                                e
                            )));
                            ui.set_buses_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle search buses
    let api_search_buses = api_client.clone();
    main_ui.on_search_buses({
        let ui_handle = main_ui.as_weak();
        move |search_text| {
            let ui = ui_handle.unwrap();
            let api = api_search_buses.clone();
            let ui_weak = ui.as_weak();
            let query = search_text.to_string();

            // If search is empty, reload all buses
            if query.trim().is_empty() {
                std::thread::spawn(move || {
                    let rt = tokio::runtime::Runtime::new().unwrap();
                    let result = rt.block_on(async {
                        let client = api.lock().unwrap();
                        client.get_buses().await
                    });

                    let result_send = result.map_err(|e| e.to_string());

                    let _ = slint::invoke_from_event_loop(move || {
                        let ui = ui_weak.unwrap();

                        match result_send {
                            Ok(buses) => {
                                let bus_data: Vec<_> = buses
                                    .iter()
                                    .map(|bus| BusData {
                                        bus_id: bus.bus_id as i32,
                                        model: slint::SharedString::from(&bus.model),
                                        route_count: bus.route_count() as i32,
                                    })
                                    .collect();

                                let model = std::rc::Rc::new(slint::VecModel::from(bus_data));
                                ui.set_buses(model.into());
                            }
                            Err(e) => {
                                eprintln!("❌ Failed to reload buses: {}", e);
                            }
                        }
                    });
                });
            } else {
                // Client-side filtering for real-time search
                std::thread::spawn(move || {
                    let rt = tokio::runtime::Runtime::new().unwrap();
                    let result = rt.block_on(async {
                        let client = api.lock().unwrap();
                        client.get_buses().await
                    });

                    let result_send = result.map_err(|e| e.to_string());

                    let _ = slint::invoke_from_event_loop(move || {
                        let ui = ui_weak.unwrap();

                        match result_send {
                            Ok(buses) => {
                                let filtered_buses: Vec<_> = buses
                                    .iter()
                                    .filter(|bus| {
                                        bus.model.to_lowercase().contains(&query.to_lowercase())
                                    })
                                    .map(|bus| BusData {
                                        bus_id: bus.bus_id as i32,
                                        model: slint::SharedString::from(&bus.model),
                                        route_count: bus.route_count() as i32,
                                    })
                                    .collect();

                                let count = filtered_buses.len();
                                let model = std::rc::Rc::new(slint::VecModel::from(filtered_buses));
                                ui.set_buses(model.into());
                                println!("🔍 Found {} buses matching '{}'", count, query);
                            }
                            Err(e) => {
                                eprintln!("❌ Failed to search buses: {}", e);
                            }
                        }
                    });
                });
            }
        }
    });

    // Handle add bus
    let api_add_bus = api_client.clone();
    main_ui.on_add_bus({
        let ui_handle = main_ui.as_weak();
        move |model| {
            let ui = ui_handle.unwrap();
            let api = api_add_bus.clone();
            let ui_weak = ui.as_weak();
            let model_str = model.to_string();

            // Validate model is not empty
            if model_str.trim().is_empty() {
                ui.set_buses_error(slint::SharedString::from("Модель не может быть пустой"));
                ui.set_buses_has_error(true);
                return;
            }

            ui.set_buses_loading(true);

            std::thread::spawn(move || {
                use crate::models::CreateBusRequest;

                let rt = tokio::runtime::Runtime::new().unwrap();
                let request = CreateBusRequest {
                    model: model_str.clone(),
                };

                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.create_bus(request).await
                });

                let result_send = result
                    .map(|bus| bus.display_name())
                    .map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_buses_loading(false);

                    match result_send {
                        Ok(display_name) => {
                            println!("✅ Created bus: {}", display_name);
                            // Reload buses
                            ui.invoke_load_buses();
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to create bus: {}", e);
                            ui.set_buses_error(slint::SharedString::from(format!(
                                "Ошибка создания: {}",
                                e
                            )));
                            ui.set_buses_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle edit bus
    let api_edit_bus = api_client.clone();
    main_ui.on_edit_bus({
        let ui_handle = main_ui.as_weak();
        move |bus_id, model| {
            let ui = ui_handle.unwrap();
            let api = api_edit_bus.clone();
            let ui_weak = ui.as_weak();
            let model_str = model.to_string();

            // Validate model is not empty
            if model_str.trim().is_empty() {
                ui.set_buses_error(slint::SharedString::from("Модель не может быть пустой"));
                ui.set_buses_has_error(true);
                return;
            }

            ui.set_buses_loading(true);

            std::thread::spawn(move || {
                use crate::models::UpdateBusRequest;

                let rt = tokio::runtime::Runtime::new().unwrap();
                let request = UpdateBusRequest {
                    bus_id: bus_id as i64,
                    model: model_str.clone(),
                };

                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.update_bus(bus_id as i64, request).await
                });

                let result_send = result
                    .map(|bus| bus.display_name())
                    .map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_buses_loading(false);

                    match result_send {
                        Ok(display_name) => {
                            println!("✅ Updated bus: {}", display_name);
                            // Reload buses
                            ui.invoke_load_buses();
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to update bus: {}", e);
                            ui.set_buses_error(slint::SharedString::from(format!(
                                "Ошибка обновления: {}",
                                e
                            )));
                            ui.set_buses_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle delete bus
    let api_delete_bus = api_client.clone();
    main_ui.on_delete_bus({
        let ui_handle = main_ui.as_weak();
        move |bus_id| {
            let ui = ui_handle.unwrap();
            let api = api_delete_bus.clone();
            let ui_weak = ui.as_weak();

            ui.set_buses_loading(true);

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.delete_bus(bus_id as i64).await
                });

                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_buses_loading(false);

                    match result_send {
                        Ok(_) => {
                            println!("✅ Deleted bus {}", bus_id);
                            // Reload buses
                            ui.invoke_load_buses();
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to delete bus: {}", e);
                            ui.set_buses_error(slint::SharedString::from(format!(
                                "Ошибка удаления: {}",
                                e
                            )));
                            ui.set_buses_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // ========== ROUTE MANAGEMENT CALLBACKS ==========

    // Handle load routes
    let api_load_routes = api_client.clone();
    main_ui.on_load_routes({
        let ui_handle = main_ui.as_weak();
        move || {
            println!("🚗🚗🚗 on_load_routes callback triggered!");
            let ui = ui_handle.unwrap();
            let api = api_load_routes.clone();
            let ui_weak = ui.as_weak();

            // Set loading state
            ui.set_routes_loading(true);
            ui.set_routes_error(slint::SharedString::from(""));
            ui.set_routes_has_error(false);

            println!("🚗 Spawning thread to load routes...");
            std::thread::spawn(move || {
                println!("🚗 Inside thread, creating tokio runtime...");
                let rt = tokio::runtime::Runtime::new().unwrap();
                println!("🚗 Calling get_routes()...");
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    println!("🚗 Got API client lock, calling get_routes...");
                    client.get_routes().await
                });

                println!(
                    "🚗 get_routes() returned: {:?}",
                    result.as_ref().map(|r| r.len()).map_err(|e| e.to_string())
                );

                // Convert result to Send-able types
                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_routes_loading(false);

                    match result_send {
                        Ok(routes) => {
                            let route_data: Vec<_> = routes
                                .iter()
                                .map(|route| RouteData {
                                    route_id: route.route_id as i32,
                                    start_point: slint::SharedString::from(&route.start_point),
                                    end_point: slint::SharedString::from(&route.end_point),
                                    bus_model: slint::SharedString::from(&route.bus_model()),
                                    driver_name: slint::SharedString::from(&route.driver_name()),
                                    travel_time: slint::SharedString::from(
                                        route.travel_time.as_deref().unwrap_or(""),
                                    ),
                                })
                                .collect();

                            let count = route_data.len();
                            let model = std::rc::Rc::new(slint::VecModel::from(route_data));
                            ui.set_routes(model.into());
                            println!("✅ Loaded {} routes", count);
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to load routes: {}", e);
                            ui.set_routes_error(slint::SharedString::from(format!(
                                "Ошибка загрузки: {}",
                                e
                            )));
                            ui.set_routes_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle search routes
    let api_search_routes = api_client.clone();
    main_ui.on_search_routes({
        let ui_handle = main_ui.as_weak();
        move |search_text| {
            let ui = ui_handle.unwrap();
            let api = api_search_routes.clone();
            let ui_weak = ui.as_weak();
            let query = search_text.to_string();

            // If search is empty, reload all routes
            if query.trim().is_empty() {
                std::thread::spawn(move || {
                    let rt = tokio::runtime::Runtime::new().unwrap();
                    let result = rt.block_on(async {
                        let client = api.lock().unwrap();
                        client.get_routes().await
                    });

                    let result_send = result.map_err(|e| e.to_string());

                    let _ = slint::invoke_from_event_loop(move || {
                        let ui = ui_weak.unwrap();

                        match result_send {
                            Ok(routes) => {
                                let route_data: Vec<_> = routes
                                    .iter()
                                    .map(|route| RouteData {
                                        route_id: route.route_id as i32,
                                        start_point: slint::SharedString::from(&route.start_point),
                                        end_point: slint::SharedString::from(&route.end_point),
                                        bus_model: slint::SharedString::from(&route.bus_model()),
                                        driver_name: slint::SharedString::from(
                                            &route.driver_name(),
                                        ),
                                        travel_time: slint::SharedString::from(
                                            route.travel_time.as_deref().unwrap_or(""),
                                        ),
                                    })
                                    .collect();

                                let model = std::rc::Rc::new(slint::VecModel::from(route_data));
                                ui.set_routes(model.into());
                            }
                            Err(e) => {
                                eprintln!("❌ Failed to reload routes: {}", e);
                            }
                        }
                    });
                });
            } else {
                // Client-side filtering for real-time search
                std::thread::spawn(move || {
                    let rt = tokio::runtime::Runtime::new().unwrap();
                    let result = rt.block_on(async {
                        let client = api.lock().unwrap();
                        client.get_routes().await
                    });

                    let result_send = result.map_err(|e| e.to_string());

                    let _ = slint::invoke_from_event_loop(move || {
                        let ui = ui_weak.unwrap();

                        match result_send {
                            Ok(routes) => {
                                let filtered_routes: Vec<_> = routes
                                    .iter()
                                    .filter(|route| {
                                        route
                                            .start_point
                                            .to_lowercase()
                                            .contains(&query.to_lowercase())
                                            || route
                                                .end_point
                                                .to_lowercase()
                                                .contains(&query.to_lowercase())
                                            || route
                                                .bus_model()
                                                .to_lowercase()
                                                .contains(&query.to_lowercase())
                                            || route
                                                .driver_name()
                                                .to_lowercase()
                                                .contains(&query.to_lowercase())
                                    })
                                    .map(|route| RouteData {
                                        route_id: route.route_id as i32,
                                        start_point: slint::SharedString::from(&route.start_point),
                                        end_point: slint::SharedString::from(&route.end_point),
                                        bus_model: slint::SharedString::from(&route.bus_model()),
                                        driver_name: slint::SharedString::from(
                                            &route.driver_name(),
                                        ),
                                        travel_time: slint::SharedString::from(
                                            route.travel_time.as_deref().unwrap_or(""),
                                        ),
                                    })
                                    .collect();

                                let count = filtered_routes.len();
                                let model =
                                    std::rc::Rc::new(slint::VecModel::from(filtered_routes));
                                ui.set_routes(model.into());
                                println!("🔍 Found {} routes matching '{}'", count, query);
                            }
                            Err(e) => {
                                eprintln!("❌ Failed to search routes: {}", e);
                            }
                        }
                    });
                });
            }
        }
    });

    // Handle load dropdown data for route dialogs
    let api_load_dropdown = api_client.clone();
    main_ui.on_load_route_dropdown_data({
        let ui_handle = main_ui.as_weak();
        move || {
            let ui = ui_handle.unwrap();
            let api = api_load_dropdown.clone();
            let ui_weak = ui.as_weak();

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();

                // Load buses
                let buses_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_buses().await
                });

                // Load employees (drivers)
                let employees_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_employees().await
                });

                let buses_send = buses_result.map_err(|e| e.to_string());
                let employees_send = employees_result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();

                    match buses_send {
                        Ok(buses) => {
                            let bus_options: Vec<slint::SharedString> = buses
                                .iter()
                                .map(|bus| slint::SharedString::from(&bus.model))
                                .collect();
                            let model = std::rc::Rc::new(slint::VecModel::from(bus_options));
                            ui.set_route_bus_options(model.into());
                            println!("✅ Loaded {} buses for dropdown", buses.len());
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to load buses for dropdown: {}", e);
                        }
                    }

                    match employees_send {
                        Ok(employees) => {
                            let driver_options: Vec<slint::SharedString> = employees
                                .iter()
                                .map(|emp| {
                                    slint::SharedString::from(format!(
                                        "{} {}",
                                        emp.name, emp.surname
                                    ))
                                })
                                .collect();
                            let model = std::rc::Rc::new(slint::VecModel::from(driver_options));
                            ui.set_route_driver_options(model.into());
                            println!("✅ Loaded {} employees for dropdown", employees.len());
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to load employees for dropdown: {}", e);
                        }
                    }
                });
            });
        }
    });

    // Handle add route
    let api_add_route = api_client.clone();
    main_ui.on_add_route({
        let ui_handle = main_ui.as_weak();
        move |start_point, end_point, travel_time, bus_index, driver_index| {
            let ui = ui_handle.unwrap();
            let api = api_add_route.clone();
            let ui_weak = ui.as_weak();
            let start = start_point.to_string();
            let end = end_point.to_string();
            let time = travel_time.to_string();

            // Validate required fields
            if start.trim().is_empty() || end.trim().is_empty() || bus_index < 0 || driver_index < 0
            {
                ui.set_routes_error(slint::SharedString::from(
                    "Все обязательные поля должны быть заполнены",
                ));
                ui.set_routes_has_error(true);
                return;
            }

            ui.set_routes_loading(true);

            std::thread::spawn(move || {
                use crate::models::CreateRouteRequest;

                let rt = tokio::runtime::Runtime::new().unwrap();

                // Load buses and employees to map indices to IDs
                let buses_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_buses().await
                });

                let employees_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_employees().await
                });

                let (buses, employees) = match (buses_result, employees_result) {
                    (Ok(b), Ok(e)) => (b, e),
                    (Err(e), _) => {
                        let error_msg = format!("Failed to load buses: {}", e);
                        let _ = slint::invoke_from_event_loop(move || {
                            let ui = ui_weak.unwrap();
                            ui.set_routes_loading(false);
                            ui.set_routes_error(slint::SharedString::from(error_msg));
                            ui.set_routes_has_error(true);
                        });
                        return;
                    }
                    (_, Err(e)) => {
                        let error_msg = format!("Failed to load employees: {}", e);
                        let _ = slint::invoke_from_event_loop(move || {
                            let ui = ui_weak.unwrap();
                            ui.set_routes_loading(false);
                            ui.set_routes_error(slint::SharedString::from(error_msg));
                            ui.set_routes_has_error(true);
                        });
                        return;
                    }
                };

                // Map indices to IDs
                let bus_id = if bus_index >= 0 && (bus_index as usize) < buses.len() {
                    buses[bus_index as usize].bus_id
                } else {
                    let _ = slint::invoke_from_event_loop(move || {
                        let ui = ui_weak.unwrap();
                        ui.set_routes_loading(false);
                        ui.set_routes_error(slint::SharedString::from("Неверный индекс автобуса"));
                        ui.set_routes_has_error(true);
                    });
                    return;
                };

                let driver_id = if driver_index >= 0 && (driver_index as usize) < employees.len() {
                    employees[driver_index as usize].emp_id
                } else {
                    let _ = slint::invoke_from_event_loop(move || {
                        let ui = ui_weak.unwrap();
                        ui.set_routes_loading(false);
                        ui.set_routes_error(slint::SharedString::from("Неверный индекс водителя"));
                        ui.set_routes_has_error(true);
                    });
                    return;
                };

                let request = CreateRouteRequest {
                    start_point: start,
                    end_point: end,
                    driver_id,
                    bus_id,
                    travel_time: if time.is_empty() { None } else { Some(time) },
                };

                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.create_route(request).await
                });

                let result_send = result
                    .map(|route| route.display_name())
                    .map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_routes_loading(false);

                    match result_send {
                        Ok(display_name) => {
                            println!("✅ Created route: {}", display_name);
                            // Reload routes
                            ui.invoke_load_routes();
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to create route: {}", e);
                            ui.set_routes_error(slint::SharedString::from(format!(
                                "Ошибка создания: {}",
                                e
                            )));
                            ui.set_routes_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle edit route
    let api_edit_route = api_client.clone();
    main_ui.on_edit_route({
        let ui_handle = main_ui.as_weak();
        move |route_id, start_point, end_point, travel_time, bus_index, driver_index| {
            let ui = ui_handle.unwrap();
            let api = api_edit_route.clone();
            let ui_weak = ui.as_weak();
            let start = start_point.to_string();
            let end = end_point.to_string();
            let time = travel_time.to_string();

            // Validate required fields
            if start.trim().is_empty() || end.trim().is_empty() || bus_index < 0 || driver_index < 0
            {
                ui.set_routes_error(slint::SharedString::from(
                    "Все обязательные поля должны быть заполнены",
                ));
                ui.set_routes_has_error(true);
                return;
            }

            ui.set_routes_loading(true);

            std::thread::spawn(move || {
                use crate::models::UpdateRouteRequest;

                let rt = tokio::runtime::Runtime::new().unwrap();

                // Load buses and employees to map indices to IDs
                let buses_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_buses().await
                });

                let employees_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_employees().await
                });

                let (buses, employees) = match (buses_result, employees_result) {
                    (Ok(b), Ok(e)) => (b, e),
                    (Err(e), _) => {
                        let error_msg = format!("Failed to load buses: {}", e);
                        let _ = slint::invoke_from_event_loop(move || {
                            let ui = ui_weak.unwrap();
                            ui.set_routes_loading(false);
                            ui.set_routes_error(slint::SharedString::from(error_msg));
                            ui.set_routes_has_error(true);
                        });
                        return;
                    }
                    (_, Err(e)) => {
                        let error_msg = format!("Failed to load employees: {}", e);
                        let _ = slint::invoke_from_event_loop(move || {
                            let ui = ui_weak.unwrap();
                            ui.set_routes_loading(false);
                            ui.set_routes_error(slint::SharedString::from(error_msg));
                            ui.set_routes_has_error(true);
                        });
                        return;
                    }
                };

                // Map indices to IDs
                let bus_id = if bus_index >= 0 && (bus_index as usize) < buses.len() {
                    buses[bus_index as usize].bus_id
                } else {
                    let _ = slint::invoke_from_event_loop(move || {
                        let ui = ui_weak.unwrap();
                        ui.set_routes_loading(false);
                        ui.set_routes_error(slint::SharedString::from("Неверный индекс автобуса"));
                        ui.set_routes_has_error(true);
                    });
                    return;
                };

                let driver_id = if driver_index >= 0 && (driver_index as usize) < employees.len() {
                    employees[driver_index as usize].emp_id
                } else {
                    let _ = slint::invoke_from_event_loop(move || {
                        let ui = ui_weak.unwrap();
                        ui.set_routes_loading(false);
                        ui.set_routes_error(slint::SharedString::from("Неверный индекс водителя"));
                        ui.set_routes_has_error(true);
                    });
                    return;
                };

                let request = UpdateRouteRequest {
                    route_id: route_id as i64,
                    start_point: start,
                    end_point: end,
                    driver_id,
                    bus_id,
                    travel_time: if time.is_empty() { None } else { Some(time) },
                };

                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.update_route(route_id as i64, request).await
                });

                let result_send = result
                    .map(|route| route.display_name())
                    .map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_routes_loading(false);

                    match result_send {
                        Ok(display_name) => {
                            println!("✅ Updated route: {}", display_name);
                            // Reload routes
                            ui.invoke_load_routes();
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to update route: {}", e);
                            ui.set_routes_error(slint::SharedString::from(format!(
                                "Ошибка обновления: {}",
                                e
                            )));
                            ui.set_routes_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle delete route
    let api_delete_route = api_client.clone();
    main_ui.on_delete_route({
        let ui_handle = main_ui.as_weak();
        move |route_id| {
            let ui = ui_handle.unwrap();
            let api = api_delete_route.clone();
            let ui_weak = ui.as_weak();

            ui.set_routes_loading(true);

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.delete_route(route_id as i64).await
                });

                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_routes_loading(false);

                    match result_send {
                        Ok(_) => {
                            println!("✅ Deleted route {}", route_id);
                            // Reload routes
                            ui.invoke_load_routes();
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to delete route: {}", e);
                            ui.set_routes_error(slint::SharedString::from(format!(
                                "Ошибка удаления: {}",
                                e
                            )));
                            ui.set_routes_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // ========== ROUTE SCHEDULE MANAGEMENT CALLBACKS ==========

    // Handle load routes for selector
    let api_load_routes_selector = api_client.clone();
    main_ui.on_load_routes_for_selector({
        let ui_handle = main_ui.as_weak();
        move || {
            let ui = ui_handle.unwrap();
            let api = api_load_routes_selector.clone();
            let ui_weak = ui.as_weak();

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_routes().await
                });

                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();

                    match result_send {
                        Ok(routes) => {
                            let route_options: Vec<RouteOption> = routes
                                .iter()
                                .map(|route| RouteOption {
                                    route_id: route.route_id as i32,
                                    display_name: slint::SharedString::from(format!(
                                        "{} → {}",
                                        route.start_point, route.end_point
                                    )),
                                })
                                .collect();

                            // Also create route names for ComboBox
                            let route_names: Vec<slint::SharedString> = routes
                                .iter()
                                .map(|route| {
                                    slint::SharedString::from(format!(
                                        "{} → {}",
                                        route.start_point, route.end_point
                                    ))
                                })
                                .collect();

                            let model = std::rc::Rc::new(slint::VecModel::from(route_options));
                            ui.set_schedule_routes(model.into());

                            let names_model = std::rc::Rc::new(slint::VecModel::from(route_names));
                            ui.set_schedule_route_names(names_model.into());

                            println!("✅ Loaded {} routes for selector", routes.len());
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to load routes for selector: {}", e);
                            ui.set_schedules_error(slint::SharedString::from(format!(
                                "Ошибка загрузки маршрутов: {}",
                                e
                            )));
                            ui.set_schedules_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle load schedules
    let api_load_schedules = api_client.clone();
    main_ui.on_load_schedules({
        let ui_handle = main_ui.as_weak();
        move || {
            let ui = ui_handle.unwrap();
            let api = api_load_schedules.clone();
            let ui_weak = ui.as_weak();

            let selected_route_index = ui.get_selected_schedule_route_index();
            let selected_date = ui.get_selected_schedule_date().to_string();

            // Validate route is selected
            if selected_route_index < 0 {
                println!("No route selected, skipping schedule load");
                return;
            }

            ui.set_schedules_loading(true);
            ui.set_schedules_error(slint::SharedString::from(""));
            ui.set_schedules_has_error(false);

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();

                // Get route ID from the selected index
                let routes_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_routes().await
                });

                let route_id = match routes_result {
                    Ok(routes) => {
                        if selected_route_index >= 0 && (selected_route_index as usize) < routes.len() {
                            routes[selected_route_index as usize].route_id
                        } else {
                            let _ = slint::invoke_from_event_loop(move || {
                                let ui = ui_weak.unwrap();
                                ui.set_schedules_loading(false);
                                ui.set_schedules_error(slint::SharedString::from("Неверный индекс маршрута"));
                                ui.set_schedules_has_error(true);
                            });
                            return;
                        }
                    }
                    Err(e) => {
                        let error_msg = format!("Failed to load routes: {}", e);
                        let _ = slint::invoke_from_event_loop(move || {
                            let ui = ui_weak.unwrap();
                            ui.set_schedules_loading(false);
                            ui.set_schedules_error(slint::SharedString::from(error_msg));
                            ui.set_schedules_has_error(true);
                        });
                        return;
                    }
                };

                // Load schedules for the route
                println!("📅 Loading schedules for route ID: {}", route_id);
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_route_schedules_by_route(route_id).await
                });
                println!("📅 Schedules result: {:?}", result.as_ref().map(|s| s.len()).map_err(|e| e.to_string()));

                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_schedules_loading(false);

                    match result_send {
                        Ok(mut schedules) => {
                            println!("📅 Found {} route schedules for route {}", schedules.len(), route_id);

                            // Filter by date if provided
                            if !selected_date.is_empty() {
                                use chrono::{NaiveDate, Datelike};
                                if let Ok(filter_date) = NaiveDate::parse_from_str(&selected_date, "%Y-%m-%d") {
                                    let filter_weekday = filter_date.weekday();
                                    let filter_year = filter_date.year();
                                    println!("📅 Filtering schedules for date: {} (year: {}, weekday: {})", selected_date, filter_year, filter_weekday);
                                    println!("📅 Total schedules before filter: {}", schedules.len());

                                    // Debug: Show what dates we actually have in the data
                                    println!("🔍 STEP 1: Checking for exact date matches...");
                                    let sample_size = schedules.len().min(5);
                                    for i in 0..sample_size {
                                        let s = &schedules[i];
                                        println!("  Sample {}: ID={}, departure={}, arrival={}",
                                            i, s.route_schedule_id,
                                            s.departure_time.format("%Y-%m-%d %H:%M:%S"),
                                            s.arrival_time.format("%Y-%m-%d %H:%M:%S"));
                                    }

                                    // CRITICAL FIX: Check BOTH departure_time AND arrival_time dates
                                    // The actual schedule date might be in either field
                                    let mut exact_matches: Vec<_> = schedules.iter()
                                        .filter(|schedule| {
                                            let departure_date = schedule.departure_time.date_naive();
                                            let arrival_date = schedule.arrival_time.date_naive();

                                            // Match if EITHER date matches the filter
                                            let matches = departure_date == filter_date || arrival_date == filter_date;

                                            if matches {
                                                println!("  ✓ Schedule {} MATCHES: departure={}, arrival={}, filter={}",
                                                    schedule.route_schedule_id, departure_date, arrival_date, filter_date);
                                            }

                                            matches
                                        })
                                        .cloned()
                                        .collect();

                                    println!("📊 Exact match results: {} matches out of {} total", exact_matches.len(), schedules.len());

                                    if !exact_matches.is_empty() {
                                        println!("✅ Found {} schedules with EXACT DATE match", exact_matches.len());
                                        schedules = exact_matches;
                                    } else {
                                        println!("⚠️ No exact date matches found");
                                        println!("📊 Analyzing date distribution in database:");

                                        // Show date distribution
                                        let mut year_counts = std::collections::HashMap::new();
                                        for s in schedules.iter().take(100) {
                                            let dep_year = s.departure_time.date_naive().year();
                                            let arr_year = s.arrival_time.date_naive().year();
                                            *year_counts.entry(dep_year).or_insert(0) += 1;
                                            *year_counts.entry(arr_year).or_insert(0) += 1;
                                        }
                                        println!("  Year distribution (first 100 schedules): {:?}", year_counts);

                                        // Try matching by year and weekday
                                        println!("🔍 STEP 2: Trying year + weekday matching...");
                                        schedules.retain(|schedule| {
                                            let departure_date = schedule.departure_time.date_naive();
                                            let arrival_date = schedule.arrival_time.date_naive();
                                            let departure_year = departure_date.year();
                                            let arrival_year = arrival_date.year();
                                            let departure_weekday = schedule.departure_time.weekday();
                                            let arrival_weekday = schedule.arrival_time.weekday();

                                            // Check if EITHER date is in the correct year
                                            let year_matches = departure_year == filter_year || arrival_year == filter_year;
                                            if !year_matches {
                                                return false;
                                            }

                                            // Check weekday match
                                            let weekday_matches = departure_weekday == filter_weekday || arrival_weekday == filter_weekday;

                                            // Also check days_of_week array if populated
                                            let days_array_matches = if !schedule.days_of_week.is_empty() {
                                                schedule.days_of_week.iter().any(|day| {
                                                    let day_lower = day.to_lowercase();
                                                    match filter_weekday {
                                                        chrono::Weekday::Mon => day_lower.contains("понедельник") || day_lower.contains("пн") || day_lower.contains("monday") || day_lower.contains("mon"),
                                                        chrono::Weekday::Tue => day_lower.contains("вторник") || day_lower.contains("вт") || day_lower.contains("tuesday") || day_lower.contains("tue"),
                                                        chrono::Weekday::Wed => day_lower.contains("среда") || day_lower.contains("ср") || day_lower.contains("wednesday") || day_lower.contains("wed"),
                                                        chrono::Weekday::Thu => day_lower.contains("четверг") || day_lower.contains("чт") || day_lower.contains("thursday") || day_lower.contains("thu"),
                                                        chrono::Weekday::Fri => day_lower.contains("пятница") || day_lower.contains("пт") || day_lower.contains("friday") || day_lower.contains("fri"),
                                                        chrono::Weekday::Sat => day_lower.contains("суббота") || day_lower.contains("сб") || day_lower.contains("saturday") || day_lower.contains("sat"),
                                                        chrono::Weekday::Sun => day_lower.contains("воскресенье") || day_lower.contains("вс") || day_lower.contains("sunday") || day_lower.contains("sun"),
                                                    }
                                                })
                                            } else {
                                                false
                                            };

                                            weekday_matches || days_array_matches
                                        });

                                        if schedules.is_empty() {
                                            println!("❌ No schedules found for date {} ({}, year {})", selected_date, filter_weekday, filter_year);
                                        } else {
                                            println!("✅ Found {} schedules by year+weekday matching", schedules.len());
                                        }
                                    }
                                } else {
                                    println!("❌ Failed to parse date: {}", selected_date);
                                }
                            }

                            let total_count = schedules.len();
                            let page_size = 100;
                            let current_page = ui.get_current_page() as usize;
                            let start_idx = current_page * page_size;
                            let end_idx = (start_idx + page_size).min(total_count);

                            // Only convert the current page of schedules
                            let schedule_data: Vec<_> = schedules[start_idx..end_idx].iter().map(|schedule| {
                                ScheduleData {
                                    schedule_id: schedule.route_schedule_id as i32,
                                    route_id: schedule.route_id.unwrap_or(0) as i32,
                                    start_point: slint::SharedString::from(&schedule.start_point),
                                    end_point: slint::SharedString::from(&schedule.end_point),
                                    departure_time: slint::SharedString::from(schedule.departure_time.format("%Y-%m-%d %H:%M").to_string()),
                                    arrival_time: slint::SharedString::from(schedule.arrival_time.format("%Y-%m-%d %H:%M").to_string()),
                                    price: schedule.price as f32,
                                    available_seats: schedule.available_seats,
                                    route_stops: slint::SharedString::from(schedule.route_stops.join(", ")),
                                    is_active: schedule.is_active,
                                    status: slint::SharedString::from(schedule.status_text()),
                                }
                            }).collect();

                            let schedule_count = schedule_data.len();
                            let model = std::rc::Rc::new(slint::VecModel::from(schedule_data));
                            ui.set_schedules(model.into());
                            ui.set_total_schedules(total_count as i32);
                            ui.set_page_size(page_size as i32);
                            println!("✅ Loaded {} schedules (showing page {} of {}, {} total)",
                                schedule_count, current_page + 1, (total_count + page_size - 1) / page_size, total_count);
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to load schedules: {}", e);
                            ui.set_schedules_error(slint::SharedString::from(format!("Ошибка загрузки: {}", e)));
                            ui.set_schedules_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle route selected
    let api_route_selected = api_client.clone();
    main_ui.on_schedule_route_selected({
        let ui_handle = main_ui.as_weak();
        move |route_index| {
            println!("Route selected: {}", route_index);
            let ui = ui_handle.unwrap();
            ui.set_selected_schedule_route_index(route_index);
            ui.invoke_load_schedules();
        }
    });

    // Handle date changed
    main_ui.on_schedule_date_changed({
        let ui_handle = main_ui.as_weak();
        move |date_str| {
            println!("Date changed: {}", date_str);
            let ui = ui_handle.unwrap();
            ui.set_selected_schedule_date(date_str);
            ui.invoke_load_schedules();
        }
    });

    // Handle add schedule
    let api_add_schedule = api_client.clone();
    main_ui.on_add_schedule({
        let ui_handle = main_ui.as_weak();
        move || {
            println!("Add schedule clicked");
            let ui = ui_handle.unwrap();
            let api = api_add_schedule.clone();
            let ui_weak = ui.as_weak();

            // Get selected route index to pre-populate route stops
            let selected_route_index = ui.get_selected_schedule_route_index();

            if selected_route_index < 0 {
                ui.set_schedules_error(slint::SharedString::from("Выберите маршрут"));
                ui.set_schedules_has_error(true);
                return;
            }

            // Clear all dialog fields
            ui.set_schedule_departure_time(slint::SharedString::from(""));
            ui.set_schedule_arrival_time(slint::SharedString::from(""));
            ui.set_schedule_price(slint::SharedString::from(""));
            ui.set_schedule_available_seats(slint::SharedString::from(""));
            ui.set_schedule_is_active(true);
            ui.set_schedule_is_recurring(true);
            ui.set_schedule_stop_duration(slint::SharedString::from("5"));
            ui.set_schedule_notes(slint::SharedString::from(""));
            ui.set_schedule_error(slint::SharedString::from(""));
            ui.set_schedule_validation_error(slint::SharedString::from(""));
            ui.set_schedule_selected_stops_count(0);

            // Load route stops for the selected route
            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();

                // Get routes to find the selected one
                let routes_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_routes().await
                });

                let result_send = routes_result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();

                    match result_send {
                        Ok(routes) => {
                            if selected_route_index >= 0
                                && (selected_route_index as usize) < routes.len()
                            {
                                let route = &routes[selected_route_index as usize];

                                // Pre-populate route stops from the route
                                let stops =
                                    vec![route.start_point.clone(), route.end_point.clone()];

                                // Convert to RouteStopItem format
                                use crate::RouteStopItem;
                                let stop_items: Vec<RouteStopItem> = stops
                                    .iter()
                                    .map(|stop| RouteStopItem {
                                        name: slint::SharedString::from(stop),
                                        selected: false,
                                    })
                                    .collect();

                                let model = std::rc::Rc::new(slint::VecModel::from(stop_items));
                                ui.set_schedule_available_stops(model.into());

                                // Show dialog
                                ui.set_schedule_dialog_mode(slint::SharedString::from("add"));
                                ui.set_schedule_dialog_title(slint::SharedString::from(
                                    "Добавить расписание",
                                ));
                                ui.set_show_schedule_dialog(true);

                                println!("✅ Loaded {} route stops for add dialog", stops.len());
                            }
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to load route for add dialog: {}", e);
                            ui.set_schedules_error(slint::SharedString::from(format!(
                                "Ошибка: {}",
                                e
                            )));
                            ui.set_schedules_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle edit schedule
    let api_edit_schedule = api_client.clone();
    main_ui.on_edit_schedule({
        let ui_handle = main_ui.as_weak();
        move |schedule_id| {
            println!("Edit schedule {}", schedule_id);
            let ui = ui_handle.unwrap();
            let api = api_edit_schedule.clone();
            let ui_weak = ui.as_weak();

            ui.set_schedule_id(schedule_id);

            // Load the schedule data
            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_route_schedule(schedule_id as i64).await
                });

                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();

                    match result_send {
                        Ok(schedule) => {
                            println!("✅ Loaded schedule for edit: {}", schedule.display_name());

                            // Pre-populate dialog fields with schedule data
                            ui.set_schedule_departure_time(slint::SharedString::from(
                                schedule.departure_time.format("%H:%M").to_string(),
                            ));
                            ui.set_schedule_arrival_time(slint::SharedString::from(
                                schedule.arrival_time.format("%H:%M").to_string(),
                            ));
                            ui.set_schedule_price(slint::SharedString::from(format!(
                                "{:.2}",
                                schedule.price
                            )));
                            ui.set_schedule_available_seats(slint::SharedString::from(
                                schedule.available_seats.to_string(),
                            ));
                            ui.set_schedule_is_active(schedule.is_active);
                            ui.set_schedule_is_recurring(schedule.is_recurring);
                            ui.set_schedule_stop_duration(slint::SharedString::from(
                                schedule.stop_duration_minutes.to_string(),
                            ));
                            ui.set_schedule_notes(slint::SharedString::from(
                                schedule.notes.unwrap_or_default(),
                            ));

                            // Convert route stops to RouteStopItem format with pre-selection
                            use crate::RouteStopItem;
                            let stop_items: Vec<RouteStopItem> = schedule
                                .route_stops
                                .iter()
                                .map(|stop| {
                                    RouteStopItem {
                                        name: slint::SharedString::from(stop),
                                        selected: true, // Pre-select all existing stops
                                    }
                                })
                                .collect();

                            let selected_count = stop_items.len() as i32;
                            let model = std::rc::Rc::new(slint::VecModel::from(stop_items));
                            ui.set_schedule_available_stops(model.into());
                            ui.set_schedule_selected_stops_count(selected_count);

                            // Set estimated times and distances if available
                            if let Some(times) = schedule.estimated_stop_times {
                                let times_vec: Vec<slint::SharedString> =
                                    times.iter().map(|t| slint::SharedString::from(t)).collect();
                                let model = std::rc::Rc::new(slint::VecModel::from(times_vec));
                                ui.set_schedule_estimated_times(model.into());
                            }

                            if let Some(distances) = schedule.stop_distances {
                                let distances_vec: Vec<slint::SharedString> = distances
                                    .iter()
                                    .map(|d| slint::SharedString::from(format!("{:.1}", d)))
                                    .collect();
                                let model = std::rc::Rc::new(slint::VecModel::from(distances_vec));
                                ui.set_schedule_stop_distances(model.into());
                            }

                            // Show dialog
                            ui.set_schedule_dialog_mode(slint::SharedString::from("edit"));
                            ui.set_schedule_dialog_title(slint::SharedString::from(
                                "Редактировать расписание",
                            ));
                            ui.set_show_schedule_dialog(true);
                            ui.set_schedule_error(slint::SharedString::from(""));
                            ui.set_schedule_validation_error(slint::SharedString::from(""));
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to load schedule for edit: {}", e);
                            ui.set_schedules_error(slint::SharedString::from(format!(
                                "Ошибка загрузки: {}",
                                e
                            )));
                            ui.set_schedules_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle delete schedule - show confirmation dialog
    main_ui.on_delete_schedule({
        let ui_handle = main_ui.as_weak();
        move |schedule_id| {
            println!("Delete schedule {}", schedule_id);
            let ui = ui_handle.unwrap();

            // Find schedule description for confirmation
            let schedules = ui.get_schedules();
            let mut schedule_desc = String::new();
            for i in 0..schedules.row_count() {
                let schedule = schedules.row_data(i).unwrap();
                if schedule.schedule_id == schedule_id {
                    schedule_desc = format!(
                        "{} → {} ({})",
                        schedule.start_point, schedule.end_point, schedule.departure_time
                    );
                    break;
                }
            }

            ui.set_show_delete_schedule_dialog(true);
            ui.set_delete_schedule_id(schedule_id);
            ui.set_delete_schedule_description(slint::SharedString::from(schedule_desc));
        }
    });

    // Handle confirm delete schedule
    let api_confirm_delete_schedule = api_client.clone();
    main_ui.on_confirm_delete_schedule({
        let ui_handle = main_ui.as_weak();
        move || {
            let ui = ui_handle.unwrap();
            let api = api_confirm_delete_schedule.clone();
            let ui_weak = ui.as_weak();
            let schedule_id = ui.get_delete_schedule_id();

            ui.set_schedules_loading(true);
            ui.set_show_delete_schedule_dialog(false);

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.delete_route_schedule(schedule_id as i64).await
                });

                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_schedules_loading(false);

                    match result_send {
                        Ok(_) => {
                            println!("✅ Deleted schedule {}", schedule_id);
                            // Reload schedules
                            ui.invoke_load_schedules();
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to delete schedule: {}", e);
                            ui.set_schedules_error(slint::SharedString::from(format!(
                                "Ошибка удаления: {}",
                                e
                            )));
                            ui.set_schedules_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle save schedule
    let api_save_schedule = api_client.clone();
    main_ui.on_save_schedule({
        let ui_handle = main_ui.as_weak();
        move || {
            let ui = ui_handle.unwrap();
            let api = api_save_schedule.clone();
            let ui_weak = ui.as_weak();

            let mode = ui.get_schedule_dialog_mode().to_string();
            let schedule_id = ui.get_schedule_id();
            let departure_time = ui.get_schedule_departure_time().to_string();
            let arrival_time = ui.get_schedule_arrival_time().to_string();
            let price_str = ui.get_schedule_price().to_string();
            let seats_str = ui.get_schedule_available_seats().to_string();
            let is_active = ui.get_schedule_is_active();
            let is_recurring = ui.get_schedule_is_recurring();
            let stop_duration_str = ui.get_schedule_stop_duration().to_string();
            let notes = ui.get_schedule_notes().to_string();
            let selected_route_index = ui.get_selected_schedule_route_index();

            // Get selected stops
            let stops_model = ui.get_schedule_available_stops();
            let mut selected_stops = Vec::new();
            for i in 0..stops_model.row_count() {
                let stop = stops_model.row_data(i).unwrap();
                if stop.selected {
                    selected_stops.push(stop.name.to_string());
                }
            }

            // Validate
            if departure_time.trim().is_empty() || arrival_time.trim().is_empty() {
                ui.set_schedule_validation_error(slint::SharedString::from(
                    "Укажите время отправления и прибытия",
                ));
                return;
            }

            if price_str.trim().is_empty() || seats_str.trim().is_empty() {
                ui.set_schedule_validation_error(slint::SharedString::from(
                    "Укажите цену и количество мест",
                ));
                return;
            }

            if selected_stops.len() < 2 {
                ui.set_schedule_validation_error(slint::SharedString::from(
                    "Выберите минимум 2 остановки",
                ));
                return;
            }

            let price: f64 = match price_str.parse() {
                Ok(p) => p,
                Err(_) => {
                    ui.set_schedule_validation_error(slint::SharedString::from(
                        "Неверный формат цены",
                    ));
                    return;
                }
            };

            let seats: i32 = match seats_str.parse() {
                Ok(s) => s,
                Err(_) => {
                    ui.set_schedule_validation_error(slint::SharedString::from(
                        "Неверный формат количества мест",
                    ));
                    return;
                }
            };

            let stop_duration: i32 = stop_duration_str.parse().unwrap_or(5);

            ui.set_schedules_loading(true);
            ui.set_schedule_validation_error(slint::SharedString::from(""));

            std::thread::spawn(move || {
                use crate::models::{CreateRouteScheduleRequest, UpdateRouteScheduleRequest};
                use chrono::{NaiveTime, Timelike, Utc};

                let rt = tokio::runtime::Runtime::new().unwrap();

                // Parse times
                let dep_time = match NaiveTime::parse_from_str(&departure_time, "%H:%M") {
                    Ok(t) => t,
                    Err(_) => {
                        let _ = slint::invoke_from_event_loop(move || {
                            let ui = ui_weak.unwrap();
                            ui.set_schedules_loading(false);
                            ui.set_schedule_validation_error(slint::SharedString::from(
                                "Неверный формат времени отправления (HH:MM)",
                            ));
                        });
                        return;
                    }
                };

                let arr_time = match NaiveTime::parse_from_str(&arrival_time, "%H:%M") {
                    Ok(t) => t,
                    Err(_) => {
                        let _ = slint::invoke_from_event_loop(move || {
                            let ui = ui_weak.unwrap();
                            ui.set_schedules_loading(false);
                            ui.set_schedule_validation_error(slint::SharedString::from(
                                "Неверный формат времени прибытия (HH:MM)",
                            ));
                        });
                        return;
                    }
                };

                // Create DateTime from times (use today's date as base)
                let now = Utc::now();
                let departure_datetime = now
                    .date_naive()
                    .and_hms_opt(dep_time.hour(), dep_time.minute(), 0)
                    .unwrap()
                    .and_local_timezone(Utc)
                    .unwrap();

                let arrival_datetime = now
                    .date_naive()
                    .and_hms_opt(arr_time.hour(), arr_time.minute(), 0)
                    .unwrap()
                    .and_local_timezone(Utc)
                    .unwrap();

                // Get route ID
                let routes_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_routes().await
                });

                let route_id = match routes_result {
                    Ok(routes) => {
                        if selected_route_index >= 0
                            && (selected_route_index as usize) < routes.len()
                        {
                            Some(routes[selected_route_index as usize].route_id)
                        } else {
                            None
                        }
                    }
                    Err(_) => None,
                };

                let start_point = selected_stops.first().cloned().unwrap_or_default();
                let end_point = selected_stops.last().cloned().unwrap_or_default();

                if mode == "add" {
                    let request = CreateRouteScheduleRequest {
                        start_point,
                        route_stops: selected_stops,
                        end_point,
                        departure_time: departure_datetime,
                        arrival_time: arrival_datetime,
                        price,
                        available_seats: seats,
                        days_of_week: vec![],
                        bus_types: vec![],
                        route_id,
                        is_active,
                        valid_from: Utc::now(),
                        valid_until: None,
                        stop_duration_minutes: stop_duration,
                        is_recurring,
                        estimated_stop_times: None,
                        stop_distances: None,
                        notes: if notes.is_empty() { None } else { Some(notes) },
                    };

                    let result = rt.block_on(async {
                        let client = api.lock().unwrap();
                        client.create_route_schedule(request).await
                    });

                    let result_send = result.map(|s| s.display_name()).map_err(|e| e.to_string());

                    let _ = slint::invoke_from_event_loop(move || {
                        let ui = ui_weak.unwrap();
                        ui.set_schedules_loading(false);

                        match result_send {
                            Ok(display_name) => {
                                println!("✅ Created schedule: {}", display_name);
                                ui.set_show_schedule_dialog(false);
                                ui.invoke_load_schedules();
                            }
                            Err(e) => {
                                eprintln!("❌ Failed to create schedule: {}", e);
                                ui.set_schedule_error(slint::SharedString::from(format!(
                                    "Ошибка создания: {}",
                                    e
                                )));
                            }
                        }
                    });
                } else if mode == "edit" {
                    let request = UpdateRouteScheduleRequest {
                        route_schedule_id: schedule_id as i64,
                        start_point,
                        route_stops: selected_stops,
                        end_point,
                        departure_time: departure_datetime,
                        arrival_time: arrival_datetime,
                        price,
                        available_seats: seats,
                        days_of_week: vec![],
                        bus_types: vec![],
                        route_id,
                        is_active,
                        valid_from: Utc::now(),
                        valid_until: None,
                        stop_duration_minutes: stop_duration,
                        is_recurring,
                        estimated_stop_times: None,
                        stop_distances: None,
                        notes: if notes.is_empty() { None } else { Some(notes) },
                    };

                    let result = rt.block_on(async {
                        let client = api.lock().unwrap();
                        client
                            .update_route_schedule(schedule_id as i64, request)
                            .await
                    });

                    let result_send = result.map(|s| s.display_name()).map_err(|e| e.to_string());

                    let _ = slint::invoke_from_event_loop(move || {
                        let ui = ui_weak.unwrap();
                        ui.set_schedules_loading(false);

                        match result_send {
                            Ok(display_name) => {
                                println!("✅ Updated schedule: {}", display_name);
                                ui.set_show_schedule_dialog(false);
                                ui.invoke_load_schedules();
                            }
                            Err(e) => {
                                eprintln!("❌ Failed to update schedule: {}", e);
                                ui.set_schedule_error(slint::SharedString::from(format!(
                                    "Ошибка обновления: {}",
                                    e
                                )));
                            }
                        }
                    });
                }
            });
        }
    });

    // Handle stop selection changed
    main_ui.on_schedule_stop_selection_changed({
        let ui_handle = main_ui.as_weak();
        move |index, selected| {
            let ui = ui_handle.unwrap();
            let stops_model = ui.get_schedule_available_stops();

            // Update the selected state
            if index >= 0 && (index as usize) < stops_model.row_count() {
                let mut stop = stops_model.row_data(index as usize).unwrap();
                stop.selected = selected;
                stops_model.set_row_data(index as usize, stop);

                // Count selected stops
                let mut count = 0;
                for i in 0..stops_model.row_count() {
                    if stops_model.row_data(i).unwrap().selected {
                        count += 1;
                    }
                }
                ui.set_schedule_selected_stops_count(count);
            }
        }
    });

    // Handle next page
    main_ui.on_next_page({
        let ui_handle = main_ui.as_weak();
        move || {
            let ui = ui_handle.unwrap();
            let current_page = ui.get_current_page();
            let total_schedules = ui.get_total_schedules();
            let page_size = ui.get_page_size();
            let total_pages = (total_schedules + page_size - 1) / page_size;

            if current_page < total_pages - 1 {
                ui.set_current_page(current_page + 1);
                ui.invoke_load_schedules();
            }
        }
    });

    // Handle previous page
    main_ui.on_previous_page({
        let ui_handle = main_ui.as_weak();
        move || {
            let ui = ui_handle.unwrap();
            let current_page = ui.get_current_page();

            if current_page > 0 {
                ui.set_current_page(current_page - 1);
                ui.invoke_load_schedules();
            }
        }
    });

    // Handle schedule selected - show route stops from cached data
    main_ui.on_schedule_selected({
        let ui_handle = main_ui.as_weak();
        let api = api_client.clone();
        move |schedule_id| {
            println!("Schedule selected: {}", schedule_id);
            let ui = ui_handle.unwrap();
            let api_clone = api.clone();
            let ui_weak = ui.as_weak();

            // Get the selected route index to know which route's schedules to search
            let selected_route_index = ui.get_selected_schedule_route_index();

            if selected_route_index < 0 {
                println!("No route selected");
                return;
            }

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();

                // Get route ID
                let routes_result = rt.block_on(async {
                    let client = api_clone.lock().unwrap();
                    client.get_routes().await
                });

                let route_id = match routes_result {
                    Ok(routes) => {
                        if selected_route_index >= 0
                            && (selected_route_index as usize) < routes.len()
                        {
                            routes[selected_route_index as usize].route_id
                        } else {
                            return;
                        }
                    }
                    Err(_) => return,
                };

                // Get all schedules for this route from cache
                let schedules_result = rt.block_on(async {
                    let client = api_clone.lock().unwrap();
                    client.get_route_schedules_by_route(route_id).await
                });

                let result_send = schedules_result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();

                    match result_send {
                        Ok(schedules) => {
                            // Find the schedule with matching ID
                            if let Some(schedule) = schedules
                                .iter()
                                .find(|s| s.route_schedule_id == schedule_id as i64)
                            {
                                // Convert route stops to SharedString vector
                                let stops: Vec<slint::SharedString> = schedule
                                    .route_stops
                                    .iter()
                                    .map(|stop| slint::SharedString::from(stop))
                                    .collect();

                                let model = std::rc::Rc::new(slint::VecModel::from(stops));
                                ui.set_route_stops_list(model.into());
                                println!(
                                    "✅ Loaded {} route stops for schedule {}",
                                    schedule.route_stops.len(),
                                    schedule_id
                                );
                            } else {
                                println!("❌ Schedule {} not found in cached data", schedule_id);
                            }
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to load schedules: {}", e);
                        }
                    }
                });
            });
        }
    });

    // ========== JOBS MANAGEMENT CALLBACKS ==========

    // Handle load jobs
    let api_load_jobs = api_client.clone();
    main_ui.on_load_jobs({
        let ui_handle = main_ui.as_weak();
        move || {
            let ui = ui_handle.unwrap();
            let api = api_load_jobs.clone();
            let ui_weak = ui.as_weak();

            // Set loading state
            ui.set_jobs_loading(true);
            ui.set_jobs_error(slint::SharedString::from(""));
            ui.set_jobs_has_error(false);

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_jobs().await
                });

                // Convert result to Send-able types
                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_jobs_loading(false);

                    match result_send {
                        Ok(jobs) => {
                            let job_data: Vec<_> = jobs
                                .iter()
                                .map(|job| JobData {
                                    job_id: job.job_id,
                                    job_title: slint::SharedString::from(&job.job_title),
                                    internship: slint::SharedString::from(
                                        job.internship.as_deref().unwrap_or(""),
                                    ),
                                })
                                .collect();

                            let count = job_data.len();
                            let model = std::rc::Rc::new(slint::VecModel::from(job_data));
                            ui.set_jobs(model.into());
                            println!("✅ Loaded {} jobs", count);
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to load jobs: {}", e);
                            ui.set_jobs_error(slint::SharedString::from(format!(
                                "Ошибка загрузки: {}",
                                e
                            )));
                            ui.set_jobs_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle search jobs
    let api_search_jobs = api_client.clone();
    main_ui.on_search_jobs({
        let ui_handle = main_ui.as_weak();
        move |search_text| {
            let ui = ui_handle.unwrap();
            let api = api_search_jobs.clone();
            let ui_weak = ui.as_weak();
            let query = search_text.to_string();

            // If search is empty, reload all jobs
            if query.trim().is_empty() {
                std::thread::spawn(move || {
                    let rt = tokio::runtime::Runtime::new().unwrap();
                    let result = rt.block_on(async {
                        let client = api.lock().unwrap();
                        client.get_jobs().await
                    });

                    let result_send = result.map_err(|e| e.to_string());

                    let _ = slint::invoke_from_event_loop(move || {
                        let ui = ui_weak.unwrap();

                        match result_send {
                            Ok(jobs) => {
                                let job_data: Vec<_> = jobs
                                    .iter()
                                    .map(|job| JobData {
                                        job_id: job.job_id,
                                        job_title: slint::SharedString::from(&job.job_title),
                                        internship: slint::SharedString::from(
                                            job.internship.as_deref().unwrap_or(""),
                                        ),
                                    })
                                    .collect();

                                let model = std::rc::Rc::new(slint::VecModel::from(job_data));
                                ui.set_jobs(model.into());
                            }
                            Err(e) => {
                                eprintln!("❌ Failed to reload jobs: {}", e);
                            }
                        }
                    });
                });
            } else {
                // Use search API endpoint
                std::thread::spawn(move || {
                    let rt = tokio::runtime::Runtime::new().unwrap();
                    let result = rt.block_on(async {
                        let client = api.lock().unwrap();
                        client.search_jobs(Some(query.as_str()), None).await
                    });

                    let result_send = result.map_err(|e| e.to_string());

                    let _ = slint::invoke_from_event_loop(move || {
                        let ui = ui_weak.unwrap();

                        match result_send {
                            Ok(jobs) => {
                                let job_data: Vec<_> = jobs
                                    .iter()
                                    .map(|job| JobData {
                                        job_id: job.job_id,
                                        job_title: slint::SharedString::from(&job.job_title),
                                        internship: slint::SharedString::from(
                                            job.internship.as_deref().unwrap_or(""),
                                        ),
                                    })
                                    .collect();

                                let count = job_data.len();
                                let model = std::rc::Rc::new(slint::VecModel::from(job_data));
                                ui.set_jobs(model.into());
                                println!("🔍 Found {} jobs matching '{}'", count, query);
                            }
                            Err(e) => {
                                eprintln!("❌ Failed to search jobs: {}", e);
                            }
                        }
                    });
                });
            }
        }
    });

    // Handle add job
    let api_add_job = api_client.clone();
    main_ui.on_add_job({
        let ui_handle = main_ui.as_weak();
        move |title, internship| {
            let ui = ui_handle.unwrap();
            let api = api_add_job.clone();
            let ui_weak = ui.as_weak();
            let title_str = title.to_string();
            let internship_str = internship.to_string();

            // Validate title is not empty
            if title_str.trim().is_empty() {
                ui.set_jobs_error(slint::SharedString::from(
                    "Название должности не может быть пустым",
                ));
                ui.set_jobs_has_error(true);
                return;
            }

            ui.set_jobs_loading(true);

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();

                let intern = if internship_str.trim().is_empty() {
                    None
                } else {
                    Some(internship_str.as_str())
                };

                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.create_job(&title_str, intern).await
                });

                let result_send = result
                    .map(|job| job.job_title.clone())
                    .map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_jobs_loading(false);

                    match result_send {
                        Ok(job_title) => {
                            println!("✅ Created job: {}", job_title);
                            // Reload jobs
                            ui.invoke_load_jobs();
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to create job: {}", e);
                            ui.set_jobs_error(slint::SharedString::from(format!(
                                "Ошибка создания: {}",
                                e
                            )));
                            ui.set_jobs_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle edit job
    let api_edit_job = api_client.clone();
    main_ui.on_edit_job({
        let ui_handle = main_ui.as_weak();
        move |job_id, title, internship| {
            let ui = ui_handle.unwrap();
            let api = api_edit_job.clone();
            let ui_weak = ui.as_weak();
            let title_str = title.to_string();
            let internship_str = internship.to_string();

            // Validate title is not empty
            if title_str.trim().is_empty() {
                ui.set_jobs_error(slint::SharedString::from(
                    "Название должности не может быть пустым",
                ));
                ui.set_jobs_has_error(true);
                return;
            }

            ui.set_jobs_loading(true);

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();

                let intern = if internship_str.trim().is_empty() {
                    None
                } else {
                    Some(internship_str.as_str())
                };

                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.update_job(job_id, &title_str, intern).await
                });

                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_jobs_loading(false);

                    match result_send {
                        Ok(_) => {
                            println!("✅ Updated job: {}", title_str);
                            // Reload jobs
                            ui.invoke_load_jobs();
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to update job: {}", e);
                            ui.set_jobs_error(slint::SharedString::from(format!(
                                "Ошибка обновления: {}",
                                e
                            )));
                            ui.set_jobs_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle delete job
    let api_delete_job = api_client.clone();
    main_ui.on_delete_job({
        let ui_handle = main_ui.as_weak();
        move |job_id| {
            let ui = ui_handle.unwrap();
            let api = api_delete_job.clone();
            let ui_weak = ui.as_weak();

            ui.set_jobs_loading(true);

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.delete_job(job_id).await
                });

                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_jobs_loading(false);

                    match result_send {
                        Ok(_) => {
                            println!("✅ Deleted job {}", job_id);
                            // Reload jobs
                            ui.invoke_load_jobs();
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to delete job: {}", e);
                            ui.set_jobs_error(slint::SharedString::from(format!(
                                "Ошибка удаления: {}",
                                e
                            )));
                            ui.set_jobs_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // ========== USERS MANAGEMENT CALLBACKS ==========

    // Handle load users
    let api_load_users = api_client.clone();
    main_ui.on_load_users({
        let ui_handle = main_ui.as_weak();
        move || {
            let ui = ui_handle.unwrap();
            let api = api_load_users.clone();
            let ui_weak = ui.as_weak();

            ui.set_users_loading(true);
            ui.set_users_error(slint::SharedString::from(""));
            ui.set_users_has_error(false);

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_users().await
                });

                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_users_loading(false);

                    match result_send {
                        Ok(users) => {
                            let user_data: Vec<_> = users
                                .iter()
                                .map(|user| UserData {
                                    user_id: user.user_id as i32,
                                    login: slint::SharedString::from(&user.login),
                                    email: slint::SharedString::from(
                                        user.email.as_deref().unwrap_or(""),
                                    ),
                                    phone: slint::SharedString::from(
                                        user.phone_number.as_deref().unwrap_or(""),
                                    ),
                                    role: user.role,
                                    role_name: slint::SharedString::from(user.role_name()),
                                    is_active: user.is_active,
                                    is_windows_auth: user.is_windows_auth,
                                    windows_identity: slint::SharedString::from(
                                        user.windows_identity.as_deref().unwrap_or(""),
                                    ),
                                })
                                .collect();

                            let count = user_data.len();
                            let model = std::rc::Rc::new(slint::VecModel::from(user_data));
                            ui.set_users(model.into());
                            println!("✅ Loaded {} users", count);
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to load users: {}", e);
                            ui.set_users_error(slint::SharedString::from(format!(
                                "Ошибка загрузки: {}",
                                e
                            )));
                            ui.set_users_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle search users
    let api_search_users = api_client.clone();
    main_ui.on_search_users({
        let ui_handle = main_ui.as_weak();
        move |search_text| {
            let ui = ui_handle.unwrap();
            let api = api_search_users.clone();
            let ui_weak = ui.as_weak();
            let query = search_text.to_string();

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_users().await
                });

                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();

                    match result_send {
                        Ok(users) => {
                            let filtered_users: Vec<_> = if query.trim().is_empty() {
                                users
                                    .iter()
                                    .map(|user| UserData {
                                        user_id: user.user_id as i32,
                                        login: slint::SharedString::from(&user.login),
                                        email: slint::SharedString::from(
                                            user.email.as_deref().unwrap_or(""),
                                        ),
                                        phone: slint::SharedString::from(
                                            user.phone_number.as_deref().unwrap_or(""),
                                        ),
                                        role: user.role,
                                        role_name: slint::SharedString::from(user.role_name()),
                                        is_active: user.is_active,
                                        is_windows_auth: user.is_windows_auth,
                                        windows_identity: slint::SharedString::from(
                                            user.windows_identity.as_deref().unwrap_or(""),
                                        ),
                                    })
                                    .collect()
                            } else {
                                users
                                    .iter()
                                    .filter(|user| {
                                        let query_lower = query.to_lowercase();
                                        user.login.to_lowercase().contains(&query_lower)
                                            || user
                                                .email
                                                .as_ref()
                                                .map(|e| e.to_lowercase().contains(&query_lower))
                                                .unwrap_or(false)
                                    })
                                    .map(|user| UserData {
                                        user_id: user.user_id as i32,
                                        login: slint::SharedString::from(&user.login),
                                        email: slint::SharedString::from(
                                            user.email.as_deref().unwrap_or(""),
                                        ),
                                        phone: slint::SharedString::from(
                                            user.phone_number.as_deref().unwrap_or(""),
                                        ),
                                        role: user.role,
                                        role_name: slint::SharedString::from(user.role_name()),
                                        is_active: user.is_active,
                                        is_windows_auth: user.is_windows_auth,
                                        windows_identity: slint::SharedString::from(
                                            user.windows_identity.as_deref().unwrap_or(""),
                                        ),
                                    })
                                    .collect()
                            };

                            let count = filtered_users.len();
                            let model = std::rc::Rc::new(slint::VecModel::from(filtered_users));
                            ui.set_users(model.into());
                            println!("🔍 Found {} users matching '{}'", count, query);
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to search users: {}", e);
                        }
                    }
                });
            });
        }
    });

    // Handle add user
    let api_add_user = api_client.clone();
    main_ui.on_add_user({
        let ui_handle = main_ui.as_weak();
        move |login, password, email, phone, role, is_active, is_windows_auth, windows_identity| {
            let ui = ui_handle.unwrap();
            let api = api_add_user.clone();
            let ui_weak = ui.as_weak();

            let login_str = login.to_string();
            let password_str = password.to_string();
            let email_str = email.to_string();
            let phone_str = phone.to_string();
            let windows_identity_str = windows_identity.to_string();

            // Validate required fields
            if login_str.trim().is_empty() || password_str.trim().is_empty() {
                ui.set_users_error(slint::SharedString::from("Логин и пароль обязательны"));
                ui.set_users_has_error(true);
                return;
            }

            ui.set_users_loading(true);

            std::thread::spawn(move || {
                use models::CreateUserRequest;

                let request = CreateUserRequest {
                    login: login_str,
                    password: password_str,
                    role,
                    phone_number: if phone_str.is_empty() {
                        None
                    } else {
                        Some(phone_str)
                    },
                    email: if email_str.is_empty() {
                        None
                    } else {
                        Some(email_str)
                    },
                    is_windows_auth,
                    windows_identity: if windows_identity_str.is_empty() {
                        None
                    } else {
                        Some(windows_identity_str)
                    },
                };

                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.create_user(&request).await
                });

                let result_send = result.map(|u| u.login.clone()).map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_users_loading(false);

                    match result_send {
                        Ok(login) => {
                            println!("✅ Created user: {}", login);
                            // Reload users
                            ui.invoke_load_users();
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to create user: {}", e);
                            ui.set_users_error(slint::SharedString::from(format!(
                                "Ошибка создания: {}",
                                e
                            )));
                            ui.set_users_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle edit user
    let api_edit_user = api_client.clone();
    main_ui.on_edit_user({
        let ui_handle = main_ui.as_weak();
        move |user_id,
              login,
              password,
              email,
              phone,
              role,
              is_active,
              is_windows_auth,
              windows_identity| {
            let ui = ui_handle.unwrap();
            let api = api_edit_user.clone();
            let ui_weak = ui.as_weak();

            let login_str = login.to_string();
            let password_str = password.to_string();
            let email_str = email.to_string();
            let phone_str = phone.to_string();
            let windows_identity_str = windows_identity.to_string();

            // Validate required fields
            if login_str.trim().is_empty() {
                ui.set_users_error(slint::SharedString::from("Логин обязателен"));
                ui.set_users_has_error(true);
                return;
            }

            ui.set_users_loading(true);

            std::thread::spawn(move || {
                use models::UpdateUserRequest;

                let request = UpdateUserRequest {
                    login: Some(login_str),
                    password: if password_str.is_empty() {
                        None
                    } else {
                        Some(password_str)
                    },
                    role: Some(role),
                    phone_number: if phone_str.is_empty() {
                        None
                    } else {
                        Some(phone_str)
                    },
                    email: if email_str.is_empty() {
                        None
                    } else {
                        Some(email_str)
                    },
                    is_active: Some(is_active),
                    is_windows_auth: Some(is_windows_auth),
                    windows_identity: if windows_identity_str.is_empty() {
                        None
                    } else {
                        Some(windows_identity_str)
                    },
                };

                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.update_user(user_id as i64, &request).await
                });

                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_users_loading(false);

                    match result_send {
                        Ok(_) => {
                            println!("✅ Updated user {}", user_id);
                            // Reload users
                            ui.invoke_load_users();
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to update user: {}", e);
                            ui.set_users_error(slint::SharedString::from(format!(
                                "Ошибка обновления: {}",
                                e
                            )));
                            ui.set_users_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle delete user
    let api_delete_user = api_client.clone();
    main_ui.on_delete_user({
        let ui_handle = main_ui.as_weak();
        move |user_id| {
            let ui = ui_handle.unwrap();
            let api = api_delete_user.clone();
            let ui_weak = ui.as_weak();

            ui.set_users_loading(true);

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.delete_user(user_id as i64).await
                });

                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_users_loading(false);

                    match result_send {
                        Ok(_) => {
                            println!("✅ Deleted user {}", user_id);
                            // Reload users
                            ui.invoke_load_users();
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to delete user: {}", e);
                            ui.set_users_error(slint::SharedString::from(format!(
                                "Ошибка удаления: {}",
                                e
                            )));
                            ui.set_users_has_error(true);
                        }
                    }
                });
            });
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

    // ========== MAINTENANCE MANAGEMENT CALLBACKS ==========

    // Handle load maintenance records
    let api_load_maintenance = api_client.clone();
    main_ui.on_load_maintenance({
        let ui_handle = main_ui.as_weak();
        move || {
            let ui = ui_handle.unwrap();
            let api = api_load_maintenance.clone();
            let ui_weak = ui.as_weak();

            ui.set_maintenance_loading(true);
            ui.set_maintenance_error(slint::SharedString::from(""));
            ui.set_maintenance_has_error(false);

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_maintenance_records().await
                });

                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_maintenance_loading(false);

                    match result_send {
                        Ok(records) => {
                            let maintenance_data: Vec<_> = records
                                .iter()
                                .map(|m| MaintenanceData {
                                    maintenance_id: m.maintenance_id as i32,
                                    bus_id: m.bus_id as i32,
                                    bus_model: slint::SharedString::from(
                                        m.avtobus.as_ref().map(|b| b.model.as_str()).unwrap_or("N/A")
                                    ),
                                    last_service_date: slint::SharedString::from(&m.last_service_date),
                                    next_service_date: slint::SharedString::from(&m.next_service_date),
                                    mileage_threshold: slint::SharedString::from(&m.mileage_threshold),
                                    maintenance_type: slint::SharedString::from(&m.maintenance_type),
                                    service_engineer: slint::SharedString::from(&m.service_engineer),
                                    found_issues: slint::SharedString::from(&m.found_issues),
                                    roadworthiness: slint::SharedString::from(&m.roadworthiness),
                                })
                                .collect();

                            let count = maintenance_data.len();
                            let model = std::rc::Rc::new(slint::VecModel::from(maintenance_data));
                            ui.set_maintenance_records(model.into());
                            println!("✅ Loaded {} maintenance records", count);
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to load maintenance records: {}", e);
                            ui.set_maintenance_error(slint::SharedString::from(format!(
                                "Ошибка загрузки: {}",
                                e
                            )));
                            ui.set_maintenance_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle search maintenance
    let api_search_maintenance = api_client.clone();
    main_ui.on_search_maintenance({
        let ui_handle = main_ui.as_weak();
        move |search_text| {
            let ui = ui_handle.unwrap();
            let api = api_search_maintenance.clone();
            let ui_weak = ui.as_weak();
            let query = search_text.to_string();

            if query.trim().is_empty() {
                // Reload all records
                std::thread::spawn(move || {
                    let rt = tokio::runtime::Runtime::new().unwrap();
                    let result = rt.block_on(async {
                        let client = api.lock().unwrap();
                        client.get_maintenance_records().await
                    });

                    let result_send = result.map_err(|e| e.to_string());

                    let _ = slint::invoke_from_event_loop(move || {
                        let ui = ui_weak.unwrap();

                        match result_send {
                            Ok(records) => {
                                let maintenance_data: Vec<_> = records
                                    .iter()
                                    .map(|m| MaintenanceData {
                                        maintenance_id: m.maintenance_id as i32,
                                        bus_id: m.bus_id as i32,
                                        bus_model: slint::SharedString::from(
                                            m.avtobus.as_ref().map(|b| b.model.as_str()).unwrap_or("N/A")
                                        ),
                                        last_service_date: slint::SharedString::from(&m.last_service_date),
                                        next_service_date: slint::SharedString::from(&m.next_service_date),
                                        mileage_threshold: slint::SharedString::from(&m.mileage_threshold),
                                        maintenance_type: slint::SharedString::from(&m.maintenance_type),
                                        service_engineer: slint::SharedString::from(&m.service_engineer),
                                        found_issues: slint::SharedString::from(&m.found_issues),
                                        roadworthiness: slint::SharedString::from(&m.roadworthiness),
                                    })
                                    .collect();

                                let model = std::rc::Rc::new(slint::VecModel::from(maintenance_data));
                                ui.set_maintenance_records(model.into());
                            }
                            Err(e) => {
                                eprintln!("❌ Failed to reload maintenance: {}", e);
                            }
                        }
                    });
                });
            } else {
                // Search with API
                std::thread::spawn(move || {
                    let rt = tokio::runtime::Runtime::new().unwrap();
                    let result = rt.block_on(async {
                        let client = api.lock().unwrap();
                        client.search_maintenance(&query).await
                    });

                    let result_send = result.map_err(|e| e.to_string());

                    let _ = slint::invoke_from_event_loop(move || {
                        let ui = ui_weak.unwrap();

                        match result_send {
                            Ok(records) => {
                                let maintenance_data: Vec<_> = records
                                    .iter()
                                    .map(|m| MaintenanceData {
                                        maintenance_id: m.maintenance_id as i32,
                                        bus_id: m.bus_id as i32,
                                        bus_model: slint::SharedString::from(
                                            m.avtobus.as_ref().map(|b| b.model.as_str()).unwrap_or("N/A")
                                        ),
                                        last_service_date: slint::SharedString::from(&m.last_service_date),
                                        next_service_date: slint::SharedString::from(&m.next_service_date),
                                        mileage_threshold: slint::SharedString::from(&m.mileage_threshold),
                                        maintenance_type: slint::SharedString::from(&m.maintenance_type),
                                        service_engineer: slint::SharedString::from(&m.service_engineer),
                                        found_issues: slint::SharedString::from(&m.found_issues),
                                        roadworthiness: slint::SharedString::from(&m.roadworthiness),
                                    })
                                    .collect();

                                let count = maintenance_data.len();
                                let model = std::rc::Rc::new(slint::VecModel::from(maintenance_data));
                                ui.set_maintenance_records(model.into());
                                println!("🔍 Found {} maintenance records matching '{}'", count, query);
                            }
                            Err(e) => {
                                eprintln!("❌ Failed to search maintenance: {}", e);
                            }
                        }
                    });
                });
            }
        }
    });

    // ========== REPORTS CALLBACKS ==========

    // Handle refresh income report
    let api_income_report = api_client.clone();
    main_ui.on_refresh_income_report({
        let ui_handle = main_ui.as_weak();
        move || {
            let ui = ui_handle.unwrap();
            let api = api_income_report.clone();
            let ui_weak = ui.as_weak();

            let start_date = ui.get_reports_start_date().to_string();
            let end_date = ui.get_reports_end_date().to_string();

            ui.set_reports_loading(true);
            ui.set_reports_error(slint::SharedString::from(""));
            ui.set_reports_has_error(false);

            std::thread::spawn(move || {
                let rt = tokio::runtime::Runtime::new().unwrap();
                
                let start_opt = if start_date.is_empty() { None } else { Some(start_date.as_str()) };
                let end_opt = if end_date.is_empty() { None } else { Some(end_date.as_str()) };

                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_income_report(start_opt, end_opt).await
                });

                let result_send = result.map_err(|e| e.to_string());

                let _ = slint::invoke_from_event_loop(move || {
                    let ui = ui_weak.unwrap();
                    ui.set_reports_loading(false);

                    match result_send {
                        Ok(sales) => {
                            // Calculate totals
                            let total_income: f64 = sales.iter()
                                .filter_map(|s| s.bilet.as_ref())
                                .map(|t| t.ticket_price)
                                .sum();
                            let total_tickets = sales.len() as i32;
                            let avg_price = if total_tickets > 0 {
                                total_income / total_tickets as f64
                            } else {
                                0.0
                            };

                            ui.set_total_income(total_income as f32);
                            ui.set_total_tickets_sold(total_tickets);
                            ui.set_average_ticket_price(avg_price as f32);

                            println!("✅ Loaded income report: {} sales, {} ₽ total", total_tickets, total_income);
                        }
                        Err(e) => {
                            eprintln!("❌ Failed to load income report: {}", e);
                            ui.set_reports_error(slint::SharedString::from(format!(
                                "Ошибка загрузки: {}",
                                e
                            )));
                            ui.set_reports_has_error(true);
                        }
                    }
                });
            });
        }
    });

    // Handle navigation changes
    let api_client_nav = api_client.clone();
    main_ui.on_navigation_changed({
        let ui_weak = main_ui.as_weak();
        move |group, index| {
            use navigation::AppRoute;

            if let Some(route) = AppRoute::from_indices(group, index) {
                println!(
                    "Navigation: {:?} (Group: {}, Index: {})",
                    route, group, index
                );

                // Load data based on route
                match route {
                    AppRoute::Employees => {
                        println!("Loading employees...");
                        load_employees_impl(ui_weak.clone(), api_client_nav.clone());
                    }
                    AppRoute::Jobs => {
                        println!("Loading jobs...");
                        let ui = ui_weak.unwrap();
                        ui.invoke_load_jobs();
                    }
                    AppRoute::Users => {
                        println!("Loading users...");
                        let ui = ui_weak.unwrap();
                        ui.invoke_load_users();
                    }
                    AppRoute::Buses => {
                        println!("Loading buses...");
                        let ui = ui_weak.unwrap();
                        ui.invoke_load_buses();
                    }
                    AppRoute::Routes => {
                        println!("Loading routes...");
                        let ui = ui_weak.unwrap();
                        ui.invoke_load_routes();
                    }
                    AppRoute::Schedules => {
                        println!("Loading route schedules...");
                        let ui = ui_weak.unwrap();
                        ui.invoke_load_routes_for_selector();
                    }
                    AppRoute::Maintenance => {
                        println!("Loading maintenance records...");
                        let ui = ui_weak.unwrap();
                        ui.invoke_load_maintenance();
                    }
                    AppRoute::Reports => {
                        println!("Loading reports...");
                        let ui = ui_weak.unwrap();
                        ui.invoke_refresh_income_report();
                    }
                    _ => {
                        println!("Route {} not yet implemented", route.display_name());
                    }
                }
            }
        }
    });

    // Handle view employee detail
    let api_detail = api_client.clone();
    main_ui.on_view_employee_detail_clicked({
        let ui_handle = main_ui.as_weak();
        move |emp_id| {
            let ui = ui_handle.unwrap();
            let api = api_detail.clone();
            let ui_weak = ui.as_weak();

            slint::spawn_local(async move {
                let rt = tokio::runtime::Runtime::new().unwrap();

                // Load employee
                let emp_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_employee(emp_id as i64).await
                });

                // Load all related data in parallel
                let docs_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_employee_documents(emp_id as i64).await
                });

                let training_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_employee_training(emp_id as i64).await
                });

                let contacts_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_emergency_contacts(emp_id as i64).await
                });

                let vacations_result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client.get_vacation_requests(emp_id as i64).await
                });

                match emp_result {
                    Ok(employee) => {
                        let full_name = format!(
                            "{} {} {}",
                            employee.surname,
                            employee.name,
                            employee.patronym.unwrap_or_default().trim()
                        );

                        println!("Loading detail for: {}", full_name);

                        ui_weak.unwrap().set_detail_employee_id(emp_id);
                        ui_weak
                            .unwrap()
                            .set_detail_employee_name(slint::SharedString::from(full_name));

                        // Convert and set documents
                        match &docs_result {
                            Ok(docs) => {
                                println!("✅ Loaded {} documents", docs.len());
                                let doc_data: Vec<_> = docs
                                    .iter()
                                    .map(|doc| DocumentData {
                                        document_id: doc.document_id as i32,
                                        document_type: slint::SharedString::from(
                                            &doc.document_type,
                                        ),
                                        document_number: slint::SharedString::from(
                                            &doc.document_number,
                                        ),
                                        issue_date: slint::SharedString::from(
                                            date_utils::format_date_for_ui(Some(doc.issue_date)),
                                        ),
                                        expiry_date: slint::SharedString::from(
                                            doc.expiry_date
                                                .map(|d| date_utils::format_date_for_ui(Some(d)))
                                                .unwrap_or_default(),
                                        ),
                                        status: slint::SharedString::from(doc.status_badge()),
                                    })
                                    .collect();
                                let model = std::rc::Rc::new(slint::VecModel::from(doc_data));
                                ui_weak.unwrap().set_employee_documents(model.into());
                            }
                            Err(e) => eprintln!("❌ Failed to load documents: {}", e),
                        }

                        // Convert and set training
                        match &training_result {
                            Ok(training) => {
                                println!("✅ Loaded {} training records", training.len());
                                let training_data: Vec<_> = training
                                    .iter()
                                    .map(|train| TrainingData {
                                        training_id: train.training_id as i32,
                                        training_name: slint::SharedString::from(
                                            &train.training_name,
                                        ),
                                        certificate_number: slint::SharedString::from(
                                            train.certificate_number.as_deref().unwrap_or(""),
                                        ),
                                        completion_date: slint::SharedString::from(
                                            date_utils::format_date_for_ui(Some(
                                                train.completion_date,
                                            )),
                                        ),
                                        expiry_date: slint::SharedString::from(
                                            train
                                                .expiry_date
                                                .map(|d| date_utils::format_date_for_ui(Some(d)))
                                                .unwrap_or_default(),
                                        ),
                                        status: slint::SharedString::from(train.status_text()),
                                        is_mandatory: train.is_mandatory,
                                    })
                                    .collect();
                                let model = std::rc::Rc::new(slint::VecModel::from(training_data));
                                ui_weak.unwrap().set_employee_training(model.into());
                            }
                            Err(e) => eprintln!("❌ Failed to load training: {}", e),
                        }

                        // Convert and set contacts
                        match &contacts_result {
                            Ok(contacts) => {
                                println!("✅ Loaded {} contacts", contacts.len());
                                let contact_data: Vec<_> = contacts
                                    .iter()
                                    .map(|contact| EmergencyContactData {
                                        contact_id: contact.contact_id as i32,
                                        contact_name: slint::SharedString::from(
                                            &contact.contact_name,
                                        ),
                                        relationship: slint::SharedString::from(
                                            &contact.relationship,
                                        ),
                                        phone_number: slint::SharedString::from(
                                            &contact.phone_number,
                                        ),
                                        alternate_phone: slint::SharedString::from(
                                            contact.alternate_phone_number.as_deref().unwrap_or(""),
                                        ),
                                        is_primary: contact.is_primary,
                                    })
                                    .collect();
                                let model = std::rc::Rc::new(slint::VecModel::from(contact_data));
                                ui_weak.unwrap().set_employee_contacts(model.into());
                            }
                            Err(e) => eprintln!("❌ Failed to load contacts: {}", e),
                        }

                        // Convert and set vacations
                        match &vacations_result {
                            Ok(vacations) => {
                                println!("✅ Loaded {} vacation requests", vacations.len());
                                let vacation_data: Vec<_> = vacations
                                    .iter()
                                    .map(|vac| VacationData {
                                        request_id: vac.request_id as i32,
                                        start_date: slint::SharedString::from(
                                            date_utils::format_date_for_ui(Some(vac.start_date)),
                                        ),
                                        end_date: slint::SharedString::from(
                                            date_utils::format_date_for_ui(Some(vac.end_date)),
                                        ),
                                        vacation_type: slint::SharedString::from(
                                            &vac.vacation_type,
                                        ),
                                        days_requested: vac.days_requested,
                                        status: slint::SharedString::from(&vac.status),
                                        reason: slint::SharedString::from(
                                            vac.reason.as_deref().unwrap_or(""),
                                        ),
                                    })
                                    .collect();
                                let model = std::rc::Rc::new(slint::VecModel::from(vacation_data));
                                ui_weak.unwrap().set_employee_vacations(model.into());
                            }
                            Err(e) => eprintln!("❌ Failed to load vacations: {}", e),
                        }

                        ui_weak.unwrap().set_show_employee_detail(true);
                        println!("Loaded detail view for employee {}", emp_id);
                    }
                    Err(e) => {
                        eprintln!("Failed to load employee detail: {}", e);
                    }
                }
            })
            .unwrap();
        }
    });

    // Placeholder callbacks for document operations
    main_ui.on_add_document_clicked({
        move || {
            println!("Add document clicked - TODO: Implement dialog");
        }
    });

    main_ui.on_edit_document_clicked({
        move |doc_id| {
            println!("Edit document {} - TODO: Implement dialog", doc_id);
        }
    });

    let api_delete_doc = api_client.clone();
    main_ui.on_delete_document_clicked({
        let ui_handle = main_ui.as_weak();
        move |doc_id| {
            println!("Delete document {}", doc_id);
            let api = api_delete_doc.clone();
            let ui = ui_handle.unwrap();
            let emp_id = ui.get_detail_employee_id();

            slint::spawn_local(async move {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client
                        .delete_employee_document(emp_id as i64, doc_id as i64)
                        .await
                });

                match result {
                    Ok(_) => {
                        println!("Document {} deleted successfully", doc_id);
                        // Reload documents
                        // TODO: Reload document list
                    }
                    Err(e) => {
                        eprintln!("Failed to delete document: {}", e);
                    }
                }
            })
            .unwrap();
        }
    });

    // Placeholder callbacks for training operations
    main_ui.on_add_training_clicked({
        move || {
            println!("Add training clicked - TODO: Implement dialog");
        }
    });

    main_ui.on_edit_training_clicked({
        move |train_id| {
            println!("Edit training {} - TODO: Implement dialog", train_id);
        }
    });

    let api_delete_train = api_client.clone();
    main_ui.on_delete_training_clicked({
        let ui_handle = main_ui.as_weak();
        move |train_id| {
            println!("Delete training {}", train_id);
            let api = api_delete_train.clone();
            let ui = ui_handle.unwrap();
            let emp_id = ui.get_detail_employee_id();

            slint::spawn_local(async move {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client
                        .delete_employee_training(emp_id as i64, train_id as i64)
                        .await
                });

                match result {
                    Ok(_) => {
                        println!("Training {} deleted successfully", train_id);
                    }
                    Err(e) => {
                        eprintln!("Failed to delete training: {}", e);
                    }
                }
            })
            .unwrap();
        }
    });

    // Placeholder callbacks for contact operations
    main_ui.on_add_contact_clicked({
        move || {
            println!("Add contact clicked - TODO: Implement dialog");
        }
    });

    main_ui.on_edit_contact_clicked({
        move |contact_id| {
            println!("Edit contact {} - TODO: Implement dialog", contact_id);
        }
    });

    let api_delete_contact = api_client.clone();
    main_ui.on_delete_contact_clicked({
        let ui_handle = main_ui.as_weak();
        move |contact_id| {
            println!("Delete contact {}", contact_id);
            let api = api_delete_contact.clone();
            let ui = ui_handle.unwrap();
            let emp_id = ui.get_detail_employee_id();

            slint::spawn_local(async move {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client
                        .delete_emergency_contact(emp_id as i64, contact_id as i64)
                        .await
                });

                match result {
                    Ok(_) => {
                        println!("Contact {} deleted successfully", contact_id);
                    }
                    Err(e) => {
                        eprintln!("Failed to delete contact: {}", e);
                    }
                }
            })
            .unwrap();
        }
    });

    // Placeholder callbacks for vacation operations
    main_ui.on_add_vacation_clicked({
        move || {
            println!("Add vacation clicked - TODO: Implement dialog");
        }
    });

    let api_approve_vac = api_client.clone();
    main_ui.on_approve_vacation_clicked({
        let ui_handle = main_ui.as_weak();
        move |req_id| {
            println!("Approve vacation {}", req_id);
            let api = api_approve_vac.clone();
            let ui = ui_handle.unwrap();
            let emp_id = ui.get_detail_employee_id();

            slint::spawn_local(async move {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client
                        .approve_vacation_request(emp_id as i64, req_id as i64, None)
                        .await
                });

                match result {
                    Ok(_) => {
                        println!("Vacation {} approved successfully", req_id);
                        // TODO: Reload vacation list
                    }
                    Err(e) => {
                        eprintln!("Failed to approve vacation: {}", e);
                    }
                }
            })
            .unwrap();
        }
    });

    let api_reject_vac = api_client.clone();
    main_ui.on_reject_vacation_clicked({
        let ui_handle = main_ui.as_weak();
        move |req_id| {
            println!("Reject vacation {}", req_id);
            let api = api_reject_vac.clone();
            let ui = ui_handle.unwrap();
            let emp_id = ui.get_detail_employee_id();

            slint::spawn_local(async move {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client
                        .reject_vacation_request(emp_id as i64, req_id as i64, None)
                        .await
                });

                match result {
                    Ok(_) => {
                        println!("Vacation {} rejected successfully", req_id);
                    }
                    Err(e) => {
                        eprintln!("Failed to reject vacation: {}", e);
                    }
                }
            })
            .unwrap();
        }
    });

    let api_delete_vac = api_client.clone();
    main_ui.on_delete_vacation_clicked({
        let ui_handle = main_ui.as_weak();
        move |req_id| {
            println!("Delete vacation {}", req_id);
            let api = api_delete_vac.clone();
            let ui = ui_handle.unwrap();
            let emp_id = ui.get_detail_employee_id();

            slint::spawn_local(async move {
                let rt = tokio::runtime::Runtime::new().unwrap();
                let result = rt.block_on(async {
                    let client = api.lock().unwrap();
                    client
                        .delete_vacation_request(emp_id as i64, req_id as i64)
                        .await
                });

                match result {
                    Ok(_) => {
                        println!("Vacation {} deleted successfully", req_id);
                    }
                    Err(e) => {
                        eprintln!("Failed to delete vacation: {}", e);
                    }
                }
            })
            .unwrap();
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

fn load_employees_impl(ui_weak: slint::Weak<AppWindow>, api_client: Arc<Mutex<ApiClient>>) {
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
                    .map(|emp| EmployeeData {
                        id: emp.emp_id as i32,
                        surname: emp.surname.clone().into(),
                        name: emp.name.clone().into(),
                        department: emp.department_name().into(),
                        position: emp.job_title().into(),
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
