use super::ApiClient;
use crate::models::{Route, CreateRouteRequest, UpdateRouteRequest};
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
    /// Get all routes
    pub async fn get_routes(&self) -> Result<Vec<Route>, Box<dyn std::error::Error>> {
        println!("🚗 Fetching routes from: api/Routes");

        let response = self.get("api/Routes").await
            .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

        println!("🚗 Routes response status: {}", response.status());

        if response.status().is_success() {
            let json_str = response.text().await?;
            println!("🚗 Raw routes JSON (first 500 chars): {}", &json_str[..json_str.len().min(500)]);

            // Parse as raw JSON first
            let json_value: Value = serde_json::from_str(&json_str)?;
            
            // Check if it's wrapped in $values (ASP.NET Core ReferenceHandler.Preserve format)
            let routes_array = if let Some(values) = json_value.get("$values") {
                println!("🚗 Found $values wrapper, extracting array");
                values.as_array()
                    .ok_or_else(|| "Expected $values to be an array")?
            } else if let Some(arr) = json_value.as_array() {
                println!("🚗 Direct array format");
                arr
            } else {
                return Err("Unexpected JSON format - not an array or $values wrapper".into());
            };

            println!("🚗 Processing {} items from routes array", routes_array.len());

            // First pass: Build a reference map of ALL objects with $id
            let ref_map = build_ref_map(&json_value);
            println!("🚗 Built reference map with {} entries", ref_map.len());

            // Second pass: Collect all route objects (both direct and via $ref)
            let mut route_map = std::collections::HashMap::new();
            
            for (idx, route_value) in routes_array.iter().enumerate() {
                // Check if this is a $ref pointer
                if let Some(ref_id) = route_value.get("$ref").and_then(|v| v.as_str()) {
                    println!("  Item {} is $ref pointer to: {}", idx, ref_id);
                    // Resolve the reference
                    if let Some(resolved) = ref_map.get(ref_id) {
                        if let Some(obj) = resolved.as_object() {
                            if obj.contains_key("routeId") {
                                let route_id = obj.get("routeId").and_then(|v| v.as_i64()).unwrap_or(0);
                                println!("    ✓ Resolved to route with routeId={}", route_id);
                                route_map.insert(ref_id.to_string(), resolved.clone());
                            }
                        }
                    } else {
                        println!("    ✗ Could not resolve $ref {}", ref_id);
                    }
                } else if let Some(obj) = route_value.as_object() {
                    if let Some(id) = obj.get("$id").and_then(|v| v.as_str()) {
                        if obj.contains_key("routeId") {
                            let route_id = obj.get("routeId").and_then(|v| v.as_i64()).unwrap_or(0);
                            println!("  Found route with $id={}, routeId={}", id, route_id);
                            route_map.insert(id.to_string(), route_value.clone());
                        } else {
                            println!("  Item {} has $id={} but no routeId field", idx, id);
                        }
                    } else {
                        println!("  Item {} has no $id field", idx);
                    }
                } else {
                    println!("  Item {} is not an object", idx);
                }
            }

            println!("📊 Total unique routes found: {}", route_map.len());

            // Parse each unique route - manually to avoid nested object issues
            let mut routes = Vec::new();
            for (_id, route_value) in route_map.iter() {
                let route_obj = match route_value.as_object() {
                    Some(obj) => obj,
                    None => continue,
                };

                // Manually extract fields to avoid deep nesting issues
                let route_id = route_obj.get("routeId")
                    .and_then(|v| v.as_i64())
                    .unwrap_or(0);
                
                let start_point = route_obj.get("startPoint")
                    .and_then(|v| v.as_str())
                    .unwrap_or("")
                    .to_string();
                
                let end_point = route_obj.get("endPoint")
                    .and_then(|v| v.as_str())
                    .unwrap_or("")
                    .to_string();
                
                let driver_id = route_obj.get("driverId")
                    .and_then(|v| v.as_i64())
                    .unwrap_or(0);
                
                let bus_id = route_obj.get("busId")
                    .and_then(|v| v.as_i64())
                    .unwrap_or(0);
                
                let travel_time = route_obj.get("travelTime")
                    .and_then(|v| v.as_str())
                    .map(|s| s.to_string());

                let route = Route {
                    route_id,
                    start_point: start_point.clone(),
                    end_point: end_point.clone(),
                    driver_id,
                    bus_id,
                    travel_time,
                    employee: None,  // Skip nested objects to avoid circular refs
                    bus: None,       // Skip nested objects to avoid circular refs
                    tickets: None,   // Skip nested objects to avoid circular refs
                    ref_id: None,
                    ref_pointer: None,
                };

                println!("✓ Parsed route: {} → {} (ID: {})", start_point, end_point, route_id);
                routes.push(route);
            }

            // Sort routes by ID in ascending order
            routes.sort_by_key(|route| route.route_id);
            
            println!("✅ Successfully loaded {} routes (sorted by ID)", routes.len());
            Ok(routes)
        } else {
            let status = response.status();
            let error_text = response.text().await?;
            println!("❌ Failed to fetch routes: {} - {}", status, error_text);
            Err(format!("Failed to fetch routes: {} - {}", status, error_text).into())
        }
    }

    /// Get a specific route by ID
    pub async fn get_route(&self, route_id: i64) -> Result<Route, Box<dyn std::error::Error>> {
        let endpoint = format!("api/Routes/{}", route_id);
        log::debug!("Fetching route from: {}", endpoint);

        let response = self.get(&endpoint).await
            .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

        log::debug!("Route response status: {}", response.status());

        if response.status().is_success() {
            let json_str = response.text().await?;
            log::debug!("Raw route JSON: {}", json_str);

            // Handle $ref resolution for circular references
            let json_value: Value = serde_json::from_str(&json_str)?;
            let ref_map = build_ref_map(&json_value);
            let resolved_value = resolve_refs(&json_value, &ref_map);

            let mut route: Route = serde_json::from_value(resolved_value)?;
            
            // Clear circular reference fields after resolution
            route.ref_id = None;
            route.ref_pointer = None;

            log::info!("Successfully loaded route: {}", route.display_name());
            Ok(route)
        } else {
            let status = response.status();
            let error_text = response.text().await?;
            log::error!("Failed to fetch route {}: {} - {}", route_id, status, error_text);
            Err(format!("Failed to fetch route {}: {} - {}", route_id, status, error_text).into())
        }
    }

    /// Create a new route
    pub async fn create_route(&self, request: CreateRouteRequest) -> Result<Route, Box<dyn std::error::Error>> {
        log::debug!("Creating route at: api/Routes");

        let response = self.post("api/Routes", &request).await
            .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

        log::debug!("Create route response status: {}", response.status());

        if response.status().is_success() {
            let json_str = response.text().await?;
            log::debug!("Created route JSON: {}", json_str);

            // Handle $ref resolution for circular references
            let json_value: Value = serde_json::from_str(&json_str)?;
            let ref_map = build_ref_map(&json_value);
            let resolved_value = resolve_refs(&json_value, &ref_map);

            let mut route: Route = serde_json::from_value(resolved_value)?;
            
            // Clear circular reference fields after resolution
            route.ref_id = None;
            route.ref_pointer = None;

            log::info!("Successfully created route: {}", route.display_name());
            Ok(route)
        } else {
            let status = response.status();
            let error_text = response.text().await?;
            log::error!("Failed to create route: {} - {}", status, error_text);
            Err(format!("Failed to create route: {} - {}", status, error_text).into())
        }
    }

    /// Update an existing route
    pub async fn update_route(&self, route_id: i64, request: UpdateRouteRequest) -> Result<Route, Box<dyn std::error::Error>> {
        let endpoint = format!("api/Routes/{}", route_id);
        log::debug!("Updating route at: {}", endpoint);

        let response = self.put(&endpoint, &request).await
            .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

        log::debug!("Update route response status: {}", response.status());

        if response.status().is_success() {
            let json_str = response.text().await?;
            log::debug!("Updated route JSON: {}", json_str);

            // Handle $ref resolution for circular references
            let json_value: Value = serde_json::from_str(&json_str)?;
            let ref_map = build_ref_map(&json_value);
            let resolved_value = resolve_refs(&json_value, &ref_map);

            let mut route: Route = serde_json::from_value(resolved_value)?;
            
            // Clear circular reference fields after resolution
            route.ref_id = None;
            route.ref_pointer = None;

            log::info!("Successfully updated route: {}", route.display_name());
            Ok(route)
        } else {
            let status = response.status();
            let error_text = response.text().await?;
            log::error!("Failed to update route {}: {} - {}", route_id, status, error_text);
            Err(format!("Failed to update route {}: {} - {}", route_id, status, error_text).into())
        }
    }

    /// Delete a route
    pub async fn delete_route(&self, route_id: i64) -> Result<(), Box<dyn std::error::Error>> {
        let endpoint = format!("api/Routes/{}", route_id);
        log::debug!("Deleting route at: {}", endpoint);

        let response = self.delete(&endpoint).await
            .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

        log::debug!("Delete route response status: {}", response.status());

        if response.status().is_success() {
            log::info!("Successfully deleted route {}", route_id);
            Ok(())
        } else {
            let status = response.status();
            let error_text = response.text().await?;
            log::error!("Failed to delete route {}: {} - {}", route_id, status, error_text);
            Err(format!("Failed to delete route {}: {} - {}", route_id, status, error_text).into())
        }
    }

    /// Search routes by start/end point
    pub async fn search_routes(&self, query: &str) -> Result<Vec<Route>, Box<dyn std::error::Error>> {
        let routes = self.get_routes().await?;
        
        let filtered_routes: Vec<Route> = routes
            .into_iter()
            .filter(|route| {
                route.start_point.to_lowercase().contains(&query.to_lowercase()) ||
                route.end_point.to_lowercase().contains(&query.to_lowercase())
            })
            .collect();

        log::debug!("Found {} routes matching query: '{}'", filtered_routes.len(), query);
        Ok(filtered_routes)
    }

    /// Get routes by bus ID
    pub async fn get_routes_by_bus(&self, bus_id: i64) -> Result<Vec<Route>, Box<dyn std::error::Error>> {
        let routes = self.get_routes().await?;
        
        let filtered_routes: Vec<Route> = routes
            .into_iter()
            .filter(|route| route.bus_id == bus_id)
            .collect();

        log::debug!("Found {} routes for bus {}", filtered_routes.len(), bus_id);
        Ok(filtered_routes)
    }

    /// Get routes by driver ID
    pub async fn get_routes_by_driver(&self, driver_id: i64) -> Result<Vec<Route>, Box<dyn std::error::Error>> {
        let routes = self.get_routes().await?;
        
        let filtered_routes: Vec<Route> = routes
            .into_iter()
            .filter(|route| route.driver_id == driver_id)
            .collect();

        log::debug!("Found {} routes for driver {}", filtered_routes.len(), driver_id);
        Ok(filtered_routes)
    }
}
