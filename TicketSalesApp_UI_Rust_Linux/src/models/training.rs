use chrono::{DateTime, NaiveDate, Utc};
use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase")]
pub struct EmployeeTraining {
    pub training_id: i64,
    pub training_name: String,
    pub description: Option<String>,
    pub completion_date: NaiveDate,
    pub expiry_date: Option<NaiveDate>,
    pub certificate_number: Option<String>,
    pub issuing_organization: Option<String>,
    pub is_mandatory: bool,
    pub file_path: Option<String>,
    pub notes: Option<String>,
    pub employee_id: i64,
    pub created_at: DateTime<Utc>,
    pub updated_at: Option<DateTime<Utc>>,
}

impl EmployeeTraining {
    pub fn is_expired(&self) -> bool {
        if let Some(expiry) = self.expiry_date {
            expiry < chrono::Local::now().date_naive()
        } else {
            false
        }
    }
    
    pub fn is_expiring_soon(&self, days: i64) -> bool {
        if let Some(expiry) = self.expiry_date {
            let today = chrono::Local::now().date_naive();
            let diff = expiry.signed_duration_since(today);
            diff.num_days() <= days && diff.num_days() >= 0
        } else {
            false
        }
    }
}
