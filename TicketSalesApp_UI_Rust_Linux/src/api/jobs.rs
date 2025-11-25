use crate::models::Job;
use crate::api::{ApiClient, ApiError};
use serde_json::Value;
use std::collections::HashMap;

impl ApiClient {
    /// Get all jobs with ReferenceHandler.Preserve support
    pub async fn get_jobs(&self) -> Result<Vec<Job>, ApiError> {
        let response = self.get("api/Jobs").await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            println!("[DEBUG Jobs] Raw response (first 500 chars): {}", 
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
            
            // Second pass: parse jobs
            let mut jobs = Vec::new();
            for item in values_array {
                if let Some(obj) = item.as_object() {
                    // Skip $ref pointers
                    if obj.contains_key("$ref") {
                        continue;
                    }
                    
                    // Parse job fields
                    let get_i32 = |key: &str| obj.get(key).and_then(|v| v.as_i64()).map(|n| n as i32);
                    let get_str = |key: &str| obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
                    
                    if let Some(job_id) = get_i32("jobId") {
                        let job = Job {
                            job_id,
                            job_title: get_str("jobTitle").unwrap_or_default(),
                            internship: get_str("internship"),
                            ref_id: get_str("$id"),
                        };
                        
                        println!("  ✓ Parsed job: {} (ID: {})", job.job_title, job.job_id);
                        jobs.push(job);
                    }
                }
            }
            
            jobs.sort_by_key(|j| j.job_id);
            println!("✅ Successfully parsed {} jobs\n", jobs.len());
            Ok(jobs)
        } else {
            let error = response.text().await.unwrap_or_default();
            Err(ApiError::ServerError(format!("Failed to get jobs: {}", error)))
        }
    }
    
    /// Get a specific job by ID with ReferenceHandler.Preserve support
    pub async fn get_job(&self, id: i32) -> Result<Job, ApiError> {
        let endpoint = format!("api/Jobs/{}", id);
        let response = self.get(&endpoint).await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            let json: Value = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
            
            let obj = json.as_object()
                .ok_or_else(|| ApiError::ServerError("Expected object".to_string()))?;
            
            let get_i32 = |key: &str| obj.get(key).and_then(|v| v.as_i64()).map(|n| n as i32);
            let get_str = |key: &str| obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
            
            let job = Job {
                job_id: get_i32("jobId").ok_or_else(|| ApiError::ServerError("Missing jobId".to_string()))?,
                job_title: get_str("jobTitle").ok_or_else(|| ApiError::ServerError("Missing jobTitle".to_string()))?,
                internship: get_str("internship"),
                ref_id: get_str("$id"),
            };
            
            Ok(job)
        } else {
            let error = response.text().await.unwrap_or_default();
            Err(ApiError::ServerError(format!("Failed to get job: {}", error)))
        }
    }
    
    /// Create a new job
    pub async fn create_job(&self, job_title: &str, internship: Option<&str>) -> Result<Job, ApiError> {
        use serde_json::json;
        
        let mut body = json!({
            "jobTitle": job_title,
        });
        
        if let Some(intern) = internship {
            if !intern.is_empty() {
                body["internship"] = json!(intern);
            }
        }
        
        let response = self.post("api/Jobs", &body).await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            let json: Value = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
            
            let obj = json.as_object()
                .ok_or_else(|| ApiError::ServerError("Expected object".to_string()))?;
            
            let get_i32 = |key: &str| obj.get(key).and_then(|v| v.as_i64()).map(|n| n as i32);
            let get_str = |key: &str| obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
            
            let job = Job {
                job_id: get_i32("jobId").ok_or_else(|| ApiError::ServerError("Missing jobId".to_string()))?,
                job_title: get_str("jobTitle").ok_or_else(|| ApiError::ServerError("Missing jobTitle".to_string()))?,
                internship: get_str("internship"),
                ref_id: get_str("$id"),
            };
            
            Ok(job)
        } else {
            let status = response.status();
            let error = response.text().await.unwrap_or_default();
            
            if status.as_u16() == 403 {
                Err(ApiError::Forbidden)
            } else {
                Err(ApiError::ServerError(format!("Failed to create job: {}", error)))
            }
        }
    }
    
    /// Update an existing job
    pub async fn update_job(&self, id: i32, job_title: &str, internship: Option<&str>) -> Result<(), ApiError> {
        use serde_json::json;
        
        let mut body = json!({
            "jobId": id,
            "jobTitle": job_title,
        });
        
        if let Some(intern) = internship {
            if !intern.is_empty() {
                body["internship"] = json!(intern);
            }
        }
        
        let endpoint = format!("api/Jobs/{}", id);
        let response = self.put(&endpoint, &body).await?;
        
        if response.status().is_success() {
            Ok(())
        } else {
            let status = response.status();
            let error = response.text().await.unwrap_or_default();
            
            if status.as_u16() == 403 {
                Err(ApiError::Forbidden)
            } else {
                Err(ApiError::ServerError(format!("Failed to update job: {}", error)))
            }
        }
    }
    
    /// Delete a job
    pub async fn delete_job(&self, id: i32) -> Result<(), ApiError> {
        let endpoint = format!("api/Jobs/{}", id);
        let response = self.delete(&endpoint).await?;
        
        if response.status().is_success() {
            Ok(())
        } else {
            let status = response.status();
            let error = response.text().await.unwrap_or_default();
            
            if status.as_u16() == 403 {
                Err(ApiError::Forbidden)
            } else {
                Err(ApiError::ServerError(format!("Failed to delete job: {}", error)))
            }
        }
    }
    
    /// Search jobs by title or description
    pub async fn search_jobs(&self, job_title: Option<&str>, internship: Option<&str>) -> Result<Vec<Job>, ApiError> {
        let mut endpoint = "api/Jobs?".to_string();
        let mut params = Vec::new();
        
        if let Some(title) = job_title {
            params.push(format!("jobTitle={}", urlencoding::encode(title)));
        }
        
        if let Some(intern) = internship {
            params.push(format!("internship={}", urlencoding::encode(intern)));
        }
        
        endpoint.push_str(&params.join("&"));
        
        let response = self.get(&endpoint).await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            let json: Value = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
            
            // Handle ReferenceHandler.Preserve format
            let root = json.as_object()
                .ok_or_else(|| ApiError::ServerError("Expected object".to_string()))?;
            
            let values_array = root.get("$values")
                .and_then(|v| v.as_array())
                .ok_or_else(|| ApiError::ServerError("Missing $values array".to_string()))?;
            
            let mut jobs = Vec::new();
            for item in values_array {
                if let Some(obj) = item.as_object() {
                    if obj.contains_key("$ref") {
                        continue;
                    }
                    
                    let get_i32 = |key: &str| obj.get(key).and_then(|v| v.as_i64()).map(|n| n as i32);
                    let get_str = |key: &str| obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
                    
                    if let Some(job_id) = get_i32("jobId") {
                        let job = Job {
                            job_id,
                            job_title: get_str("jobTitle").unwrap_or_default(),
                            internship: get_str("internship"),
                            ref_id: get_str("$id"),
                        };
                        jobs.push(job);
                    }
                }
            }
            
            jobs.sort_by_key(|j| j.job_id);
            Ok(jobs)
        } else {
            let status = response.status();
            let error = response.text().await.unwrap_or_default();
            
            if status.as_u16() == 403 {
                Err(ApiError::Forbidden)
            } else {
                Err(ApiError::ServerError(format!("Failed to search jobs: {}", error)))
            }
        }
    }
}