use super::Bus;
use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase")]
pub struct Maintenance {
    pub maintenance_id: i64,
    pub bus_id: i64,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub avtobus: Option<Bus>,
    pub last_service_date: String,
    pub mileage_threshold: String,
    pub maintenance_type: String,
    pub service_engineer: String,
    pub found_issues: String,
    pub next_service_date: String,
    pub roadworthiness: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase")]
pub struct CreateMaintenanceRequest {
    pub bus_id: i64,
    pub last_service_date: String,
    pub mileage_threshold: String,
    pub maintenance_type: String,
    pub service_engineer: String,
    pub found_issues: String,
    pub next_service_date: String,
    pub roadworthiness: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase")]
pub struct UpdateMaintenanceRequest {
    pub maintenance_id: i64,
    pub bus_id: i64,
    pub last_service_date: String,
    pub mileage_threshold: String,
    pub maintenance_type: String,
    pub service_engineer: String,
    pub found_issues: String,
    pub next_service_date: String,
    pub roadworthiness: String,
}
