use super::{ApiClient, ApiError};
use crate::models::{AuthResponse, LoginRequest};

impl ApiClient {
    /// Login with username and password
    pub async fn login(&mut self, username: &str, password: &str) -> Result<AuthResponse, ApiError> {
        let request = LoginRequest {
            username: username.to_string(),
            password: password.to_string(),
        };

        let response = self.post("api/auth/login", &request).await?;
        let auth_response = Self::handle_response::<AuthResponse>(response).await?;

        // Store the token
        self.set_token(auth_response.token.clone());

        Ok(auth_response)
    }

    /// Logout and clear token
    pub fn logout(&mut self) {
        self.clear_token();
    }

    /// Check if user is authenticated
    pub fn is_authenticated(&self) -> bool {
        self.token.is_some()
    }
}
