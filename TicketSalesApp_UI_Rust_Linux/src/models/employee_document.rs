use serde::{Deserialize, Serialize};
use chrono::{NaiveDate, NaiveDateTime};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct EmployeeDocument {
    pub document_id: i64,
    pub document_type: String,
    pub document_number: String,
    pub issue_date: NaiveDate,
    pub expiry_date: Option<NaiveDate>,
    pub issued_by: Option<String>,
    pub file_path: Option<String>,
    pub notes: Option<String>,
    pub employee_id: i64,
    pub created_at: Option<NaiveDateTime>,
    pub updated_at: Option<NaiveDateTime>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct CreateEmployeeDocument {
    pub document_type: String,
    pub document_number: String,
    pub issue_date: NaiveDate,
    pub expiry_date: Option<NaiveDate>,
    pub issued_by: Option<String>,
    pub file_path: Option<String>,
    pub notes: Option<String>,
    pub employee_id: i64,
}

impl EmployeeDocument {
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
            let days_until_expiry = (expiry - today).num_days();
            days_until_expiry <= days && days_until_expiry > 0
        } else {
            false
        }
    }
    
    pub fn status_badge(&self) -> &'static str {
        if self.expiry_date.is_none() {
            return "Бессрочный";
        }
        
        if self.is_expired() {
            "Истек"
        } else if self.is_expiring_soon(30) {
            "Истекает скоро"
        } else {
            "Действителен"
        }
    }
}
