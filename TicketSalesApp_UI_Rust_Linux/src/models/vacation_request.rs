use chrono::{DateTime, NaiveDate, Utc};
use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase")]
pub struct VacationRequest {
    pub request_id: i64,
    pub employee_id: i64,
    pub start_date: NaiveDate,
    pub end_date: NaiveDate,
    pub vacation_type: String,
    pub reason: Option<String>,
    pub status: String,
    pub approved_by_user_id: Option<i64>,
    pub approval_date: Option<DateTime<Utc>>,
    pub approval_notes: Option<String>,
    pub days_requested: i32,
    pub created_at: DateTime<Utc>,
    pub updated_at: Option<DateTime<Utc>>,
}

impl VacationRequest {
    pub fn is_pending(&self) -> bool {
        self.status == "Pending"
    }
    
    pub fn is_approved(&self) -> bool {
        self.status == "Approved"
    }
    
    pub fn is_rejected(&self) -> bool {
        self.status == "Rejected"
    }
}
