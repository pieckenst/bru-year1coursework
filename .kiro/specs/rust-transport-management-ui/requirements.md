# Requirements Document

## Introduction

This specification defines the requirements for implementing Bus Management, Route Management, Route Schedules Management, Jobs Management, and Users Management user interfaces in the Rust/Slint-based Linux application for the BRU Avtopark system. The implementation will mirror the functionality of the existing Avalonia C# application while adapting to the Slint UI framework and Rust programming patterns.

## Glossary

- **System**: The Rust/Slint-based Linux application for BRU Avtopark
- **User**: An authenticated administrator or operator using the application
- **Bus**: A vehicle (Avtobus) in the fleet with a model identifier
- **Route**: A transportation route (Marshut) with start/end points, assigned driver, and bus
- **Route Schedule**: A specific scheduled instance of a route with departure/arrival times, stops, and pricing
- **Job**: A position or role within the organization with title, description, and salary information
- **System User**: An account in the system with login credentials, role, and permissions
- **Administrator**: A user with role value 1 who has full access to create, edit, and delete records
- **API**: The backend REST API service running on localhost:5000
- **Dialog**: A modal window for creating or editing records
- **Navigation**: The application's sidebar menu system for switching between views

## Requirements

### Requirement 1: Bus Management View

**User Story:** As a user, I want to view and manage the bus fleet, so that I can maintain accurate records of all vehicles in the system.

#### Acceptance Criteria

1. WHEN the user navigates to the Bus Management section THEN the system SHALL display a list of all buses with their ID and model
2. WHEN the bus list is displayed THEN the system SHALL show action buttons for adding, editing, and deleting buses
3. WHEN the user clicks the refresh button THEN the system SHALL reload the bus list from the API
4. WHEN the API request fails THEN the system SHALL display an error message to the user
5. WHEN the bus list is empty THEN the system SHALL display an appropriate empty state message

### Requirement 2: Bus Creation

**User Story:** As a user, I want to add new buses to the fleet, so that I can track newly acquired vehicles.

#### Acceptance Criteria

1. WHEN the user clicks the "Add Bus" button THEN the system SHALL display a dialog with a model input field
2. WHEN the user enters a bus model and clicks save THEN the system SHALL send a POST request to the API with the bus data
3. WHEN the bus creation succeeds THEN the system SHALL close the dialog and refresh the bus list
4. WHEN the user attempts to save with an empty model field THEN the system SHALL prevent submission and display a validation error
5. WHEN the API returns an error THEN the system SHALL display the error message in the dialog
6. WHEN the user clicks cancel THEN the system SHALL close the dialog without saving

### Requirement 3: Bus Editing

**User Story:** As a user, I want to edit existing bus information, so that I can correct errors or update vehicle details.

#### Acceptance Criteria

1. WHEN the user clicks the edit button for a bus THEN the system SHALL display a dialog pre-filled with the current bus model
2. WHEN the user modifies the model and clicks save THEN the system SHALL send a PUT request to the API with the updated data
3. WHEN the bus update succeeds THEN the system SHALL close the dialog and refresh the bus list
4. WHEN the user attempts to save with an empty model field THEN the system SHALL prevent submission and display a validation error
5. WHEN the API returns an error THEN the system SHALL display the error message in the dialog

### Requirement 4: Bus Deletion

**User Story:** As a user, I want to delete buses from the system, so that I can remove decommissioned vehicles.

#### Acceptance Criteria

1. WHEN the user clicks the delete button for a bus THEN the system SHALL display a confirmation dialog showing the bus model
2. WHEN the user confirms deletion THEN the system SHALL send a DELETE request to the API
3. WHEN the deletion succeeds THEN the system SHALL close the dialog and refresh the bus list
4. WHEN the user cancels the deletion THEN the system SHALL close the dialog without deleting
5. WHEN the API returns an error THEN the system SHALL display the error message to the user

### Requirement 5: Route Management View

**User Story:** As a user, I want to view and manage transportation routes, so that I can maintain accurate route information.

#### Acceptance Criteria

1. WHEN the user navigates to the Route Management section THEN the system SHALL display a list of all routes with start point, end point, bus model, and driver name
2. WHEN the route list is displayed THEN the system SHALL show action buttons for adding, editing, and deleting routes
3. WHEN the system loads routes THEN the system SHALL include related bus and employee data from the API
4. WHEN the user clicks the refresh button THEN the system SHALL reload the route list from the API
5. WHEN the API request fails THEN the system SHALL display an error message to the user

### Requirement 6: Route Creation

**User Story:** As a user, I want to create new routes, so that I can define new transportation paths.

#### Acceptance Criteria

1. WHEN the user clicks the "Add Route" button THEN the system SHALL display a dialog with fields for start point, end point, travel time, bus selection, and driver selection
2. WHEN the dialog opens THEN the system SHALL load available buses and drivers from the API for selection
3. WHEN the user fills all required fields and clicks save THEN the system SHALL send a POST request to the API with the route data
4. WHEN the route creation succeeds THEN the system SHALL close the dialog and refresh the route list
5. WHEN the user attempts to save with missing required fields THEN the system SHALL prevent submission and display validation errors
6. WHEN the API returns an error THEN the system SHALL display the error message in the dialog

### Requirement 7: Route Editing

**User Story:** As a user, I want to edit existing routes, so that I can update route details when changes occur.

#### Acceptance Criteria

1. WHEN the user clicks the edit button for a route THEN the system SHALL display a dialog pre-filled with the current route data
2. WHEN the dialog opens THEN the system SHALL load available buses and drivers and pre-select the current assignments
3. WHEN the user modifies fields and clicks save THEN the system SHALL send a PUT request to the API with the updated data
4. WHEN the route update succeeds THEN the system SHALL close the dialog and refresh the route list
5. WHEN the user attempts to save with missing required fields THEN the system SHALL prevent submission and display validation errors

### Requirement 8: Route Deletion

**User Story:** As a user, I want to delete routes from the system, so that I can remove discontinued routes.

#### Acceptance Criteria

1. WHEN the user clicks the delete button for a route THEN the system SHALL display a confirmation dialog showing the route start and end points
2. WHEN the user confirms deletion THEN the system SHALL send a DELETE request to the API
3. WHEN the deletion succeeds THEN the system SHALL close the dialog and refresh the route list
4. WHEN the user cancels the deletion THEN the system SHALL close the dialog without deleting
5. WHEN the API returns an error THEN the system SHALL display the error message to the user

### Requirement 9: Route Schedules Management View

**User Story:** As a user, I want to view and manage route schedules, so that I can maintain accurate timetables for each route.

#### Acceptance Criteria

1. WHEN the user navigates to the Route Schedules section THEN the system SHALL display a route selector and date picker
2. WHEN the user selects a route and date THEN the system SHALL display all schedules for that route on that date
3. WHEN schedules are displayed THEN the system SHALL show departure time, arrival time, price, available seats, and route stops
4. WHEN the user clicks the refresh button THEN the system SHALL reload the schedules from the API
5. WHEN no schedules exist for the selected route and date THEN the system SHALL display an appropriate empty state message

### Requirement 10: Route Schedule Creation

**User Story:** As a user, I want to create new route schedules, so that I can define specific departure times and pricing for routes.

#### Acceptance Criteria

1. WHEN the user clicks the "Add Schedule" button THEN the system SHALL display a dialog with fields for departure time, arrival time, price, seats, and route stops
2. WHEN the dialog opens THEN the system SHALL pre-populate route stops based on the selected route configuration
3. WHEN the user fills all required fields and clicks save THEN the system SHALL send a POST request to the API with the schedule data
4. WHEN the schedule creation succeeds THEN the system SHALL close the dialog and refresh the schedule list
5. WHEN the user attempts to save with missing required fields THEN the system SHALL prevent submission and display validation errors
6. WHEN the user selects route stops THEN the system SHALL allow multi-selection with a minimum of two stops required
7. WHEN the API returns an error THEN the system SHALL display the error message in the dialog

### Requirement 11: Route Schedule Editing

**User Story:** As a user, I want to edit existing route schedules, so that I can update times, pricing, or stops when needed.

#### Acceptance Criteria

1. WHEN the user clicks the edit button for a schedule THEN the system SHALL display a dialog pre-filled with the current schedule data
2. WHEN the dialog opens THEN the system SHALL pre-select the current route stops in the multi-select list
3. WHEN the user modifies fields and clicks save THEN the system SHALL send a PUT request to the API with the updated data
4. WHEN the schedule update succeeds THEN the system SHALL close the dialog and refresh the schedule list
5. WHEN the user attempts to save with fewer than two stops selected THEN the system SHALL prevent submission and display a validation error

### Requirement 12: Route Schedule Deletion

**User Story:** As a user, I want to delete route schedules from the system, so that I can remove cancelled or outdated schedules.

#### Acceptance Criteria

1. WHEN the user clicks the delete button for a schedule THEN the system SHALL display a confirmation dialog showing the schedule start and end points
2. WHEN the user confirms deletion THEN the system SHALL send a DELETE request to the API
3. WHEN the deletion succeeds THEN the system SHALL close the dialog and refresh the schedule list
4. WHEN the user cancels the deletion THEN the system SHALL close the dialog without deleting
5. WHEN the API returns an error THEN the system SHALL display the error message to the user

### Requirement 13: Search and Filter Functionality

**User Story:** As a user, I want to search and filter records, so that I can quickly find specific buses, routes, or schedules.

#### Acceptance Criteria

1. WHEN the user enters text in the bus search field THEN the system SHALL filter the bus list to show only buses with matching model names
2. WHEN the user enters text in the route search field THEN the system SHALL filter the route list to show only routes with matching start points, end points, bus models, or driver names
3. WHEN the search field is cleared THEN the system SHALL display all records again
4. WHEN the search returns no results THEN the system SHALL display an appropriate empty state message
5. WHEN the user performs a search THEN the system SHALL update the display in real-time without requiring a button click

### Requirement 14: Navigation Integration

**User Story:** As a user, I want to navigate between Bus, Route, and Route Schedules views, so that I can access all transportation management features.

#### Acceptance Criteria

1. WHEN the user clicks the Bus Management navigation item THEN the system SHALL display the bus management view
2. WHEN the user clicks the Route Management navigation item THEN the system SHALL display the route management view
3. WHEN the user clicks the Route Schedules navigation item THEN the system SHALL display the route schedules view
4. WHEN the user switches views THEN the system SHALL preserve the authentication state
5. WHEN the user switches views THEN the system SHALL load the appropriate data for the new view

### Requirement 15: API Client Integration

**User Story:** As a developer, I want to integrate with the existing API client, so that all HTTP requests use consistent authentication and error handling.

#### Acceptance Criteria

1. WHEN the system makes API requests THEN the system SHALL use the existing ApiClient with authentication tokens
2. WHEN the API returns a 401 Unauthorized response THEN the system SHALL handle the authentication failure appropriately
3. WHEN the API returns a 403 Forbidden response THEN the system SHALL display a permission denied message
4. WHEN the API request times out THEN the system SHALL display a timeout error message
5. WHEN the API returns a network error THEN the system SHALL display a connection error message

### Requirement 16: Data Serialization and Deserialization

**User Story:** As a developer, I want to properly serialize and deserialize API data, so that the system correctly handles all data types and circular references.

#### Acceptance Criteria

1. WHEN the system receives JSON data from the API THEN the system SHALL deserialize it using the existing model structures
2. WHEN the API returns circular references with $ref and $id THEN the system SHALL handle them without errors
3. WHEN the system sends data to the API THEN the system SHALL serialize it in the format expected by the API
4. WHEN date/time fields are serialized THEN the system SHALL use ISO 8601 format
5. WHEN optional fields are null THEN the system SHALL omit them from serialization or handle them appropriately

### Requirement 17: Slint UI Component Design

**User Story:** As a user, I want a consistent and intuitive user interface, so that I can efficiently perform my tasks.

#### Acceptance Criteria

1. WHEN dialogs are displayed THEN the system SHALL use Material Design styling consistent with the existing employee management UI
2. WHEN lists are displayed THEN the system SHALL use alternating row colors for readability
3. WHEN buttons are displayed THEN the system SHALL use appropriate icons and colors (e.g., red for delete)
4. WHEN the user hovers over interactive elements THEN the system SHALL provide visual feedback
5. WHEN forms are displayed THEN the system SHALL use clear labels and appropriate input controls

### Requirement 18: Error Handling and User Feedback

**User Story:** As a user, I want clear feedback on my actions, so that I understand what is happening in the system.

#### Acceptance Criteria

1. WHEN an operation is in progress THEN the system SHALL display a loading indicator
2. WHEN an operation succeeds THEN the system SHALL provide visual confirmation (e.g., closing dialog, refreshing list)
3. WHEN an operation fails THEN the system SHALL display a clear error message explaining what went wrong
4. WHEN validation fails THEN the system SHALL highlight the problematic fields and explain the requirements
5. WHEN the system is loading data THEN the system SHALL disable action buttons to prevent duplicate requests

### Requirement 19: Jobs Management View

**User Story:** As an administrator, I want to view and manage job positions, so that I can maintain accurate records of all roles in the organization.

#### Acceptance Criteria

1. WHEN the user navigates to the Jobs Management section THEN the system SHALL display a list of all jobs with their ID, title, description, and base salary
2. WHEN the job list is displayed THEN the system SHALL show action buttons for adding, editing, and deleting jobs
3. WHEN the user clicks the refresh button THEN the system SHALL reload the job list from the API
4. WHEN the API request fails THEN the system SHALL display an error message to the user
5. WHEN the job list is empty THEN the system SHALL display an appropriate empty state message

### Requirement 20: Job Creation

**User Story:** As an administrator, I want to add new job positions, so that I can define roles for employees.

#### Acceptance Criteria

1. WHEN the user clicks the "Add Job" button THEN the system SHALL display a dialog with fields for job title, description, and base salary
2. WHEN the user enters a job title and clicks save THEN the system SHALL send a POST request to the API with the job data
3. WHEN the job creation succeeds THEN the system SHALL close the dialog and refresh the job list
4. WHEN the user attempts to save with an empty job title field THEN the system SHALL prevent submission and display a validation error
5. WHEN the API returns an error THEN the system SHALL display the error message in the dialog
6. WHEN the user clicks cancel THEN the system SHALL close the dialog without saving

### Requirement 21: Job Editing

**User Story:** As an administrator, I want to edit existing job information, so that I can update role details or salary information.

#### Acceptance Criteria

1. WHEN the user clicks the edit button for a job THEN the system SHALL display a dialog pre-filled with the current job title, description, and base salary
2. WHEN the user modifies fields and clicks save THEN the system SHALL send a PUT request to the API with the updated data
3. WHEN the job update succeeds THEN the system SHALL close the dialog and refresh the job list
4. WHEN the user attempts to save with an empty job title field THEN the system SHALL prevent submission and display a validation error
5. WHEN the API returns an error THEN the system SHALL display the error message in the dialog

### Requirement 22: Job Deletion

**User Story:** As an administrator, I want to delete job positions from the system, so that I can remove obsolete roles.

#### Acceptance Criteria

1. WHEN the user clicks the delete button for a job THEN the system SHALL display a confirmation dialog showing the job title
2. WHEN the user confirms deletion THEN the system SHALL send a DELETE request to the API
3. WHEN the deletion succeeds THEN the system SHALL close the dialog and refresh the job list
4. WHEN the user cancels the deletion THEN the system SHALL close the dialog without deleting
5. WHEN the API returns an error THEN the system SHALL display the error message to the user
6. IF the user is not an administrator THEN the system SHALL return a 403 Forbidden response

### Requirement 23: Job Search Functionality

**User Story:** As a user, I want to search for jobs by title or description, so that I can quickly find specific positions.

#### Acceptance Criteria

1. WHEN the user enters text in the job search field THEN the system SHALL filter the job list to show only jobs with matching titles or descriptions
2. WHEN the search field is cleared THEN the system SHALL display all jobs again
3. WHEN the search returns no results THEN the system SHALL display an appropriate empty state message
4. WHEN the user performs a search THEN the system SHALL update the display in real-time without requiring a button click
5. WHEN the system sends a search request to the API THEN the system SHALL include jobTitle and internship query parameters

### Requirement 24: Users Management View

**User Story:** As an administrator, I want to view and manage system users, so that I can control access to the application.

#### Acceptance Criteria

1. WHEN the user navigates to the Users Management section THEN the system SHALL display a list of all users with their ID, login, email, phone number, role, and active status
2. WHEN the user list is displayed THEN the system SHALL show action buttons for adding, editing, and deleting users
3. WHEN the system loads users THEN the system SHALL include user roles and permissions from the API
4. WHEN the user clicks the refresh button THEN the system SHALL reload the user list from the API
5. WHEN the API request fails THEN the system SHALL display an error message to the user
6. IF the current user is not an administrator THEN the system SHALL return a 403 Forbidden response

### Requirement 25: User Creation

**User Story:** As an administrator, I want to create new user accounts, so that I can grant system access to employees.

#### Acceptance Criteria

1. WHEN the user clicks the "Add User" button THEN the system SHALL display a dialog with fields for login, password, role, phone number, email, and Windows authentication settings
2. WHEN the dialog opens THEN the system SHALL provide a role selector with options for Administrator, Cashier, Controller, and Senior Cashier
3. WHEN the user fills required fields and clicks save THEN the system SHALL send a POST request to the API with the user data
4. WHEN the user creation succeeds THEN the system SHALL close the dialog and refresh the user list
5. WHEN the user attempts to save with an existing login THEN the system SHALL display a validation error indicating the login already exists
6. WHEN the user attempts to save with missing required fields THEN the system SHALL prevent submission and display validation errors
7. WHEN the API returns an error THEN the system SHALL display the error message in the dialog

### Requirement 26: User Editing

**User Story:** As an administrator, I want to edit existing user accounts, so that I can update user information or change permissions.

#### Acceptance Criteria

1. WHEN the user clicks the edit button for a user THEN the system SHALL display a dialog pre-filled with the current user data
2. WHEN the dialog opens THEN the system SHALL show all user fields including login, role, phone number, email, active status, and Windows authentication settings
3. WHEN the user modifies fields and clicks save THEN the system SHALL send a PUT request to the API with the updated data
4. WHEN the user update succeeds THEN the system SHALL close the dialog and refresh the user list
5. WHEN the user attempts to change the login to an existing login THEN the system SHALL display a validation error
6. WHEN the API returns an error THEN the system SHALL display the error message in the dialog

### Requirement 27: User Deletion

**User Story:** As an administrator, I want to delete user accounts from the system, so that I can revoke access for former employees.

#### Acceptance Criteria

1. WHEN the user clicks the delete button for a user THEN the system SHALL display a confirmation dialog showing the user login
2. WHEN the user confirms deletion THEN the system SHALL send a DELETE request to the API
3. WHEN the deletion succeeds THEN the system SHALL close the dialog and refresh the user list
4. WHEN the user cancels the deletion THEN the system SHALL close the dialog without deleting
5. WHEN the current user attempts to delete their own account THEN the system SHALL prevent the deletion and display an error message
6. WHEN the user attempts to delete the last administrator account THEN the system SHALL prevent the deletion and display an error message
7. WHEN the API returns an error THEN the system SHALL display the error message to the user

### Requirement 28: User Role and Permission Management

**User Story:** As an administrator, I want to view and manage user roles and permissions, so that I can control what actions users can perform.

#### Acceptance Criteria

1. WHEN the user views a user's details THEN the system SHALL display the user's assigned roles
2. WHEN the user views a user's details THEN the system SHALL display the user's effective permissions
3. WHEN the user assigns a role to a user THEN the system SHALL send a POST request to the API with the role assignment
4. WHEN the user removes a role from a user THEN the system SHALL send a DELETE request to the API
5. WHEN role assignments change THEN the system SHALL refresh the user's role and permission display

### Requirement 29: Current User Information

**User Story:** As a user, I want to view my own account information, so that I can verify my access level and contact details.

#### Acceptance Criteria

1. WHEN the system loads THEN the system SHALL fetch the current user information from the API using the authentication token
2. WHEN the API returns the current user THEN the system SHALL display the user's login and role in the navigation area
3. WHEN the authentication token is invalid THEN the system SHALL return a 401 Unauthorized response and redirect to login
4. WHEN the API request fails THEN the system SHALL display an error message
5. WHEN the current user information is displayed THEN the system SHALL show the user's roles and permissions

### Requirement 30: Administrator-Only Access Control

**User Story:** As a system, I want to restrict certain operations to administrators, so that only authorized users can modify critical data.

#### Acceptance Criteria

1. WHEN a non-administrator user attempts to create a job THEN the system SHALL return a 403 Forbidden response
2. WHEN a non-administrator user attempts to update a job THEN the system SHALL return a 403 Forbidden response
3. WHEN a non-administrator user attempts to delete a job THEN the system SHALL return a 403 Forbidden response
4. WHEN a non-administrator user attempts to access the users list THEN the system SHALL return a 403 Forbidden response
5. WHEN a non-administrator user attempts to create, update, or delete a user THEN the system SHALL return a 403 Forbidden response
6. WHEN the system receives a 403 Forbidden response THEN the system SHALL display a permission denied message to the user

### Requirement 31: Navigation Integration for Jobs and Users

**User Story:** As a user, I want to navigate to Jobs and Users management views, so that I can access administrative features.

#### Acceptance Criteria

1. WHEN the user clicks the Jobs Management navigation item THEN the system SHALL display the jobs management view
2. WHEN the user clicks the Users Management navigation item THEN the system SHALL display the users management view
3. WHEN the user switches to these views THEN the system SHALL preserve the authentication state
4. WHEN the user switches to these views THEN the system SHALL load the appropriate data for the new view
5. IF the user is not an administrator THEN the system SHALL hide or disable the Users Management navigation item
