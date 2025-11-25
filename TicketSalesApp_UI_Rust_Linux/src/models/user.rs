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
    
    pub fn from_i32(value: i32) -> Option<Self> {
        match value {
            0 => Some(UserRole::Administrator),
            1 => Some(UserRole::Cashier),
            2 => Some(UserRole::Controller),
            3 => Some(UserRole::SeniorCashier),
            _ => None,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct User {
    #[serde(rename = "userId")]
    pub user_id: i64,
    
    #[serde(rename = "login")]
    pub login: String,
    
    #[serde(rename = "email")]
    pub email: Option<String>,
    
    #[serde(rename = "phoneNumber")]
    pub phone_number: Option<String>,
    
    #[serde(rename = "role")]
    pub role: i32,
    
    #[serde(rename = "isActive")]
    pub is_active: bool,
    
    #[serde(rename = "isWindowsAuth")]
    pub is_windows_auth: bool,
    
    #[serde(rename = "windowsIdentity")]
    pub windows_identity: Option<String>,
    
    #[serde(rename = "createdAt")]
    pub created_at: Option<String>,
    
    #[serde(rename = "lastLoginAt")]
    pub last_login_at: Option<String>,
    
    #[serde(rename = "userRoles")]
    pub user_roles: Option<Vec<UserRoleAssignment>>,
    
    #[serde(skip_serializing_if = "Option::is_none", rename = "$id")]
    pub ref_id: Option<String>,
}

impl User {
    pub fn role_name(&self) -> String {
        UserRole::from_i32(self.role)
            .map(|r| r.as_str().to_string())
            .unwrap_or_else(|| "Неизвестно".to_string())
    }
    
    pub fn is_admin(&self) -> bool {
        self.role == 0
    }
    
    pub fn display_name(&self) -> String {
        format!("{} ({})", self.login, self.role_name())
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UserRoleAssignment {
    #[serde(rename = "userRoleId")]
    pub user_role_id: i64,
    
    #[serde(rename = "userId")]
    pub user_id: i64,
    
    #[serde(rename = "roleId")]
    pub role_id: String,
    
    #[serde(rename = "role")]
    pub role: Option<Role>,
    
    #[serde(skip_serializing_if = "Option::is_none", rename = "$id")]
    pub ref_id: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Role {
    #[serde(rename = "roleId")]
    pub role_id: String,
    
    #[serde(rename = "roleName")]
    pub role_name: String,
    
    #[serde(skip_serializing_if = "Option::is_none", rename = "$id")]
    pub ref_id: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Permission {
    #[serde(rename = "permissionId")]
    pub permission_id: String,
    
    #[serde(rename = "permissionName")]
    pub permission_name: String,
    
    #[serde(skip_serializing_if = "Option::is_none", rename = "$id")]
    pub ref_id: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CreateUserRequest {
    #[serde(rename = "Login")]
    pub login: String,
    
    #[serde(rename = "Password")]
    pub password: String,
    
    #[serde(rename = "Role")]
    pub role: i32,
    
    #[serde(rename = "PhoneNumber")]
    pub phone_number: Option<String>,
    
    #[serde(rename = "Email")]
    pub email: Option<String>,
    
    #[serde(rename = "IsWindowsAuth")]
    pub is_windows_auth: bool,
    
    #[serde(rename = "WindowsIdentity")]
    pub windows_identity: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UpdateUserRequest {
    #[serde(rename = "Login")]
    pub login: Option<String>,
    
    #[serde(rename = "Password")]
    pub password: Option<String>,
    
    #[serde(rename = "Role")]
    pub role: Option<i32>,
    
    #[serde(rename = "PhoneNumber")]
    pub phone_number: Option<String>,
    
    #[serde(rename = "Email")]
    pub email: Option<String>,
    
    #[serde(rename = "IsActive")]
    pub is_active: Option<bool>,
    
    #[serde(rename = "IsWindowsAuth")]
    pub is_windows_auth: Option<bool>,
    
    #[serde(rename = "WindowsIdentity")]
    pub windows_identity: Option<String>,
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
