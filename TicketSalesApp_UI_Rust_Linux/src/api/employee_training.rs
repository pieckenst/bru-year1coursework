use crate::models::{EmployeeTraining, CreateEmployeeTraining};
use super::{ApiClient, ApiError};
use serde_json::Value;

impl ApiClient {
    /// Get all training records for an employee
    pub async fn get_employee_training(&self, employee_id: i64) -> Result<Vec<EmployeeTraining>, ApiError> {
        let url = format!("{}/api/Employees/{}/trainings", self.base_url, employee_id);
        println!("🌐 Calling URL: {}", url);
        
        let response = self.client
            .get(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to get training: {}", response.status())));
        }
        
        let text = response.text().await
            .map_err(|e| ApiError::ParseError(e.to_string()))?;
        
        // Parse as raw JSON first
        let json: Value = serde_json::from_str(&text)
            .map_err(|e| ApiError::ParseError(format!("JSON parse error: {}", e)))?;
        
        // Extract $values array
        let training_array = json.get("$values")
            .and_then(|v| v.as_array())
            .ok_or_else(|| ApiError::ParseError("Missing $values in response".to_string()))?;
        
        // Manually parse each training record
        let mut trainings = Vec::new();
        for train_value in training_array {
            let train_obj = match train_value.as_object() {
                Some(obj) => obj,
                None => continue,
            };
            
            let get_i64 = |key: &str| train_obj.get(key).and_then(|v| v.as_i64());
            let get_str = |key: &str| train_obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
            let get_bool = |key: &str| train_obj.get(key).and_then(|v| v.as_bool());
            let get_date = |key: &str| {
                train_obj.get(key)
                    .and_then(|v| v.as_str())
                    .and_then(|s| chrono::NaiveDate::parse_from_str(&s[..10], "%Y-%m-%d").ok())
            };
            let get_datetime = |key: &str| {
                train_obj.get(key)
                    .and_then(|v| v.as_str())
                    .and_then(|s| chrono::NaiveDateTime::parse_from_str(s, "%Y-%m-%dT%H:%M:%S%.f").ok())
            };
            
            let training = EmployeeTraining {
                training_id: get_i64("trainingId").unwrap_or(0),
                training_name: get_str("trainingName").unwrap_or_default(),
                description: get_str("description"),
                completion_date: get_date("completionDate").unwrap_or_default(),
                expiry_date: get_date("expiryDate"),
                certificate_number: get_str("certificateNumber"),
                issuing_organization: get_str("issuingOrganization"),
                is_mandatory: get_bool("isMandatory").unwrap_or(false),
                file_path: get_str("filePath"),
                notes: get_str("notes"),
                employee_id: get_i64("employeeId").unwrap_or(0),
                created_at: get_datetime("createdAt"),
                updated_at: get_datetime("updatedAt"),
            };
            
            trainings.push(training);
        }
        
        println!("✅ Parsed {} training records", trainings.len());
        Ok(trainings)
    }
    
    /// Create a new training record
    pub async fn create_employee_training(&self, training: &CreateEmployeeTraining) -> Result<EmployeeTraining, ApiError> {
        let url = format!("{}/Employees/{}/trainings", self.base_url, training.employee_id);
        
        let response = self.client
            .post(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .json(training)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to create training: {}", response.status())));
        }
        
        let text = response.text().await
            .map_err(|e| ApiError::ParseError(e.to_string()))?;
        
        if text.is_empty() {
            return Err(ApiError::ParseError("Empty response body".to_string()));
        }
        
        serde_json::from_str(&text)
            .map_err(|e| ApiError::ParseError(format!("Parse error: {} - Response: {}", e, text)))
    }
    
    /// Update a training record
    pub async fn update_employee_training(&self, employee_id: i64, training_id: i64, training: &EmployeeTraining) -> Result<EmployeeTraining, ApiError> {
        let url = format!("{}/Employees/{}/trainings/{}", self.base_url, employee_id, training_id);
        
        let response = self.client
            .put(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .json(training)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to update training: {}", response.status())));
        }
        
        let text = response.text().await
            .map_err(|e| ApiError::ParseError(e.to_string()))?;
        
        if text.is_empty() {
            return Ok(training.clone());
        }
        
        serde_json::from_str(&text)
            .map_err(|e| ApiError::ParseError(format!("Parse error: {} - Response: {}", e, text)))
    }
    
    /// Delete a training record
    pub async fn delete_employee_training(&self, employee_id: i64, training_id: i64) -> Result<(), ApiError> {
        let url = format!("{}/Employees/{}/trainings/{}", self.base_url, employee_id, training_id);
        
        let response = self.client
            .delete(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to delete training: {}", response.status())));
        }
        
        Ok(())
    }
}
