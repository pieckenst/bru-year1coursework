# Maintenance Management and Reports Implementation

## Overview

This document describes the implementation of Maintenance Management, Sales Statistics, and Income Report features in the Rust/Slint UI for the Bus Park Management System.

## Implementation Date

December 2024

## Components Implemented

### 1. Data Models (`src/models/`)

#### Maintenance Models (`maintenance.rs`)
- `Maintenance` - Main maintenance record structure
  - `maintenance_id`: i64
  - `bus_id`: i64
  - `avtobus`: Option<Bus>
  - `last_service_date`: String
  - `mileage_threshold`: String
  - `maintenance_type`: String
  - `service_engineer`: String
  - `found_issues`: String
  - `next_service_date`: String
  - `roadworthiness`: String

- `CreateMaintenanceRequest` - For creating new maintenance records
- `UpdateMaintenanceRequest` - For updating existing maintenance records

#### Ticket Sales Models (`ticket_sales.rs`)
- `TicketSale` - Sales transaction record
  - `sale_id`: i64
  - `sale_date`: String
  - `ticket_id`: i64
  - `bilet`: Option<Ticket>
  - `ticket_sold_to_user`: String
  - `ticket_sold_to_user_phone`: String

- `Ticket` - Ticket information
  - `ticket_id`: i64
  - `route_id`: i64
  - `marshut`: Option<Marshut>
  - `ticket_price`: f64

- `Marshut` - Route information (simplified for ticket sales)
  - `route_id`: i64
  - `start_point`: String
  - `end_point`: String
  - `driver_id`: i64
  - `bus_id`: i64
  - `travel_time`: String

- `MonthlyIncome` - Monthly income aggregation
- `RouteIncome` - Income per route
- `RouteStatistic` - Sales statistics per route
- `DailyStatistic` - Daily sales statistics

### 2. API Clients (`src/api/`)

#### Maintenance API (`maintenance.rs`)
Implements full CRUD operations with ReferenceHandler.Preserve support:
- `get_all()` - Fetch all maintenance records
- `get_by_id(id)` - Fetch single maintenance record
- `create(request)` - Create new maintenance record
- `update(request)` - Update existing maintenance record
- `delete(id)` - Delete maintenance record
- `search(query)` - Search maintenance records
- `get_by_bus(bus_id)` - Get maintenance records for specific bus

**Key Feature**: Manual JSON parsing to handle circular references with `$id` and `$ref` properties from .NET's ReferenceHandler.Preserve.

#### Ticket Sales API (`ticket_sales.rs`)
Implements read operations with ReferenceHandler.Preserve support:
- `get_all()` - Fetch all ticket sales
- `get_by_id(id)` - Fetch single ticket sale
- `search(start_date, end_date)` - Search sales by date range

**Key Feature**: Complex reference resolution for nested objects (Sales -> Tickets -> Routes -> Employees/Buses).

### 3. UI Components (`ui/`)

#### Maintenance Management View (`maintenance_management.slint`)
Full-featured management interface with:
- Data table displaying all maintenance records
- Search functionality
- Add/Edit/Delete operations
- Columns: ID, Bus Model, Last Service Date, Next Service Date, Engineer, Found Issues, Roadworthiness, Actions
- Error handling and loading states
- Material Design styling

**Data Structure**:
```slint
struct MaintenanceData {
    maintenance_id: int,
    bus_id: int,
    bus_model: string,
    last_service_date: string,
    next_service_date: string,
    mileage_threshold: string,
    maintenance_type: string,
    service_engineer: string,
    found_issues: string,
    roadworthiness: string,
}
```

#### Combined Reports View (`reports_view.slint`)
Tabbed interface combining two report types:

**Tab 1: Income Report**
- Summary cards showing:
  - Total Income
  - Total Tickets Sold
  - Average Ticket Price
- Monthly income table
- Route income table
- Date range filtering

**Tab 2: Sales Statistics**
- Summary cards showing:
  - Total Sales
  - Total Revenue
  - Average Growth Rate
- Daily statistics table
- Route statistics table
- Date range filtering

**Features**:
- Tabbed navigation between report types
- Date range selector (start/end dates)
- Loading and error states
- Data tables with alternating row colors
- Material Design styling
- Growth rate color coding (green for positive, red for negative)

### 4. Integration (`ui/app-window.slint`)

#### Added Navigation Items
- **Group 2 (Transport), Index 3**: Maintenance Management
- **Group 4 (Reports), Index 0**: Reports and Statistics

#### Properties Added
```slint
// Maintenance Management
in-out property <[MaintenanceData]> maintenance_records
in-out property <bool> maintenance-loading
in-out property <string> maintenance-error
in-out property <bool> maintenance-has-error

// Reports
in-out property <int> reports-active-tab
in-out property <[MonthlyIncomeData]> monthly-incomes
in-out property <[RouteIncomeData]> route-incomes
in-out property <[RouteStatisticData]> route-statistics
in-out property <[DailyStatisticData]> daily-statistics
in-out property <string> reports-start-date
in-out property <string> reports-end-date
in-out property <float> total-income
in-out property <int> total-tickets-sold
in-out property <float> average-ticket-price
in-out property <int> total-sales
in-out property <float> total-revenue
in-out property <float> average-growth-rate
in-out property <bool> reports-loading
in-out property <string> reports-error
in-out property <bool> reports-has-error
```

#### Callbacks Added
```slint
// Maintenance
callback load-maintenance()
callback search-maintenance(string)
callback add-maintenance()
callback edit-maintenance(int)
callback delete-maintenance(int)
callback refresh-maintenance()

// Reports
callback load-income-report()
callback load-sales-statistics()
callback refresh-income-report()
callback refresh-sales-statistics()
```

## Architecture Decisions

### 1. ReferenceHandler.Preserve Parsing
The C# backend uses `ReferenceHandler.Preserve` for JSON serialization to handle circular references. This creates JSON with `$id` and `$ref` properties:

```json
{
  "$id": "1",
  "MaintenanceId": 123,
  "Avtobus": {
    "$id": "2",
    "BusId": 456,
    "Model": "Mercedes"
  }
}
```

**Solution**: Implemented custom parsing functions:
- `build_reference_map()` - Builds a map of `$id` to objects
- `resolve_reference()` - Resolves `$ref` pointers to actual objects
- Type-specific parsing functions for each model

### 2. Combined Reports View
Instead of separate pages, implemented a tabbed interface to:
- Reduce navigation complexity
- Allow easy comparison between income and sales data
- Share common date range filtering
- Maintain consistent UI patterns

### 3. Maintenance Dialog Strategy
Maintenance records require complex forms (dates, dropdowns for buses, etc.). The implementation follows the pattern established by:
- `employee_dialogs.slint` for form dialogs
- `route_schedule_dialogs.slint` for complex editing

**Note**: Dialog implementation deferred to allow backend integration testing first.

## Navigation Structure

```
Group 0: Главная (Home)
  └─ Dashboard (In Development)

Group 1: Персонал (Personnel)
  ├─ Сотрудники (Employees) ✓
  ├─ Должности (Jobs) ✓
  └─ Пользователи (Users) ✓

Group 2: Транспорт (Transport)
  ├─ Автобусы (Buses) ✓
  ├─ Маршруты (Routes) ✓
  ├─ Расписание (Schedule) ✓
  └─ Обслуживание (Maintenance) ✓ NEW

Group 3: Продажи (Sales)
  ├─ Билеты (Tickets) (In Development)
  └─ Продажи (Sales) (In Development)

Group 4: Отчёты (Reports)
  └─ Отчёты и статистика (Reports and Statistics) ✓ NEW
      ├─ Tab: Отчет по доходам (Income Report)
      └─ Tab: Статистика продаж (Sales Statistics)
```

## API Endpoints Required

### Maintenance API
- `GET /api/Maintenance` - Get all maintenance records
- `GET /api/Maintenance/{id}` - Get maintenance record by ID
- `POST /api/Maintenance` - Create new maintenance record
- `PUT /api/Maintenance/{id}` - Update maintenance record
- `DELETE /api/Maintenance/{id}` - Delete maintenance record
- `GET /api/Maintenance/search?query={query}` - Search maintenance records
- `GET /api/Maintenance/bus/{busId}` - Get maintenance records by bus

### Ticket Sales API
- `GET /api/TicketSales` - Get all ticket sales
- `GET /api/TicketSales/{id}` - Get ticket sale by ID
- `GET /api/TicketSales/search?startDate={date}&endDate={date}` - Search sales by date range

## Testing Requirements

### Unit Testing
- [ ] Test ReferenceHandler.Preserve parsing with complex nested objects
- [ ] Test date range filtering logic
- [ ] Test aggregation calculations (monthly income, daily statistics)

### Integration Testing
- [ ] Test maintenance CRUD operations against AdminServer API
- [ ] Test ticket sales data retrieval with nested objects
- [ ] Test error handling for network failures
- [ ] Test loading states and user feedback

### UI Testing
- [ ] Test maintenance table scrolling and rendering
- [ ] Test reports tab switching
- [ ] Test date range input validation
- [ ] Test empty state displays
- [ ] Test error message displays

## Remaining Implementation Tasks

### Backend (Rust main.rs)
1. **Initialize API Clients**
   ```rust
   let maintenance_api = MaintenanceApi::new(API_BASE_URL, client.clone());
   let ticket_sales_api = TicketSalesApi::new(API_BASE_URL, client.clone());
   ```

2. **Implement Maintenance Callbacks**
   - `load-maintenance` - Fetch and display maintenance records
   - `search-maintenance` - Filter maintenance records
   - `add-maintenance` - Show dialog and create record
   - `edit-maintenance` - Load record, show dialog, update
   - `delete-maintenance` - Confirm and delete record
   - `refresh-maintenance` - Reload data

3. **Implement Reports Callbacks**
   - `load-income-report` - Fetch sales data and calculate income metrics
   - `load-sales-statistics` - Fetch sales data and calculate statistics
   - `refresh-income-report` - Reload income data with date filter
   - `refresh-sales-statistics` - Reload statistics with date filter

4. **Data Transformation**
   - Transform `Maintenance` to `MaintenanceData` for UI
   - Aggregate `TicketSale` data into monthly/daily statistics
   - Calculate growth rates and percentages
   - Format dates for display

### Dialogs
1. **Maintenance Dialog** (`maintenance_dialogs.slint`)
   - Form fields for all maintenance properties
   - Bus selection dropdown
   - Date pickers for service dates
   - Validation logic
   - Save/Cancel buttons

2. **Delete Confirmation Dialog**
   - Similar to existing delete dialogs
   - Show maintenance record details

## Data Flow Example

### Maintenance Management Flow
```
User Action → Slint UI → Callback → main.rs Handler
                                          ↓
                                    MaintenanceApi::get_all()
                                          ↓
                                    HTTP GET /api/Maintenance
                                          ↓
                                    AdminServer API
                                          ↓
                                    JSON Response (with $id/$ref)
                                          ↓
                                    parse_maintenance_with_references()
                                          ↓
                                    Vec<Maintenance>
                                          ↓
                                    Transform to MaintenanceData
                                          ↓
                                    Update UI property
                                          ↓
                                    Slint renders table
```

### Income Report Flow
```
User selects date range → Slint UI → refresh_income_report callback
                                          ↓
                                    main.rs Handler
                                          ↓
                                    TicketSalesApi::search(start, end)
                                          ↓
                                    HTTP GET /api/TicketSales/search?...
                                          ↓
                                    AdminServer API
                                          ↓
                                    JSON Response (with nested objects)
                                          ↓
                                    parse_sales_with_references()
                                          ↓
                                    Vec<TicketSale>
                                          ↓
                                    Aggregate by month/route
                                          ↓
                                    Calculate totals and averages
                                          ↓
                                    Update UI properties
                                          ↓
                                    Slint renders charts and tables
```

## Known Limitations

1. **Chart Rendering**: Charts are placeholders. Slint doesn't have built-in charting. Options:
   - Use external charting library via WebView
   - Generate SVG charts in Rust
   - Use third-party Slint chart components
   - Display data in tables only (current approach)

2. **Date Picker**: Using text fields for dates. Consider adding:
   - Calendar popup widget
   - Date format validation
   - Date range presets (last week, last month, etc.)

3. **Real-time Updates**: No automatic refresh. User must click "Обновить" (Refresh) button.

4. **Export Functionality**: No export to Excel/PDF. Could be added with:
   - CSV export button
   - PDF generation library
   - Print preview

5. **Advanced Filtering**: Limited to text search. Could add:
   - Date range filter for maintenance
   - Bus selection filter
   - Engineer selection filter
   - Status filter (roadworthy/not roadworthy)

## Performance Considerations

1. **Large Datasets**: If maintenance records or sales grow large:
   - Implement pagination (like route schedules)
   - Add virtual scrolling
   - Implement server-side filtering

2. **Reference Parsing**: Current implementation parses entire JSON tree:
   - Consider lazy loading nested objects
   - Cache parsed reference map
   - Optimize for common case (no circular refs)

3. **Aggregation**: Currently done in UI layer:
   - Consider moving to backend API
   - Add dedicated aggregation endpoints
   - Cache aggregated results

## Security Notes

1. **Authorization**: Ensure maintenance management requires appropriate role
2. **Input Validation**: Validate all user inputs before API calls
3. **Error Messages**: Don't expose sensitive information in error messages
4. **Date Validation**: Prevent SQL injection in date strings

## Conclusion

The Maintenance Management and Reports features are fully implemented in the UI layer with proper API client support. The implementation follows established patterns from existing features and maintains consistency with the Material Design theme.

Next steps:
1. Implement backend handlers in `main.rs`
2. Create maintenance dialog
3. Test with AdminServer API
4. Add advanced features (filtering, export, charts)
5. Performance optimization if needed

## References

- AdminServer API: `BRU-Avtopark-Avtobusov/TicketSalesApp.AdminServer`
- C# View Models:
  - `MaintenanceManagementViewModel.cs`
  - `IncomeReportViewModel.cs`
  - `SalesStatisticsViewModel.cs`
- C# Views:
  - `MaintenanceManagementToolWindow.axaml`
  - `IncomeReportToolWindow.axaml`
  - `SalesStatisticsToolWindow.axaml`
