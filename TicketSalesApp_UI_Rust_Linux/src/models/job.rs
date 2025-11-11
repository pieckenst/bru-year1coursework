use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Job {
    #[serde(rename = "jobId")]
    pub job_id: i32,
    
    #[serde(rename = "jobTitle")]
    pub job_title: String,
    
    #[serde(rename = "jobDescription")]
    pub job_description: Option<String>,
    
    #[serde(rename = "baseSalary")]
    pub base_salary: Option<f64>,
    
    #[serde(skip_serializing_if = "Option::is_none", rename = "$id")]
    pub ref_id: Option<String>,
}

impl Job {
    /// Create a new Job
    pub fn new(job_id: i32, job_title: String) -> Self {
        Job {
            job_id,
            job_title,
            job_description: None,
            base_salary: None,
            ref_id: None,
        }
    }
}
