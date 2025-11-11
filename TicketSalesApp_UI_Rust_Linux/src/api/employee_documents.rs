use crate::models::{EmployeeDocument, CreateEmployeeDocument};
use super::{ApiClient, ApiError};
use serde_json::Value;

impl ApiClient {
    /// Get all documents for an employee
    pub async fn get_employee_documents(&self, employee_id: i64) -> Result<Vec<EmployeeDocument>, ApiError> {
        let url = format!("{}/employees/{}/documents", self.base_url, employee_id);
        
        let response = self.client
            .get(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to get documents: {}", response.status())));
        }
        
        let text = response.text().await
            .map_err(|e| ApiError::ParseError(e.to_string()))?;
        
        // Handle ReferenceHandler.Preserve format
        let json: Value = serde_json::from_str(&text)
            .map_err(|e| ApiError::ParseError(e.to_string()))?;
        
        // Check if it's the $values wrapper format
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
    
    /// Create a new document for an employee
    pub async fn create_employee_document(&self, document: &CreateEmployeeDocument) -> Result<EmployeeDocument, ApiError> {
        let url = format!("{}/employees/{}/documents", self.base_url, document.employee_id);
        
        let response = self.client
            .post(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .json(document)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to create document: {}", response.status())));
        }
        
        let text = response.text().await
            .map_err(|e| ApiError::ParseError(e.to_string()))?;
        
        if text.is_empty() {
            return Err(ApiError::ParseError("Empty response body".to_string()));
        }
        
        serde_json::from_str(&text)
            .map_err(|e| ApiError::ParseError(format!("Parse error: {} - Response: {}", e, text)))
    }
    
    /// Update an employee document
    pub async fn update_employee_document(&self, employee_id: i64, document_id: i64, document: &EmployeeDocument) -> Result<EmployeeDocument, ApiError> {
        let url = format!("{}/employees/{}/documents/{}", self.base_url, employee_id, document_id);
        
        let response = self.client
            .put(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .json(document)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to update document: {}", response.status())));
        }
        
        let text = response.text().await
            .map_err(|e| ApiError::ParseError(e.to_string()))?;
        
        // Handle empty response (204 No Content)
        if text.is_empty() {
            return Ok(document.clone());
        }
        
        serde_json::from_str(&text)
            .map_err(|e| ApiError::ParseError(format!("Parse error: {} - Response: {}", e, text)))
    }
    
    /// Delete an employee document
    pub async fn delete_employee_document(&self, employee_id: i64, document_id: i64) -> Result<(), ApiError> {
        let url = format!("{}/employees/{}/documents/{}", self.base_url, employee_id, document_id);
        
        let response = self.client
            .delete(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to delete document: {}", response.status())));
        }
        
        Ok(())
    }
}
