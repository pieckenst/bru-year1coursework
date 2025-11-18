use super::ApiClient;
use crate::models::{RouteSchedule, CreateRouteScheduleRequest, UpdateRouteScheduleRequest};
use std::collections::HashMap;
use std::sync::{Arc, Mutex};
use serde_json::Value;
use log;
use once_cell::sync::Lazy;

// Global cache for route schedules to avoid re-fetching 72k+ records
static SCHEDULE_CACHE: Lazy<Arc<Mutex<Option<Vec<RouteSchedule>>>>> = 
    Lazy::new(|| Arc::new(Mutex::new(None)));

// Helper functions for $ref resolution (Windows Forms: BuildGlobalIdMap)
fn build_ref_map(json_value: &Value) -> HashMap<String, Value> {
    let mut ref_map = HashMap::new();
    
    fn collect_refs(value: &Value, map: &mut HashMap<String, Value>) {
        match value {
            Value::Object(obj) => {
                if let Some(id) = obj.get("$id") {
                    if let Some(id_str) = id.as_str() {
                        if !map.contains_key(id_str) {
                            map.insert(id_str.to_string(), value.clone());
                        }
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

// Clean JSON token by removing $id and resolving nested $ref (Windows Forms: CleanAndTransformJsonToken)
// ITERATIVE VERSION to prevent stack overflow with large datasets
fn clean_json_token(value: &Value, ref_map: &HashMap<String, Value>) -> Value {
    // For simple $ref resolution at the top level
    if let Value::Object(obj) = value {
        if obj.len() == 1 {
            if let Some(Value::String(ref_id)) = obj.get("$ref") {
                if let Some(resolved) = ref_map.get(ref_id) {
                    // Recursively resolve this one reference
                    return clean_json_token(resolved, ref_map);
                } else {
                    return Value::Object(serde_json::Map::new());
                }
            }
        }
    }
    
    // For complex objects, use shallow cleaning only
    match value {
        Value::Object(obj) => {
            let mut cleaned_obj = serde_json::Map::new();
            
            for (key, val) in obj.iter() {
                // Skip $id properties
                if key == "$id" {
                    continue;
                }
                
                // Handle $ref at this level only (shallow)
                let cleaned_value = if let Value::Object(val_obj) = val {
                    if val_obj.len() == 1 {
                        if let Some(Value::String(ref_id)) = val_obj.get("$ref") {
                            // Resolve reference but don't recurse deeply
                            if let Some(resolved) = ref_map.get(ref_id) {
                                resolved.clone()
                            } else {
                                Value::Object(serde_json::Map::new())
                            }
                        } else {
                            val.clone()
                        }
                    } else {
                        val.clone()
                    }
                } else {
                    val.clone()
                };
                
                // Check if cleaned value is a {$values: array} wrapper and unwrap it
                if let Value::Object(cleaned_obj_inner) = &cleaned_value {
                    if cleaned_obj_inner.len() == 1 {
                        if let Some(Value::Array(arr)) = cleaned_obj_inner.get("$values") {
                            // Unwrap the $values array
                            cleaned_obj.insert(key.clone(), Value::Array(arr.clone()));
                            continue;
                        }
                    }
                }
                
                // Only add non-null values and non-empty objects
                if !cleaned_value.is_null() {
                    if let Value::Object(ref inner_obj) = cleaned_value {
                        if inner_obj.is_empty() {
                            continue;
                        }
                    }
                    cleaned_obj.insert(key.clone(), cleaned_value);
                }
            }
            
            Value::Object(cleaned_obj)
        }
        Value::Array(arr) => {
            // For arrays, just resolve $ref at the top level
            let cleaned_items: Vec<Value> = arr.iter()
                .map(|item| {
                    if let Value::Object(obj) = item {
                        if obj.len() == 1 {
                            if let Some(Value::String(ref_id)) = obj.get("$ref") {
                                if let Some(resolved) = ref_map.get(ref_id) {
                                    return resolved.clone();
                                }
                            }
                        }
                    }
                    item.clone()
                })
                .filter(|item| {
                    // Filter out null values and empty objects
                    if item.is_null() {
                        return false;
                    }
                    if let Value::Object(obj) = item {
                        return !obj.is_empty();
                    }
                    true
                })
                .collect();
            
            Value::Array(cleaned_items)
        }
        _ => value.clone(),
    }
}

// Manually parse a schedule object (Windows Forms style XML parsing)
fn parse_schedule_manually(value: &Value) -> Result<RouteSchedule, Box<dyn std::error::Error>> {
    use chrono::{DateTime, Utc};
    
    let obj = value.as_object()
        .ok_or("Schedule is not an object")?;
    
    // Check if object is empty (from cleaned $ref)
    if obj.is_empty() {
        return Err("Empty schedule object (likely unresolved $ref)".into());
    }
    
    // Extract routeScheduleId (required)
    let route_schedule_id = obj.get("routeScheduleId")
        .and_then(|v| v.as_i64())
        .ok_or("Missing or invalid routeScheduleId")?;
    
    // Extract other fields with defaults
    let route_id = obj.get("routeId").and_then(|v| v.as_i64());
    let start_point = obj.get("startPoint").and_then(|v| v.as_str()).unwrap_or("").to_string();
    let end_point = obj.get("endPoint").and_then(|v| v.as_str()).unwrap_or("").to_string();
    
    // Parse route stops array (filter out empty strings like Windows Forms)
    let route_stops = if let Some(Value::Array(stops)) = obj.get("routeStops") {
        let valid_stops: Vec<String> = stops.iter()
            .filter_map(|v| v.as_str())
            .filter(|s| !s.trim().is_empty())
            .map(|s| s.to_string())
            .collect();
        
        if valid_stops.is_empty() {
            vec![start_point.clone(), end_point.clone()]
        } else {
            valid_stops
        }
    } else {
        vec![start_point.clone(), end_point.clone()]
    };
    
    // Helper function to parse DateTime
    let parse_datetime = |s: &str| -> DateTime<Utc> {
        DateTime::parse_from_rfc3339(s)
            .map(|dt| dt.with_timezone(&Utc))
            .unwrap_or_else(|_| Utc::now())
    };
    
    // Parse times
    let departure_time = obj.get("departureTime")
        .and_then(|v| v.as_str())
        .map(|s| parse_datetime(s))
        .unwrap_or_else(Utc::now);
    
    let arrival_time = obj.get("arrivalTime")
        .and_then(|v| v.as_str())
        .map(|s| parse_datetime(s))
        .unwrap_or_else(Utc::now);
    
    // Parse price
    let price = obj.get("price")
        .and_then(|v| v.as_f64())
        .unwrap_or(0.0);
    
    // Parse available seats
    let available_seats = obj.get("availableSeats")
        .and_then(|v| v.as_i64())
        .unwrap_or(0) as i32;
    
    // Parse days of week
    let days_of_week = if let Some(Value::Array(days)) = obj.get("daysOfWeek") {
        days.iter()
            .filter_map(|v| v.as_str())
            .map(|s| s.to_string())
            .collect()
    } else {
        vec![]
    };
    
    // Parse bus types
    let bus_types = if let Some(Value::Array(types)) = obj.get("busTypes") {
        types.iter()
            .filter_map(|v| v.as_str())
            .map(|s| s.to_string())
            .collect()
    } else {
        vec![]
    };
    
    // Parse is_active
    let is_active = obj.get("isActive")
        .and_then(|v| v.as_bool())
        .unwrap_or(true);
    
    // Parse valid_from
    let valid_from = obj.get("validFrom")
        .and_then(|v| v.as_str())
        .map(|s| parse_datetime(s))
        .unwrap_or_else(Utc::now);
    
    // Parse valid_until
    let valid_until = obj.get("validUntil")
        .and_then(|v| v.as_str())
        .map(|s| parse_datetime(s));
    
    // Parse stop_duration_minutes
    let stop_duration_minutes = obj.get("stopDurationMinutes")
        .and_then(|v| v.as_i64())
        .unwrap_or(5) as i32;
    
    // Parse is_recurring
    let is_recurring = obj.get("isRecurring")
        .and_then(|v| v.as_bool())
        .unwrap_or(true);
    
    // Parse estimated_stop_times
    let estimated_stop_times = obj.get("estimatedStopTimes")
        .and_then(|v| v.as_array())
        .map(|arr| {
            arr.iter()
                .filter_map(|v| v.as_str())
                .map(|s| s.to_string())
                .collect()
        });
    
    // Parse stop_distances
    let stop_distances = obj.get("stopDistances")
        .and_then(|v| v.as_array())
        .map(|arr| {
            arr.iter()
                .filter_map(|v| v.as_f64())
                .collect()
        });
    
    // Parse notes
    let notes = obj.get("notes")
        .and_then(|v| v.as_str())
        .map(|s| s.to_string());
    
    // Parse created_at
    let created_at = obj.get("createdAt")
        .and_then(|v| v.as_str())
        .map(|s| parse_datetime(s))
        .unwrap_or_else(Utc::now);
    
    // Parse updated_at
    let updated_at = obj.get("updatedAt")
        .and_then(|v| v.as_str())
        .map(|s| parse_datetime(s));
    
    // Parse updated_by
    let updated_by = obj.get("updatedBy")
        .and_then(|v| v.as_str())
        .map(|s| s.to_string());
    
    Ok(RouteSchedule {
        route_schedule_id,
        route_id,
        start_point,
        end_point,
        route_stops,
        departure_time,
        arrival_time,
        price,
        available_seats,
        days_of_week,
        bus_types,
        route: None,
        is_active,
        valid_from,
        valid_until,
        stop_duration_minutes,
        is_recurring,
        estimated_stop_times,
        stop_distances,
        notes,
        created_at,
        updated_at,
        updated_by,
        ref_id: None,
        ref_pointer: None,
    })
}

fn resolve_refs(value: &Value, ref_map: &HashMap<String, Value>) -> Value {
    resolve_refs_with_depth(value, ref_map, 0, 10)
}

fn resolve_refs_with_depth(value: &Value, ref_map: &HashMap<String, Value>, depth: usize, max_depth: usize) -> Value {
    if depth >= max_depth {
        log::warn!("Max recursion depth reached while resolving references");
        return value.clone();
    }
    
    match value {
        Value::Object(obj) => {
            if let Some(ref_value) = obj.get("$ref") {
                if let Some(ref_str) = ref_value.as_str() {
                    if let Some(resolved) = ref_map.get(ref_str) {
                        return resolve_refs_with_depth(resolved, ref_map, depth + 1, max_depth);
                    }
                }
                return value.clone();
            }
            
            let mut new_obj = serde_json::Map::new();
            for (k, v) in obj {
                new_obj.insert(k.clone(), resolve_refs_with_depth(v, ref_map, depth + 1, max_depth));
            }
            Value::Object(new_obj)
        }
        Value::Array(arr) => {
            Value::Array(arr.iter().map(|v| resolve_refs_with_depth(v, ref_map, depth + 1, max_depth)).collect())
        }
        _ => value.clone(),
    }
}

impl ApiClient {
    /// Get all route schedules with pagination support and caching
    pub async fn get_route_schedules(&self) -> Result<Vec<RouteSchedule>, Box<dyn std::error::Error>> {
        // Check cache first
        {
            let cache = SCHEDULE_CACHE.lock().unwrap();
            if let Some(cached_schedules) = cache.as_ref() {
                println!("📅 Using cached schedules ({} total)", cached_schedules.len());
                return Ok(cached_schedules.clone());
            }
        }
        println!("📅 Fetching all route schedules with pagination...");
        
        let mut all_schedules = Vec::new();
        let mut current_page = 1;
        let page_size = 500; // Match API default
        
        loop {
            println!("📅 Fetching page {} (size: {})", current_page, page_size);
            
            let url = format!("api/RouteSchedules?PageNumber={}&PageSize={}", current_page, page_size);
            let response = self.get(&url).await
                .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

            println!("📅 Page {} response status: {}", current_page, response.status());
            
            if !response.status().is_success() {
                return Err(format!("Failed to fetch route schedules page {}: {}", current_page, response.status()).into());
            }
            
            // Extract pagination metadata from X-Pagination header before consuming response
            let pagination_header = response.headers().get("X-Pagination")
                .and_then(|h| h.to_str().ok())
                .unwrap_or("{}")
                .to_string();
            
            let json_str = response.text().await?;
            println!(
                "📅 Raw route schedules JSON page {} (first 500 chars): {}",
                current_page,
                &json_str[..json_str.len().min(500)]
            );

            // Parse as raw JSON first
            let json_value: Value = serde_json::from_str(&json_str)?;
            
            // Build a global reference map for this page (Windows Forms: BuildGlobalIdMap)
            let ref_map = build_ref_map(&json_value);
            println!(
                "📅 Built global reference map with {} entries for page {}",
                ref_map.len(),
                current_page
            );
            
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

            println!(
                "📅 Processing {} items from route schedules array (page {})",
                schedules_array.len(),
                current_page
            );

            // Collect all schedule objects (both direct and via $ref)
            let mut schedule_map = std::collections::HashMap::new();
            
            for (idx, schedule_value) in schedules_array.iter().enumerate() {
                if let Some(ref_id) = schedule_value.get("$ref").and_then(|v| v.as_str()) {
                    println!("  Item {} is $ref pointer to: {}", idx, ref_id);
                    // Resolve the reference from global map
                    if let Some(resolved) = ref_map.get(ref_id) {
                        if let Some(obj) = resolved.as_object() {
                            if let Some(schedule_id) = obj.get("routeScheduleId").and_then(|v| v.as_i64()) {
                                println!("  ✓ Resolved to schedule with routeScheduleId={}", schedule_id);
                                schedule_map.insert(schedule_id, resolved.clone());
                            } else {
                                println!("  ✗ Resolved object has no routeScheduleId field");
                            }
                        }
                    }
                } else if let Some(obj) = schedule_value.as_object() {
                    if let Some(schedule_id) = obj.get("routeScheduleId").and_then(|v| v.as_i64()) {
                        let id_str = obj.get("$id").and_then(|v| v.as_str()).unwrap_or("no-id");
                        println!("  Found schedule with $id={}, routeScheduleId={}", id_str, schedule_id);
                        schedule_map.insert(schedule_id, schedule_value.clone());
                    } else {
                        println!("  Item {} is object but has no routeScheduleId (keys: {:?})", idx, obj.keys().collect::<Vec<_>>());
                    }
                }
            }

            println!("📅 Page {} unique route schedules found: {}", current_page, schedule_map.len());

            // Convert to RouteSchedule objects for this page
            for (schedule_id, schedule_value) in schedule_map {
                // Clean the JSON to remove $id and resolve nested $ref
                let cleaned_value = clean_json_token(&schedule_value, &ref_map);
                
                match parse_schedule_manually(&cleaned_value) {
                    Ok(schedule) => {
                        all_schedules.push(schedule);
                    }
                    Err(e) => {
                        println!("❌ Failed to parse schedule {}: {}", schedule_id, e);
                    }
                }
            }
            
            // Parse pagination metadata to determine if there are more pages
            let pagination_info: serde_json::Value = serde_json::from_str(&pagination_header)
                .unwrap_or_else(|_| serde_json::json!({}));
            
            let has_next = pagination_info.get("HasNext")
                .and_then(|v| v.as_bool())
                .unwrap_or(false);
            
            let total_pages = pagination_info.get("TotalPages")
                .and_then(|v| v.as_i64())
                .unwrap_or(1) as i32;
            
            println!("📅 Page {}/{} completed. Total schedules so far: {}. Has next: {}", 
                current_page, total_pages, all_schedules.len(), has_next);
            
            if !has_next || current_page >= total_pages {
                println!("📅 Finished fetching all {} pages. Total schedules: {}", current_page, all_schedules.len());
                break;
            }
            
            current_page += 1;
        }
        
        // Sort schedules by ID in ascending order
        all_schedules.sort_by_key(|schedule| schedule.route_schedule_id);
        
        println!("✅ Successfully parsed {} total schedules from all pages", all_schedules.len());
        log::info!("Successfully loaded {} route schedules (sorted by ID)", all_schedules.len());
        
        // Cache the results for future use
        {
            let mut cache = SCHEDULE_CACHE.lock().unwrap();
            *cache = Some(all_schedules.clone());
            println!("💾 Cached {} schedules for future requests", all_schedules.len());
        }
        
        Ok(all_schedules)
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
        println!("📅 Fetching schedules for route ID: {}", route_id);
        let schedules = self.get_route_schedules().await?;
        println!("📅 Total schedules fetched: {}", schedules.len());
        
        let filtered_schedules: Vec<RouteSchedule> = schedules
            .into_iter()
            .filter(|schedule| {
                let matches = schedule.route_id == Some(route_id);
                if matches {
                    println!("  ✓ Schedule {} matches route {}", schedule.route_schedule_id, route_id);
                }
                matches
            })
            .collect();

        println!("📅 Found {} route schedules for route {}", filtered_schedules.len(), route_id);
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
