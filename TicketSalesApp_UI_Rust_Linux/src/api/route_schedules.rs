use super::ApiClient;
use crate::models::{RouteSchedule, CreateRouteScheduleRequest, UpdateRouteScheduleRequest};
use std::collections::HashMap;
use serde_json::Value;
use log;

// Helper functions for $ref resolution
fn build_ref_map(json_value: &Value) -> HashMap<String, Value> {
    let mut ref_map = HashMap::new();
    
    fn collect_refs(value: &Value, map: &mut HashMap<String, Value>) {
        match value {
            Value::Object(obj) => {
                if let Some(id) = obj.get("$id") {
                    if let Some(id_str) = id.as_str() {
                        map.insert(id_str.to_string(), value.clone());
                    }
                }
                for (_, v) in obj {
                    collect_refs(v, map);
                }
            }
            Value::Array(arr) => {
                for item in arr {
                    collect_refs(item, map);
                }
            }
            _ => {}
        }
    }
    
    collect_refs(json_value, &mut ref_map);
    ref_map
}

fn resolve_refs(value: &Value, ref_map: &HashMap<String, Value>) -> Value {
    match value {
        Value::Object(obj) => {
            if let Some(ref_value) = obj.get("$ref") {
                if let Some(ref_str) = ref_value.as_str() {
                    if let Some(resolved) = ref_map.get(ref_str) {
                        return resolve_refs(resolved, ref_map);
                    }
                }
                return value.clone();
            }
            
            let mut new_obj = serde_json::Map::new();
            for (k, v) in obj {
                new_obj.insert(k.clone(), resolve_refs(v, ref_map));
            }
            Value::Object(new_obj)
        }
        Value::Array(arr) => {
            Value::Array(arr.iter().map(|v| resolve_refs(v, ref_map)).collect())
        }
        _ => value.clone(),
    }
}

impl ApiClient {
    /// Get all route schedules
    pub async fn get_route_schedules(&self) -> Result<Vec<RouteSchedule>, Box<dyn std::error::Error>> {
        log::debug!("Fetching route schedules from: api/RouteSchedules");

        let response = self.get("api/RouteSchedules").await
            .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

        log::debug!("Route schedules response status: {}", response.status());

        if response.status().is_success() {
            let json_str = response.text().await?;
            log::debug!("Raw route schedules JSON (first 500 chars): {}", &json_str[..json_str.len().min(500)]);

            // Parse as raw JSON first
            let json_value: Value = serde_json::from_str(&json_str)?;
            
            // Check if it's wrapped in $values (ASP.NET Core ReferenceHandler.Preserve format)
            let schedules_array = if let Some(values) = json_value.get("$values") {
                log::debug!("Found $values wrapper, extracting array");
                values.as_array()
                    .ok_or_else(|| "Expected $values to be an array")?
            } else if let Some(arr) = json_value.as_array() {
                log::debug!("Direct array format");
                arr
            } else {
                return Err("Unexpected JSON format - not an array or $values wrapper".into());
            };

            log::debug!("Processing {} items from route schedules array", schedules_array.len());

            // Collect all schedule objects by their $id
            let mut schedule_map = std::collections::HashMap::new();
            
            for (idx, schedule_value) in schedules_array.iter().enumerate() {
                if let Some(ref_id) = schedule_value.get("$ref").and_then(|v| v.as_str()) {
                    log::debug!("  Item {} is $ref pointer to: {}", idx, ref_id);
                } else if let Some(obj) = schedule_value.as_object() {
                    if let Some(id) = obj.get("$id").and_then(|v| v.as_str()) {
                        if obj.contains_key("routeScheduleId") || obj.contains_key("scheduleId") {
                            let schedule_id = obj.get("routeScheduleId")
                                .or_else(|| obj.get("scheduleId"))
                                .and_then(|v| v.as_i64())
                                .unwrap_or(0);
                            log::debug!("  Found schedule with $id={}, scheduleId={}", id, schedule_id);
                            schedule_map.insert(id.to_string(), schedule_value.clone());
                        }
                    }
                }
            }

            log::debug!("Total unique route schedules found: {}", schedule_map.len());

            // Parse each unique schedule
            let mut schedules = Vec::new();
            for (_id, schedule_value) in schedule_map.iter() {
                let ref_map = build_ref_map(schedule_value);
                let resolved_value = resolve_refs(schedule_value, &ref_map);
                
                match serde_json::from_value::<RouteSchedule>(resolved_value) {
                    Ok(mut schedule) => {
                        // Clear circular reference fields after resolution
                        schedule.ref_id = None;
                        schedule.ref_pointer = None;
                        log::debug!("✓ Parsed schedule: {} (ID: {})", schedule.display_name(), schedule.route_schedule_id);
                        schedules.push(schedule);
                    }
                    Err(e) => {
                        log::warn!("Failed to deserialize route schedule: {}", e);
                        continue;
                    }
                }
            }

            // Sort schedules by ID in ascending order
            schedules.sort_by_key(|schedule| schedule.route_schedule_id);
            
            log::info!("Successfully loaded {} route schedules (sorted by ID)", schedules.len());
            Ok(schedules)
        } else {
            let status = response.status();
            let error_text = response.text().await?;
            log::error!("Failed to fetch route schedules: {} - {}", status, error_text);
            Err(format!("Failed to fetch route schedules: {} - {}", status, error_text).into())
        }
    }

    /// Get a specific route schedule by ID
    pub async fn get_route_schedule(&self, schedule_id: i64) -> Result<RouteSchedule, Box<dyn std::error::Error>> {
        let endpoint = format!("api/RouteSchedules/{}", schedule_id);
        log::debug!("Fetching route schedule from: {}", endpoint);

        let response = self.get(&endpoint).await
            .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

        log::debug!("Route schedule response status: {}", response.status());

        if response.status().is_success() {
            let json_str = response.text().await?;
            log::debug!("Raw route schedule JSON: {}", json_str);

            // Handle $ref resolution for circular references
            let json_value: Value = serde_json::from_str(&json_str)?;
            let ref_map = build_ref_map(&json_value);
            let resolved_value = resolve_refs(&json_value, &ref_map);

            let mut schedule: RouteSchedule = serde_json::from_value(resolved_value)?;
            
            // Clear circular reference fields after resolution
            schedule.ref_id = None;
            schedule.ref_pointer = None;

            log::info!("Successfully loaded route schedule: {}", schedule.display_name());
            Ok(schedule)
        } else {
            let status = response.status();
            let error_text = response.text().await?;
            log::error!("Failed to fetch route schedule {}: {} - {}", schedule_id, status, error_text);
            Err(format!("Failed to fetch route schedule {}: {} - {}", schedule_id, status, error_text).into())
        }
    }

    /// Create a new route schedule
    pub async fn create_route_schedule(&self, request: CreateRouteScheduleRequest) -> Result<RouteSchedule, Box<dyn std::error::Error>> {
        log::debug!("Creating route schedule at: api/RouteSchedules");

        let response = self.post("api/RouteSchedules", &request).await
            .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

        log::debug!("Create route schedule response status: {}", response.status());

        if response.status().is_success() {
            let json_str = response.text().await?;
            log::debug!("Created route schedule JSON: {}", json_str);

            // Handle $ref resolution for circular references
            let json_value: Value = serde_json::from_str(&json_str)?;
            let ref_map = build_ref_map(&json_value);
            let resolved_value = resolve_refs(&json_value, &ref_map);

            let mut schedule: RouteSchedule = serde_json::from_value(resolved_value)?;
            
            // Clear circular reference fields after resolution
            schedule.ref_id = None;
            schedule.ref_pointer = None;

            log::info!("Successfully created route schedule: {}", schedule.display_name());
            Ok(schedule)
        } else {
            let status = response.status();
            let error_text = response.text().await?;
            log::error!("Failed to create route schedule: {} - {}", status, error_text);
            Err(format!("Failed to create route schedule: {} - {}", status, error_text).into())
        }
    }

    /// Update an existing route schedule
    pub async fn update_route_schedule(&self, schedule_id: i64, request: UpdateRouteScheduleRequest) -> Result<RouteSchedule, Box<dyn std::error::Error>> {
        let endpoint = format!("api/RouteSchedules/{}", schedule_id);
        log::debug!("Updating route schedule at: {}", endpoint);

        let response = self.put(&endpoint, &request).await
            .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

        log::debug!("Update route schedule response status: {}", response.status());

        if response.status().is_success() {
            let json_str = response.text().await?;
            log::debug!("Updated route schedule JSON: {}", json_str);

            // Handle $ref resolution for circular references
            let json_value: Value = serde_json::from_str(&json_str)?;
            let ref_map = build_ref_map(&json_value);
            let resolved_value = resolve_refs(&json_value, &ref_map);

            let mut schedule: RouteSchedule = serde_json::from_value(resolved_value)?;
            
            // Clear circular reference fields after resolution
            schedule.ref_id = None;
            schedule.ref_pointer = None;

            log::info!("Successfully updated route schedule: {}", schedule.display_name());
            Ok(schedule)
        } else {
            let status = response.status();
            let error_text = response.text().await?;
            log::error!("Failed to update route schedule {}: {} - {}", schedule_id, status, error_text);
            Err(format!("Failed to update route schedule {}: {} - {}", schedule_id, status, error_text).into())
        }
    }

    /// Delete a route schedule
    pub async fn delete_route_schedule(&self, schedule_id: i64) -> Result<(), Box<dyn std::error::Error>> {
        let endpoint = format!("api/RouteSchedules/{}", schedule_id);
        log::debug!("Deleting route schedule at: {}", endpoint);

        let response = self.delete(&endpoint).await
            .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

        log::debug!("Delete route schedule response status: {}", response.status());

        if response.status().is_success() {
            log::info!("Successfully deleted route schedule {}", schedule_id);
            Ok(())
        } else {
            let status = response.status();
            let error_text = response.text().await?;
            log::error!("Failed to delete route schedule {}: {} - {}", schedule_id, status, error_text);
            Err(format!("Failed to delete route schedule {}: {} - {}", schedule_id, status, error_text).into())
        }
    }

    /// Search route schedules by start/end point
    pub async fn search_route_schedules(&self, query: &str) -> Result<Vec<RouteSchedule>, Box<dyn std::error::Error>> {
        let schedules = self.get_route_schedules().await?;
        
        let filtered_schedules: Vec<RouteSchedule> = schedules
            .into_iter()
            .filter(|schedule| {
                schedule.start_point.to_lowercase().contains(&query.to_lowercase()) ||
                schedule.end_point.to_lowercase().contains(&query.to_lowercase())
            })
            .collect();

        log::debug!("Found {} route schedules matching query: '{}'", filtered_schedules.len(), query);
        Ok(filtered_schedules)
    }

    /// Get route schedules by route ID
    pub async fn get_route_schedules_by_route(&self, route_id: i64) -> Result<Vec<RouteSchedule>, Box<dyn std::error::Error>> {
        let schedules = self.get_route_schedules().await?;
        
        let filtered_schedules: Vec<RouteSchedule> = schedules
            .into_iter()
            .filter(|schedule| schedule.route_id == Some(route_id))
            .collect();

        log::debug!("Found {} route schedules for route {}", filtered_schedules.len(), route_id);
        Ok(filtered_schedules)
    }

    /// Get active route schedules
    pub async fn get_active_route_schedules(&self) -> Result<Vec<RouteSchedule>, Box<dyn std::error::Error>> {
        let schedules = self.get_route_schedules().await?;
        
        let active_schedules: Vec<RouteSchedule> = schedules
            .into_iter()
            .filter(|schedule| schedule.is_currently_valid())
            .collect();

        log::debug!("Found {} active route schedules", active_schedules.len());
        Ok(active_schedules)
    }

    /// Get route schedules by day of week
    pub async fn get_route_schedules_by_day(&self, day: &str) -> Result<Vec<RouteSchedule>, Box<dyn std::error::Error>> {
        let schedules = self.get_route_schedules().await?;
        
        let filtered_schedules: Vec<RouteSchedule> = schedules
            .into_iter()
            .filter(|schedule| {
                schedule.days_of_week.iter().any(|d| d.to_lowercase() == day.to_lowercase())
            })
            .collect();

        log::debug!("Found {} route schedules for day '{}'", filtered_schedules.len(), day);
        Ok(filtered_schedules)
    }
}
