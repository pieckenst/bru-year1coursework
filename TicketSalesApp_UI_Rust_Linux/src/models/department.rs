use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Department {
    // ReferenceHandler.Preserve metadata
    #[serde(rename = "$id", skip_serializing, default)]
    pub ref_id: Option<String>,
    #[serde(rename = "$ref", skip_serializing, default)]
    pub ref_pointer: Option<String>,
    
    #[serde(default)]
    pub department_id: i64,
    #[serde(default)]
    pub department_name: String,
    #[serde(default)]
    pub department_code: Option<String>,
    #[serde(default)]
    pub description: Option<String>,
    #[serde(default)]
    pub parent_department_id: Option<i64>,
    #[serde(default)]
    pub is_active: bool,
    
    // Catch-all for any extra fields including circular references
    #[serde(flatten)]
    pub extra: std::collections::HashMap<String, serde_json::Value>,
}

impl Department {
    pub fn new(department_id: i64, department_name: String) -> Self {
        Self {
            ref_id: None,
            ref_pointer: None,
            department_id,
            department_name,
            department_code: None,
            description: None,
            parent_department_id: None,
            is_active: true,
            extra: std::collections::HashMap::new(),
        }
    }
}
