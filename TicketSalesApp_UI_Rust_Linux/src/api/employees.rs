use super::{ApiClient, ApiError};
use crate::models::{CreateEmployeeRequest, Employee, EmployeeTraining, VacationRequest};
use serde::Deserialize;
use serde_json::Value;

// Wrapper for ASP.NET Core's ReferenceHandler.Preserve format
#[derive(Deserialize)]
struct RefWrapper {
    #[serde(rename = "$values")]
    values: Vec<Value>,
}

impl ApiClient {
    /// Get all employees - manually parse to handle circular refs
    pub async fn get_employees(&self) -> Result<Vec<Employee>, ApiError> {
        let response = self.get("api/employees").await?;
        let text = response.text().await
            .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
        
        // Parse as raw JSON first
        let json: Value = serde_json::from_str(&text)
            .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
        
        // Extract $values array
        let employees_array = json.get("$values")
            .and_then(|v| v.as_array())
            .ok_or_else(|| ApiError::ServerError("Missing $values in response".to_string()))?;
        
        // First pass: collect all employee, department, and job objects by their $id
        println!("\n🔍 Analyzing {} items in $values array:", employees_array.len());
        let mut employee_map = std::collections::HashMap::new();
        let mut department_map = std::collections::HashMap::new();
        let mut job_map = std::collections::HashMap::new();
        
        // Recursive function to find all objects in the JSON tree
        fn collect_objects(
            value: &Value, 
            emp_map: &mut std::collections::HashMap<String, Value>,
            dept_map: &mut std::collections::HashMap<String, Value>,
            job_map: &mut std::collections::HashMap<String, Value>
        ) {
            if let Some(obj) = value.as_object() {
                if let Some(id) = obj.get("$id").and_then(|v| v.as_str()) {
                    // Check if it's an employee (has empId)
                    if obj.contains_key("empId") {
                        println!("  Found employee with $id={}, empId={}", 
                            id, 
                            obj.get("empId").and_then(|v| v.as_i64()).unwrap_or(0)
                        );
                        emp_map.insert(id.to_string(), Value::Object(obj.clone()));
                    }
                    // Check if it's a department (has departmentId)
                    else if obj.contains_key("departmentId") {
                        let dept_name = obj.get("departmentName")
                            .and_then(|v| v.as_str())
                            .unwrap_or("Unknown");
                        println!("  Found department with $id={}, name={}", id, dept_name);
                        dept_map.insert(id.to_string(), Value::Object(obj.clone()));
                    }
                    // Check if it's a job (has jobId and jobTitle)
                    else if obj.contains_key("jobId") && obj.contains_key("jobTitle") {
                        let job_title = obj.get("jobTitle")
                            .and_then(|v| v.as_str())
                            .unwrap_or("Unknown");
                        println!("  Found job with $id={}, title={}", id, job_title);
                        job_map.insert(id.to_string(), Value::Object(obj.clone()));
                    }
                }
                // Recursively search all nested objects and arrays
                for (_, v) in obj.iter() {
                    collect_objects(v, emp_map, dept_map, job_map);
                }
            } else if let Some(arr) = value.as_array() {
                for item in arr {
                    collect_objects(item, emp_map, dept_map, job_map);
                }
            }
        }
        
        // Collect all objects from the entire JSON tree
        for emp_value in employees_array {
            if let Some(ref_id) = emp_value.get("$ref").and_then(|v| v.as_str()) {
                println!("  Item is $ref pointer to: {}", ref_id);
            } else {
                collect_objects(emp_value, &mut employee_map, &mut department_map, &mut job_map);
            }
        }
        
        println!("\n📊 Total unique employees found: {}", employee_map.len());
        println!("📊 Total unique departments found: {}", department_map.len());
        println!("📊 Total unique jobs found: {}\n", job_map.len());
        
        // Parse each unique employee
        let mut employees = Vec::new();
        for (_id, emp_value) in employee_map.iter() {
            
            // Manually extract scalar fields to avoid deep nesting issues
            let emp_obj = match emp_value.as_object() {
                Some(obj) => obj,
                None => continue,
            };
            
            // Helper to get field
            let get_i64 = |key: &str| emp_obj.get(key)?.as_i64();
            let get_str = |key: &str| emp_obj.get(key)?.as_str().map(|s| s.to_string());
            let get_date = |key: &str| {
                emp_obj.get(key)?
                    .as_str()
                    .and_then(|s| chrono::NaiveDate::parse_from_str(&s[..10], "%Y-%m-%d").ok())
            };
            let get_datetime = |key: &str| {
                emp_obj.get(key)?
                    .as_str()
                    .and_then(|s| chrono::DateTime::parse_from_rfc3339(s).ok())
                    .map(|dt| dt.with_timezone(&chrono::Utc))
            };
            let get_bool = |key: &str| emp_obj.get(key)?.as_bool();
            
            // Build Employee with only scalar fields
            let emp = Employee {
                ref_id: get_str("$id"),
                emp_id: get_i64("empId").unwrap_or(0),
                surname: get_str("surname").unwrap_or_default(),
                name: get_str("name").unwrap_or_default(),
                patronym: get_str("patronym"),
                employed_since: get_date("employedSince").unwrap_or_default(),
                job_id: get_i64("jobId").unwrap_or(0),
                department_id: get_i64("departmentId"),
                date_of_birth: get_date("dateOfBirth"),
                personal_phone: get_str("personalPhone"),
                work_phone: get_str("workPhone"),
                address: get_str("address"),
                email: get_str("email"),
                passport_series: get_str("passportSeries"),
                passport_number: get_str("passportNumber"),
                inn: get_str("inn"),
                snils: get_str("snils"),
                driver_license_number: get_str("driverLicenseNumber"),
                driver_license_category: get_str("driverLicenseCategory"),
                driver_license_issue_date: get_date("driverLicenseIssueDate"),
                driver_license_expiry_date: get_date("driverLicenseExpiryDate"),
                medical_certificate_number: get_str("medicalCertificateNumber"),
                medical_certificate_issue_date: get_date("medicalCertificateIssueDate"),
                medical_certificate_expiry_date: get_date("medicalCertificateExpiryDate"),
                last_medical_check_date: get_date("lastMedicalCheckDate"),
                next_medical_check_date: get_date("nextMedicalCheckDate"),
                has_passenger_transport_certification: get_bool("hasPassengerTransportCertification").unwrap_or(false),
                has_dangerous_goods_certification: get_bool("hasDangerousGoodsCertification").unwrap_or(false),
                is_active: get_bool("isActive").unwrap_or(true),
                termination_date: get_date("terminationDate"),
                termination_reason: get_str("terminationReason"),
                created_at: get_datetime("createdAt").unwrap_or_else(|| chrono::Utc::now()),
                updated_at: get_datetime("updatedAt"),
                // Resolve job $ref if present
                job: {
                    let job_value = emp_obj.get("job");
                    if let Some(job) = job_value {
                        // Check if it's a $ref pointer
                        if let Some(ref_id) = job.get("$ref").and_then(|v| v.as_str()) {
                            // Look up the actual job object
                            if let Some(resolved_job) = job_map.get(ref_id) {
                                println!("  ✓ Resolved job $ref {} -> {}", 
                                    ref_id,
                                    resolved_job.get("jobTitle")
                                        .and_then(|v| v.as_str())
                                        .unwrap_or("Unknown")
                                );
                                Some(resolved_job.clone())
                            } else {
                                println!("  ⚠ Job $ref {} not found in map", ref_id);
                                Some(job.clone())
                            }
                        } else {
                            // It's a full object
                            if let Some(title) = job.get("jobTitle") {
                                println!("  ✓ Job (full object): {}", title);
                            }
                            Some(job.clone())
                        }
                    } else {
                        println!("  ⚠ No job field");
                        None
                    }
                },
                // Resolve department $ref if present
                department: {
                    let dept_value = emp_obj.get("department");
                    if let Some(dept) = dept_value {
                        // Check if it's a $ref pointer
                        if let Some(ref_id) = dept.get("$ref").and_then(|v| v.as_str()) {
                            // Look up the actual department object
                            if let Some(resolved_dept) = department_map.get(ref_id) {
                                println!("  ✓ Resolved department $ref {} -> {}", 
                                    ref_id,
                                    resolved_dept.get("departmentName")
                                        .and_then(|v| v.as_str())
                                        .unwrap_or("Unknown")
                                );
                                Some(resolved_dept.clone())
                            } else {
                                println!("  ⚠ Department $ref {} not found in map", ref_id);
                                Some(dept.clone())
                            }
                        } else {
                            // It's a full object
                            if let Some(name) = dept.get("departmentName") {
                                println!("  ✓ Department (full object): {}", name);
                            }
                            Some(dept.clone())
                        }
                    } else {
                        println!("  ⚠ No department field");
                        None
                    }
                },
            };
            
            println!("✓ Parsed employee: {} {} (ID: {})", emp.surname, emp.name, emp.emp_id);
            employees.push(emp);
        }
        
        // Sort employees by ID
        employees.sort_by_key(|e| e.emp_id);
        
        println!("\n✅ Successfully parsed {}/{} employees total\n", employees.len(), employees_array.len());
        Ok(employees)
    }

    /// Get employee by ID
    pub async fn get_employee(&self, id: i64) -> Result<Employee, ApiError> {
        let endpoint = format!("api/employees/{}", id);
        let response = self.get(&endpoint).await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            println!("[DEBUG] Raw response (first 500 chars): {}", 
                     if text.len() > 500 { &text[..500] } else { &text });
            
            // Parse with ReferenceHandler.Preserve handling
            let json: Value = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
            
            // The response is a single employee object with $id and potentially $ref
            // We need to extract the actual employee data, ignoring circular references
            let emp_obj = json.as_object()
                .ok_or_else(|| ApiError::ServerError("Expected object".to_string()))?;
            
            // Helper functions to extract fields
            let get_i64 = |key: &str| emp_obj.get(key).and_then(|v| v.as_i64());
            let get_str = |key: &str| emp_obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
            let get_bool = |key: &str| emp_obj.get(key).and_then(|v| v.as_bool()).unwrap_or(false);
            let get_date = |key: &str| {
                emp_obj.get(key).and_then(|v| v.as_str()).and_then(|s| {
                    chrono::NaiveDate::parse_from_str(&s[..10], "%Y-%m-%d").ok()
                })
            };
            let get_datetime = |key: &str| {
                emp_obj.get(key).and_then(|v| v.as_str()).and_then(|s| {
                    chrono::DateTime::parse_from_rfc3339(s).ok().map(|dt| dt.with_timezone(&chrono::Utc))
                })
            };
            
            // Extract job info - check if it's an object or just an ID
            let job_id = if let Some(job_val) = emp_obj.get("job") {
                if let Some(job_obj) = job_val.as_object() {
                    job_obj.get("jobId").and_then(|v| v.as_i64()).unwrap_or(0)
                } else {
                    0
                }
            } else {
                emp_obj.get("jobId").and_then(|v| v.as_i64()).unwrap_or(0)
            };
            
            // Extract department ID
            let department_id = if let Some(dept_val) = emp_obj.get("department") {
                if let Some(dept_obj) = dept_val.as_object() {
                    dept_obj.get("departmentId").and_then(|v| v.as_i64())
                } else {
                    None
                }
            } else {
                emp_obj.get("departmentId").and_then(|v| v.as_i64())
            };
            
            let employee = Employee {
                ref_id: None,
                emp_id: get_i64("empId").ok_or_else(|| ApiError::ServerError("Missing empId".to_string()))?,
                surname: get_str("surname").ok_or_else(|| ApiError::ServerError("Missing surname".to_string()))?,
                name: get_str("name").ok_or_else(|| ApiError::ServerError("Missing name".to_string()))?,
                patronym: get_str("patronym"),
                employed_since: get_date("employedSince").ok_or_else(|| ApiError::ServerError("Missing employedSince".to_string()))?,
                job_id,
                department_id,
                date_of_birth: get_date("dateOfBirth"),
                personal_phone: get_str("personalPhone"),
                work_phone: get_str("workPhone"),
                address: get_str("address"),
                email: get_str("email"),
                passport_series: get_str("passportSeries"),
                passport_number: get_str("passportNumber"),
                inn: get_str("inn"),
                snils: get_str("snils"),
                driver_license_number: get_str("driverLicenseNumber"),
                driver_license_category: get_str("driverLicenseCategory"),
                driver_license_issue_date: get_date("driverLicenseIssueDate"),
                driver_license_expiry_date: get_date("driverLicenseExpiryDate"),
                medical_certificate_number: get_str("medicalCertificateNumber"),
                medical_certificate_issue_date: get_date("medicalCertificateIssueDate"),
                medical_certificate_expiry_date: get_date("medicalCertificateExpiryDate"),
                last_medical_check_date: get_date("lastMedicalCheckDate"),
                next_medical_check_date: get_date("nextMedicalCheckDate"),
                has_passenger_transport_certification: get_bool("hasPassengerTransportCertification"),
                has_dangerous_goods_certification: get_bool("hasDangerousGoodsCertification"),
                is_active: get_bool("isActive"),
                termination_date: get_date("terminationDate"),
                termination_reason: get_str("terminationReason"),
                created_at: get_datetime("createdAt").unwrap_or_else(|| chrono::Utc::now()),
                updated_at: get_datetime("updatedAt"),
                job: None,
                department: None,
            };
            
            Ok(employee)
        } else {
            let error = response.text().await.unwrap_or_default();
            Err(ApiError::ServerError(format!("Failed to get employee: {}", error)))
        }
    }

    /// Create new employee
    pub async fn create_employee(&self, employee: &CreateEmployeeRequest) -> Result<Employee, ApiError> {
        let response = self.post("api/employees", employee).await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            // Handle empty response
            if text.trim().is_empty() {
                println!("⚠️ Create returned empty response - returning minimal employee object");
                // Return a minimal employee object - the real data will be fetched on reload
                return Err(ApiError::ServerError("Created but no employee data returned".to_string()));
            }
            
            // Parse with $ref handling
            let json: Value = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
            
            let employee: Employee = serde_json::from_value(json)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse employee: {}", e)))?;
            
            Ok(employee)
        } else {
            let error = response.text().await.unwrap_or_default();
            Err(ApiError::ServerError(format!("Failed to create employee: {}", error)))
        }
    }

    /// Update employee
    pub async fn update_employee(&self, id: i64, employee: &Employee) -> Result<Employee, ApiError> {
        let endpoint = format!("api/employees/{}", id);
        let response = self.put(&endpoint, employee).await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            // Handle empty response (204 No Content) - common for successful updates
            if text.trim().is_empty() {
                println!("✅ Update successful (empty response)");
                return Ok(employee.clone());
            }
            
            // Parse with $ref handling if we got a response body
            let json: Value = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
            
            let updated_employee: Employee = serde_json::from_value(json)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse employee: {}", e)))?;
            
            Ok(updated_employee)
        } else {
            let error = response.text().await.unwrap_or_default();
            Err(ApiError::ServerError(format!("Failed to update employee: {}", error)))
        }
    }

    /// Delete employee
    pub async fn delete_employee(&self, id: i64) -> Result<(), ApiError> {
        let endpoint = format!("api/employees/{}", id);
        let response = self.delete(&endpoint).await?;
        
        if response.status().is_success() {
            Ok(())
        } else {
            Err(ApiError::ServerError("Failed to delete employee".to_string()))
        }
    }

    /// Get employee trainings
    pub async fn get_employee_trainings(&self, employee_id: i64) -> Result<Vec<EmployeeTraining>, ApiError> {
        let endpoint = format!("api/employees/{}/trainings", employee_id);
        let response = self.get(&endpoint).await?;
        Self::handle_response(response).await
    }

    /// Get employee vacation requests
    pub async fn get_employee_vacation_requests(&self, employee_id: i64) -> Result<Vec<VacationRequest>, ApiError> {
        let endpoint = format!("api/employees/{}/vacation-requests", employee_id);
        let response = self.get(&endpoint).await?;
        Self::handle_response(response).await
    }
}
