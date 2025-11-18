# Design Document

## Overview

This design document outlines the architecture and implementation approach for adding Bus Management, Route Management, and Route Schedules Management functionality to the Rust/Slint-based Linux application. The implementation will follow the existing patterns established in the employee management module while adapting to the specific needs of transportation management.

The system will provide three main management interfaces:
1. **Bus Management**: CRUD operations for fleet vehicles
2. **Route Management**: CRUD operations for transportation routes with driver and bus assignments
3. **Route Schedules**: Complex schedule management with multiple stops, times, and pricing

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Slint UI Layer                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │ Bus Mgmt UI  │  │ Route Mgmt   │  │ Route Schedules  │  │
│  │  (Slint)     │  │  UI (Slint)  │  │  UI (Slint)      │  │
│  └──────────────┘  └──────────────┘  └──────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                   Rust Application Layer                    │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              Navigation & State Management           │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │                  API Client Layer                    │   │
│  │  (Existing ApiClient with Authentication)            │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │                   Data Models                        │   │
│  │  (Bus, Route, RouteSchedule with Serde)              │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                  REST API (localhost:5000)                  │
│  /api/Buses, /api/Routes, /api/RouteSchedules              │
└─────────────────────────────────────────────────────────────┘
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
}
```

### 3. Navigation Integration

The existing navigation system will be extended with new routes:

```rust
pub enum AppRoute {
    // Existing routes
    Dashboard,
    Employees,
    // New routes
    BusManagement,
    RouteManagement,
    RouteSchedules,
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
            _ => None,
        }
    }
}
```

## Data Models

The data models already exist in `src/models/` and include:

1. **Bus** (`bus.rs`): Represents a vehicle with ID and model
2. **Route** (`route.rs`): Represents a route with start/end points, driver, and bus
3. **RouteSchedule** (`route_schedule.rs`): Represents a schedule with times, stops, and pricing

These models include:
- Serde serialization/deserialization with proper field naming
- Handling of circular references ($ref, $id)
- Helper methods for display and validation
- Request/response DTOs for API operations

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Navigation triggers view rendering
*For any* navigation state change to bus/route/schedule management, the system should render the appropriate view with the correct data loading initiated.
**Validates: Requirements 1.1, 5.1, 14.1, 14.2, 14.3**

### Property 2: Refresh triggers API reload
*For any* management view (bus, route, schedule), clicking the refresh button should trigger an API request to reload the data for that view.
**Validates: Requirements 1.3, 5.4, 9.4**

### Property 3: API errors display error messages
*For any* API request that fails, the system should display an error message to the user indicating the failure.
**Validates: Requirements 1.4, 2.5, 3.5, 4.5, 5.5, 6.6, 12.5, 18.3**

### Property 4: Form submission with valid data triggers API call
*For any* create/edit form with all required fields filled with valid data, clicking save should trigger the appropriate POST/PUT API request with the form data.
**Validates: Requirements 2.2, 3.2, 6.3, 7.3, 10.3, 11.3**

### Property 5: Empty/whitespace validation prevents submission
*For any* form field that requires non-empty input, attempting to submit with empty or whitespace-only values should prevent submission and display a validation error.
**Validates: Requirements 2.4, 3.4**

### Property 6: Successful operations close dialogs and refresh data
*For any* successful create/update/delete operation, the system should close the dialog (if open) and refresh the data list.
**Validates: Requirements 2.3, 3.3, 4.3, 6.4, 7.4, 8.3, 10.4, 11.4, 12.3, 18.2**

### Property 7: Cancel closes dialog without API calls
*For any* dialog with a cancel button, clicking cancel should close the dialog without triggering any API calls.
**Validates: Requirements 2.6, 4.4, 8.4, 12.4**

### Property 8: Edit dialogs pre-populate with existing data
*For any* record being edited, opening the edit dialog should pre-populate all fields with the current values from that record.
**Validates: Requirements 3.1, 7.1, 11.1**

### Property 9: Delete confirmation shows record identifier
*For any* record being deleted, the confirmation dialog should display identifying information about that record (e.g., bus model, route endpoints).
**Validates: Requirements 4.1, 8.1, 12.1**

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

### Circular Reference Handling

The existing models already handle circular references using `$ref` and `$id` fields. When deserializing:
1. Check if `ref_pointer` is Some - if so, it's a reference placeholder
2. Use the `is_reference()` method to detect references
3. Skip processing of reference placeholders in UI display logic

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
