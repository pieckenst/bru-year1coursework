use crate::models::{Bus, CreateMaintenanceRequest, Maintenance, UpdateMaintenanceRequest};
use reqwest::Client;
use serde_json::Value;
use std::collections::HashMap;
use std::error::Error;

pub struct MaintenanceApi {
    base_url: String,
    client: Client,
}

impl MaintenanceApi {
    pub fn new(base_url: &str, client: Client) -> Self {
        Self {
            base_url: base_url.to_string(),
            client,
        }
    }

    pub async fn get_all(&self) -> Result<Vec<Maintenance>, Box<dyn Error>> {
        let url = format!("{}/api/Maintenance", self.base_url);
        let response = self.client.get(&url).send().await?;

        if response.status().is_success() {
            let json_text = response.text().await?;
            let maintenance_records = Self::parse_maintenance_with_references(&json_text)?;
            Ok(maintenance_records)
        } else {
            Err(format!("Failed to fetch maintenance records: {}", response.status()).into())
        }
    }

    pub async fn get_by_id(&self, id: i64) -> Result<Maintenance, Box<dyn Error>> {
        let url = format!("{}/api/Maintenance/{}", self.base_url, id);
        let response = self.client.get(&url).send().await?;

        if response.status().is_success() {
            let json_text = response.text().await?;
            let maintenance = Self::parse_single_maintenance_with_references(&json_text)?;
            Ok(maintenance)
        } else {
            Err(format!("Failed to fetch maintenance record: {}", response.status()).into())
        }
    }

    pub async fn create(
        &self,
        request: CreateMaintenanceRequest,
    ) -> Result<Maintenance, Box<dyn Error>> {
        let url = format!("{}/api/Maintenance", self.base_url);
        let response = self.client.post(&url).json(&request).send().await?;

        if response.status().is_success() {
            let json_text = response.text().await?;
            let maintenance = Self::parse_single_maintenance_with_references(&json_text)?;
            Ok(maintenance)
        } else {
            let error_text = response
                .text()
                .await
                .unwrap_or_else(|_| "Unknown error".to_string());
            Err(format!("Failed to create maintenance record: {}", error_text).into())
        }
    }

    pub async fn update(
        &self,
        request: UpdateMaintenanceRequest,
    ) -> Result<Maintenance, Box<dyn Error>> {
        let url = format!(
            "{}/api/Maintenance/{}",
            self.base_url, request.maintenance_id
        );
        let response = self.client.put(&url).json(&request).send().await?;

        if response.status().is_success() {
            let json_text = response.text().await?;
            let maintenance = Self::parse_single_maintenance_with_references(&json_text)?;
            Ok(maintenance)
        } else {
            let error_text = response
                .text()
                .await
                .unwrap_or_else(|_| "Unknown error".to_string());
            Err(format!("Failed to update maintenance record: {}", error_text).into())
        }
    }

    pub async fn delete(&self, id: i64) -> Result<(), Box<dyn Error>> {
        let url = format!("{}/api/Maintenance/{}", self.base_url, id);
        let response = self.client.delete(&url).send().await?;

        if response.status().is_success() {
            Ok(())
        } else {
            Err(format!("Failed to delete maintenance record: {}", response.status()).into())
        }
    }

    pub async fn search(&self, query: &str) -> Result<Vec<Maintenance>, Box<dyn Error>> {
        let url = format!("{}/api/Maintenance/search?query={}", self.base_url, query);
        let response = self.client.get(&url).send().await?;

        if response.status().is_success() {
            let json_text = response.text().await?;
            let maintenance_records = Self::parse_maintenance_with_references(&json_text)?;
            Ok(maintenance_records)
        } else {
            Err(format!(
                "Failed to search maintenance records: {}",
                response.status()
            )
            .into())
        }
    }

    pub async fn get_by_bus(&self, bus_id: i64) -> Result<Vec<Maintenance>, Box<dyn Error>> {
        let url = format!("{}/api/Maintenance/bus/{}", self.base_url, bus_id);
        let response = self.client.get(&url).send().await?;

        if response.status().is_success() {
            let json_text = response.text().await?;
            let maintenance_records = Self::parse_maintenance_with_references(&json_text)?;
            Ok(maintenance_records)
        } else {
            Err(format!(
                "Failed to fetch maintenance records for bus: {}",
                response.status()
            )
            .into())
        }
    }

    fn parse_maintenance_with_references(
        json_text: &str,
    ) -> Result<Vec<Maintenance>, Box<dyn Error>> {
        let json_value: Value = serde_json::from_str(json_text)?;

        // Build reference map
        let mut ref_map: HashMap<String, Value> = HashMap::new();
        Self::build_reference_map(&json_value, &mut ref_map);

        // Parse the array
        if let Value::Array(maintenance_array) = &json_value {
            let mut maintenance_records = Vec::new();
            for maintenance_value in maintenance_array {
                let maintenance = Self::parse_maintenance_object(maintenance_value, &ref_map)?;
                maintenance_records.push(maintenance);
            }
            Ok(maintenance_records)
        } else {
            Err("Expected array of maintenance records".into())
        }
    }

    fn parse_single_maintenance_with_references(
        json_text: &str,
    ) -> Result<Maintenance, Box<dyn Error>> {
        let json_value: Value = serde_json::from_str(json_text)?;

        // Build reference map
        let mut ref_map: HashMap<String, Value> = HashMap::new();
        Self::build_reference_map(&json_value, &mut ref_map);

        Self::parse_maintenance_object(&json_value, &ref_map)
    }

    fn build_reference_map(value: &Value, ref_map: &mut HashMap<String, Value>) {
        match value {
            Value::Object(obj) => {
                if let Some(Value::String(id)) = obj.get("$id") {
                    ref_map.insert(id.clone(), value.clone());
                }
                for (_, v) in obj.iter() {
                    Self::build_reference_map(v, ref_map);
                }
            }
            Value::Array(arr) => {
                for v in arr {
                    Self::build_reference_map(v, ref_map);
                }
            }
            _ => {}
        }
    }

    fn resolve_reference(value: &Value, ref_map: &HashMap<String, Value>) -> Value {
        if let Value::Object(obj) = value {
            if let Some(Value::String(ref_id)) = obj.get("$ref") {
                if let Some(resolved) = ref_map.get(ref_id) {
                    return resolved.clone();
                }
            }
        }
        value.clone()
    }

    fn parse_maintenance_object(
        value: &Value,
        ref_map: &HashMap<String, Value>,
    ) -> Result<Maintenance, Box<dyn Error>> {
        let resolved = Self::resolve_reference(value, ref_map);

        if let Value::Object(obj) = resolved {
            let maintenance_id = obj
                .get("MaintenanceId")
                .and_then(|v| v.as_i64())
                .unwrap_or(0);

            let bus_id = obj.get("BusId").and_then(|v| v.as_i64()).unwrap_or(0);

            let last_service_date = obj
                .get("LastServiceDate")
                .and_then(|v| v.as_str())
                .unwrap_or("")
                .to_string();

            let mileage_threshold = obj
                .get("MileageThreshold")
                .and_then(|v| v.as_str())
                .unwrap_or("")
                .to_string();

            let maintenance_type = obj
                .get("MaintenanceType")
                .and_then(|v| v.as_str())
                .unwrap_or("")
                .to_string();

            let service_engineer = obj
                .get("ServiceEngineer")
                .and_then(|v| v.as_str())
                .unwrap_or("")
                .to_string();

            let found_issues = obj
                .get("FoundIssues")
                .and_then(|v| v.as_str())
                .unwrap_or("")
                .to_string();

            let next_service_date = obj
                .get("NextServiceDate")
                .and_then(|v| v.as_str())
                .unwrap_or("")
                .to_string();

            let roadworthiness = obj
                .get("Roadworthiness")
                .and_then(|v| v.as_str())
                .unwrap_or("")
                .to_string();

            let avtobus = obj
                .get("Avtobus")
                .and_then(|v| Self::parse_bus_object(v, ref_map).ok());

            Ok(Maintenance {
                maintenance_id,
                bus_id,
                avtobus,
                last_service_date,
                mileage_threshold,
                maintenance_type,
                service_engineer,
                found_issues,
                next_service_date,
                roadworthiness,
            })
        } else {
            Err("Invalid maintenance object".into())
        }
    }

    fn parse_bus_object(
        value: &Value,
        ref_map: &HashMap<String, Value>,
    ) -> Result<Bus, Box<dyn Error>> {
        let resolved = Self::resolve_reference(value, ref_map);

        if let Value::Object(obj) = resolved {
            let bus_id = obj.get("BusId").and_then(|v| v.as_i64()).unwrap_or(0);

            let model = obj
                .get("Model")
                .and_then(|v| v.as_str())
                .unwrap_or("")
                .to_string();

            Ok(Bus {
                bus_id,
                model,
                routes: None,
                maintenance_records: None,
                ref_id: None,
                ref_pointer: None,
            })
        } else {
            Err("Invalid bus object".into())
        }
    }
}
