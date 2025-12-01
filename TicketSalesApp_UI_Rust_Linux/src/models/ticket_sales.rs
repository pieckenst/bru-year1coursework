use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase")]
pub struct TicketSale {
    pub sale_id: i64,
    pub sale_date: String,
    pub ticket_id: i64,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub bilet: Option<Ticket>,
    pub ticket_sold_to_user: String,
    pub ticket_sold_to_user_phone: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase")]
pub struct Ticket {
    pub ticket_id: i64,
    pub route_id: i64,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub marshut: Option<Marshut>,
    pub ticket_price: f64,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub sales: Option<Vec<TicketSale>>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase")]
pub struct Marshut {
    pub route_id: i64,
    pub start_point: String,
    pub end_point: String,
    pub driver_id: i64,
    pub bus_id: i64,
    pub travel_time: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MonthlyIncome {
    pub month: String,
    pub year: i32,
    pub total_income: f64,
    pub tickets_sold: i32,
    pub average_ticket_price: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct RouteIncome {
    pub route_name: String,
    pub total_income: f64,
    pub tickets_sold: i32,
    pub average_ticket_price: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct RouteStatistic {
    pub route_name: String,
    pub total_sales: i32,
    pub total_revenue: f64,
    pub sales_percentage: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DailyStatistic {
    pub date: String,
    pub total_sales: i32,
    pub total_revenue: f64,
    pub growth_rate: f64,
}
