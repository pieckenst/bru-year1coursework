use super::ApiClient;
use crate::models::{Bus, CreateBusRequest, UpdateBusRequest};
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
    /// Get all buses - manually parse to handle circular refs like employees
    pub async fn get_buses(&self) -> Result<Vec<Bus>, Box<dyn std::error::Error>> {
        println!("🚌 Fetching buses from: api/Buses");

        let response = self.get("api/Buses").await
            .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

        println!("🚌 Buses response status: {}", response.status());

        if response.status().is_success() {
            let json_str = response.text().await?;
            println!("🚌 Raw buses JSON (first 500 chars): {}", &json_str[..json_str.len().min(500)]);

            // Parse as raw JSON first
            let json_value: Value = serde_json::from_str(&json_str)?;
            
            // Check if it's wrapped in $values (ASP.NET Core ReferenceHandler.Preserve format)
            let buses_array = if let Some(values) = json_value.get("$values") {
                println!("🚌 Found $values wrapper, extracting array");
                values.as_array()
                    .ok_or_else(|| "Expected $values to be an array")?
            } else if let Some(arr) = json_value.as_array() {
                println!("🚌 Direct array format");
                arr
            } else {
                return Err("Unexpected JSON format - not an array or $values wrapper".into());
            };

            println!("🚌 Processing {} items from buses array", buses_array.len());

            // Collect all bus objects by their $id
            let mut bus_map = std::collections::HashMap::new();
            
            for (idx, bus_value) in buses_array.iter().enumerate() {
                if let Some(ref_id) = bus_value.get("$ref").and_then(|v| v.as_str()) {
                    println!("  Item {} is $ref pointer to: {}", idx, ref_id);
                } else if let Some(obj) = bus_value.as_object() {
                    if let Some(id) = obj.get("$id").and_then(|v| v.as_str()) {
                        if obj.contains_key("busId") {
                            let bus_id = obj.get("busId").and_then(|v| v.as_i64()).unwrap_or(0);
                            let model = obj.get("model").and_then(|v| v.as_str()).unwrap_or("Unknown");
                            println!("  Found bus with $id={}, busId={}, model={}", id, bus_id, model);
                            bus_map.insert(id.to_string(), bus_value.clone());
                        }
                    }
                }
            }

            println!("📊 Total unique buses found: {}", bus_map.len());

            // Parse each unique bus
            let mut buses = Vec::new();
            for (_id, bus_value) in bus_map.iter() {
                let bus_obj = match bus_value.as_object() {
                    Some(obj) => obj,
                    None => continue,
                };

                // Manually extract fields to avoid deep nesting issues
                let bus_id = bus_obj.get("busId")
                    .and_then(|v| v.as_i64())
                    .ok_or("Missing busId")?;
                
                let model = bus_obj.get("model")
                    .and_then(|v| v.as_str())
                    .ok_or("Missing model")?
                    .to_string();

                let bus = Bus {
                    bus_id,
                    model: model.clone(),
                    routes: None,
                    maintenance_records: None,
                    ref_id: None,
                    ref_pointer: None,
                };

                println!("✓ Parsed bus: {} (ID: {})", model, bus_id);
                buses.push(bus);
            }

            // Sort buses by ID in ascending order
            buses.sort_by_key(|bus| bus.bus_id);
            
            println!("✅ Successfully parsed {}/{} buses total (sorted by ID)", buses.len(), bus_map.len());
            Ok(buses)
        } else {
            let status = response.status();
            let error_text = response.text().await?;
            eprintln!("❌ Failed to fetch buses: {} - {}", status, error_text);
            Err(format!("Failed to fetch buses: {} - {}", status, error_text).into())
        }
    }

    /// Get a specific bus by ID
    pub async fn get_bus(&self, bus_id: i64) -> Result<Bus, Box<dyn std::error::Error>> {
        let endpoint = format!("api/Buses/{}", bus_id);
        log::debug!("Fetching bus from: {}", endpoint);

        let response = self.get(&endpoint).await
            .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

        log::debug!("Bus response status: {}", response.status());

        if response.status().is_success() {
            let json_str = response.text().await?;
            log::debug!("Raw bus JSON: {}", json_str);

            // Handle $ref resolution for circular references
            let json_value: Value = serde_json::from_str(&json_str)?;
            let ref_map = build_ref_map(&json_value);
            let resolved_value = resolve_refs(&json_value, &ref_map);

            let mut bus: Bus = serde_json::from_value(resolved_value)?;
            
            // Clear circular reference fields after resolution
            bus.ref_id = None;
            bus.ref_pointer = None;

            log::info!("Successfully loaded bus: {}", bus.display_name());
            Ok(bus)
        } else {
            let status = response.status();
            let error_text = response.text().await?;
            log::error!("Failed to fetch bus {}: {} - {}", bus_id, status, error_text);
            Err(format!("Failed to fetch bus {}: {} - {}", bus_id, status, error_text).into())
        }
    }

    /// Create a new bus
    pub async fn create_bus(&self, request: CreateBusRequest) -> Result<Bus, Box<dyn std::error::Error>> {
        log::debug!("Creating bus at: api/Buses");

        let response = self.post("api/Buses", &request).await
            .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

        log::debug!("Create bus response status: {}", response.status());

        if response.status().is_success() {
            let json_str = response.text().await?;
            log::debug!("Created bus JSON: {}", json_str);

            // Handle $ref resolution for circular references
            let json_value: Value = serde_json::from_str(&json_str)?;
            let ref_map = build_ref_map(&json_value);
            let resolved_value = resolve_refs(&json_value, &ref_map);

            let mut bus: Bus = serde_json::from_value(resolved_value)?;
            
            // Clear circular reference fields after resolution
            bus.ref_id = None;
            bus.ref_pointer = None;

            log::info!("Successfully created bus: {}", bus.display_name());
            Ok(bus)
        } else {
            let status = response.status();
            let error_text = response.text().await?;
            log::error!("Failed to create bus: {} - {}", status, error_text);
            Err(format!("Failed to create bus: {} - {}", status, error_text).into())
        }
    }

    /// Update an existing bus
    pub async fn update_bus(&self, bus_id: i64, request: UpdateBusRequest) -> Result<Bus, Box<dyn std::error::Error>> {
        let endpoint = format!("api/Buses/{}", bus_id);
        log::debug!("Updating bus at: {}", endpoint);

        let response = self.put(&endpoint, &request).await
            .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

        log::debug!("Update bus response status: {}", response.status());

        if response.status().is_success() {
            let json_str = response.text().await?;
            log::debug!("Updated bus JSON: {}", json_str);

            // Handle $ref resolution for circular references
            let json_value: Value = serde_json::from_str(&json_str)?;
            let ref_map = build_ref_map(&json_value);
            let resolved_value = resolve_refs(&json_value, &ref_map);

            let mut bus: Bus = serde_json::from_value(resolved_value)?;
            
            // Clear circular reference fields after resolution
            bus.ref_id = None;
            bus.ref_pointer = None;

            log::info!("Successfully updated bus: {}", bus.display_name());
            Ok(bus)
        } else {
            let status = response.status();
            let error_text = response.text().await?;
            log::error!("Failed to update bus {}: {} - {}", bus_id, status, error_text);
            Err(format!("Failed to update bus {}: {} - {}", bus_id, status, error_text).into())
        }
    }

    /// Delete a bus
    pub async fn delete_bus(&self, bus_id: i64) -> Result<(), Box<dyn std::error::Error>> {
        let endpoint = format!("api/Buses/{}", bus_id);
        log::debug!("Deleting bus at: {}", endpoint);

        let response = self.delete(&endpoint).await
            .map_err(|e| Box::new(e) as Box<dyn std::error::Error>)?;

        log::debug!("Delete bus response status: {}", response.status());

        if response.status().is_success() {
            log::info!("Successfully deleted bus {}", bus_id);
            Ok(())
        } else {
            let status = response.status();
            let error_text = response.text().await?;
            log::error!("Failed to delete bus {}: {} - {}", bus_id, status, error_text);
            Err(format!("Failed to delete bus {}: {} - {}", bus_id, status, error_text).into())
        }
    }

    /// Search buses by model name
    pub async fn search_buses(&self, query: &str) -> Result<Vec<Bus>, Box<dyn std::error::Error>> {
        let buses = self.get_buses().await?;
        
        let filtered_buses: Vec<Bus> = buses
            .into_iter()
            .filter(|bus| bus.model.to_lowercase().contains(&query.to_lowercase()))
            .collect();

        log::debug!("Found {} buses matching query: '{}'", filtered_buses.len(), query);
        Ok(filtered_buses)
    }
}
