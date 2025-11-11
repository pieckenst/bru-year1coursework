/// Navigation route mapping
/// Maps NavigationDrawer group/index to application routes
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum AppRoute {
    Dashboard,
    Employees,
    Jobs,
    Users,
    Buses,
    Routes,
    Schedules,
    Maintenance,
    Tickets,
    Sales,
    Reports,
}

impl AppRoute {
    /// Convert navigation drawer indices to route
    /// Group 0: Dashboard (1 item)
    /// Group 1: Personnel - Employees, Jobs, Users (3 items)
    /// Group 2: Transport - Buses, Routes, Schedules, Maintenance (4 items)
    /// Group 3: Sales - Tickets, Sales (2 items)
    /// Group 4: Reports - Reports & Statistics (1 item)
    pub fn from_indices(group: i32, index: i32) -> Option<Self> {
        match (group, index) {
            // Group 0: Dashboard
            (0, 0) => Some(AppRoute::Dashboard),
            
            // Group 1: Personnel
            (1, 0) => Some(AppRoute::Employees),
            (1, 1) => Some(AppRoute::Jobs),
            (1, 2) => Some(AppRoute::Users),
            
            // Group 2: Transport
            (2, 0) => Some(AppRoute::Buses),
            (2, 1) => Some(AppRoute::Routes),
            (2, 2) => Some(AppRoute::Schedules),
            (2, 3) => Some(AppRoute::Maintenance),
            
            // Group 3: Sales
            (3, 0) => Some(AppRoute::Tickets),
            (3, 1) => Some(AppRoute::Sales),
            
            // Group 4: Reports
            (4, 0) => Some(AppRoute::Reports),
            
            _ => None,
        }
    }
    
    /// Convert route to navigation drawer indices
    pub fn to_indices(&self) -> (i32, i32) {
        match self {
            AppRoute::Dashboard => (0, 0),
            AppRoute::Employees => (1, 0),
            AppRoute::Jobs => (1, 1),
            AppRoute::Users => (1, 2),
            AppRoute::Buses => (2, 0),
            AppRoute::Routes => (2, 1),
            AppRoute::Schedules => (2, 2),
            AppRoute::Maintenance => (2, 3),
            AppRoute::Tickets => (3, 0),
            AppRoute::Sales => (3, 1),
            AppRoute::Reports => (4, 0),
        }
    }
    
    /// Get display name for the route
    pub fn display_name(&self) -> &'static str {
        match self {
            AppRoute::Dashboard => "Панель управления",
            AppRoute::Employees => "Сотрудники",
            AppRoute::Jobs => "Должности",
            AppRoute::Users => "Пользователи",
            AppRoute::Buses => "Автобусы",
            AppRoute::Routes => "Маршруты",
            AppRoute::Schedules => "Расписание",
            AppRoute::Maintenance => "Обслуживание",
            AppRoute::Tickets => "Билеты",
            AppRoute::Sales => "Продажи",
            AppRoute::Reports => "Отчёты и статистика",
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_route_mapping() {
        // Test Dashboard
        assert_eq!(AppRoute::from_indices(0, 0), Some(AppRoute::Dashboard));
        assert_eq!(AppRoute::Dashboard.to_indices(), (0, 0));
        
        // Test Personnel
        assert_eq!(AppRoute::from_indices(1, 0), Some(AppRoute::Employees));
        assert_eq!(AppRoute::Employees.to_indices(), (1, 0));
        
        // Test Transport
        assert_eq!(AppRoute::from_indices(2, 1), Some(AppRoute::Routes));
        assert_eq!(AppRoute::Routes.to_indices(), (2, 1));
        
        // Test invalid
        assert_eq!(AppRoute::from_indices(10, 10), None);
    }
}
