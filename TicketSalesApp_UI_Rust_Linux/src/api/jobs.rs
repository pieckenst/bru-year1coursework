use crate::models::Job;
use crate::api::{ApiClient, ApiError};

impl ApiClient {
    /// Get all jobs
    pub async fn get_jobs(&self) -> Result<Vec<Job>, ApiError> {
        let url = format!("{}/api/Jobs", self.base_url);
        let mut request = self.client.get(&url);
        
        if let Some(token) = &self.token {
            request = request.bearer_auth(token);
        }
        
        let response = request.send().await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(e.to_string()))?;
            let jobs: Vec<Job> = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse jobs: {}", e)))?;
            Ok(jobs)
        } else {
            let error = response.text().await.unwrap_or_default();
            Err(ApiError::ServerError(format!("Failed to get jobs: {}", error)))
        }
    }
    
    /// Get a specific job by ID
    pub async fn get_job(&self, id: i32) -> Result<Job, ApiError> {
        let url = format!("{}/api/Jobs/{}", self.base_url, id);
        let mut request = self.client.get(&url);
        
        if let Some(token) = &self.token {
            request = request.bearer_auth(token);
        }
        
        let response = request.send().await
            .map_err(|e| ApiError::NetworkError(e.to_string()))?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(e.to_string()))?;
            let job: Job = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse job: {}", e)))?;
            Ok(job)
        } else {
            let error = response.text().await.unwrap_or_default();
            Err(ApiError::ServerError(format!("Failed to get job: {}", error)))
        }
    }
}
