# Implementation Plan

- [-] 1. Implement Bus Management Rust Backend


  - Wire up existing bus_management.slint UI to API client
  - Implement all CRUD operations with proper error handling
  - Add search/filter functionality
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1-2.6, 3.1-3.5, 4.1-4.5, 13.1, 13.3, 13.5_

- [x] 1.1 Add API client methods for bus operations

  - Implement `get_buses()`, `get_bus()`, `create_bus()`, `update_bus()`, `delete_bus()` in ApiClient
  - Handle JSON serialization/deserialization with circular references
  - Add proper error handling for all HTTP status codes
  - _Requirements: 2.2, 3.2, 4.2, 15.1, 16.1, 16.3_

- [ ]* 1.2 Write property test for bus API serialization round-trip
  - **Property 18: JSON deserialization succeeds for valid data**
  - **Property 19: JSON serialization produces correct format**
  - **Validates: Requirements 16.1, 16.3**

- [x] 1.3 Implement bus management callbacks in main.rs


  - Add `load_buses()` callback to fetch and display bus data
  - Add `search_buses()` callback for real-time filtering
  - Add `add_bus()` callback to create new buses
  - Add `edit_bus()` callback to update existing buses
  - Add `delete_bus()` callback to remove buses
  - Handle loading states and error messages
  - _Requirements: 1.3, 2.2, 2.3, 3.2, 3.3, 4.2, 4.3, 13.1, 13.5_

- [ ]* 1.4 Write property test for bus search filtering
  - **Property 13: Search filters display in real-time**
  - **Validates: Requirements 13.1, 13.5**

- [ ]* 1.5 Write property test for empty/whitespace validation
  - **Property 5: Empty/whitespace validation prevents submission**
  - **Validates: Requirements 2.4, 3.4**



- [x] 1.6 Add navigation integration for bus management




  - Update AppRoute enum to include BusManagement
  - Add navigation handler in main.rs to show bus management view
  - Ensure authentication state is preserved during navigation
  - _Requirements: 14.1, 14.4, 14.5_

- [ ]* 1.7 Write property test for navigation state preservation
  - **Property 15: Navigation preserves authentication**
  - **Validates: Requirements 14.4**

- [x] 2. Create Route Management UI and Backend




  - Create route_management.slint with Material Design styling
  - Implement Rust backend with CRUD operations
  - Add dropdown loading for buses and drivers
  - _Requirements: 5.1-5.5, 6.1-6.6, 7.1-7.5, 8.1-8.5, 13.2, 13.3, 13.5_


- [x] 2.1 Create route_management.slint UI component

  - Define RouteData struct with all required fields
  - Create main view with search bar and action buttons
  - Implement route list with columns for start/end points, bus, driver, travel time
  - Add empty state handling
  - _Requirements: 5.1, 5.2_

- [x] 2.2 Create route add/edit dialogs in Slint



  - Create dialog with fields for start point, end point, travel time
  - Add ComboBox controls for bus and driver selection
  - Implement validation UI (required field indicators)
  - Add error message display area
  - Style dialogs with Material Design
  - _Requirements: 6.1, 7.1_

- [x] 2.3 Create route delete confirmation dialog

  - Create simple confirmation dialog showing route details
  - Add cancel and confirm buttons
  - _Requirements: 8.1_



- [x] 2.4 Add API client methods for route operations

  - Implement `get_routes()`, `get_route()`, `create_route()`, `update_route()`, `delete_route()`
  - Handle nested bus and employee data in responses
  - Add proper error handling
  - _Requirements: 5.3, 6.3, 7.3, 8.2, 15.1_

- [ ]* 2.5 Write property test for route data with nested relationships
  - **Property 25: Route data includes nested relationships**


  - **Validates: Requirements 5.3**


- [x] 2.6 Implement route management callbacks in main.rs

  - Add `load_routes()` to fetch routes with nested data
  - Add `load_buses_for_dropdown()` and `load_drivers_for_dropdown()` for dialog population
  - Add `search_routes()` for multi-field filtering
  - Add `add_route()`, `edit_route()`, `delete_route()` callbacks
  - Handle loading states and error messages
  - _Requirements: 5.4, 6.2, 6.3, 6.4, 7.2, 7.3, 7.4, 8.2, 8.3, 13.2_

- [ ]* 2.7 Write property test for required field validation
  - **Property 10: Required field validation prevents submission**
  - **Validates: Requirements 6.5, 7.5**


- [ ]* 2.8 Write property test for dialog dropdown loading
  - **Property 12: Dialog loading populates dropdown data**
  - **Validates: Requirements 6.2, 7.2**



- [x] 2.9 Add navigation integration for route management
  - Update AppRoute enum to include RouteManagement
  - Add navigation handler to show route management view
  - _Requirements: 14.2, 14.5_

- [ ] 3. Create Route Schedules UI and Backend
  - Create route_schedules.slint with complex layout
  - Implement multi-select for route stops
  - Add date picker and route selector
  - Implement Rust backend with schedule operations
  - _Requirements: 9.1-9.5, 10.1-10.7, 11.1-11.5, 12.1-12.5_

- [ ] 3.1 Create route_schedules.slint UI component
  - Define ScheduleData struct with all fields including route stops array
  - Create main view with route selector ComboBox and date picker
  - Implement schedule list with columns for times, price, seats, stops
  - Add side panel for displaying route stops visualization
  - Add empty state handling
  - _Requirements: 9.1, 9.2, 9.3, 9.5_

- [ ] 3.2 Create schedule add/edit dialogs in Slint
  - Create dialog with time pickers for departure/arrival
  - Add numeric inputs for price and available seats
  - Implement multi-select ListBox for route stops
  - Add checkboxes for isActive and isRecurring
  - Display estimated stop times and distances
  - Add validation UI for minimum 2 stops requirement
  - Style with Material Design
  - _Requirements: 10.1, 10.2, 10.6, 11.1, 11.2_

- [ ] 3.3 Create schedule delete confirmation dialog
  - Create confirmation dialog showing schedule route and times
  - Add cancel and confirm buttons
  - _Requirements: 12.1_

- [ ] 3.4 Add API client methods for route schedule operations
  - Implement `get_route_schedules()`, `get_route_schedule()`, `create_route_schedule()`, `update_route_schedule()`, `delete_route_schedule()`
  - Handle complex nested data (arrays of stops, times, distances)
  - Add date/time serialization with ISO 8601 format
  - Add proper error handling
  - _Requirements: 9.2, 10.3, 11.3, 12.2, 15.1, 16.4_

- [ ]* 3.5 Write property test for date serialization format
  - **Property 20: Date serialization uses ISO 8601**
  - **Validates: Requirements 16.4**

- [ ] 3.6 Implement route schedule callbacks in main.rs
  - Add `load_routes_for_selector()` to populate route dropdown
  - Add `load_schedules()` to fetch schedules for selected route and date
  - Add `load_route_stops()` to pre-populate stops based on route configuration
  - Add `add_schedule()`, `edit_schedule()`, `delete_schedule()` callbacks
  - Calculate estimated stop times and distances based on departure/arrival times
  - Handle loading states and error messages
  - _Requirements: 9.2, 9.4, 10.2, 10.3, 10.4, 11.3, 11.4, 12.2, 12.3_

- [ ]* 3.7 Write property test for multi-select validation
  - **Property 11: Multi-select requires minimum selections**
  - **Validates: Requirements 10.6, 11.5**

- [ ]* 3.8 Write property test for schedule data completeness
  - **Property 24: Schedule data includes all required fields**
  - **Validates: Requirements 9.3**

- [ ] 3.9 Add navigation integration for route schedules
  - Update AppRoute enum to include RouteSchedules
  - Add navigation handler to show route schedules view
  - _Requirements: 14.3, 14.5_

- [ ] 4. Implement Common Functionality
  - Add shared error handling utilities
  - Implement loading state management
  - Add validation helpers
  - _Requirements: 18.1-18.5_

- [ ] 4.1 Create error handling utilities
  - Define ApiError enum with all error types
  - Implement `user_message()` method for Russian error messages
  - Add error display helpers for dialogs and views
  - _Requirements: 1.4, 18.3_

- [ ]* 4.2 Write property test for API error handling
  - **Property 3: API errors display error messages**
  - **Validates: Requirements 1.4, 18.3**

- [ ] 4.3 Implement loading state management
  - Create helper functions to set/unset loading state
  - Disable action buttons during loading
  - Show loading indicators
  - _Requirements: 18.1, 18.5_

- [ ]* 4.4 Write property test for loading state behavior
  - **Property 22: Loading state disables actions**
  - **Validates: Requirements 18.1, 18.5**

- [ ] 4.5 Create validation helper functions
  - Implement `validate_non_empty()` for required fields
  - Implement `validate_min_selections()` for multi-select
  - Add validation error message formatting
  - _Requirements: 2.4, 6.5, 10.6, 18.4_

- [ ]* 4.6 Write property test f
or validation error display
  - **Property 23: Validation errors highlight fields**
  - **Validates: Requirements 18.4**

- [ ] 5. Implement Successful Operation Workflows
  - Ensure all successful operations close dialogs and refresh data
  - Implement cancel workflows that don't trigger API calls
  - _Requirements: 2.3, 2.6, 3.3, 4.3, 4.4, 6.4, 7.4, 8.3, 10.4, 11.4, 12.3, 12.4_

- [ ] 5.1 Implement success flow for create operations
  - Ensure successful POST requests close dialogs
  - Trigger data refresh after successful creation
  - Clear form fields after success
  - _Requirements: 2.3, 6.4, 10.4_

- [ ]* 5.2 Write property test for successful operations
  - **Property 6: Successful operations close dialogs and refresh data**
  - **Validates: Requirements 2.3, 3.3, 4.3, 6.4, 7.4, 8.3, 10.4, 11.4, 12.3**

- [ ] 5.3 Implement cancel flow for all dialogs
  - Ensure cancel buttons close dialogs without API calls
  - Clear any error messages on cancel
  - Reset form state
  - _Requirements: 2.6, 4.4, 8.4, 12.4_

- [ ]* 5.4 Write property test for cancel workflow
  - **Property 7: Cancel closes dialog without API calls**
  - **Validates: Requirements 2.6, 4.4, 8.4, 12.4**

- [ ] 5.5 Implement edit dialog pre-population
  - Load existing data when opening edit dialogs
  - Pre-select dropdown values for buses/drivers
  - Pre-select multi-select items for route stops
  - _Requirements: 3.1, 7.1, 11.1, 11.2_

- [ ]* 5.6 Write property test for edit dialog pre-population
  - **Property 8: Edit dialogs pre-populate with existing data**
  - **Validates: Requirements 3.1, 7.1, 11.1**

- [ ] 5.7 Implement delete confirmation dialogs
  - Show identifying information in confirmation dialogs
  - Display bus model, route endpoints, or schedule details
  - _Requirements: 4.1, 8.1, 12.1_

- [ ]* 5.8 Write property test for delete confirmation
  - **Property 9: Delete confirmation shows record identifier**
  - **Validates: Requirements 4.1, 8.1, 12.1**

- [ ] 6. Implement Search and Filter Functionality
  - Add real-time search for all management views
  - Implement clear search to restore full list
  - _Requirements: 13.1-13.5_

- [ ] 6.1 Implement bus search filtering
  - Filter buses by model name in real-time
  - Update display without API calls (client-side filtering)
  - Handle empty search results
  - _Requirements: 13.1, 13.4, 13.5_

- [ ] 6.2 Implement route search filtering
  - Filter routes by start point, end point, bus model, or driver name
  - Support multi-field search
  - Update display in real-time
  - Handle empty search results
  - _Requirements: 13.2, 13.4, 13.5_

- [ ] 6.3 Implement clear search functionality
  - Restore full list when search is cleared
  - Test round-trip property (filter then clear)
  - _Requirements: 13.3_

- [ ]* 6.4 Write property test for search round-trip
  - **Property 14: Clear search restores full list**
  - **Validates: Requirements 13.3**

- [ ] 7. Add Navigation System Integration
  - Update navigation module with new routes
  - Implement view switching logic
  - Ensure data loading on navigation
  - _Requirements: 14.1-14.5_

- [ ] 7.1 Update navigation.rs module
  - Add BusManagement, RouteManagement, RouteSchedules to AppRoute enum
  - Update `from_indices()` to map navigation indices to new routes
  - Update `display_name()` for new routes
  - _Requirements: 14.1, 14.2, 14.3_

- [ ] 7.2 Update navigation.slint UI
  - Add new navigation items for transport management
  - Group under "Транспорт" section
  - Add appropriate icons
  - _Requirements: 14.1, 14.2, 14.3_

- [ ] 7.3 Implement navigation handlers in main.rs
  - Add handlers for BusManagement, RouteManagement, RouteSchedules routes
  - Trigger data loading when navigating to each view
  - Preserve authentication state during navigation
  - _Requirements: 14.4, 14.5_

- [ ]* 7.4 Write property test for navigation data loading
  - **Property 16: Navigation loads appropriate data**
  - **Validates: Requirements 14.5**

- [ ] 7.5 Update app-window.slint to include new views
  - Add conditional rendering for bus management view
  - Add conditional rendering for route management view
  - Add conditional rendering for route schedules view
  - Wire up navigation callbacks
  - _Requirements: 14.1, 14.2, 14.3_

- [ ] 8. Implement API Client Authentication
  - Ensure all API calls use authenticated client
  - Handle authentication errors appropriately
  - _Requirements: 15.1-15.5_

- [ ] 8.1 Verify API client usage across all operations
  - Audit all HTTP requests to ensure they use ApiClient
  - Verify authentication tokens are included in headers
  - _Requirements: 15.1_

- [ ]* 8.2 Write property test for authenticated API calls
  - **Property 17: All API calls use authenticated client**
  - **Validates: Requirements 15.1**

- [ ] 8.3 Add HTTP status code error handling
  - Handle 401 Unauthorized responses
  - Handle 403 Forbidden responses
  - Handle timeout errors
  - Handle network errors
  - Display appropriate Russian error messages
  - _Requirements: 15.2, 15.3, 15.4, 15.5_

- [ ] 9. Handle JSON Serialization Edge Cases
  - Implement circular reference handling
  - Handle null optional fields
  - Ensure proper date/time formatting
  - _Requirements: 16.1-16.5_

- [ ] 9.1 Test circular reference handling
  - Verify $ref and $id fields are handled correctly
  - Test with API responses containing circular references
  - Ensure `is_reference()` method works correctly
  - _Requirements: 16.2_

- [ ] 9.2 Implement null optional field handling
  - Verify optional fields serialize correctly when None
  - Test that omitted fields don't cause deserialization errors
  - _Requirements: 16.5_

- [ ]* 9.3 Write property test for null field handling
  - **Property 21: Null optional fields handled correctly**
  - **Validates: Requirements 16.5**

- [ ] 9.3 Verify date/time serialization
  - Test that DateTime<Utc> serializes to ISO 8601
  - Test deserialization of various date formats from API
  - Handle timezone conversions properly
  - _Requirements: 16.4_

- [ ] 10. Add Unit Tests for Helper Methods
  - Test model display methods
  - Test validation functions
  - Test error message generation
  - _Requirements: All_

- [ ] 10.1 Write unit tests for Bus model
  - Test `display_name()` method
  - Test `route_count()` method
  - Test `is_reference()` method
  - Test serialization/deserialization

- [ ] 10.2 Write unit tests for Route model
  - Test `display_name()` and `description()` methods
  - Test `driver_name()` and `bus_model()` methods
  - Test `ticket_count()` method
  - Test serialization/deserialization

- [ ] 10.3 Write unit tests for RouteSchedule model
  - Test `display_name()` and `description()` methods
  - Test `total_travel_time()` calculation
  - Test `is_currently_valid()` logic
  - Test `days_of_week_display()` and `route_stops_display()` formatting
  - Test serialization/deserialization

- [ ] 10.4 Write unit tests for validation functions
  - Test empty string validation
  - Test whitespace-only string validation
  - Test minimum selection validation
  - Test required field validation

- [ ] 10.5 Write unit tests for error handling
  - Test ApiError enum variants
  - Test `user_message()` method for all error types
  - Test error display formatting

- [ ] 11. Final Integration and Testing
  - Perform end-to-end testing of all workflows
  - Verify all property tests pass
  - Ensure UI responsiveness and error handling
  - _Requirements: All_

- [ ] 11.1 Test complete bus management workflow
  - Test add, edit, delete operations end-to-end
  - Test search and filter functionality
  - Test error handling and validation
  - Verify loading states and user feedback

- [ ] 11.2 Test complete route management workflow
  - Test add, edit, delete operations with dropdown selections
  - Test search across multiple fields
  - Test error handling and validation
  - Verify nested data loading (buses, drivers)

- [ ] 11.3 Test complete route schedules workflow
  - Test add, edit, delete operations with complex data
  - Test multi-select route stops functionality
  - Test date picker and route selector integration
  - Test schedule visualization in side panel
  - Verify estimated times and distances calculation

- [ ] 11.4 Run all property-based tests
  - Ensure all 25 properties pass with 100+ iterations
  - Fix any failing tests
  - Document any edge cases discovered

- [ ] 11.5 Perform cross-browser/platform testing
  - Test on Linux (primary target)
  - Verify Material Design styling consistency
  - Test keyboard navigation
  - Verify accessibility

- [ ] 12. Documentation and Code Cleanup
  - Add inline documentation
  - Clean up debug logging
  - Ensure code follows Rust best practices
  - _Requirements: All_

- [ ] 12.1 Add inline documentation
  - Document all public functions and methods
  - Add module-level documentation
  - Document complex algorithms (e.g., stop time calculation)
  - Add examples for key functions

- [ ] 12.2 Clean up and optimize code
  - Remove debug print statements
  - Optimize API calls (avoid unnecessary requests)
  - Refactor duplicate code into helper functions
  - Ensure proper error propagation

- [ ] 12.3 Code review and quality checks
  - Run `cargo clippy` and fix all warnings
  - Run `cargo fmt` to format code
  - Check for unused imports and variables
  - Verify all tests pass with `cargo test`

- [ ] 13. Final Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
