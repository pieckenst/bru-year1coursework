use super::{ApiClient, ApiError};
use crate::models::Department;
use serde_json::Value;
use std::collections::HashMap;

impl ApiClient {
    /// Get all departments with ReferenceHandler.Preserve support
    pub async fn get_departments(&self) -> Result<Vec<Department>, ApiError> {
        let response = self.get("api/departments").await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            println!("[DEBUG Departments] Raw response (first 500 chars): {}", 
                     if text.len() > 500 { &text[..500] } else { &text });
            
            let json: Value = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
            
            // Handle ReferenceHandler.Preserve format
            let root = json.as_object()
                .ok_or_else(|| ApiError::ServerError("Expected object".to_string()))?;
            
            // Get $values array
            let values_array = root.get("$values")
                .and_then(|v| v.as_array())
                .ok_or_else(|| ApiError::ServerError("Missing $values array".to_string()))?;
            
            println!("🔍 Found {} items in $values array", values_array.len());
            
            // First pass: collect all objects with $id
            let mut id_map: HashMap<String, &Value> = HashMap::new();
            for item in values_array {
                if let Some(obj) = item.as_object() {
                    if let Some(id) = obj.get("$id").and_then(|v| v.as_str()) {
                        id_map.insert(id.to_string(), item);
                    }
                }
            }
            
            // Second pass: parse departments
            let mut departments = Vec::new();
            for item in values_array {
                if let Some(obj) = item.as_object() {
                    // Skip $ref pointers
                    if obj.contains_key("$ref") {
                        continue;
                    }
                    
                    // Parse department fields
                    let get_i64 = |key: &str| obj.get(key).and_then(|v| v.as_i64());
                    let get_str = |key: &str| obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
                    let get_bool = |key: &str| obj.get(key).and_then(|v| v.as_bool()).unwrap_or(true);
                    
                    if let Some(department_id) = get_i64("departmentId") {
                        let department = Department {
                            ref_id: get_str("$id"),
                            ref_pointer: None,
                            department_id,
                            department_name: get_str("departmentName").unwrap_or_default(),
                            department_code: get_str("departmentCode"),
                            description: get_str("description"),
                            parent_department_id: get_i64("parentDepartmentId"),
                            is_active: get_bool("isActive"),
                            extra: HashMap::new(),
                        };
                        
                        println!("  ✓ Parsed department: {} (ID: {})", department.department_name, department.department_id);
                        departments.push(department);
                    }
                }
            }
            
            departments.sort_by_key(|d| d.department_id);
            println!("✅ Successfully parsed {} departments\n", departments.len());
            Ok(departments)
        } else {
            let error = response.text().await.unwrap_or_default();
            Err(ApiError::ServerError(format!("Failed to get departments: {}", error)))
        }
    }

    /// Get department by ID with ReferenceHandler.Preserve support
    pub async fn get_department(&self, id: i32) -> Result<Department, ApiError> {
        let endpoint = format!("api/departments/{}", id);
        let response = self.get(&endpoint).await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            let json: Value = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
            
            let obj = json.as_object()
                .ok_or_else(|| ApiError::ServerError("Expected object".to_string()))?;
            
            let get_i64 = |key: &str| obj.get(key).and_then(|v| v.as_i64());
            let get_str = |key: &str| obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
            let get_bool = |key: &str| obj.get(key).and_then(|v| v.as_bool()).unwrap_or(true);
            
            let department = Department {
                ref_id: get_str("$id"),
                ref_pointer: None,
                department_id: get_i64("departmentId").ok_or_else(|| ApiError::ServerError("Missing departmentId".to_string()))?,
                department_name: get_str("departmentName").ok_or_else(|| ApiError::ServerError("Missing departmentName".to_string()))?,
                department_code: get_str("departmentCode"),
                description: get_str("description"),
                parent_department_id: get_i64("parentDepartmentId"),
                is_active: get_bool("isActive"),
                extra: HashMap::new(),
            };
            
            Ok(department)
        } else {
            let error = response.text().await.unwrap_or_default();
            Err(ApiError::ServerError(format!("Failed to get department: {}", error)))
        }
    }

    /// Create new department with ReferenceHandler.Preserve support
    pub async fn create_department(&self, department: &Department) -> Result<Department, ApiError> {
        let response = self.post("api/departments", department).await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            let json: Value = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
            
            let department: Department = serde_json::from_value(json)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse department: {}", e)))?;
            
            Ok(department)
        } else {
            let error = response.text().await.unwrap_or_default();
            Err(ApiError::ServerError(format!("Failed to create department: {}", error)))
        }
    }

    /// Update department with ReferenceHandler.Preserve support
    pub async fn update_department(&self, id: i32, department: &Department) -> Result<Department, ApiError> {
        let endpoint = format!("api/departments/{}", id);
        let response = self.put(&endpoint, department).await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            // Handle empty response (204 No Content)
            if text.trim().is_empty() {
                println!("✅ Department update successful (empty response)");
                return Ok(department.clone());
            }
            
            let json: Value = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
            
            let department: Department = serde_json::from_value(json)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse department: {}", e)))?;
            
            Ok(department)
        } else {
            let error = response.text().await.unwrap_or_default();
            Err(ApiError::ServerError(format!("Failed to update department: {}", error)))
        }
    }

    /// Delete department
    pub async fn delete_department(&self, id: i32) -> Result<(), ApiError> {
        let endpoint = format!("api/departments/{}", id);
        let response = self.delete(&endpoint).await?;
        
        if response.status().is_success() {
            Ok(())
        } else {
            let error = response.text().await.unwrap_or_default();
            Err(ApiError::ServerError(format!("Failed to delete department: {}", error)))
        }
    }
}
