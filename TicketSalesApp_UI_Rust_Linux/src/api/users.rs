use crate::models::{User, CreateUserRequest, UpdateUserRequest, Role, Permission};
use crate::api::{ApiClient, ApiError};
use serde_json::Value;
use std::collections::HashMap;

impl ApiClient {
    /// Get all users with ReferenceHandler.Preserve support
    pub async fn get_users(&self) -> Result<Vec<User>, ApiError> {
        let response = self.get("api/Users").await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            println!("[DEBUG Users] Raw response (first 500 chars): {}", 
                     if text.len() > 500 { &text[..500] } else { &text });
            
            let json: Value = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
            
            // Handle ReferenceHandler.Preserve format
            let root = json.as_object()
                .ok_or_else(|| ApiError::ServerError("Expected object".to_string()))?;
            
            // Get $values array
            let values_array = root.get("$values")
                .and_then(|v| v.as_array())
                .ok_or_else(|| ApiError::ServerError("Missing $values array".to_string()))?;
            
            println!("🔍 Found {} items in $values array", values_array.len());
            
            // First pass: collect all objects with $id
            let mut id_map: HashMap<String, &Value> = HashMap::new();
            for item in values_array {
                if let Some(obj) = item.as_object() {
                    if let Some(id) = obj.get("$id").and_then(|v| v.as_str()) {
                        id_map.insert(id.to_string(), item);
                    }
                }
            }
            
            // Second pass: parse users
            let mut users = Vec::new();
            for item in values_array {
                if let Some(obj) = item.as_object() {
                    // Skip $ref pointers
                    if obj.contains_key("$ref") {
                        continue;
                    }
                    
                    // Parse user fields
                    let get_i64 = |key: &str| obj.get(key).and_then(|v| v.as_i64());
                    let get_i32 = |key: &str| obj.get(key).and_then(|v| v.as_i64()).map(|n| n as i32);
                    let get_str = |key: &str| obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
                    let get_bool = |key: &str| obj.get(key).and_then(|v| v.as_bool());
                    
                    if let Some(user_id) = get_i64("userId") {
                        let user = User {
                            user_id,
                            login: get_str("login").unwrap_or_default(),
                            email: get_str("email"),
                            phone_number: get_str("phoneNumber"),
                            role: get_i32("role").unwrap_or(1),
                            is_active: get_bool("isActive").unwrap_or(true),
                            is_windows_auth: get_bool("isWindowsAuth").unwrap_or(false),
                            windows_identity: get_str("windowsIdentity"),
                            created_at: get_str("createdAt"),
                            last_login_at: get_str("lastLoginAt"),
                            user_roles: None, // Will be populated if needed
                            ref_id: get_str("$id"),
                        };
                        
                        println!("  ✓ Parsed user: {} (ID: {})", user.login, user.user_id);
                        users.push(user);
                    }
                }
            }
            
            users.sort_by_key(|u| u.user_id);
            println!("✅ Successfully parsed {} users\n", users.len());
            Ok(users)
        } else {
            let status = response.status();
            let error = response.text().await.unwrap_or_default();
            
            if status.as_u16() == 403 {
                Err(ApiError::Forbidden)
            } else {
                Err(ApiError::ServerError(format!("Failed to get users: {}", error)))
            }
        }
    }
    
    /// Get a specific user by ID with nested roles/permissions
    pub async fn get_user(&self, id: i64) -> Result<User, ApiError> {
        let endpoint = format!("api/Users/{}", id);
        let response = self.get(&endpoint).await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            let json: Value = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
            
            let obj = json.as_object()
                .ok_or_else(|| ApiError::ServerError("Expected object".to_string()))?;
            
            let get_i64 = |key: &str| obj.get(key).and_then(|v| v.as_i64());
            let get_i32 = |key: &str| obj.get(key).and_then(|v| v.as_i64()).map(|n| n as i32);
            let get_str = |key: &str| obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
            let get_bool = |key: &str| obj.get(key).and_then(|v| v.as_bool());
            
            let user = User {
                user_id: get_i64("userId").ok_or_else(|| ApiError::ServerError("Missing userId".to_string()))?,
                login: get_str("login").ok_or_else(|| ApiError::ServerError("Missing login".to_string()))?,
                email: get_str("email"),
                phone_number: get_str("phoneNumber"),
                role: get_i32("role").unwrap_or(1),
                is_active: get_bool("isActive").unwrap_or(true),
                is_windows_auth: get_bool("isWindowsAuth").unwrap_or(false),
                windows_identity: get_str("windowsIdentity"),
                created_at: get_str("createdAt"),
                last_login_at: get_str("lastLoginAt"),
                user_roles: None, // TODO: Parse nested user_roles if present
                ref_id: get_str("$id"),
            };
            
            Ok(user)
        } else {
            let status = response.status();
            let error = response.text().await.unwrap_or_default();
            
            if status.as_u16() == 403 {
                Err(ApiError::Forbidden)
            } else {
                Err(ApiError::ServerError(format!("Failed to get user: {}", error)))
            }
        }
    }
    
    /// Create a new user
    pub async fn create_user(&self, request: &CreateUserRequest) -> Result<User, ApiError> {
        let response = self.post("api/Users", request).await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            let json: Value = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
            
            let obj = json.as_object()
                .ok_or_else(|| ApiError::ServerError("Expected object".to_string()))?;
            
            let get_i64 = |key: &str| obj.get(key).and_then(|v| v.as_i64());
            let get_i32 = |key: &str| obj.get(key).and_then(|v| v.as_i64()).map(|n| n as i32);
            let get_str = |key: &str| obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
            let get_bool = |key: &str| obj.get(key).and_then(|v| v.as_bool());
            
            let user = User {
                user_id: get_i64("userId").ok_or_else(|| ApiError::ServerError("Missing userId".to_string()))?,
                login: get_str("login").unwrap_or_default(),
                email: get_str("email"),
                phone_number: get_str("phoneNumber"),
                role: get_i32("role").unwrap_or(1),
                is_active: get_bool("isActive").unwrap_or(true),
                is_windows_auth: get_bool("isWindowsAuth").unwrap_or(false),
                windows_identity: get_str("windowsIdentity"),
                created_at: get_str("createdAt"),
                last_login_at: get_str("lastLoginAt"),
                user_roles: None,
                ref_id: get_str("$id"),
            };
            
            Ok(user)
        } else {
            let status = response.status();
            let error = response.text().await.unwrap_or_default();
            
            if status.as_u16() == 403 {
                Err(ApiError::Forbidden)
            } else {
                Err(ApiError::ServerError(format!("Failed to create user: {}", error)))
            }
        }
    }
    
    /// Update an existing user
    pub async fn update_user(&self, id: i64, request: &UpdateUserRequest) -> Result<(), ApiError> {
        let endpoint = format!("api/Users/{}", id);
        let response = self.put(&endpoint, request).await?;
        
        if response.status().is_success() {
            Ok(())
        } else {
            let status = response.status();
            let error = response.text().await.unwrap_or_default();
            
            if status.as_u16() == 403 {
                Err(ApiError::Forbidden)
            } else {
                Err(ApiError::ServerError(format!("Failed to update user: {}", error)))
            }
        }
    }
    
    /// Delete a user
    pub async fn delete_user(&self, id: i64) -> Result<(), ApiError> {
        let endpoint = format!("api/Users/{}", id);
        let response = self.delete(&endpoint).await?;
        
        if response.status().is_success() {
            Ok(())
        } else {
            let status = response.status();
            let error = response.text().await.unwrap_or_default();
            
            if status.as_u16() == 403 {
                Err(ApiError::Forbidden)
            } else {
                Err(ApiError::ServerError(format!("Failed to delete user: {}", error)))
            }
        }
    }
    
    /// Get the current authenticated user
    pub async fn get_current_user(&self) -> Result<User, ApiError> {
        let response = self.get("api/Users/current").await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            let json: Value = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
            
            let obj = json.as_object()
                .ok_or_else(|| ApiError::ServerError("Expected object".to_string()))?;
            
            let get_i64 = |key: &str| obj.get(key).and_then(|v| v.as_i64());
            let get_i32 = |key: &str| obj.get(key).and_then(|v| v.as_i64()).map(|n| n as i32);
            let get_str = |key: &str| obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
            let get_bool = |key: &str| obj.get(key).and_then(|v| v.as_bool());
            
            let user = User {
                user_id: get_i64("userId").ok_or_else(|| ApiError::ServerError("Missing userId".to_string()))?,
                login: get_str("login").unwrap_or_default(),
                email: get_str("email"),
                phone_number: get_str("phoneNumber"),
                role: get_i32("role").unwrap_or(1),
                is_active: get_bool("isActive").unwrap_or(true),
                is_windows_auth: get_bool("isWindowsAuth").unwrap_or(false),
                windows_identity: get_str("windowsIdentity"),
                created_at: get_str("createdAt"),
                last_login_at: get_str("lastLoginAt"),
                user_roles: None,
                ref_id: get_str("$id"),
            };
            
            Ok(user)
        } else {
            let status = response.status();
            
            if status.as_u16() == 401 {
                Err(ApiError::Unauthorized)
            } else {
                let error = response.text().await.unwrap_or_default();
                Err(ApiError::ServerError(format!("Failed to get current user: {}", error)))
            }
        }
    }
    
    /// Get roles for a specific user
    pub async fn get_user_roles(&self, id: i64) -> Result<Vec<Role>, ApiError> {
        let endpoint = format!("api/Users/{}/roles", id);
        let response = self.get(&endpoint).await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            let json: Value = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
            
            // Handle ReferenceHandler.Preserve format
            let root = json.as_object()
                .ok_or_else(|| ApiError::ServerError("Expected object".to_string()))?;
            
            let values_array = root.get("$values")
                .and_then(|v| v.as_array())
                .ok_or_else(|| ApiError::ServerError("Missing $values array".to_string()))?;
            
            let mut roles = Vec::new();
            for item in values_array {
                if let Some(obj) = item.as_object() {
                    if obj.contains_key("$ref") {
                        continue;
                    }
                    
                    let get_str = |key: &str| obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
                    
                    if let Some(role_id) = get_str("roleId") {
                        let role = Role {
                            role_id,
                            role_name: get_str("roleName").unwrap_or_default(),
                            ref_id: get_str("$id"),
                        };
                        roles.push(role);
                    }
                }
            }
            
            Ok(roles)
        } else {
            let status = response.status();
            let error = response.text().await.unwrap_or_default();
            
            if status.as_u16() == 403 {
                Err(ApiError::Forbidden)
            } else {
                Err(ApiError::ServerError(format!("Failed to get user roles: {}", error)))
            }
        }
    }
    
    /// Get permissions for a specific user
    pub async fn get_user_permissions(&self, id: i64) -> Result<Vec<Permission>, ApiError> {
        let endpoint = format!("api/Users/{}/permissions", id);
        let response = self.get(&endpoint).await?;
        
        if response.status().is_success() {
            let text = response.text().await
                .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
            
            let json: Value = serde_json::from_str(&text)
                .map_err(|e| ApiError::ServerError(format!("Failed to parse JSON: {}", e)))?;
            
            // Handle ReferenceHandler.Preserve format
            let root = json.as_object()
                .ok_or_else(|| ApiError::ServerError("Expected object".to_string()))?;
            
            let values_array = root.get("$values")
                .and_then(|v| v.as_array())
                .ok_or_else(|| ApiError::ServerError("Missing $values array".to_string()))?;
            
            let mut permissions = Vec::new();
            for item in values_array {
                if let Some(obj) = item.as_object() {
                    if obj.contains_key("$ref") {
                        continue;
                    }
                    
                    let get_str = |key: &str| obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
                    
                    if let Some(permission_id) = get_str("permissionId") {
                        let permission = Permission {
                            permission_id,
                            permission_name: get_str("permissionName").unwrap_or_default(),
                            ref_id: get_str("$id"),
                        };
                        permissions.push(permission);
                    }
                }
            }
            
            Ok(permissions)
        } else {
            let status = response.status();
            let error = response.text().await.unwrap_or_default();
            
            if status.as_u16() == 403 {
                Err(ApiError::Forbidden)
            } else {
                Err(ApiError::ServerError(format!("Failed to get user permissions: {}", error)))
            }
        }
    }
    
    /// Assign a role to a user
    pub async fn assign_role_to_user(&self, user_id: i64, role_id: &str) -> Result<(), ApiError> {
        let endpoint = format!("api/Users/{}/roles/{}", user_id, role_id);
        let response = self.post_empty(&endpoint).await?;
        
        if response.status().is_success() {
            Ok(())
        } else {
            let status = response.status();
            let error = response.text().await.unwrap_or_default();
            
            if status.as_u16() == 403 {
                Err(ApiError::Forbidden)
            } else {
                Err(ApiError::ServerError(format!("Failed to assign role: {}", error)))
            }
        }
    }
    
    /// Remove a role from a user
    pub async fn remove_role_from_user(&self, user_id: i64, role_id: &str) -> Result<(), ApiError> {
        let endpoint = format!("api/Users/{}/roles/{}", user_id, role_id);
        let response = self.delete(&endpoint).await?;
        
        if response.status().is_success() {
            Ok(())
        } else {
            let status = response.status();
            let error = response.text().await.unwrap_or_default();
            
            if status.as_u16() == 403 {
                Err(ApiError::Forbidden)
            } else {
                Err(ApiError::ServerError(format!("Failed to remove role: {}", error)))
            }
        }
    }
}
