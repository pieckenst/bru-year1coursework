use crate::models::{EmployeeDocument, CreateEmployeeDocument};
use super::{ApiClient, ApiError};
use serde_json::Value;

impl ApiClient {
    /// Get all documents for an employee
    pub async fn get_employee_documents(&self, employee_id: i64) -> Result<Vec<EmployeeDocument>, ApiError> {
        let url = format!("{}/api/Employees/{}/documents", self.base_url, employee_id);
        println!("🌐 Calling URL: {}", url);
        
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
        
        println!("📦 Documents JSON response: {}", &text[..text.len().min(500)]);
        
        // Parse as raw JSON first
        let json: Value = serde_json::from_str(&text)
            .map_err(|e| ApiError::ParseError(format!("JSON parse error: {}", e)))?;
        
        // Extract $values array
        let docs_array = json.get("$values")
            .and_then(|v| v.as_array())
            .ok_or_else(|| ApiError::ParseError("Missing $values in response".to_string()))?;
        
        // Manually parse each document to avoid circular refs
        let mut documents = Vec::new();
        for doc_value in docs_array {
            let doc_obj = match doc_value.as_object() {
                Some(obj) => obj,
                None => continue,
            };
            
            // Helper functions
            let get_i64 = |key: &str| doc_obj.get(key).and_then(|v| v.as_i64());
            let get_str = |key: &str| doc_obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
            let get_date = |key: &str| {
                doc_obj.get(key)
                    .and_then(|v| v.as_str())
                    .and_then(|s| chrono::NaiveDate::parse_from_str(&s[..10], "%Y-%m-%d").ok())
            };
            let get_datetime = |key: &str| {
                doc_obj.get(key)
                    .and_then(|v| v.as_str())
                    .and_then(|s| chrono::NaiveDateTime::parse_from_str(s, "%Y-%m-%dT%H:%M:%S%.f").ok())
            };
            
            let document = EmployeeDocument {
                document_id: get_i64("documentId").unwrap_or(0),
                document_type: get_str("documentType").unwrap_or_default(),
                document_number: get_str("documentNumber").unwrap_or_default(),
                issue_date: get_date("issueDate").unwrap_or_default(),
                expiry_date: get_date("expiryDate"),
                issued_by: get_str("issuedBy"),
                file_path: get_str("filePath"),
                notes: get_str("notes"),
                employee_id: get_i64("employeeId").unwrap_or(0),
                created_at: get_datetime("createdAt"),
                updated_at: get_datetime("updatedAt"),
            };
            
            documents.push(document);
        }
        
        println!("✅ Parsed {} documents", documents.len());
        Ok(documents)
    }
    
    /// Create a new document for an employee
    pub async fn create_employee_document(&self, document: &CreateEmployeeDocument) -> Result<EmployeeDocument, ApiError> {
        let url = format!("{}/Employees/{}/documents", self.base_url, document.employee_id);
        
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
        let url = format!("{}/Employees/{}/documents/{}", self.base_url, employee_id, document_id);
        
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
        let url = format!("{}/Employees/{}/documents/{}", self.base_url, employee_id, document_id);
        
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
