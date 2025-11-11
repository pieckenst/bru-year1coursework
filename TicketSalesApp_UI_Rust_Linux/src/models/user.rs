use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
pub enum UserRole {
    Administrator = 0,
    Cashier = 1,
    Controller = 2,
    SeniorCashier = 3,
}

impl UserRole {
    pub fn as_str(&self) -> &'static str {
        match self {
            UserRole::Administrator => "Администратор",
            UserRole::Cashier => "Кассир",
            UserRole::Controller => "Контролёр",
            UserRole::SeniorCashier => "Старший кассир",
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase")]
pub struct User {
    pub user_id: i64,
    pub username: String,
    pub role: i32,
    pub is_active: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase")]
pub struct LoginRequest {
    #[serde(rename = "Login")]
    pub username: String,
    pub password: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AuthResponse {
    pub token: String,
    pub user: Option<User>,
    pub message: Option<String>,
}
