use crate::models::{EmergencyContact, CreateEmergencyContact};
use super::{ApiClient, ApiError};
use serde_json::Value;

impl ApiClient {
    /// Get all emergency contacts for an employee
    pub async fn get_emergency_contacts(&self, employee_id: i64) -> Result<Vec<EmergencyContact>, ApiError> {
        let url = format!("{}/api/Employees/{}/emergency-contacts", self.base_url, employee_id);
        println!("🌐 Calling URL: {}", url);
        
        let response = self.client
            .get(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to get emergency contacts: {}", response.status())));
        }
        
        let text = response.text().await
            .map_err(|e| ApiError::ParseError(e.to_string()))?;
        
        println!("📦 Contacts JSON response: {}", &text[..text.len().min(500)]);
        
        // Handle ReferenceHandler.Preserve format
        let json: Value = serde_json::from_str(&text)
            .map_err(|e| ApiError::ParseError(format!("JSON parse error: {} - Response: {}", e, &text[..text.len().min(200)])))?;
        
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
    
    /// Create a new emergency contact
    pub async fn create_emergency_contact(&self, contact: &CreateEmergencyContact) -> Result<EmergencyContact, ApiError> {
        let url = format!("{}/employees/{}/emergency-contacts", self.base_url, contact.employee_id);
        
        let response = self.client
            .post(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .json(contact)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to create emergency contact: {}", response.status())));
        }
        
        let text = response.text().await
            .map_err(|e| ApiError::ParseError(e.to_string()))?;
        
        if text.is_empty() {
            return Err(ApiError::ParseError("Empty response body".to_string()));
        }
        
        serde_json::from_str(&text)
            .map_err(|e| ApiError::ParseError(format!("Parse error: {} - Response: {}", e, text)))
    }
    
    /// Update an emergency contact
    pub async fn update_emergency_contact(&self, employee_id: i64, contact_id: i64, contact: &EmergencyContact) -> Result<EmergencyContact, ApiError> {
        let url = format!("{}/employees/{}/emergency-contacts/{}", self.base_url, employee_id, contact_id);
        
        let response = self.client
            .put(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .json(contact)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to update emergency contact: {}", response.status())));
        }
        
        let text = response.text().await
            .map_err(|e| ApiError::ParseError(e.to_string()))?;
        
        if text.is_empty() {
            return Ok(contact.clone());
        }
        
        serde_json::from_str(&text)
            .map_err(|e| ApiError::ParseError(format!("Parse error: {} - Response: {}", e, text)))
    }
    
    /// Delete an emergency contact
    pub async fn delete_emergency_contact(&self, employee_id: i64, contact_id: i64) -> Result<(), ApiError> {
        let url = format!("{}/employees/{}/emergency-contacts/{}", self.base_url, employee_id, contact_id);
        
        let response = self.client
            .delete(&url)
            .bearer_auth(self.token.as_ref().ok_or(ApiError::Unauthorized)?)
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
            
        if !response.status().is_success() {
            return Err(ApiError::RequestFailed(format!("Failed to delete emergency contact: {}", response.status())));
        }
        
        Ok(())
    }
}
