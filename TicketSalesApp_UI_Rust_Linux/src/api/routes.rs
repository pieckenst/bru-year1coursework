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
    /// Get all routes
    pub async fn get_routes(&self) -> Result<Vec<Route>, Box<dyn std::error::Error>> {
        log::debug!("Fetching routes from: api/Routes");

        let response = self.get("api/Routes").await
            .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

        log::debug!("Routes response status: {}", response.status());

        if response.status().is_success() {
            let json_str = response.text().await?;
            log::debug!("Raw routes JSON (first 500 chars): {}", &json_str[..json_str.len().min(500)]);

            // Parse as raw JSON first
            let json_value: Value = serde_json::from_str(&json_str)?;
            
            // Check if it's wrapped in $values (ASP.NET Core ReferenceHandler.Preserve format)
            let routes_array = if let Some(values) = json_value.get("$values") {
                log::debug!("Found $values wrapper, extracting array");
                values.as_array()
                    .ok_or_else(|| "Expected $values to be an array")?
            } else if let Some(arr) = json_value.as_array() {
                log::debug!("Direct array format");
                arr
            } else {
                return Err("Unexpected JSON format - not an array or $values wrapper".into());
            };

            log::debug!("Processing {} items from routes array", routes_array.len());

            // Collect all route objects by their $id
            let mut route_map = std::collections::HashMap::new();
            
            for (idx, route_value) in routes_array.iter().enumerate() {
                if let Some(ref_id) = route_value.get("$ref").and_then(|v| v.as_str()) {
                    log::debug!("  Item {} is $ref pointer to: {}", idx, ref_id);
                } else if let Some(obj) = route_value.as_object() {
                    if let Some(id) = obj.get("$id").and_then(|v| v.as_str()) {
                        if obj.contains_key("routeId") {
                            let route_id = obj.get("routeId").and_then(|v| v.as_i64()).unwrap_or(0);
                            log::debug!("  Found route with $id={}, routeId={}", id, route_id);
                            route_map.insert(id.to_string(), route_value.clone());
                        }
                    }
                }
            }

            log::debug!("Total unique routes found: {}", route_map.len());

            // Parse each unique route
            let mut routes = Vec::new();
            for (_id, route_value) in route_map.iter() {
                let ref_map = build_ref_map(route_value);
                let resolved_value = resolve_refs(route_value, &ref_map);
                
                match serde_json::from_value::<Route>(resolved_value) {
                    Ok(mut route) => {
                        // Clear circular reference fields after resolution
                        route.ref_id = None;
                        route.ref_pointer = None;
                        log::debug!("✓ Parsed route: {} (ID: {})", route.display_name(), route.route_id);
                        routes.push(route);
                    }
                    Err(e) => {
                        log::warn!("Failed to deserialize route: {}", e);
                        continue;
                    }
                }
            }

            // Sort routes by ID in ascending order
            routes.sort_by_key(|route| route.route_id);
            
            log::info!("Successfully loaded {} routes (sorted by ID)", routes.len());
            Ok(routes)
        } else {
            let status = response.status();
            let error_text = response.text().await?;
            log::error!("Failed to fetch routes: {} - {}", status, error_text);
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
