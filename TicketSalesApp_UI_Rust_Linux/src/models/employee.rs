use chrono::{NaiveDate, DateTime, Utc};
use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Employee {
    // ReferenceHandler.Preserve metadata - skip it
    #[serde(rename = "$id", skip_serializing, default)]
    pub ref_id: Option<String>,
    
    pub emp_id: i64,
    pub surname: String,
    pub name: String,
    pub patronym: Option<String>,
    pub employed_since: NaiveDate,
    pub job_id: i64,
    pub department_id: Option<i64>,
    
    // Personal details
    pub date_of_birth: Option<NaiveDate>,
    pub personal_phone: Option<String>,
    pub work_phone: Option<String>,
    pub address: Option<String>,
    pub email: Option<String>,
    
    // Documents
    pub passport_series: Option<String>,
    pub passport_number: Option<String>,
    pub inn: Option<String>,
    pub snils: Option<String>,
    
    // Driver information
    pub driver_license_number: Option<String>,
    pub driver_license_category: Option<String>,
    pub driver_license_issue_date: Option<NaiveDate>,
    pub driver_license_expiry_date: Option<NaiveDate>,
    
    // Medical information
    pub medical_certificate_number: Option<String>,
    pub medical_certificate_issue_date: Option<NaiveDate>,
    pub medical_certificate_expiry_date: Option<NaiveDate>,
    pub last_medical_check_date: Option<NaiveDate>,
    pub next_medical_check_date: Option<NaiveDate>,
    
    // Certifications
    pub has_passenger_transport_certification: bool,
    pub has_dangerous_goods_certification: bool,
    
    // Work status
    pub is_active: bool,
    pub termination_date: Option<NaiveDate>,
    pub termination_reason: Option<String>,
    
    // Audit fields
    pub created_at: DateTime<Utc>,
    pub updated_at: Option<DateTime<Utc>>,
    
    // Navigation properties - store as raw JSON to avoid deep nesting issues
    #[serde(skip_serializing_if = "Option::is_none", default)]
    pub job: Option<serde_json::Value>,
    
    #[serde(skip_serializing_if = "Option::is_none", default)]
    pub department: Option<serde_json::Value>,
}

impl Employee {
    pub fn job_title(&self) -> String {
        self.job.as_ref()
            .and_then(|j| j.get("jobTitle"))
            .and_then(|t| t.as_str())
            .unwrap_or("N/A")
            .to_string()
    }
    
    pub fn department_name(&self) -> String {
        self.department.as_ref()
            .and_then(|d| d.get("departmentName"))
            .and_then(|n| n.as_str())
            .unwrap_or("N/A")
            .to_string()
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
#[serde(untagged)]
enum Job {
    Reference {
        #[serde(rename = "$ref")]
        ref_pointer: String,
    },
    Full {
        #[serde(rename = "$id", skip_serializing_if = "Option::is_none")]
        ref_id: Option<String>,
        job_id: i32,
        job_title: String,
        #[serde(skip_serializing_if = "Option::is_none")]
        internship: Option<String>,
        // Catch rest
        #[serde(flatten)]
        extra: std::collections::HashMap<String, serde_json::Value>,
    },
}

impl Job {
    pub fn job_id(&self) -> i32 {
        match self {
            Job::Full { job_id, .. } => *job_id,
            Job::Reference { .. } => 0,
        }
    }
    
    pub fn job_title(&self) -> &str {
        match self {
            Job::Full { job_title, .. } => job_title.as_str(),
            Job::Reference { .. } => "N/A",
        }
    }
}

impl Employee {
    pub fn full_name(&self) -> String {
        if let Some(patronym) = &self.patronym {
            format!("{} {} {}", self.surname, self.name, patronym)
        } else {
            format!("{} {}", self.surname, self.name)
        }
    }
    
    pub fn is_driver(&self) -> bool {
        self.driver_license_number.is_some()
    }
    
    pub fn medical_valid(&self) -> bool {
        if let Some(expiry) = self.medical_certificate_expiry_date {
            expiry > chrono::Local::now().date_naive()
        } else {
            false
        }
    }
    
    pub fn license_valid(&self) -> bool {
        if let Some(expiry) = self.driver_license_expiry_date {
            expiry > chrono::Local::now().date_naive()
        } else {
            false
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase")]
pub struct CreateEmployeeRequest {
    pub surname: String,
    pub name: String,
    pub patronym: Option<String>,
    pub employed_since: NaiveDate,
    pub job_id: Option<i32>,
    pub department_id: Option<i32>,
    pub date_of_birth: Option<NaiveDate>,
    pub personal_phone: Option<String>,
    pub work_phone: Option<String>,
    pub address: Option<String>,
    pub email: Option<String>,
    pub passport_series: Option<String>,
    pub passport_number: Option<String>,
    pub inn: Option<String>,
    pub snils: Option<String>,
    pub driver_license_number: Option<String>,
    pub driver_license_category: Option<String>,
    pub driver_license_issue_date: Option<NaiveDate>,
    pub driver_license_expiry_date: Option<NaiveDate>,
    pub medical_certificate_number: Option<String>,
    pub medical_certificate_issue_date: Option<NaiveDate>,
    pub medical_certificate_expiry_date: Option<NaiveDate>,
}
