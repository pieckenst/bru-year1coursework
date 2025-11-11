use crate::models::{EmployeeTraining, CreateEmployeeTraining};
use super::{ApiClient, ApiError};
use serde_json::Value;

impl ApiClient {
    /// Get all training records for an employee
    pub async fn get_employee_training(&self, employee_id: i64) -> Result<Vec<EmployeeTraining>, ApiError> {
        let url = format!("{}/employees/{}/training", self.base_url, employee_id);
        
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
        
        // Handle ReferenceHandler.Preserve format
        let json: Value = serde_json::from_str(&text)
            .map_err(|e| ApiError::ParseError(e.to_string()))?;
        
        if let Some(values_array) = json.get("$values") {
            serde_json::from_value(values_array.clone())
                .map_err(|e| ApiError::ParseError(e.to_string()))
        } else if json.is_array() {
            serde_json::from_value(json)
                .map_err(|e| ApiError::ParseError(e.to_string()))
        } else {
            Err(ApiError::ParseError("Unexpected JSON format".to_string()))
        }
    }
    
    /// Create a new training record
    pub async fn create_employee_training(&self, training: &CreateEmployeeTraining) -> Result<EmployeeTraining, ApiError> {
        let url = format!("{}/employees/{}/training", self.base_url, training.employee_id);
        
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
        let url = format!("{}/employees/{}/training/{}", self.base_url, employee_id, training_id);
        
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
        let url = format!("{}/employees/{}/training/{}", self.base_url, employee_id, training_id);
        
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
