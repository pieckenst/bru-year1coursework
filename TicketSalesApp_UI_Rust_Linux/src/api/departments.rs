use super::{ApiClient, ApiError};
use crate::models::Department;

impl ApiClient {
    /// Get all departments
    pub async fn get_departments(&self) -> Result<Vec<Department>, ApiError> {
        let response = self.get("api/departments").await?;
        Self::handle_response(response).await
    }

    /// Get department by ID
    pub async fn get_department(&self, id: i32) -> Result<Department, ApiError> {
        let endpoint = format!("api/departments/{}", id);
        let response = self.get(&endpoint).await?;
        Self::handle_response(response).await
    }

    /// Create new department
    pub async fn create_department(&self, department: &Department) -> Result<Department, ApiError> {
        let response = self.post("api/departments", department).await?;
        Self::handle_response(response).await
    }

    /// Update department
    pub async fn update_department(&self, id: i32, department: &Department) -> Result<(), ApiError> {
        let endpoint = format!("api/departments/{}", id);
        let response = self.put(&endpoint, department).await?;
        
        if response.status().is_success() {
            Ok(())
        } else {
            Err(ApiError::ServerError("Failed to update department".to_string()))
        }
    }

    /// Delete department
    pub async fn delete_department(&self, id: i32) -> Result<(), ApiError> {
        let endpoint = format!("api/departments/{}", id);
        let response = self.delete(&endpoint).await?;
        
        if response.status().is_success() {
            Ok(())
        } else {
            Err(ApiError::ServerError("Failed to delete department".to_string()))
        }
    }
}
