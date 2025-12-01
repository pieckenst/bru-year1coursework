use crate::models::ticket_sales::{Marshut, Ticket, TicketSale};
use reqwest::Client;
use serde_json::Value;
use std::collections::HashMap;
use std::error::Error;

pub struct TicketSalesApi {
    base_url: String,
    client: Client,
}

impl TicketSalesApi {
    pub fn new(base_url: &str, client: Client) -> Self {
        Self {
            base_url: base_url.to_string(),
            client,
        }
    }

    pub async fn get_all(&self) -> Result<Vec<TicketSale>, Box<dyn Error>> {
        let url = format!("{}/api/TicketSales", self.base_url);
        let response = self.client.get(&url).send().await?;

        if response.status().is_success() {
            let json_text = response.text().await?;
            let sales = Self::parse_sales_with_references(&json_text)?;
            Ok(sales)
        } else {
            Err(format!("Failed to fetch ticket sales: {}", response.status()).into())
        }
    }

    pub async fn get_by_id(&self, id: i64) -> Result<TicketSale, Box<dyn Error>> {
        let url = format!("{}/api/TicketSales/{}", self.base_url, id);
        let response = self.client.get(&url).send().await?;

        if response.status().is_success() {
            let json_text = response.text().await?;
            let sale = Self::parse_single_sale_with_references(&json_text)?;
            Ok(sale)
        } else {
            Err(format!("Failed to fetch ticket sale: {}", response.status()).into())
        }
    }

    pub async fn search(
        &self,
        start_date: Option<&str>,
        end_date: Option<&str>,
    ) -> Result<Vec<TicketSale>, Box<dyn Error>> {
        let mut url = format!("{}/api/TicketSales/search", self.base_url);
        let mut params = vec![];

        if let Some(start) = start_date {
            params.push(format!("startDate={}", start));
        }
        if let Some(end) = end_date {
            params.push(format!("endDate={}", end));
        }

        if !params.is_empty() {
            url.push_str("?");
            url.push_str(&params.join("&"));
        }

        let response = self.client.get(&url).send().await?;

        if response.status().is_success() {
            let json_text = response.text().await?;
            let sales = Self::parse_sales_with_references(&json_text)?;
            Ok(sales)
        } else {
            Err(format!("Failed to search ticket sales: {}", response.status()).into())
        }
    }

    fn parse_sales_with_references(json_text: &str) -> Result<Vec<TicketSale>, Box<dyn Error>> {
        let json_value: Value = serde_json::from_str(json_text)?;

        // Build reference map
        let mut ref_map: HashMap<String, Value> = HashMap::new();
        Self::build_reference_map(&json_value, &mut ref_map);

        // Parse the array
        if let Value::Array(sales_array) = &json_value {
            let mut sales = Vec::new();
            for sale_value in sales_array {
                let sale = Self::parse_sale_object(sale_value, &ref_map)?;
                sales.push(sale);
            }
            Ok(sales)
        } else {
            Err("Expected array of sales".into())
        }
    }

    fn parse_single_sale_with_references(json_text: &str) -> Result<TicketSale, Box<dyn Error>> {
        let json_value: Value = serde_json::from_str(json_text)?;

        // Build reference map
        let mut ref_map: HashMap<String, Value> = HashMap::new();
        Self::build_reference_map(&json_value, &mut ref_map);

        Self::parse_sale_object(&json_value, &ref_map)
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

    fn parse_sale_object(
        value: &Value,
        ref_map: &HashMap<String, Value>,
    ) -> Result<TicketSale, Box<dyn Error>> {
        let resolved = Self::resolve_reference(value, ref_map);

        if let Value::Object(obj) = resolved {
            let sale_id = obj.get("SaleId").and_then(|v| v.as_i64()).unwrap_or(0);

            let sale_date = obj
                .get("SaleDate")
                .and_then(|v| v.as_str())
                .unwrap_or("")
                .to_string();

            let ticket_id = obj.get("TicketId").and_then(|v| v.as_i64()).unwrap_or(0);

            let ticket_sold_to_user = obj
                .get("TicketSoldToUser")
                .and_then(|v| v.as_str())
                .unwrap_or("")
                .to_string();

            let ticket_sold_to_user_phone = obj
                .get("TicketSoldToUserPhone")
                .and_then(|v| v.as_str())
                .unwrap_or("")
                .to_string();

            let bilet = obj
                .get("Bilet")
                .and_then(|v| Self::parse_ticket_object(v, ref_map).ok());

            Ok(TicketSale {
                sale_id,
                sale_date,
                ticket_id,
                bilet,
                ticket_sold_to_user,
                ticket_sold_to_user_phone,
            })
        } else {
            Err("Invalid sale object".into())
        }
    }

    fn parse_ticket_object(
        value: &Value,
        ref_map: &HashMap<String, Value>,
    ) -> Result<Ticket, Box<dyn Error>> {
        let resolved = Self::resolve_reference(value, ref_map);

        if let Value::Object(obj) = resolved {
            let ticket_id = obj.get("TicketId").and_then(|v| v.as_i64()).unwrap_or(0);

            let route_id = obj.get("RouteId").and_then(|v| v.as_i64()).unwrap_or(0);

            let ticket_price = obj
                .get("TicketPrice")
                .and_then(|v| v.as_f64())
                .unwrap_or(0.0);

            let marshut = obj
                .get("Marshut")
                .and_then(|v| Self::parse_marshut_object(v, ref_map).ok());

            Ok(Ticket {
                ticket_id,
                route_id,
                marshut,
                ticket_price,
                sales: None, // Avoid circular reference
            })
        } else {
            Err("Invalid ticket object".into())
        }
    }

    fn parse_marshut_object(
        value: &Value,
        ref_map: &HashMap<String, Value>,
    ) -> Result<Marshut, Box<dyn Error>> {
        let resolved = Self::resolve_reference(value, ref_map);

        if let Value::Object(obj) = resolved {
            let route_id = obj.get("RouteId").and_then(|v| v.as_i64()).unwrap_or(0);

            let start_point = obj
                .get("StartPoint")
                .and_then(|v| v.as_str())
                .unwrap_or("")
                .to_string();

            let end_point = obj
                .get("EndPoint")
                .and_then(|v| v.as_str())
                .unwrap_or("")
                .to_string();

            let driver_id = obj.get("DriverId").and_then(|v| v.as_i64()).unwrap_or(0);

            let bus_id = obj.get("BusId").and_then(|v| v.as_i64()).unwrap_or(0);

            let travel_time = obj
                .get("TravelTime")
                .and_then(|v| v.as_str())
                .unwrap_or("")
                .to_string();

            Ok(Marshut {
                route_id,
                start_point,
                end_point,
                driver_id,
                bus_id,
                travel_time,
            })
        } else {
            Err("Invalid marshut object".into())
        }
    }
}
