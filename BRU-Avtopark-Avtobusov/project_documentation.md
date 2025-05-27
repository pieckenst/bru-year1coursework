# BRU-Avtopark-Avtobusov Project Documentation

## Project Overview
This is a comprehensive bus fleet management system built with a layered architecture, consisting of Core models, Services layer, Admin Server API, and UI components.

## Core Models

### User Management

#### User (User.cs)
- **Primary Key**: UserId (long)
- **GUID Identifier**: GuidId
- **Properties**:
  - Login (required)
  - PasswordHash (required)
  - Role (legacy: 0 = User, 1 = Admin)
  - CreatedAt
  - LastLoginAt
  - IsActive
  - UserRoles (navigation property)

#### Roles (Roles.cs)
- **Primary Key**: RoleId (Guid)
- **Properties**:
  - LegacyRoleId (int)
  - Name (required)
  - Description (required)
  - IsActive
  - CreatedAt/UpdatedAt
  - CreatedBy/UpdatedBy
  - NormalizedName
  - IsSystem
  - Priority
  - RolePermissions/UserRoles (navigation)

#### Permissions (Permissions.cs)
- **Primary Key**: PermissionId (Guid)
- **Properties**:
  - Name (required, max 50 chars)
  - Description (required, max 200 chars)
  - Category (required)
  - IsActive
  - CreatedAt

### Fleet Management

#### Bus (Avtobus.cs)
- **Primary Key**: BusId (long)
- **Properties**:
  - Model (required)
  - Routes (navigation)
  - Obsluzhivanies (maintenance records)

#### Maintenance (Obsluzhivanie.cs)
- **Primary Key**: MaintenanceId (long)
- **Properties**:
  - BusId (foreign key)
  - LastServiceDate
  - ServiceEngineer
  - FoundIssues
  - NextServiceDate
  - Roadworthiness

#### Route (Marshut.cs)
- **Primary Key**: RouteId (long)
- **Properties**:
  - StartPoint (required)
  - EndPoint (required)
  - DriverId (foreign key)
  - BusId (foreign key)
  - TravelTime
  - Tickets (navigation)

### Employee Management

#### Employee (Employee.cs)
- **Primary Key**: EmpId (long)
- **Properties**:
  - Surname (required)
  - Name (required)
  - Patronym
  - EmployedSince
  - JobId (foreign key)

#### Job (Job.cs)
- **Primary Key**: JobId (long)
- **Properties**:
  - JobTitle (required)
  - Internship
  - Employees (navigation)

### Ticket Management

#### Ticket (Bilet.cs)
- **Primary Key**: TicketId (long)
- **Properties**:
  - RouteId (foreign key)
  - TicketPrice (required)
  - Sales (navigation)

#### Sale (Prodazha.cs)
- **Primary Key**: SaleId (long)
- **Properties**:
  - SaleDate
  - TicketId (foreign key)

## API Endpoints

### Authentication
- **POST /api/auth/login**: User authentication.
- **POST /api/auth/register**: User registration.

### Users Management
- **GET /api/users**: List all users.
- **GET /api/users/{id}**: Get user details.
- **POST /api/users**: Create new user.
- **PUT /api/users/{id}**: Update user.
- **DELETE /api/users/{id}**: Delete user.
- **GET /api/users/{id}/roles**: Get user roles.
- **GET /api/users/{id}/permissions**: Get user permissions.
- **POST /api/users/{id}/roles**: Assign role to user.
- **DELETE /api/users/{id}/roles/{roleId}**: Remove role from user.

### Roles Management
- **GET /api/roles**: List all roles.
- **GET /api/roles/{id}**: Get role details.
- **POST /api/roles**: Create new role.
- **PUT /api/roles/{id}**: Update role.
- **DELETE /api/roles/{id}**: Delete role.
- **GET /api/roles/{id}/permissions**: Get role permissions.

### Permissions Management
- **GET /api/permissions**: List all permissions.
- **GET /api/permissions/{id}**: Get permission details.
- **POST /api/permissions**: Create new permission.
- **PUT /api/permissions/{id}**: Update permission.
- **DELETE /api/permissions/{id}**: Delete permission.
- **GET /api/permissions/categories**: List permission categories.
- **GET /api/permissions/category/{category}**: List permissions by category.

### Bus Management
- **GET /api/buses**: - List all buses
- **GET /api/buses/{id}**: - Get bus details
- **POST /api/buses**: - Add new bus
- **PUT /api/buses/{id}**: - Update bus
- **DELETE /api/buses/{id}**: - Delete bus
- **GET /api/buses/search**: - Search buses by model/service status
### Maintenance Management
- **GET /api/maintenance**: - List all maintenance records
- **GET /api/maintenance/{id}**: - Get maintenance record
- **POST /api/maintenance**: - Create maintenance record
- **PUT /api/maintenance/{id}**: - Update maintenance record
- **DELETE /api/maintenance/{id}**: - Delete maintenance record
- **GET /api/maintenance/search**: - Search maintenance records
- **GET /api/maintenance/due-maintenance**: - Get due maintenance records
### Route Management
- **GET /api/routes**: - List all routes
- **GET /api/routes/{id}**: - Get route details
- **POST /api/routes**: - Create new route
- **PUT /api/routes/{id}**: - Update route
- **DELETE /api/routes/{id}**: - Delete route
- **GET /api/routes/search**: - Search routes
### Employee Management
- **GET /api/employees**: - List all employees
- **GET /api/employees/{id}**: - Get employee details
- **POST /api/employees**: - Add new employee
- **PUT /api/employees/{id}**: - Update employee
- **DELETE /api/employees/{id}**: - Delete employee
- **GET /api/employees/search**: - Search employees
### Job Management
- **GET /api/jobs**: - List all jobs
- **GET /api/jobs/{id}**: - Get job details
- **POST /api/jobs**: - Create new job
- **PUT /api/jobs/{id}**: - Update job
- **DELETE /api/jobs/{id}**: - Delete job
- **GET /api/jobs/search**: - Search jobs
### Ticket Management
- **GET /api/tickets**: List all tickets.
- **GET /api/tickets/{id}**: Get ticket details.
- **POST /api/tickets**: Create new ticket.
- **PUT /api/tickets/{id}**: Update ticket.
- **DELETE /api/tickets/{id}**: Delete ticket.

### Ticket Sales Management
- **GET /api/ticketsales**: List all ticket sales.
- **GET /api/ticketsales/{id}**: Get sale details.
- **POST /api/ticketsales**: Create new sale.
- **PUT /api/ticketsales/{id}**: Update sale.
- **DELETE /api/ticketsales/{id}**: Delete sale.

## Security
- **Authentication & Authorization**: JWT-based authentication, Role-based access control (RBAC), Permission-based authorization.
- **Logging**: Comprehensive logging using Serilog.

## Data Validation
- Required field validation, Foreign key constraints, Business rule validation, Concurrency handling.

## Error Handling
- Standardized error responses, Detailed logging of errors, Proper HTTP status codes.

## Conclusion
This documentation reflects the current state of the project, including core models, API endpoints, and security measures. Future updates will include additional features and enhancements as outlined in the project plan.