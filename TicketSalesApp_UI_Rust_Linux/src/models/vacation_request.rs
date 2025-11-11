use chrono::{NaiveDate, NaiveDateTime};
use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct VacationRequest {
    pub request_id: i64,
    pub employee_id: i64,
    pub start_date: NaiveDate,
    pub end_date: NaiveDate,
    pub vacation_type: String,
    pub reason: Option<String>,
    pub status: String,
    pub approved_by_user_id: Option<i64>,
    pub approval_date: Option<NaiveDateTime>,
    pub approval_notes: Option<String>,
    pub days_requested: i32,
    pub created_at: Option<NaiveDateTime>,
    pub updated_at: Option<NaiveDateTime>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct CreateVacationRequest {
    pub employee_id: i64,
    pub start_date: NaiveDate,
    pub end_date: NaiveDate,
    pub vacation_type: String,
    pub reason: Option<String>,
    pub status: String,
    pub days_requested: i32,
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
    
    pub fn calculate_days(&self) -> i32 {
        (self.end_date - self.start_date).num_days() as i32 + 1
    }
}
