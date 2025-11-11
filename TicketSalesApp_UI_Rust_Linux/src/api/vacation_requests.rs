use crate::models::{VacationRequest, CreateVacationRequest};
use super::{ApiClient, ApiError};
use serde_json::Value;

impl ApiClient {
    /// Get all vacation requests for an employee
    pub async fn get_vacation_requests(&self, employee_id: i64) -> Result<Vec<VacationRequest>, ApiError> {
        let url = format!("{}/api/Employees/{}/vacation-requests", self.base_url, employee_id);
        println!("🌐 Calling URL: {}", url);
        
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
        
        println!("📦 Vacations JSON response: {}", &text[..text.len().min(500)]);
        
        let json: Value = serde_json::from_str(&text)
            .map_err(|e| ApiError::ParseError(format!("JSON parse error: {}", e)))?;
        
        // Extract $values array
        let vacations_array = json.get("$values")
            .and_then(|v| v.as_array())
            .ok_or_else(|| ApiError::ParseError("Missing $values in response".to_string()))?;
        
        // Manually parse each vacation request
        let mut vacations = Vec::new();
        for vac_value in vacations_array {
            let vac_obj = match vac_value.as_object() {
                Some(obj) => obj,
                None => continue,
            };
            
            let get_i64 = |key: &str| vac_obj.get(key).and_then(|v| v.as_i64());
            let get_i32 = |key: &str| vac_obj.get(key).and_then(|v| v.as_i64()).map(|v| v as i32);
            let get_str = |key: &str| vac_obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
            let get_date = |key: &str| {
                vac_obj.get(key)
                    .and_then(|v| v.as_str())
                    .and_then(|s| chrono::NaiveDate::parse_from_str(&s[..10], "%Y-%m-%d").ok())
            };
            let get_datetime = |key: &str| {
                vac_obj.get(key)
                    .and_then(|v| v.as_str())
                    .and_then(|s| chrono::NaiveDateTime::parse_from_str(s, "%Y-%m-%dT%H:%M:%S%.f").ok())
            };
            
            let vacation = VacationRequest {
                request_id: get_i64("requestId").unwrap_or(0),
                employee_id: get_i64("employeeId").unwrap_or(0),
                start_date: get_date("startDate").unwrap_or_default(),
                end_date: get_date("endDate").unwrap_or_default(),
                vacation_type: get_str("vacationType").unwrap_or_default(),
                reason: get_str("reason"),
                status: get_str("status").unwrap_or_default(),
                approved_by_user_id: get_i64("approvedByUserId"),
                approval_date: get_datetime("approvalDate"),
                approval_notes: get_str("approvalNotes"),
                days_requested: get_i32("daysRequested").unwrap_or(0),
                created_at: get_datetime("createdAt"),
                updated_at: get_datetime("updatedAt"),
            };
            
            vacations.push(vacation);
        }
        
        println!("✅ Parsed {} vacation requests", vacations.len());
        Ok(vacations)
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
