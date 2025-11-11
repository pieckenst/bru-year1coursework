pub mod auth;
pub mod employees;
pub mod departments;
pub mod jobs;

use reqwest::{Client, Response, StatusCode};
use serde::{Deserialize, Serialize};
use std::error::Error;
use std::fmt;

/// Base API client for communicating with the ASP.NET backend
#[derive(Clone)]
pub struct ApiClient {
    base_url: String,
    client: Client,
    token: Option<String>,
}

#[derive(Debug)]
pub enum ApiError {
    NetworkError(String),
    AuthenticationError(String),
    NotFound(String),
    ServerError(String),
    ValidationError(String),
}

impl fmt::Display for ApiError {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        match self {
            ApiError::NetworkError(msg) => write!(f, "Network error: {}", msg),
            ApiError::AuthenticationError(msg) => write!(f, "Authentication error: {}", msg),
            ApiError::NotFound(msg) => write!(f, "Not found: {}", msg),
            ApiError::ServerError(msg) => write!(f, "Server error: {}", msg),
            ApiError::ValidationError(msg) => write!(f, "Validation error: {}", msg),
        }
    }
}

impl Error for ApiError {}

impl ApiClient {
    pub fn new(base_url: &str) -> Self {
        Self {
            base_url: base_url.to_string(),
            client: Client::new(),
            token: None,
        }
    }

    pub fn set_token(&mut self, token: String) {
        self.token = Some(token);
    }

    pub fn get_token(&self) -> Option<&str> {
        self.token.as_deref()
    }

    pub fn clear_token(&mut self) {
        self.token = None;
    }

    /// Build a GET request with authentication
    pub(crate) async fn get(&self, endpoint: &str) -> Result<Response, ApiError> {
        let url = format!("{}/{}", self.base_url, endpoint);
        let mut request = self.client.get(&url);

        if let Some(token) = &self.token {
            request = request.bearer_auth(token);
        }

        request
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))
    }

    /// Build a POST request with authentication
    pub(crate) async fn post<T: Serialize>(
        &self,
        endpoint: &str,
        body: &T,
    ) -> Result<Response, ApiError> {
        let url = format!("{}/{}", self.base_url, endpoint);
        let mut request = self.client.post(&url).json(body);

        if let Some(token) = &self.token {
            request = request.bearer_auth(token);
        }

        request
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))
    }

    /// Build a PUT request with authentication
    pub(crate) async fn put<T: Serialize>(
        &self,
        endpoint: &str,
        body: &T,
    ) -> Result<Response, ApiError> {
        let url = format!("{}/{}", self.base_url, endpoint);
        let mut request = self.client.put(&url).json(body);

        if let Some(token) = &self.token {
            request = request.bearer_auth(token);
        }

        request
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))
    }

    /// Build a DELETE request with authentication
    pub(crate) async fn delete(&self, endpoint: &str) -> Result<Response, ApiError> {
        let url = format!("{}/{}", self.base_url, endpoint);
        let mut request = self.client.delete(&url);

        if let Some(token) = &self.token {
            request = request.bearer_auth(token);
        }

        request
            .send()
            .await
            .map_err(|e| ApiError::NetworkError(e.to_string()))
    }

    /// Handle common response status codes
    pub(crate) async fn handle_response<T: for<'de> Deserialize<'de>>(
        response: Response,
    ) -> Result<T, ApiError> {
        match response.status() {
            StatusCode::OK | StatusCode::CREATED => {
                // Debug: print raw response text
                let text = response.text().await
                    .map_err(|e| ApiError::ServerError(format!("Failed to read response: {}", e)))?;
                println!("[DEBUG] Raw response (first 500 chars): {}", &text[..text.len().min(500)]);
                
                serde_json::from_str::<T>(&text)
                    .map_err(|e| ApiError::ServerError(format!("Failed to parse response: {}", e)))
            },
            StatusCode::UNAUTHORIZED => Err(ApiError::AuthenticationError(
                "Unauthorized access".to_string(),
            )),
            StatusCode::NOT_FOUND => {
                let msg = response.text().await.unwrap_or_default();
                Err(ApiError::NotFound(msg))
            }
            StatusCode::BAD_REQUEST => {
                let msg = response.text().await.unwrap_or_default();
                Err(ApiError::ValidationError(msg))
            }
            _ => {
                let msg = response.text().await.unwrap_or_default();
                Err(ApiError::ServerError(format!(
                    "Server error: {}",
                    msg
                )))
            }
        }
    }
}
