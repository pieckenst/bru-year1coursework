use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Job {
    #[serde(rename = "jobId")]
    pub job_id: i32,
    
    #[serde(rename = "jobTitle")]
    pub job_title: String,
    
    #[serde(rename = "internship", skip_serializing_if = "Option::is_none")]
    pub internship: Option<String>,
    
    #[serde(skip_serializing_if = "Option::is_none", rename = "$id")]
    pub ref_id: Option<String>,
}

impl Job {
    /// Create a new Job
    pub fn new(job_id: i32, job_title: String) -> Self {
        Job {
            job_id,
            job_title,
            internship: None,
            ref_id: None,
        }
    }
}
