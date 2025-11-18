use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc, NaiveDateTime};

/// RouteSchedule model
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct RouteSchedule {
    #[serde(rename = "routeScheduleId")]
    pub route_schedule_id: i64,
    
    #[serde(rename = "startPoint")]
    pub start_point: String,
    
    #[serde(rename = "routeStops")]
    pub route_stops: Vec<String>,
    
    #[serde(rename = "endPoint")]
    pub end_point: String,
    
    #[serde(rename = "departureTime")]
    pub departure_time: DateTime<Utc>,
    
    #[serde(rename = "arrivalTime")]
    pub arrival_time: DateTime<Utc>,
    
    #[serde(rename = "price")]
    pub price: f64,
    
    #[serde(rename = "availableSeats")]
    pub available_seats: i32,
    
    #[serde(rename = "daysOfWeek")]
    pub days_of_week: Vec<String>,
    
    #[serde(rename = "busTypes")]
    pub bus_types: Vec<String>,
    
    #[serde(rename = "routeId", skip_serializing_if = "Option::is_none")]
    pub route_id: Option<i64>,
    
    #[serde(rename = "marshut", skip_serializing_if = "Option::is_none")]
    pub route: Option<super::route::Route>,
    
    #[serde(rename = "isActive")]
    pub is_active: bool,
    
    #[serde(rename = "validFrom")]
    pub valid_from: DateTime<Utc>,
    
    #[serde(rename = "validUntil", skip_serializing_if = "Option::is_none")]
    pub valid_until: Option<DateTime<Utc>>,
    
    #[serde(rename = "stopDurationMinutes")]
    pub stop_duration_minutes: i32,
    
    #[serde(rename = "isRecurring")]
    pub is_recurring: bool,
    
    #[serde(rename = "estimatedStopTimes", skip_serializing_if = "Option::is_none")]
    pub estimated_stop_times: Option<Vec<String>>,
    
    #[serde(rename = "stopDistances", skip_serializing_if = "Option::is_none")]
    pub stop_distances: Option<Vec<f64>>,
    
    #[serde(rename = "notes", skip_serializing_if = "Option::is_none")]
    pub notes: Option<String>,
    
    #[serde(rename = "createdAt")]
    pub created_at: DateTime<Utc>,
    
    #[serde(rename = "updatedAt", skip_serializing_if = "Option::is_none")]
    pub updated_at: Option<DateTime<Utc>>,
    
    #[serde(rename = "updatedBy", skip_serializing_if = "Option::is_none")]
    pub updated_by: Option<String>,
    
    // For handling $ref circular references
    #[serde(rename = "$id", skip_serializing_if = "Option::is_none")]
    pub ref_id: Option<String>,
    
    #[serde(rename = "$ref", skip_serializing_if = "Option::is_none")]
    pub ref_pointer: Option<String>,
}

/// Create route schedule request
#[derive(Debug, Clone, Serialize)]
pub struct CreateRouteScheduleRequest {
    #[serde(rename = "startPoint")]
    pub start_point: String,
    
    #[serde(rename = "routeStops")]
    pub route_stops: Vec<String>,
    
    #[serde(rename = "endPoint")]
    pub end_point: String,
    
    #[serde(rename = "departureTime")]
    pub departure_time: DateTime<Utc>,
    
    #[serde(rename = "arrivalTime")]
    pub arrival_time: DateTime<Utc>,
    
    #[serde(rename = "price")]
    pub price: f64,
    
    #[serde(rename = "availableSeats")]
    pub available_seats: i32,
    
    #[serde(rename = "daysOfWeek")]
    pub days_of_week: Vec<String>,
    
    #[serde(rename = "busTypes")]
    pub bus_types: Vec<String>,
    
    #[serde(rename = "routeId", skip_serializing_if = "Option::is_none")]
    pub route_id: Option<i64>,
    
    #[serde(rename = "isActive")]
    pub is_active: bool,
    
    #[serde(rename = "validFrom")]
    pub valid_from: DateTime<Utc>,
    
    #[serde(rename = "validUntil", skip_serializing_if = "Option::is_none")]
    pub valid_until: Option<DateTime<Utc>>,
    
    #[serde(rename = "stopDurationMinutes")]
    pub stop_duration_minutes: i32,
    
    #[serde(rename = "isRecurring")]
    pub is_recurring: bool,
    
    #[serde(rename = "estimatedStopTimes", skip_serializing_if = "Option::is_none")]
    pub estimated_stop_times: Option<Vec<String>>,
    
    #[serde(rename = "stopDistances", skip_serializing_if = "Option::is_none")]
    pub stop_distances: Option<Vec<f64>>,
    
    #[serde(rename = "notes", skip_serializing_if = "Option::is_none")]
    pub notes: Option<String>,
}

/// Update route schedule request
#[derive(Debug, Clone, Serialize)]
pub struct UpdateRouteScheduleRequest {
    #[serde(rename = "routeScheduleId")]
    pub route_schedule_id: i64,
    
    #[serde(rename = "startPoint")]
    pub start_point: String,
    
    #[serde(rename = "routeStops")]
    pub route_stops: Vec<String>,
    
    #[serde(rename = "endPoint")]
    pub end_point: String,
    
    #[serde(rename = "departureTime")]
    pub departure_time: DateTime<Utc>,
    
    #[serde(rename = "arrivalTime")]
    pub arrival_time: DateTime<Utc>,
    
    #[serde(rename = "price")]
    pub price: f64,
    
    #[serde(rename = "availableSeats")]
    pub available_seats: i32,
    
    #[serde(rename = "daysOfWeek")]
    pub days_of_week: Vec<String>,
    
    #[serde(rename = "busTypes")]
    pub bus_types: Vec<String>,
    
    #[serde(rename = "routeId", skip_serializing_if = "Option::is_none")]
    pub route_id: Option<i64>,
    
    #[serde(rename = "isActive")]
    pub is_active: bool,
    
    #[serde(rename = "validFrom")]
    pub valid_from: DateTime<Utc>,
    
    #[serde(rename = "validUntil", skip_serializing_if = "Option::is_none")]
    pub valid_until: Option<DateTime<Utc>>,
    
    #[serde(rename = "stopDurationMinutes")]
    pub stop_duration_minutes: i32,
    
    #[serde(rename = "isRecurring")]
    pub is_recurring: bool,
    
    #[serde(rename = "estimatedStopTimes", skip_serializing_if = "Option::is_none")]
    pub estimated_stop_times: Option<Vec<String>>,
    
    #[serde(rename = "stopDistances", skip_serializing_if = "Option::is_none")]
    pub stop_distances: Option<Vec<f64>>,
    
    #[serde(rename = "notes", skip_serializing_if = "Option::is_none")]
    pub notes: Option<String>,
}

impl RouteSchedule {
    /// Create a new RouteSchedule
    #[allow(clippy::too_many_arguments)]
    pub fn new(
        route_schedule_id: i64,
        start_point: String,
        route_stops: Vec<String>,
        end_point: String,
        departure_time: DateTime<Utc>,
        arrival_time: DateTime<Utc>,
        price: f64,
        available_seats: i32,
        days_of_week: Vec<String>,
        bus_types: Vec<String>,
    ) -> Self {
        Self {
            route_schedule_id,
            start_point,
            route_stops,
            end_point,
            departure_time,
            arrival_time,
            price,
            available_seats,
            days_of_week,
            bus_types,
            route_id: None,
            route: None,
            is_active: true,
            valid_from: Utc::now(),
            valid_until: None,
            stop_duration_minutes: 5,
            is_recurring: true,
            estimated_stop_times: None,
            stop_distances: None,
            notes: None,
            created_at: Utc::now(),
            updated_at: None,
            updated_by: None,
            ref_id: None,
            ref_pointer: None,
        }
    }
    
    /// Get display name for the schedule
    pub fn display_name(&self) -> String {
        format!(
            "#{} {} → {} ({})",
            self.route_schedule_id,
            self.start_point,
            self.end_point,
            self.departure_time.format("%H:%M")
        )
    }
    
    /// Get route description
    pub fn description(&self) -> String {
        format!(
            "{} → {} | Отправление: {} | Прибытие: {} | Цена: {:.2}₽",
            self.start_point,
            self.end_point,
            self.departure_time.format("%H:%M"),
            self.arrival_time.format("%H:%M"),
            self.price
        )
    }
    
    /// Check if this is a reference placeholder
    pub fn is_reference(&self) -> bool {
        self.ref_pointer.is_some()
    }
    
    /// Get total travel time as a formatted string
    pub fn total_travel_time(&self) -> String {
        let duration = self.arrival_time - self.departure_time;
        let hours = duration.num_hours();
        let minutes = duration.num_minutes() % 60;
        
        if hours > 0 {
            format!("{}ч {}мин", hours, minutes)
        } else {
            format!("{}мин", minutes)
        }
    }
    
    /// Get the number of stops
    pub fn total_stops(&self) -> usize {
        self.route_stops.len()
    }
    
    /// Check if schedule is currently valid
    pub fn is_currently_valid(&self) -> bool {
        let now = Utc::now();
        self.is_active 
            && self.valid_from <= now
            && self.valid_until.map_or(true, |until| until >= now)
    }
    
    /// Get days of week as formatted string
    pub fn days_of_week_display(&self) -> String {
        if self.days_of_week.is_empty() {
            "Нет данных".to_string()
        } else {
            self.days_of_week.join(", ")
        }
    }
    
    /// Get bus types as formatted string
    pub fn bus_types_display(&self) -> String {
        if self.bus_types.is_empty() {
            "Любой".to_string()
        } else {
            self.bus_types.join(", ")
        }
    }
    
    /// Get route stops as formatted string
    pub fn route_stops_display(&self) -> String {
        if self.route_stops.is_empty() {
            "Без остановок".to_string()
        } else {
            self.route_stops.join(" → ")
        }
    }
    
    /// Format price as currency string
    pub fn price_display(&self) -> String {
        format!("{:.2}₽", self.price)
    }
    
    /// Get status text
    pub fn status_text(&self) -> String {
        if self.is_currently_valid() {
            "Действует".to_string()
        } else if !self.is_active {
            "Неактивно".to_string()
        } else {
            "Недействительно".to_string()
        }
    }
}

impl Default for RouteSchedule {
    fn default() -> Self {
        let now = Utc::now();
        Self {
            route_schedule_id: 0,
            start_point: String::new(),
            route_stops: Vec::new(),
            end_point: String::new(),
            departure_time: now,
            arrival_time: now,
            price: 0.0,
            available_seats: 0,
            days_of_week: Vec::new(),
            bus_types: Vec::new(),
            route_id: None,
            route: None,
            is_active: true,
            valid_from: now,
            valid_until: None,
            stop_duration_minutes: 5,
            is_recurring: true,
            estimated_stop_times: None,
            stop_distances: None,
            notes: None,
            created_at: now,
            updated_at: None,
            updated_by: None,
            ref_id: None,
            ref_pointer: None,
        }
    }
}
