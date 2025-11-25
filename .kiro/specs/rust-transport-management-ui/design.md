# Design Document

## Overview

This design document outlines the architecture and implementation approach for adding Bus Management, Route Management, Route Schedules Management, Jobs Management, and Users Management functionality to the Rust/Slint-based Linux application. The implementation will follow the existing patterns established in the employee management module while adapting to the specific needs of transportation and administrative management.

The system will provide five main management interfaces:
1. **Bus Management**: CRUD operations for fleet vehicles
2. **Route Management**: CRUD operations for transportation routes with driver and bus assignments
3. **Route Schedules**: Complex schedule management with multiple stops, times, and pricing
4. **Jobs Management**: CRUD operations for job positions with administrator-only access
5. **Users Management**: CRUD operations for system users with role-based access control

## Architecture

### High-Level Architecture

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                            Slint UI Layer                                    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────┐  │
│  │ Bus Mgmt │  │  Route   │  │  Route   │  │   Jobs   │  │    Users     │  │
│  │    UI    │  │ Mgmt UI  │  │Schedules │  │ Mgmt UI  │  │   Mgmt UI    │  │
│  │ (Slint)  │  │ (Slint)  │  │   UI     │  │ (Slint)  │  │   (Slint)    │  │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘  └──────────────┘  │
└──────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                          Rust Application Layer                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │              Navigation & State Management                             │  │
│  │              (Role-based access control for admin features)            │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                       API Client Layer                                 │  │
│  │           (Existing ApiClient with JWT Authentication)                 │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                          Data Models                                   │  │
│  │  (Bus, Route, RouteSchedule, Job, User, Role with Serde)              │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                         REST API (localhost:5000)                            │
│  /api/Buses, /api/Routes, /api/RouteSchedules, /api/Jobs, /api/Users        │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

1. **Slint UI Components**: Handle user interaction, display data, and trigger callbacks
2. **Rust Application Logic**: Process callbacks, manage state, coordinate API calls
3. **API Client**: Handle HTTP requests with authentication and error handling
4. **Data Models**: Serialize/deserialize JSON data with proper type safety

## Components and Interfaces

### 1. Slint UI Components

#### BusManagementView (bus_management.slint)

```slint
export struct BusData {
    id: int,
    model: string,
}

export component BusManagementView {
    in-out property <[BusData]> buses;
    in-out property <string> search-text;
    in-out property <bool> is-loading;
    in-out property <string> error-message;
    in-out property <bool> show-add-dialog;
    in-out property <bool> show-edit-dialog;
    in-out property <bool> show-delete-dialog;
    in-out property <string> dialog-bus-model;
    in-out property <int> selected-bus-id;
    
    callback refresh-clicked();
    callback add-clicked();
    callback edit-clicked(int);
    callback delete-clicked(int);
    callback save-bus(string);
    callback confirm-delete();
}
```

#### RouteManagementView (route_management.slint)

```slint
export struct RouteData {
    id: int,
    start-point: string,
    end-point: string,
    bus-model: string,
    driver-name: string,
    travel-time: string,
}

export component RouteManagementView {
    in-out property <[RouteData]> routes;
    in-out property <[string]> bus-options;
    in-out property <[string]> driver-options;
    in-out property <string> search-text;
    in-out property <bool> is-loading;
    in-out property <string> error-message;
    in-out property <bool> show-add-dialog;
    in-out property <bool> show-edit-dialog;
    in-out property <bool> show-delete-dialog;
    
    // Dialog fields
    in-out property <string> dialog-start-point;
    in-out property <string> dialog-end-point;
    in-out property <string> dialog-travel-time;
    in-out property <int> dialog-selected-bus-index;
    in-out property <int> dialog-selected-driver-index;
    in-out property <int> selected-route-id;
    
    callback refresh-clicked();
    callback add-clicked();
    callback edit-clicked(int);
    callback delete-clicked(int);
    callback save-route();
    callback confirm-delete();
}
```

#### RouteSchedulesView (route_schedules.slint)

```slint
export struct ScheduleData {
    id: int,
    start-point: string,
    end-point: string,
    departure-time: string,
    arrival-time: string,
    price: float,
    available-seats: int,
    route-stops: string,
}

export component RouteSchedulesView {
    in-out property <[RouteData]> routes;
    in-out property <[ScheduleData]> schedules;
    in-out property <[string]> available-stops;
    in-out property <int> selected-route-index;
    in-out property <string> selected-date;
    in-out property <bool> is-loading;
    in-out property <string> error-message;
    in-out property <bool> show-add-dialog;
    in-out property <bool> show-edit-dialog;
    in-out property <bool> show-delete-dialog;
    
    // Dialog fields
    in-out property <string> dialog-departure-time;
    in-out property <string> dialog-arrival-time;
    in-out property <float> dialog-price;
    in-out property <int> dialog-seats;
    in-out property <[int]> dialog-selected-stops;
    in-out property <int> selected-schedule-id;
    
    callback route-selected(int);
    callback date-changed(string);
    callback refresh-clicked();
    callback add-clicked();
    callback edit-clicked(int);
    callback delete-clicked(int);
    callback save-schedule();
    callback confirm-delete();
}
```

#### JobsManagementView (jobs_management.slint)

```slint
export struct JobData {
    id: int,
    title: string,
    description: string,
    salary: float,
}

export component JobsManagementView {
    in-out property <[JobData]> jobs;
    in-out property <string> search-text;
    in-out property <bool> is-loading;
    in-out property <string> error-message;
    in-out property <bool> show-add-dialog;
    in-out property <bool> show-edit-dialog;
    in-out property <bool> show-delete-dialog;
    in-out property <bool> is-admin;
    
    // Dialog fields
    in-out property <string> dialog-job-title;
    in-out property <string> dialog-job-description;
    in-out property <float> dialog-base-salary;
    in-out property <int> selected-job-id;
    
    callback refresh-clicked();
    callback add-clicked();
    callback edit-clicked(int);
    callback delete-clicked(int);
    callback save-job();
    callback confirm-delete();
}
```

#### UsersManagementView (users_management.slint)

```slint
export struct UserData {
    id: int,
    login: string,
    email: string,
    phone: string,
    role: int,
    role-name: string,
    is-active: bool,
    is-windows-auth: bool,
    windows-identity: string,
    created-at: string,
    last-login-at: string,
}

export struct RoleOption {
    value: int,
    label: string,
}

export component UsersManagementView {
    in-out property <[UserData]> users;
    in-out property <[RoleOption]> role-options;
    in-out property <string> search-text;
    in-out property <bool> is-loading;
    in-out property <string> error-message;
    in-out property <bool> show-add-dialog;
    in-out property <bool> show-edit-dialog;
    in-out property <bool> show-delete-dialog;
    in-out property <bool> is-admin;
    in-out property <int> current-user-id;
    
    // Dialog fields
    in-out property <string> dialog-login;
    in-out property <string> dialog-password;
    in-out property <string> dialog-email;
    in-out property <string> dialog-phone;
    in-out property <int> dialog-role-index;
    in-out property <bool> dialog-is-active;
    in-out property <bool> dialog-is-windows-auth;
    in-out property <string> dialog-windows-identity;
    in-out property <int> selected-user-id;
    
    callback refresh-clicked();
    callback add-clicked();
    callback edit-clicked(int);
    callback delete-clicked(int);
    callback save-user();
    callback confirm-delete();
    callback view-roles-clicked(int);
    callback view-permissions-clicked(int);
}
```

### 2. Rust API Client Extensions

The existing `ApiClient` will be extended with new methods:

```rust
impl ApiClient {
    // Bus operations
    pub async fn get_buses(&self) -> Result<Vec<Bus>, ApiError>;
    pub async fn get_bus(&self, id: i64) -> Result<Bus, ApiError>;
    pub async fn create_bus(&self, request: &CreateBusRequest) -> Result<Bus, ApiError>;
    pub async fn update_bus(&self, id: i64, request: &UpdateBusRequest) -> Result<(), ApiError>;
    pub async fn delete_bus(&self, id: i64) -> Result<(), ApiError>;
    
    // Route operations
    pub async fn get_routes(&self) -> Result<Vec<Route>, ApiError>;
    pub async fn get_route(&self, id: i64) -> Result<Route, ApiError>;
    pub async fn create_route(&self, request: &CreateRouteRequest) -> Result<Route, ApiError>;
    pub async fn update_route(&self, id: i64, request: &UpdateRouteRequest) -> Result<(), ApiError>;
    pub async fn delete_route(&self, id: i64) -> Result<(), ApiError>;
    
    // Route Schedule operations
    pub async fn get_route_schedules(&self, route_id: i64, date: &str) -> Result<Vec<RouteSchedule>, ApiError>;
    pub async fn get_route_schedule(&self, id: i64) -> Result<RouteSchedule, ApiError>;
    pub async fn create_route_schedule(&self, request: &CreateRouteScheduleRequest) -> Result<RouteSchedule, ApiError>;
    pub async fn update_route_schedule(&self, id: i64, request: &UpdateRouteScheduleRequest) -> Result<(), ApiError>;
    pub async fn delete_route_schedule(&self, id: i64) -> Result<(), ApiError>;
    
    // Job operations
    pub async fn get_jobs(&self) -> Result<Vec<Job>, ApiError>;
    pub async fn get_job(&self, id: i32) -> Result<Job, ApiError>;
    pub async fn create_job(&self, request: &CreateJobRequest) -> Result<Job, ApiError>;
    pub async fn update_job(&self, id: i32, request: &UpdateJobRequest) -> Result<(), ApiError>;
    pub async fn delete_job(&self, id: i32) -> Result<(), ApiError>;
    pub async fn search_jobs(&self, job_title: Option<&str>, internship: Option<&str>) -> Result<Vec<Job>, ApiError>;
    
    // User operations
    pub async fn get_users(&self) -> Result<Vec<User>, ApiError>;
    pub async fn get_user(&self, id: i64) -> Result<User, ApiError>;
    pub async fn create_user(&self, request: &CreateUserRequest) -> Result<User, ApiError>;
    pub async fn update_user(&self, id: i64, request: &UpdateUserRequest) -> Result<(), ApiError>;
    pub async fn delete_user(&self, id: i64) -> Result<(), ApiError>;
    pub async fn get_current_user(&self) -> Result<User, ApiError>;
    pub async fn get_user_roles(&self, id: i64) -> Result<Vec<Role>, ApiError>;
    pub async fn get_user_permissions(&self, id: i64) -> Result<Vec<Permission>, ApiError>;
    pub async fn assign_role_to_user(&self, user_id: i64, role_id: &str) -> Result<(), ApiError>;
    pub async fn remove_role_from_user(&self, user_id: i64, role_id: &str) -> Result<(), ApiError>;
}
```

### 3. Navigation Integration

The existing navigation system will be extended with new routes:

```rust
pub enum AppRoute {
    // Existing routes
    Dashboard,
    Employees,
    // Transport management routes
    BusManagement,
    RouteManagement,
    RouteSchedules,
    // Administrative routes
    JobsManagement,
    UsersManagement,
    // ... other routes
}

impl AppRoute {
    pub fn from_indices(group: i32, index: i32) -> Option<Self> {
        match (group, index) {
            (0, 0) => Some(Self::Dashboard),
            (1, 0) => Some(Self::Employees),
            (2, 0) => Some(Self::BusManagement),
            (2, 1) => Some(Self::RouteManagement),
            (2, 2) => Some(Self::RouteSchedules),
            (3, 0) => Some(Self::JobsManagement),
            (3, 1) => Some(Self::UsersManagement),
            _ => None,
        }
    }
    
    pub fn requires_admin(&self) -> bool {
        matches!(self, Self::UsersManagement)
    }
}
```

## Data Models

The data models exist in `src/models/` and include:

1. **Bus** (`bus.rs`): Represents a vehicle with ID and model
2. **Route** (`route.rs`): Represents a route with start/end points, driver, and bus
3. **RouteSchedule** (`route_schedule.rs`): Represents a schedule with times, stops, and pricing
4. **Job** (`job.rs`): Represents a job position with ID, title, description, and base salary
5. **User** (`user.rs`): Represents a system user with ID, login, role, email, phone, and authentication settings

### Job Model

```rust
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Job {
    #[serde(rename = "jobId")]
    pub job_id: i32,
    
    #[serde(rename = "jobTitle")]
    pub job_title: String,
    
    #[serde(rename = "jobDescription")]
    pub job_description: Option<String>,
    
    #[serde(rename = "baseSalary")]
    pub base_salary: Option<f64>,
    
    #[serde(skip_serializing_if = "Option::is_none", rename = "$id")]
    pub ref_id: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CreateJobRequest {
    #[serde(rename = "jobTitle")]
    pub job_title: String,
    
    #[serde(rename = "jobDescription")]
    pub job_description: Option<String>,
    
    #[serde(rename = "baseSalary")]
    pub base_salary: Option<f64>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UpdateJobRequest {
    #[serde(rename = "jobId")]
    pub job_id: i32,
    
    #[serde(rename = "jobTitle")]
    pub job_title: String,
    
    #[serde(rename = "jobDescription")]
    pub job_description: Option<String>,
    
    #[serde(rename = "baseSalary")]
    pub base_salary: Option<f64>,
}
```

### User Model

```rust
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
    pub user_roles: Option<Vec<UserRole>>,
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
pub struct Role {
    #[serde(rename = "roleId")]
    pub role_id: String,
    
    #[serde(rename = "roleName")]
    pub role_name: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Permission {
    #[serde(rename = "permissionId")]
    pub permission_id: String,
    
    #[serde(rename = "permissionName")]
    pub permission_name: String,
}
```

These models include:
- Serde serialization/deserialization with proper field naming
- Handling of circular references ($ref, $id)
- Helper methods for display and validation
- Request/response DTOs for API operations

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Navigation triggers view rendering
*For any* navigation state change to any management view (bus, route, schedule, jobs, users), the system should render the appropriate view with the correct data loading initiated.
**Validates: Requirements 1.1, 5.1, 14.1, 14.2, 14.3, 19.1, 24.1, 31.1, 31.2**

### Property 2: Refresh triggers API reload
*For any* management view (bus, route, schedule, jobs, users), clicking the refresh button should trigger an API request to reload the data for that view.
**Validates: Requirements 1.3, 5.4, 9.4, 19.3, 24.4**

### Property 3: API errors display error messages
*For any* API request that fails, the system should display an error message to the user indicating the failure.
**Validates: Requirements 1.4, 2.5, 3.5, 4.5, 5.5, 6.6, 12.5, 18.3, 19.4, 20.5, 21.5, 22.5, 24.5, 25.7, 26.6, 27.7, 29.4**

### Property 4: Form submission with valid data triggers API call
*For any* create/edit form with all required fields filled with valid data, clicking save should trigger the appropriate POST/PUT API request with the form data.
**Validates: Requirements 2.2, 3.2, 6.3, 7.3, 10.3, 11.3, 20.2, 21.2, 25.3, 26.3**

### Property 5: Empty/whitespace validation prevents submission
*For any* form field that requires non-empty input, attempting to submit with empty or whitespace-only values should prevent submission and display a validation error.
**Validates: Requirements 2.4, 3.4, 20.4, 21.4, 25.6**

### Property 6: Successful operations close dialogs and refresh data
*For any* successful create/update/delete operation, the system should close the dialog (if open) and refresh the data list.
**Validates: Requirements 2.3, 3.3, 4.3, 6.4, 7.4, 8.3, 10.4, 11.4, 12.3, 18.2, 20.3, 21.3, 22.3, 25.4, 26.4, 27.3**

### Property 7: Cancel closes dialog without API calls
*For any* dialog with a cancel button, clicking cancel should close the dialog without triggering any API calls.
**Validates: Requirements 2.6, 4.4, 8.4, 12.4, 20.6, 22.4, 27.4**

### Property 8: Edit dialogs pre-populate with existing data
*For any* record being edited, opening the edit dialog should pre-populate all fields with the current values from that record.
**Validates: Requirements 3.1, 7.1, 11.1, 21.1, 26.1**

### Property 9: Delete confirmation shows record identifier
*For any* record being deleted, the confirmation dialog should display identifying information about that record (e.g., bus model, route endpoints, job title, user login).
**Validates: Requirements 4.1, 8.1, 12.1, 22.1, 27.1**

### Property 10: Required field validation prevents submission
*For any* form with required fields, attempting to submit with any required field missing should prevent submission and display validation errors for all missing fields.
**Validates: Requirements 6.5, 7.5, 10.5**

### Property 11: Multi-select requires minimum selections
*For any* route schedule form, attempting to save with fewer than two route stops selected should prevent submission and display a validation error.
**Validates: Requirements 10.6, 11.5**

### Property 12: Dialog loading populates dropdown data
*For any* dialog that requires dropdown selections (buses, drivers), opening the dialog should trigger API calls to load the available options.
**Validates: Requirements 6.2, 7.2, 10.2**

### Property 13: Search filters display in real-time
*For any* search text entered in a management view, the displayed list should filter in real-time to show only matching records without requiring a button click.
**Validates: Requirements 13.1, 13.2, 13.5**

### Property 14: Clear search restores full list
*For any* filtered list, clearing the search field should restore the display to show all records (round-trip property).
**Validates: Requirements 13.3**

### Property 15: Navigation preserves authentication
*For any* navigation between views, the authentication state and token should remain unchanged (invariant).
**Validates: Requirements 14.4**

### Property 16: Navigation loads appropriate data
*For any* navigation to a new view, the system should trigger data loading appropriate for that view.
**Validates: Requirements 14.5**

### Property 17: All API calls use authenticated client
*For any* API request made by the system, it should use the existing ApiClient instance with authentication tokens (invariant).
**Validates: Requirements 15.1**

### Property 18: JSON deserialization succeeds for valid data
*For any* valid JSON response from the API matching the expected schema, deserialization should succeed and produce the correct model instances.
**Validates: Requirements 16.1**

### Property 19: JSON serialization produces correct format
*For any* model instance being sent to the API, serialization should produce JSON in the format expected by the API with correct field names.
**Validates: Requirements 16.3**

### Property 20: Date serialization uses ISO 8601
*For any* date/time field being serialized, the output should be in ISO 8601 format.
**Validates: Requirements 16.4**

### Property 21: Null optional fields handled correctly
*For any* model with optional fields set to None, serialization should either omit those fields or serialize them as null appropriately.
**Validates: Requirements 16.5**

### Property 22: Loading state disables actions
*For any* async operation in progress, the system should display a loading indicator and disable action buttons to prevent duplicate requests.
**Validates: Requirements 18.1, 18.5**

### Property 23: Validation errors highlight fields
*For any* form validation failure, the system should display error messages that explain the validation requirements.
**Validates: Requirements 18.4**

### Property 24: Schedule data includes all required fields
*For any* route schedule displayed, it should show departure time, arrival time, price, available seats, and route stops.
**Validates: Requirements 9.3**

### Property 25: Route data includes nested relationships
*For any* route loaded from the API, it should include the related bus and employee data.
**Validates: Requirements 5.3**

### Property 26: Search filters by multiple fields
*For any* search text entered in jobs management, the displayed list should filter to show only jobs where the title OR description contains the search text.
**Validates: Requirements 23.1**

### Property 27: Search clear restores full list
*For any* filtered list in any management view, clearing the search field should restore the display to show all records (round-trip property).
**Validates: Requirements 13.3, 23.2**

### Property 28: Real-time search updates display
*For any* search text change in any management view, the system should update the filtered display immediately without requiring a button click.
**Validates: Requirements 13.5, 23.4**

### Property 29: Search API includes query parameters
*For any* search request sent to the jobs API, the request should include jobTitle and internship query parameters with the search text.
**Validates: Requirements 23.5**

### Property 30: User data includes roles and permissions
*For any* user loaded from the API, it should include the user's assigned roles and effective permissions.
**Validates: Requirements 24.3**

### Property 31: Unique login validation
*For any* user creation or update attempt with a login that already exists in the system, the system should prevent submission and display a validation error indicating the login is taken.
**Validates: Requirements 25.5, 26.5**

### Property 32: Self-deletion prevention
*For any* user attempting to delete their own account, the system should prevent the deletion and display an error message.
**Validates: Requirements 27.5**

### Property 33: Last admin deletion prevention
*For any* deletion attempt on a user with administrator role, if that user is the last administrator in the system, the system should prevent the deletion and display an error message.
**Validates: Requirements 27.6**

### Property 34: Role and permission display
*For any* user details view, the system should display both the user's assigned roles and their effective permissions derived from those roles.
**Validates: Requirements 28.1, 28.2, 29.5**

### Property 35: Role assignment triggers API call
*For any* role assignment or removal operation, the system should send the appropriate POST or DELETE request to the API with the user ID and role ID.
**Validates: Requirements 28.3, 28.4**

### Property 36: Role changes refresh display
*For any* successful role assignment or removal, the system should refresh the user's role and permission display to reflect the changes.
**Validates: Requirements 28.5**

### Property 37: Current user loaded on startup
*For any* application startup with a valid authentication token, the system should fetch and display the current user's information including login, role, and permissions.
**Validates: Requirements 29.1, 29.2**

### Property 38: Invalid token triggers re-authentication
*For any* API request with an invalid or expired authentication token, the system should receive a 401 Unauthorized response and redirect the user to the login screen.
**Validates: Requirements 29.3**

### Property 39: Administrator-only operations enforce authorization
*For any* operation that requires administrator privileges (job modifications, user management), if the current user is not an administrator, the system should return a 403 Forbidden response.
**Validates: Requirements 22.6, 24.6, 30.1, 30.2, 30.3, 30.4, 30.5**

### Property 40: Forbidden responses display permission denied
*For any* 403 Forbidden response received from the API, the system should display a clear permission denied message to the user.
**Validates: Requirements 30.6**

### Property 41: Admin-only navigation visibility
*For any* user who is not an administrator, the Users Management navigation item should be hidden or disabled in the navigation menu.
**Validates: Requirements 31.5**

## Error Handling

### Error Types

1. **Network Errors**: Connection failures, timeouts
2. **HTTP Errors**: 4xx and 5xx status codes
3. **Validation Errors**: Client-side form validation failures
4. **Serialization Errors**: JSON parsing failures

### Error Handling Strategy

```rust
pub enum ApiError {
    NetworkError(String),
    HttpError { status: u16, message: String },
    ValidationError(String),
    SerializationError(String),
    Unauthorized,
    Forbidden,
    Timeout,
}

impl ApiError {
    pub fn user_message(&self) -> String {
        match self {
            Self::NetworkError(_) => "Ошибка подключения к серверу".to_string(),
            Self::HttpError { status, message } => {
                format!("Ошибка сервера ({}): {}", status, message)
            }
            Self::ValidationError(msg) => format!("Ошибка валидации: {}", msg),
            Self::SerializationError(_) => "Ошибка обработки данных".to_string(),
            Self::Unauthorized => "Требуется авторизация".to_string(),
            Self::Forbidden => "Доступ запрещен".to_string(),
            Self::Timeout => "Превышено время ожидания".to_string(),
        }
    }
}
```

### Error Display

Errors will be displayed in two contexts:
1. **In-dialog errors**: Shown within the dialog for form-related errors
2. **View-level errors**: Shown at the top of the view for data loading errors

## Testing Strategy

### Unit Testing

Unit tests will cover:
1. **Model serialization/deserialization**: Verify JSON conversion works correctly
2. **Validation logic**: Test form validation rules
3. **Helper methods**: Test display name generation, formatting functions
4. **Error handling**: Test error type conversions and message generation

Example unit tests:
```rust
#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_bus_serialization() {
        let bus = Bus::new(1, "МАЗ-103".to_string());
        let json = serde_json::to_string(&bus).unwrap();
        assert!(json.contains("\"busId\":1"));
        assert!(json.contains("\"model\":\"МАЗ-103\""));
    }
    
    #[test]
    fn test_route_display_name() {
        let route = Route::new(1, "Могилев".to_string(), "Минск".to_string(), 10, 5);
        assert_eq!(route.display_name(), "#1 Могилев → Минск");
    }
    
    #[test]
    fn test_empty_model_validation() {
        let request = CreateBusRequest { model: "".to_string() };
        assert!(validate_bus_request(&request).is_err());
    }
}
```

### Property-Based Testing

Property-based tests will use the `proptest` crate to verify universal properties across many randomly generated inputs. Each property test will run a minimum of 100 iterations.

The property-based testing library for Rust is **proptest**.

Example property tests:
```rust
use proptest::prelude::*;

proptest! {
    #[test]
    fn prop_bus_round_trip_serialization(model in "\\PC+") {
        let bus = Bus::new(1, model.clone());
        let json = serde_json::to_string(&bus).unwrap();
        let deserialized: Bus = serde_json::from_str(&json).unwrap();
        assert_eq!(bus.model, deserialized.model);
    }
    
    #[test]
    fn prop_whitespace_model_rejected(whitespace in "\\s+") {
        let request = CreateBusRequest { model: whitespace };
        assert!(validate_bus_request(&request).is_err());
    }
    
    #[test]
    fn prop_search_filter_subset(
        buses in prop::collection::vec(any::<Bus>(), 0..100),
        search_term in "\\PC{1,10}"
    ) {
        let filtered = filter_buses(&buses, &search_term);
        assert!(filtered.len() <= buses.len());
        for bus in filtered {
            assert!(bus.model.contains(&search_term));
        }
    }
}
```

### Integration Testing

Integration tests will verify:
1. **API client methods**: Test actual HTTP requests (with mock server)
2. **Navigation flow**: Test view switching and data loading
3. **Dialog workflows**: Test complete create/edit/delete flows

### Testing Requirements

- Each correctness property MUST be implemented by a SINGLE property-based test
- Each property-based test MUST be tagged with a comment referencing the design document property
- Tag format: `// Feature: rust-transport-management-ui, Property {number}: {property_text}`
- Property tests MUST run a minimum of 100 iterations
- Unit tests and property tests are complementary and both are required

## Implementation Notes

### Async Operations

All API calls will use Tokio runtime within `slint::spawn_local`:

```rust
slint::spawn_local(async move {
    let rt = tokio::runtime::Runtime::new().unwrap();
    let result = rt.block_on(async {
        let client = api.lock().unwrap();
        client.get_buses().await
    });
    
    match result {
        Ok(buses) => {
            // Update UI
        }
        Err(e) => {
            // Show error
        }
    }
}).unwrap();
```

### State Management

State will be managed through Slint properties with Rust callbacks:
- UI state (loading, errors, dialog visibility) stored in Slint properties
- Data state (buses, routes, schedules) stored in Slint models
- Callbacks trigger Rust functions that update state via UI handles

### Circular Reference Handling and ReferenceHandler.Preserve

The API uses `ReferenceHandler.Preserve` which wraps responses in a special format with `$values`, `$id`, and `$ref` fields. All API client methods must handle this format manually.

#### ReferenceHandler.Preserve Format

The API returns data in this structure:
```json
{
  "$id": "1",
  "$values": [
    {
      "$id": "2",
      "jobId": 1,
      "jobTitle": "Driver",
      "employees": {
        "$id": "3",
        "$values": [
          { "$ref": "4" }
        ]
      }
    }
  ]
}
```

#### Parsing Strategy

When deserializing API responses:

1. **Parse as generic JSON Value first**: Use `serde_json::Value` to parse the raw response
2. **Extract $values array**: The root object contains a `$values` array with the actual data
3. **Build ID map**: Create a HashMap of `$id` to object for resolving references
4. **Skip $ref pointers**: Objects with only `$ref` are reference placeholders, skip them
5. **Extract fields manually**: Use helper functions to extract fields by name

Example implementation pattern (from existing jobs.rs):
```rust
pub async fn get_jobs(&self) -> Result<Vec<Job>, ApiError> {
    let response = self.get("api/Jobs").await?;
    let text = response.text().await?;
    let json: Value = serde_json::from_str(&text)?;
    
    // Get root object and $values array
    let root = json.as_object()
        .ok_or_else(|| ApiError::ServerError("Expected object".to_string()))?;
    let values_array = root.get("$values")
        .and_then(|v| v.as_array())
        .ok_or_else(|| ApiError::ServerError("Missing $values array".to_string()))?;
    
    // Build ID map for reference resolution
    let mut id_map: HashMap<String, &Value> = HashMap::new();
    for item in values_array {
        if let Some(obj) = item.as_object() {
            if let Some(id) = obj.get("$id").and_then(|v| v.as_str()) {
                id_map.insert(id.to_string(), item);
            }
        }
    }
    
    // Parse objects, skipping $ref pointers
    let mut jobs = Vec::new();
    for item in values_array {
        if let Some(obj) = item.as_object() {
            // Skip reference pointers
            if obj.contains_key("$ref") {
                continue;
            }
            
            // Extract fields manually
            let get_i32 = |key: &str| obj.get(key).and_then(|v| v.as_i64()).map(|n| n as i32);
            let get_str = |key: &str| obj.get(key).and_then(|v| v.as_str()).map(|s| s.to_string());
            
            if let Some(job_id) = get_i32("jobId") {
                let job = Job {
                    job_id,
                    job_title: get_str("jobTitle").unwrap_or_default(),
                    // ... other fields
                };
                jobs.push(job);
            }
        }
    }
    
    Ok(jobs)
}
```

#### Users API Considerations

The Users API returns even more complex nested structures with UserRoles and Roles. The parsing must:
1. Extract the $values array from the root
2. For each user object, extract nested UserRoles array
3. For each UserRole, extract the nested Role object
4. Handle $ref pointers at multiple nesting levels
5. Build complete User objects with all relationships

Example structure:
```json
{
  "$id": "1",
  "$values": [
    {
      "$id": "2",
      "userId": 1,
      "login": "admin",
      "userRoles": {
        "$id": "3",
        "$values": [
          {
            "$id": "4",
            "role": {
              "$id": "5",
              "roleId": "guid",
              "roleName": "Administrator"
            }
          }
        ]
      }
    }
  ]
}
```

#### Implementation Requirements

- All `get_*` methods MUST parse ReferenceHandler.Preserve format from responses
- All `create_*` and `update_*` methods:
  - Send plain JSON in the request body (no $id/$ref)
  - Receive ReferenceHandler.Preserve format in the response
  - Must parse the response using the same $values extraction logic
- All `delete_*` methods typically return 204 No Content (no parsing needed)
- Use manual field extraction with helper closures for complex nested structures
- For simple structures (Job, User without deep nesting), can use direct serde deserialization after $values extraction
- Log parsing steps for debugging
- Handle missing $values gracefully with clear error messages

#### Request vs Response Handling

**Requests (POST/PUT):**
```rust
// Send plain JSON - serde handles serialization
let request = CreateJobRequest {
    job_title: "Driver".to_string(),
    job_description: Some("Bus driver".to_string()),
    base_salary: Some(2500.0),
};
let response = self.post("api/Jobs", &request).await?;
```

**Responses (GET/POST/PUT):**
```rust
// Response comes back with ReferenceHandler.Preserve format
let text = response.text().await?;
let json: Value = serde_json::from_str(&text)?;

// Extract $values array
let root = json.as_object().ok_or(...)?;
let values_array = root.get("$values").and_then(|v| v.as_array()).ok_or(...)?;

// Parse items from array
for item in values_array {
    // Skip $ref pointers, extract actual objects
}
```

### Date/Time Handling

Use `chrono` for date/time operations:
- Parse user input dates using `NaiveDate::parse_from_str`
- Serialize to ISO 8601 using `DateTime<Utc>::to_rfc3339`
- Display times using `format("%H:%M")`
- Handle timezone conversions between local and UTC

### Multi-Select Implementation

For route stops multi-select:
1. Use Slint `ListBox` with `selection-mode: multiple`
2. Store selected indices in a Slint array property
3. Map indices to actual stop names when saving
4. Validate minimum 2 selections before allowing save

## UI Layout Patterns

### List View Pattern

```
┌─────────────────────────────────────────────────────┐
│ [Add Button] [Refresh Button]        [Search Box]  │
├─────────────────────────────────────────────────────┤
│ ID │ Field 1  │ Field 2  │ Field 3  │ Actions     │
├─────────────────────────────────────────────────────┤
│ 1  │ Value    │ Value    │ Value    │ 👁️ ✏️ 🗑️    │
│ 2  │ Value    │ Value    │ Value    │ 👁️ ✏️ 🗑️    │
└─────────────────────────────────────────────────────┘
```

### Dialog Pattern

```
┌─────────────────────────────────────┐
│ Dialog Title                    [X] │
├─────────────────────────────────────┤
│ Label 1:  [Input Field]             │
│ Label 2:  [Input Field]             │
│ Label 3:  [Dropdown ▼]              │
│                                     │
│ [Error Message if any]              │
│                                     │
│           [Cancel] [Save]           │
└─────────────────────────────────────┘
```

### Schedule View Pattern

```
┌─────────────────────────────────────────────────────┐
│ Route: [Dropdown ▼]  Date: [Date Picker]           │
│ [Add Schedule] [Refresh]                            │
├─────────────────────────────────────────────────────┤
│ Time    │ Route        │ Price │ Seats │ Actions   │
├─────────────────────────────────────────────────────┤
│ 08:00   │ A → B        │ 2.50₽ │ 42    │ ✏️ 🗑️      │
│ 10:30   │ A → B        │ 2.50₽ │ 38    │ ✏️ 🗑️      │
└─────────────────────────────────────────────────────┘
```

## Performance Considerations

1. **Lazy Loading**: Load data only when navigating to a view
2. **Caching**: Consider caching bus/driver lists for dropdown population
3. **Debouncing**: Debounce search input to avoid excessive filtering
4. **Pagination**: If lists grow large, implement pagination (future enhancement)

## Security Considerations

1. **Authentication**: All API calls use authenticated client with JWT tokens
2. **Authorization**: Server enforces role-based access (admin-only for modifications)
3. **Input Validation**: Client-side validation prevents malformed requests
4. **XSS Prevention**: Slint framework handles text escaping automatically

## Accessibility Considerations

1. **Keyboard Navigation**: Ensure all interactive elements are keyboard accessible
2. **Focus Management**: Proper focus handling in dialogs
3. **Error Announcements**: Clear error messages for screen readers
4. **Color Contrast**: Use Material Design colors with sufficient contrast

## Future Enhancements

1. **Bulk Operations**: Select multiple records for batch delete
2. **Export/Import**: Export data to CSV/Excel
3. **Advanced Filtering**: Multiple filter criteria, saved filters
4. **Audit Logging**: Track who made changes and when
5. **Offline Support**: Cache data for offline viewing
6. **Real-time Updates**: WebSocket notifications for data changes
