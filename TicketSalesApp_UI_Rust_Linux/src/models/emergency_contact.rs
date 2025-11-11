use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct EmergencyContact {
    pub contact_id: i64,
    pub employee_id: i64,
    pub contact_name: String,
    pub relationship: String,
    pub phone_number: String,
    pub alternate_phone_number: Option<String>,
    pub address: Option<String>,
    pub is_primary: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct CreateEmergencyContact {
    pub employee_id: i64,
    pub contact_name: String,
    pub relationship: String,
    pub phone_number: String,
    pub alternate_phone_number: Option<String>,
    pub address: Option<String>,
    pub is_primary: bool,
}

impl EmergencyContact {
    pub fn primary_badge(&self) -> &'static str {
        if self.is_primary {
            "Основной"
        } else {
            "Дополнительный"
        }
    }
}
