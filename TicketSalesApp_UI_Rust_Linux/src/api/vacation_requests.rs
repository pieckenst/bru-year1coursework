use crate::models::{VacationRequest, CreateVacationRequest};
use super::{ApiClient, ApiError};
use serde_json::Value;

impl ApiClient {
    /// Get all vacation requests for an employee
    pub async fn get_vacation_requests(&self, employee_id: i64) -> Result<Vec<VacationRequest>, ApiError> {
        let url = format!("{}/employees/{}/vacation-requests", self.base_url, employee_id);
        
        let response = self.client
            .get(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to get vacation requests: {}", response.status())));
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
    
    /// Create a new vacation request
    pub async fn create_vacation_request(&self, request: &CreateVacationRequest) -> Result<VacationRequest, ApiError> {
        let url = format!("{}/employees/{}/vacation-requests", self.base_url, request.employee_id);
        
        let response = self.client
            .post(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .json(request)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to create vacation request: {}", response.status())));
        }
        
        let text = response.text().await
            .map_err(|e| ApiError::ParseError(e.to_string()))?;
        
        if text.is_empty() {
            return Err(ApiError::ParseError("Empty response body".to_string()));
        }
        
        serde_json::from_str(&text)
            .map_err(|e| ApiError::ParseError(format!("Parse error: {} - Response: {}", e, text)))
    }
    
    /// Update a vacation request
    pub async fn update_vacation_request(&self, employee_id: i64, request_id: i64, request: &VacationRequest) -> Result<VacationRequest, ApiError> {
        let url = format!("{}/employees/{}/vacation-requests/{}", self.base_url, employee_id, request_id);
        
        let response = self.client
            .put(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .json(request)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to update vacation request: {}", response.status())));
        }
        
        let text = response.text().await
            .map_err(|e| ApiError::ParseError(e.to_string()))?;
        
        if text.is_empty() {
            return Ok(request.clone());
        }
        
        serde_json::from_str(&text)
            .map_err(|e| ApiError::ParseError(format!("Parse error: {} - Response: {}", e, text)))
    }
    
    /// Delete a vacation request
    pub async fn delete_vacation_request(&self, employee_id: i64, request_id: i64) -> Result<(), ApiError> {
        let url = format!("{}/employees/{}/vacation-requests/{}", self.base_url, employee_id, request_id);
        
        let response = self.client
            .delete(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to delete vacation request: {}", response.status())));
        }
        
        Ok(())
    }
    
    /// Approve a vacation request (admin action)
    pub async fn approve_vacation_request(&self, employee_id: i64, request_id: i64, notes: Option<String>) -> Result<VacationRequest, ApiError> {
        let url = format!("{}/employees/{}/vacation-requests/{}/approve", self.base_url, employee_id, request_id);
        
        let body = serde_json::json!({ "notes": notes });
        
        let response = self.client
            .post(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .json(&body)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to approve vacation request: {}", response.status())));
        }
        
        let text = response.text().await
            .map_err(|e| ApiError::ParseError(e.to_string()))?;
        
        if text.is_empty() {
            return Err(ApiError::ParseError("Empty response body".to_string()));
        }
        
        serde_json::from_str(&text)
            .map_err(|e| ApiError::ParseError(format!("Parse error: {} - Response: {}", e, text)))
    }
    
    /// Reject a vacation request (admin action)
    pub async fn reject_vacation_request(&self, employee_id: i64, request_id: i64, notes: Option<String>) -> Result<VacationRequest, ApiError> {
        let url = format!("{}/employees/{}/vacation-requests/{}/reject", self.base_url, employee_id, request_id);
        
        let body = serde_json::json!({ "notes": notes });
        
        let response = self.client
            .post(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .json(&body)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to reject vacation request: {}", response.status())));
        }
        
        let text = response.text().await
            .map_err(|e| ApiError::ParseError(e.to_string()))?;
        
        if text.is_empty() {
            return Err(ApiError::ParseError("Empty response body".to_string()));
        }
        
        serde_json::from_str(&text)
            .map_err(|e| ApiError::ParseError(format!("Parse error: {} - Response: {}", e, text)))
    }
}
