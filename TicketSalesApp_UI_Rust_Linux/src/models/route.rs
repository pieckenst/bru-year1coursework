use serde::{Deserialize, Serialize};
use std::collections::HashMap;

/// Route/Marshut model
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Route {
    #[serde(rename = "routeId")]
    pub route_id: i64,
    
    #[serde(rename = "startPoint")]
    pub start_point: String,
    
    #[serde(rename = "endPoint")]
    pub end_point: String,
    
    #[serde(rename = "driverId")]
    pub driver_id: i64,
    
    #[serde(rename = "employee", skip_serializing_if = "Option::is_none")]
    pub employee: Option<super::employee::Employee>,
    
    #[serde(rename = "busId")]
    pub bus_id: i64,
    
    #[serde(rename = "avtobus", skip_serializing_if = "Option::is_none")]
    pub bus: Option<super::bus::Bus>,
    
    #[serde(rename = "travelTime", skip_serializing_if = "Option::is_none")]
    pub travel_time: Option<String>,
    
    #[serde(rename = "tickets", skip_serializing_if = "Option::is_none")]
    pub tickets: Option<Vec<HashMap<String, serde_json::Value>>>,
    
    // For handling $ref circular references
    #[serde(rename = "$id", skip_serializing_if = "Option::is_none")]
    pub ref_id: Option<String>,
    
    #[serde(rename = "$ref", skip_serializing_if = "Option::is_none")]
    pub ref_pointer: Option<String>,
}

/// Create route request
#[derive(Debug, Clone, Serialize)]
pub struct CreateRouteRequest {
    #[serde(rename = "startPoint")]
    pub start_point: String,
    
    #[serde(rename = "endPoint")]
    pub end_point: String,
    
    #[serde(rename = "driverId")]
    pub driver_id: i64,
    
    #[serde(rename = "busId")]
    pub bus_id: i64,
    
    #[serde(rename = "travelTime", skip_serializing_if = "Option::is_none")]
    pub travel_time: Option<String>,
}

/// Update route request
#[derive(Debug, Clone, Serialize)]
pub struct UpdateRouteRequest {
    #[serde(rename = "routeId")]
    pub route_id: i64,
    
    #[serde(rename = "startPoint")]
    pub start_point: String,
    
    #[serde(rename = "endPoint")]
    pub end_point: String,
    
    #[serde(rename = "driverId")]
    pub driver_id: i64,
    
    #[serde(rename = "busId")]
    pub bus_id: i64,
    
    #[serde(rename = "travelTime", skip_serializing_if = "Option::is_none")]
    pub travel_time: Option<String>,
}

impl Route {
    /// Create a new Route
    pub fn new(
        route_id: i64,
        start_point: String,
        end_point: String,
        driver_id: i64,
        bus_id: i64,
    ) -> Self {
        Self {
            route_id,
            start_point,
            end_point,
            driver_id,
            bus_id,
            employee: None,
            bus: None,
            travel_time: None,
            tickets: None,
            ref_id: None,
            ref_pointer: None,
        }
    }
    
    /// Get display name for the route
    pub fn display_name(&self) -> String {
        format!("#{} {} → {}", self.route_id, self.start_point, self.end_point)
    }
    
    /// Get route description with travel time
    pub fn description(&self) -> String {
        if let Some(time) = &self.travel_time {
            format!("{} → {} ({})", self.start_point, self.end_point, time)
        } else {
            format!("{} → {}", self.start_point, self.end_point)
        }
    }
    
    /// Check if this is a reference placeholder
    pub fn is_reference(&self) -> bool {
        self.ref_pointer.is_some()
    }
    
    /// Get driver name if available
    pub fn driver_name(&self) -> String {
        if let Some(employee) = &self.employee {
            format!("{} {}", employee.name, employee.surname)
        } else {
            format!("Driver #{}", self.driver_id)
        }
    }
    
    /// Get bus model if available
    pub fn bus_model(&self) -> String {
        if let Some(bus) = &self.bus {
            bus.model.clone()
        } else {
            format!("Bus #{}", self.bus_id)
        }
    }
    
    /// Get the number of tickets sold for this route
    pub fn ticket_count(&self) -> usize {
        self.tickets.as_ref().map_or(0, |tickets| tickets.len())
    }
}

impl Default for Route {
    fn default() -> Self {
        Self {
            route_id: 0,
            start_point: String::new(),
            end_point: String::new(),
            driver_id: 0,
            bus_id: 0,
            employee: None,
            bus: None,
            travel_time: None,
            tickets: None,
            ref_id: None,
            ref_pointer: None,
        }
    }
}
