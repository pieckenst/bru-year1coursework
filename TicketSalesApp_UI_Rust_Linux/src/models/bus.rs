use serde::{Deserialize, Serialize};
use std::collections::HashMap;

/// Bus/Avtobus model
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Bus {
    #[serde(rename = "busId")]
    pub bus_id: i64,
    
    #[serde(rename = "model")]
    pub model: String,
    
    #[serde(rename = "routes", skip_serializing_if = "Option::is_none")]
    pub routes: Option<Vec<super::route::Route>>,
    
    #[serde(rename = "obsluzhivanies", skip_serializing_if = "Option::is_none")]
    pub maintenance_records: Option<Vec<HashMap<String, serde_json::Value>>>,
    
    // For handling $ref circular references
    #[serde(rename = "$id", skip_serializing_if = "Option::is_none")]
    pub ref_id: Option<String>,
    
    #[serde(rename = "$ref", skip_serializing_if = "Option::is_none")]
    pub ref_pointer: Option<String>,
}

/// Create bus request
#[derive(Debug, Clone, Serialize)]
pub struct CreateBusRequest {
    #[serde(rename = "model")]
    pub model: String,
}

/// Update bus request
#[derive(Debug, Clone, Serialize)]
pub struct UpdateBusRequest {
    #[serde(rename = "busId")]
    pub bus_id: i64,
    
    #[serde(rename = "model")]
    pub model: String,
}

impl Bus {
    /// Create a new Bus
    pub fn new(bus_id: i64, model: String) -> Self {
        Self {
            bus_id,
            model,
            routes: None,
            maintenance_records: None,
            ref_id: None,
            ref_pointer: None,
        }
    }
    
    /// Get display name for the bus
    pub fn display_name(&self) -> String {
        format!("#{} - {}", self.bus_id, self.model)
    }
    
    /// Check if this is a reference placeholder
    pub fn is_reference(&self) -> bool {
        self.ref_pointer.is_some()
    }
    
    /// Get the number of routes assigned to this bus
    pub fn route_count(&self) -> usize {
        self.routes.as_ref().map_or(0, |routes| routes.len())
    }
}

impl Default for Bus {
    fn default() -> Self {
        Self {
            bus_id: 0,
            model: String::new(),
            routes: None,
            maintenance_records: None,
            ref_id: None,
            ref_pointer: None,
        }
    }
}
