#if MODERN
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketSalesApp.Core.Models;
using Microsoft.EntityFrameworkCore.Storage;
using System.ComponentModel.DataAnnotations.Schema;
using static System.FormattableString;

namespace TicketSalesApp.Core.Data
{
    [NotMapped]
    public class TableCheckResult
    {
        public string TableName { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public int TableCount { get; set; }
    }

    public static class DbInitializer
    {
        private static readonly Dictionary<string, Type> RequiredTables = new()
        {
            { "Users", typeof(User) },
            { "Jobs", typeof(Job) },
            { "Employees", typeof(Employee) },
            { "Avtobusy", typeof(Avtobus) },
            { "Obsluzhivanies", typeof(Obsluzhivanie) },
            { "Marshuti", typeof(Marshut) },
            { "Bilety", typeof(Bilet) },
            { "Prodazhi", typeof(Prodazha) },
            { "Roles", typeof(Roles) },
            { "Permissions", typeof(Permission) },
            { "UserRoles", typeof(UserRole) },
            { "RolePermissions", typeof(RolePermission) },
            { "RouteSchedules", typeof(RouteSchedules) }
        };

        // Helper method for GUID conversion
        private static Guid ConvertToGuid(long value)
        {
            byte[] bytes = new byte[16];
            BitConverter.GetBytes(value).CopyTo(bytes, 0);
            return new Guid(bytes);
        }

        public static async Task InitializeAsync(AppDbContext context, string provider = "SQLite", ILogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(provider, nameof(provider));

            try
            {
                var dbPath = context.Database.GetDbConnection().ConnectionString;
                logger?.LogInformation("Starting database initialization with provider: {Provider}, Database path: {DbPath}", 
                    provider, dbPath);

                // Log current database state
                await LogDatabaseState(context, logger);

                // Validate database connection
                if (!await ValidateDatabaseConnectionAsync(context, logger))
                {
                    throw new InvalidOperationException("Failed to establish database connection");
                }

                // Check if database exists
                bool dbExists = await context.Database.CanConnectAsync();
                
                if (!dbExists)
                {
                    logger?.LogInformation("Database does not exist. Creating new database...");
                    await context.Database.EnsureCreatedAsync();
                    
                    // Apply migrations for new database
                    await ApplyMigrationsAsync(context, logger);
                    
                    // Seed initial data for new database
                    await SeedDataAsync(context, provider, logger);
                }
                else
                {
                    // For existing database, validate schema
                    var schemaValidation = await ValidateDatabaseSchemaAsync(context, logger);
                    
                    if (!schemaValidation.IsValid)
                    {
                        logger?.LogWarning("Database schema validation failed: {Reasons}", 
                            string.Join(", ", schemaValidation.ValidationErrors));

                        // Check for missing tables
                        foreach (var error in schemaValidation.ValidationErrors)
                        {
                            if (error.StartsWith("Missing table:"))
                            {
                                var tableName = error.Replace("Missing table:", "").Trim();
                                if (RequiredTables.TryGetValue(tableName, out var entityType))
                                {
                                    logger?.LogInformation("Creating missing table: {TableName}", tableName);
                                    await CreateTableAsync(context, entityType, logger);
                                }
                            }
                        }

                        // Apply any pending migrations after fixing schema
                        await ApplyMigrationsAsync(context, logger);
                    }

                    // Validate table structures without dropping existing data
                    foreach (var table in RequiredTables)
                    {
                        if (!await TableExistsAsync(context, table.Key))
                        {
                            logger?.LogWarning("Table {TableName} does not exist. Creating...", table.Key);
                            await CreateTableAsync(context, table.Value, logger);
                        }
                        else if (!await ValidateTableStructureAsync(context, table.Key, table.Value, logger))
                        {
                            logger?.LogWarning("Table {TableName} structure needs migration...", table.Key);
                            // Let migrations handle structural changes instead of recreating
                            await ApplyMigrationsAsync(context, logger);
                        }
                        else
                        {
                            logger?.LogInformation("Table {TableName} structure is valid.", table.Key);
                        }
                    }

                    // Check if seeding is needed (e.g., empty tables)
                    var tableStats = await GetTableStatisticsAsync(context);
                    if (tableStats.Any(t => t.RowCount == 0))
                    {
                        logger?.LogInformation("Some tables are empty. Applying seed data...");
                        await SeedDataAsync(context, provider, logger);
                    }
                }

                logger?.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to initialize database: {ErrorMessage}", ex.Message);
                throw new Exception($"Failed to initialize database: {ex.Message}", ex);
            }
        }

        private static async Task LogDatabaseState(AppDbContext context, ILogger? logger)
        {
            if (logger == null) return;

            try
            {
                if (context.Database.IsSqlite())
                {
                    var tables = await context.Database
                        .SqlQuery<TableCheckResult>($@"
                            SELECT 
                                name as TableName, 
                                (SELECT COUNT(*) FROM sqlite_master WHERE type='table') as RowCount,
                                0 as TableCount
                            FROM sqlite_master 
                            WHERE type='table'")
                        .ToListAsync();

                    logger.LogInformation("Current database tables:");
                    foreach (var table in tables)
                    {
                        logger.LogInformation("Table: {TableName}, Row Count: {RowCount}", 
                            table.TableName, table.RowCount);
                    }
                }
                else
                {
                    var tables = await context.Database
                        .SqlQuery<TableCheckResult>($@"
                            SELECT 
                                TABLE_NAME as TableName, 
                                (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = t.TABLE_NAME) as RowCount,
                                0 as TableCount
                            FROM INFORMATION_SCHEMA.TABLES t
                            WHERE TABLE_SCHEMA = 'dbo'")
                        .ToListAsync();

                    logger.LogInformation("Current database tables:");
                    foreach (var table in tables)
                    {
                        logger.LogInformation("Table: {TableName}, Column Count: {RowCount}", 
                            table.TableName, table.RowCount);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to log database state");
            }
        }

        private static async Task<bool> ValidateDatabaseConnectionAsync(AppDbContext context, ILogger? logger)
        {
            try
            {
                logger?.LogInformation("Validating database connection...");
                return await context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Database connection validation failed");
                return false;
            }
        }

        private static async Task<(bool IsValid, List<string> ValidationErrors)> ValidateDatabaseSchemaAsync(
            AppDbContext context, ILogger? logger)
        {
            var errors = new List<string>();
            logger?.LogInformation("Validating database schema...");

            try
            {
                foreach (var table in RequiredTables)
                {
                    var exists = await TableExistsAsync(context, table.Key);
                    if (!exists)
                    {
                        errors.Add($"Missing table: {table.Key}");
                        continue;
                    }

                    // Get expected columns from entity
                    var entity = context.Model.FindEntityType(table.Value);
                    if (entity == null)
                    {
                        errors.Add($"Entity type not found for table: {table.Key}");
                        continue;
                    }

                    var expectedColumns = entity.GetProperties()
                        .Select(p => (Name: p.GetColumnName(), Type: p.ClrType))
                        .ToList();

                    // Get actual columns from database
                    var actualColumns = await GetTableColumnsAsync(context, table.Key);

                    // Compare column counts
                    if (expectedColumns.Count != actualColumns.Count)
                    {
                        logger?.LogWarning("Table {TableName} has {ActualCount} columns but {ExpectedCount} were expected",
                            table.Key, actualColumns.Count, expectedColumns.Count);
                        continue; // Let migrations handle structural changes
                    }

                    // Compare column names and types
                    foreach (var expected in expectedColumns)
                    {
                        var actual = actualColumns.FirstOrDefault(c => 
                            string.Equals(c.Name, expected.Name, StringComparison.OrdinalIgnoreCase));

                        if (actual.Name == null)
                        {
                            logger?.LogWarning("Column {ColumnName} missing in table {TableName}",
                                expected.Name, table.Key);
                            continue; // Let migrations handle structural changes
                        }
                    }
                }

                return (errors.Count == 0, errors);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Schema validation failed");
                errors.Add($"Schema validation failed: {ex.Message}");
                return (false, errors);
            }
        }

        private static async Task FixDatabaseSchemaAsync(AppDbContext context, 
            (bool IsValid, List<string> ValidationErrors) validation, ILogger? logger)
        {
            try
            {
                logger?.LogInformation("Attempting to fix database schema issues...");

                foreach (var error in validation.ValidationErrors)
                {
                    logger?.LogInformation("Fixing schema issue: {Error}", error);
                    if (error.StartsWith("Missing table:"))
                    {
                        var tableName = error.Replace("Missing table:", "").Trim();
                        if (RequiredTables.TryGetValue(tableName, out var entityType))
                        {
                            await CreateTableAsync(context, entityType, logger);
                        }
                    }
                    else if (error.StartsWith("Invalid table structure:"))
                    {
                        var tableName = error.Replace("Invalid table structure:", "").Trim();
                        if (RequiredTables.TryGetValue(tableName, out var entityType))
                        {
                            await DropTableIfExistsAsync(context, tableName, logger);
                            await CreateTableAsync(context, entityType, logger);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to fix schema issues");
                throw;
            }
        }

        private static async Task DropTableIfExistsAsync(AppDbContext context, string tableName, ILogger? logger)
        {
            try
            {
                if (context.Database.IsSqlite())
                {
                    await context.Database
                        .ExecuteSqlRawAsync($"DROP TABLE IF EXISTS {tableName}");
                }
                else
                {
                    await context.Database
                        .ExecuteSqlRawAsync($"IF OBJECT_ID('{tableName}', 'U') IS NOT NULL DROP TABLE {tableName}");
                }
                logger?.LogInformation("Dropped table {TableName}", tableName);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to drop table {TableName}", tableName);
                throw;
            }
        }

        private static async Task<bool> ValidateTableStructureAsync(
            AppDbContext context, string tableName, Type entityType, ILogger? logger)
        {
            try
            {
                var entity = context.Model.FindEntityType(entityType);
                if (entity == null) return false;

                var expectedColumns = entity.GetProperties()
                    .Select(p => new { Name = p.GetColumnName(), Type = p.GetColumnType() })
                    .ToList();

                var actualColumns = await GetTableColumnsAsync(context, tableName);

                foreach (var expected in expectedColumns)
                {
                    var actual = actualColumns.FirstOrDefault(c => 
                        string.Equals(c.Name, expected.Name, StringComparison.OrdinalIgnoreCase));
                    
                    if (actual.Name == null)
                    {
                        logger?.LogWarning("Missing column {Column} in table {Table}", expected.Name, tableName);
                        return false;
                    }

                    // SQLite type checking is less strict
                    if (!context.Database.IsSqlite() && !string.Equals(actual.Type, expected.Type, StringComparison.OrdinalIgnoreCase))
                    {
                        logger?.LogWarning("Column type mismatch in {Table}.{Column}: Expected {Expected}, Found {Actual}",
                            tableName, expected.Name, expected.Type, actual.Type);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to validate table structure for {Table}", tableName);
                return false;
            }
        }

        private static async Task<List<(string Name, string Type)>> GetTableColumnsAsync(
            AppDbContext context, string tableName)
        {
            if (context.Database.IsSqlite())
            {
                var columns = new List<(string Name, string Type)>();
                var sql = $"PRAGMA table_info({tableName})";
                using var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = sql;

                if (command.Connection!.State != System.Data.ConnectionState.Open)
                    await command.Connection.OpenAsync();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add((
                        reader.GetString(1), // name
                        reader.GetString(2)  // type
                    ));
                }
                return columns;
            }
            else
            {
                // SQL Server implementation
                var columns = new List<(string Name, string Type)>();
                var sql = @"
                    SELECT COLUMN_NAME, DATA_TYPE 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = @tableName";
                
                using var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = sql;
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@tableName";
                parameter.Value = tableName;
                command.Parameters.Add(parameter);

                if (command.Connection!.State != System.Data.ConnectionState.Open)
                    await command.Connection.OpenAsync();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add((
                        reader.GetString(0), // COLUMN_NAME
                        reader.GetString(1)  // DATA_TYPE
                    ));
                }
                return columns;
            }
        }

        private static async Task ApplyMigrationsAsync(AppDbContext context, ILogger? logger)
        {
            try
            {
                var pending = await context.Database.GetPendingMigrationsAsync();
                if (pending.Any())
                {
                    logger?.LogInformation("Applying {Count} pending migrations...", pending.Count());
            await context.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to apply migrations");
                throw;
            }
        }

        private static async Task<bool> TableExistsAsync(AppDbContext context, string tableName)
        {
            try
            {
                if (context.Database.IsSqlite())
                {
                    using var command = context.Database.GetDbConnection().CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @tableName";
                    
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@tableName";
                    parameter.Value = tableName;
                    command.Parameters.Add(parameter);

                    if (command.Connection!.State != System.Data.ConnectionState.Open)
                        await command.Connection.OpenAsync();

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
                else
                {
                    using var command = context.Database.GetDbConnection().CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName AND TABLE_SCHEMA = 'dbo'";
                    
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@tableName";
                    parameter.Value = tableName;
                    command.Parameters.Add(parameter);

                    if (command.Connection!.State != System.Data.ConnectionState.Open)
                        await command.Connection.OpenAsync();

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }
            catch (Exception ex)
            {
                var loggerFactory = context.GetService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger(typeof(DbInitializer));
                
                logger.LogError(ex, 
                    "Error checking if table {TableName} exists. Database: {Database}, Provider: {Provider}", 
                    tableName,
                    context.Database.GetDbConnection().Database,
                    context.Database.ProviderName);
                
                return false;
            }
        }

        private static async Task CreateTableAsync(AppDbContext context, Type entityType, ILogger? logger)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(entityType, nameof(entityType));

            try
            {
                var entity = context.Model.FindEntityType(entityType) 
                    ?? throw new InvalidOperationException($"Entity type {entityType.Name} not found in model");

                var createTableSql = context.Database.IsSqlite() 
                    ? GenerateSqliteCreateTableSql(entity)
                    : GenerateSqlServerCreateTableSql(entity);
                
                await context.Database.ExecuteSqlRawAsync(createTableSql);
                logger?.LogInformation("Created table for entity {EntityType}", entityType.Name);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to create table for {EntityType}", entityType.Name);
                throw;
            }
        }

        private static async Task SeedDataAsync(AppDbContext context, string provider, ILogger? logger)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(provider, nameof(provider));

            try
            {
                // Check if we need to seed initial data
                if (!await context.Users.AnyAsync())
                {
                    logger?.LogInformation("Seeding initial data...");

                    // Create default permissions first
                    var defaultPermissions = new[]
                    {
                        // User Management
                        new Permission { Name = "users.view", Description = "View users", Category = "User Management" },
                        new Permission { Name = "users.create", Description = "Create users", Category = "User Management" },
                        new Permission { Name = "users.edit", Description = "Edit users", Category = "User Management" },
                        new Permission { Name = "users.delete", Description = "Delete users", Category = "User Management" },
                        
                        // Role Management
                        new Permission { Name = "roles.view", Description = "View roles", Category = "Role Management" },
                        new Permission { Name = "roles.create", Description = "Create roles", Category = "Role Management" },
                        new Permission { Name = "roles.edit", Description = "Edit roles", Category = "Role Management" },
                        new Permission { Name = "roles.delete", Description = "Delete roles", Category = "Role Management" },
                        
                        // Employee Management
                        new Permission { Name = "employees.view", Description = "View employees", Category = "Employee Management" },
                        new Permission { Name = "employees.create", Description = "Create employees", Category = "Employee Management" },
                        new Permission { Name = "employees.edit", Description = "Edit employees", Category = "Employee Management" },
                        new Permission { Name = "employees.delete", Description = "Delete employees", Category = "Employee Management" },
                        
                        // Bus Management
                        new Permission { Name = "buses.view", Description = "View buses", Category = "Bus Management" },
                        new Permission { Name = "buses.create", Description = "Create buses", Category = "Bus Management" },
                        new Permission { Name = "buses.edit", Description = "Edit buses", Category = "Bus Management" },
                        new Permission { Name = "buses.delete", Description = "Delete buses", Category = "Bus Management" },
                        
                        // Route Management
                        new Permission { Name = "routes.view", Description = "View routes", Category = "Route Management" },
                        new Permission { Name = "routes.create", Description = "Create routes", Category = "Route Management" },
                        new Permission { Name = "routes.edit", Description = "Edit routes", Category = "Route Management" },
                        new Permission { Name = "routes.delete", Description = "Delete routes", Category = "Route Management" },
                        
                        // Ticket Management
                        new Permission { Name = "tickets.view", Description = "View tickets", Category = "Ticket Management" },
                        new Permission { Name = "tickets.create", Description = "Create tickets", Category = "Ticket Management" },
                        new Permission { Name = "tickets.edit", Description = "Edit tickets", Category = "Ticket Management" },
                        new Permission { Name = "tickets.delete", Description = "Delete tickets", Category = "Ticket Management" },
                        
                        // Sales Management
                        new Permission { Name = "sales.view", Description = "View sales", Category = "Sales Management" },
                        new Permission { Name = "sales.create", Description = "Create sales", Category = "Sales Management" },
                        new Permission { Name = "sales.edit", Description = "Edit sales", Category = "Sales Management" },
                        new Permission { Name = "sales.delete", Description = "Delete sales", Category = "Sales Management" },
                        
                        // Maintenance Management
                        new Permission { Name = "maintenance.view", Description = "View maintenance records", Category = "Maintenance Management" },
                        new Permission { Name = "maintenance.create", Description = "Create maintenance records", Category = "Maintenance Management" },
                        new Permission { Name = "maintenance.edit", Description = "Edit maintenance records", Category = "Maintenance Management" },
                        new Permission { Name = "maintenance.delete", Description = "Delete maintenance records", Category = "Maintenance Management" },
                        
                        // Reports
                        new Permission { Name = "reports.view", Description = "View reports", Category = "Reports" },
                        new Permission { Name = "reports.create", Description = "Create reports", Category = "Reports" },
                        new Permission { Name = "reports.export", Description = "Export reports", Category = "Reports" }
                    };

                    // Add permissions to database
                    await context.Permissions.AddRangeAsync(defaultPermissions);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Default permissions seeded successfully.");

                    // Create default roles
                    var roles = new[]
                    {
                        new Roles
                        {
                            RoleId = Guid.NewGuid(),
                            LegacyRoleId = 1,
                            Name = "Administrator",
                            Description = "Full system access",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            IsSystem = true,
                            Priority = 100
                        },
                        new Roles
                        {
                            RoleId = Guid.NewGuid(),
                            LegacyRoleId = 0,
                            Name = "User",
                            Description = "Basic system access",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            IsSystem = true,
                            Priority = 1
                        },
                        new Roles
                        {
                            RoleId = Guid.NewGuid(),
                            LegacyRoleId = 2,
                            Name = "Manager",
                            Description = "System management access",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            IsSystem = true,
                            Priority = 50
                        }
                    };

                    await context.Roles.AddRangeAsync(roles);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Default roles seeded successfully.");

                    // Create admin user with proper GUID
                    var adminGuid = Guid.NewGuid();
                    var admin = new User
                    {
                        Login = "admin",
                        PasswordHash = ComputeHash("admin"),
                        Role = 1, // Admin role
                        GuidId = adminGuid,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    // Ensure the admin user is recreated
                    await context.Database.ExecuteSqlRawAsync("DELETE FROM Users WHERE Login = 'admin'");
                    await context.Users.AddAsync(admin);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Admin user created successfully.");

                    // Assign admin role to admin user
                    var adminRole = roles[0]; // Administrator role
                    var adminUserRole = new UserRole
                    {
                        UserId = admin.GuidId,
                        RoleId = adminRole.RoleId,
                        AssignedAt = DateTime.UtcNow,
                        AssignedBy = "System"
                    };
                    await context.UserRoles.AddAsync(adminUserRole);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Admin role assigned to admin user successfully.");

                    // Assign all permissions to admin role
                    var adminPermissions = defaultPermissions.Select(p => new RolePermission
                    {
                        RoleId = adminRole.RoleId,
                        PermissionId = p.PermissionId,
                        GrantedAt = DateTime.UtcNow,
                        GrantedBy = "System"
                    });
                    await context.RolePermissions.AddRangeAsync(adminPermissions);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("All permissions assigned to admin role successfully.");

                    // Assign view permissions to user role
                    var userRole = roles[1]; // User role
                    var userPermissions = defaultPermissions
                        .Where(p => p.Name.EndsWith(".view"))
                        .Select(p => new RolePermission
                        {
                            RoleId = userRole.RoleId,
                            PermissionId = p.PermissionId,
                            GrantedAt = DateTime.UtcNow,
                            GrantedBy = "System"
                        });
                    await context.RolePermissions.AddRangeAsync(userPermissions);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("View permissions assigned to user role successfully.");

                    // Assign manager permissions (view + create + edit)
                    var managerRole = roles[2]; // Manager role
                    var managerPermissions = defaultPermissions
                        .Where(p => p.Name.EndsWith(".view") || p.Name.EndsWith(".create") || p.Name.EndsWith(".edit"))
                        .Select(p => new RolePermission
                        {
                            RoleId = managerRole.RoleId,
                            PermissionId = p.PermissionId,
                            GrantedAt = DateTime.UtcNow,
                            GrantedBy = "System"
                        });
                    await context.RolePermissions.AddRangeAsync(managerPermissions);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Manager permissions assigned successfully.");


                    // Create regular user with proper GUID
                    var guestGuid = Guid.NewGuid();
                    var guest = new User
                    {
                        Login = "guest",
                        PasswordHash = ComputeHash("gX9#mP2$kL5"), // Secure password
                        Role = 0, // Regular user role
                        GuidId = guestGuid,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    // Ensure the guest user is recreated
                    await context.Database.ExecuteSqlRawAsync("DELETE FROM Users WHERE Login = 'guest'");
                    await context.Users.AddAsync(guest);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Guest user created successfully.");

                    // Assign regular user role to guest user
                    var regularRole = roles[1]; // User role
                    var guestUserRole = new UserRole
                    {
                        UserId = guest.GuidId,
                        RoleId = regularRole.RoleId,
                        AssignedAt = DateTime.UtcNow,
                        AssignedBy = "System"
                    };
                    await context.UserRoles.AddAsync(guestUserRole);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Regular role assigned to guest user successfully.");





                    // Seed Jobs
                    var jobs = new[]
                    {
                        new Job { JobTitle = "Водитель автобуса", Internship = "Стажировка (2 года)" },
                        new Job { JobTitle = "Механик", Internship = "Стажировка (3 года)" },
                        new Job { JobTitle = "Диспетчер", Internship = "Стажировка (1 год)" },
                        new Job { JobTitle = "Начальник автопарка", Internship = "Стажировка (5 лет)" },
                        new Job { JobTitle = "Кассир", Internship = "Стажировка (6 месяцев)" },
                        new Job { JobTitle = "Инженер по безопасности", Internship = "Стажировка (3 года)" },
                        new Job { JobTitle = "Автоэлектрик", Internship = "Стажировка (2 года)" },
                        new Job { JobTitle = "Мойщик автобусов", Internship = "Стажировка (1 месяц)" },
                        new Job { JobTitle = "Сменный мастер", Internship = "Стажировка (4 года)" },
                        new Job { JobTitle = "Контролер", Internship = "Стажировка (1 год)" }
                    };
                    await context.Jobs.AddRangeAsync(jobs);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Jobs seeded successfully.");

                    // Seed Employees
                    var employees = new[]
                    {
                        new Employee { Surname = "Иванов", Name = "Иван", Patronym = "Иванович", EmployedSince = new DateTime(2020, 1, 15), JobId = jobs[0].JobId },
                        new Employee { Surname = "Петров", Name = "Петр", Patronym = "Петрович", EmployedSince = new DateTime(2019, 3, 20), JobId = jobs[0].JobId },
                        new Employee { Surname = "Сидоров", Name = "Алексей", Patronym = "Михайлович", EmployedSince = new DateTime(2018, 6, 10), JobId = jobs[1].JobId },
                        new Employee { Surname = "Козлов", Name = "Дмитрий", Patronym = "Сергеевич", EmployedSince = new DateTime(2021, 2, 5), JobId = jobs[2].JobId },
                        new Employee { Surname = "Морозов", Name = "Андрей", Patronym = "Владимирович", EmployedSince = new DateTime(2017, 8, 25), JobId = jobs[3].JobId },
                        new Employee { Surname = "Новиков", Name = "Сергей", Patronym = "Александрович", EmployedSince = new DateTime(2022, 4, 12), JobId = jobs[4].JobId },
                        new Employee { Surname = "Волков", Name = "Михаил", Patronym = "Дмитриевич", EmployedSince = new DateTime(2020, 11, 30), JobId = jobs[0].JobId },
                        new Employee { Surname = "Соловьев", Name = "Артем", Patronym = "Игоревич", EmployedSince = new DateTime(2019, 9, 15), JobId = jobs[1].JobId },
                        new Employee { Surname = "Васильев", Name = "Николай", Patronym = "Андреевич", EmployedSince = new DateTime(2021, 7, 8), JobId = jobs[0].JobId },
                        new Employee { Surname = "Зайцев", Name = "Владимир", Patronym = "Петрович", EmployedSince = new DateTime(2018, 12, 3), JobId = jobs[2].JobId }
                    };
                    await context.Employees.AddRangeAsync(employees);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Employees seeded successfully.");

                    // Seed Buses
                    var buses = new[]
                    {
                        new Avtobus { Model = "МАЗ-203.069" },
                        new Avtobus { Model = "МАЗ-215.069" },
                        new Avtobus { Model = "МАЗ-107.468" },
                        new Avtobus { Model = "МАЗ-103.065" },
                        new Avtobus { Model = "МАЗ-203.169" },
                        new Avtobus { Model = "МАЗ-105.065" },
                        new Avtobus { Model = "МАЗ-203.L65" },
                        new Avtobus { Model = "МАЗ-206.068" },
                        new Avtobus { Model = "МАЗ-103.465" },
                        new Avtobus { Model = "МАЗ-107.066" }
                    };
                    await context.Avtobusy.AddRangeAsync(buses);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Buses seeded successfully.");

                    // Seed Routes (Mogilev bus routes)
                    var routes = new[]
                    {
                        new Marshut { StartPoint = "Вейнянка", EndPoint = "Фатина", DriverId = employees[0].EmpId, BusId = buses[0].BusId, TravelTime = "45 минут" },
                        new Marshut { StartPoint = "Мал. Боровка", EndPoint = "Солтановка", DriverId = employees[1].EmpId, BusId = buses[1].BusId, TravelTime = "50 минут" },
                        new Marshut { StartPoint = "Вокзал", EndPoint = "Спутник", DriverId = employees[6].EmpId, BusId = buses[2].BusId, TravelTime = "40 минут" },
                        new Marshut { StartPoint = "Мясокомбинат", EndPoint = "Заводская", DriverId = employees[8].EmpId, BusId = buses[3].BusId, TravelTime = "35 минут" },
                        new Marshut { StartPoint = "Броды", EndPoint = "Казимировка", DriverId = employees[0].EmpId, BusId = buses[4].BusId, TravelTime = "55 минут" },
                        new Marshut { StartPoint = "Гребеневский рынок", EndPoint = "Холмы", DriverId = employees[1].EmpId, BusId = buses[5].BusId, TravelTime = "45 минут" },
                        new Marshut { StartPoint = "Автовокзал", EndPoint = "Полыковичи", DriverId = employees[6].EmpId, BusId = buses[6].BusId, TravelTime = "40 минут" },
                        new Marshut { StartPoint = "Центр", EndPoint = "Сидоровичи", DriverId = employees[8].EmpId, BusId = buses[7].BusId, TravelTime = "60 минут" },
                        new Marshut { StartPoint = "Площадь Славы", EndPoint = "Буйничи", DriverId = employees[0].EmpId, BusId = buses[8].BusId, TravelTime = "30 минут" },
                        new Marshut { StartPoint = "Заднепровье", EndPoint = "Химволокно", DriverId = employees[1].EmpId, BusId = buses[9].BusId, TravelTime = "25 минут" },
                        new Marshut { StartPoint = "Вокзал", EndPoint = "Соломинка", DriverId = employees[0].EmpId, BusId = buses[0].BusId, TravelTime = "35 минут" },
                        new Marshut { StartPoint = "Площадь Ленина", EndPoint = "Чаусы", DriverId = employees[1].EmpId, BusId = buses[1].BusId, TravelTime = "50 минут" },
                        new Marshut { StartPoint = "Могилев-2", EndPoint = "Дашковка", DriverId = employees[6].EmpId, BusId = buses[2].BusId, TravelTime = "40 минут" },
                        new Marshut { StartPoint = "Кожзавод", EndPoint = "Сухари", DriverId = employees[8].EmpId, BusId = buses[3].BusId, TravelTime = "45 минут" },
                        new Marshut { StartPoint = "Гребеневский рынок", EndPoint = "Любуж", DriverId = employees[0].EmpId, BusId = buses[4].BusId, TravelTime = "30 минут" }
                    };
                    await context.Marshuti.AddRangeAsync(routes);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Routes seeded successfully.");

                    // Seed Tickets with realistic Mogilev prices
                    var tickets = new[]
                    {
                        new Bilet { RouteId = routes[0].RouteId, TicketPrice = 0.85m }, // Вейнянка - Фатина
                        new Bilet { RouteId = routes[1].RouteId, TicketPrice = 0.85m }, // Мал. Боровка - Солтановка
                        new Bilet { RouteId = routes[2].RouteId, TicketPrice = 0.75m }, // Вокзал - Спутник
                        new Bilet { RouteId = routes[3].RouteId, TicketPrice = 0.75m }, // Мясокомбинат - Заводская
                        new Bilet { RouteId = routes[4].RouteId, TicketPrice = 0.85m }, // Броды - Казимировка
                        new Bilet { RouteId = routes[5].RouteId, TicketPrice = 0.85m }, // Гребеневский рынок - Холмы
                        new Bilet { RouteId = routes[6].RouteId, TicketPrice = 0.75m }, // Автовокзал - Полыковичи
                        new Bilet { RouteId = routes[7].RouteId, TicketPrice = 0.95m }, // Центр - Сидоровичи
                        new Bilet { RouteId = routes[8].RouteId, TicketPrice = 0.75m }, // Площадь Славы - Буйничи
                        new Bilet { RouteId = routes[9].RouteId, TicketPrice = 0.75m }, // Заднепровье - Химволокно
                        new Bilet { RouteId = routes[10].RouteId, TicketPrice = 0.75m }, // Вокзал - Соломинка
                        new Bilet { RouteId = routes[11].RouteId, TicketPrice = 0.75m }, // Площадь Ленина - Чаусы
                        new Bilet { RouteId = routes[12].RouteId, TicketPrice = 0.75m }, // Могилев-2 - Дашковка
                        new Bilet { RouteId = routes[13].RouteId, TicketPrice = 0.75m }, // Кожзавод - Сухари
                        new Bilet { RouteId = routes[14].RouteId, TicketPrice = 0.75m } // Гребеневский рынок - Любуж
                    };
                    await context.Bilety.AddRangeAsync(tickets);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Tickets seeded successfully.");

                    // Seed Sales with historical data from September 2021
                    var sales = new[]
                    {
                        // September 2021
                        new Prodazha { TicketId = tickets[0].TicketId, SaleDate = new DateTime(2021, 9, 1, 8, 30, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[1].TicketId, SaleDate = new DateTime(2021, 9, 1, 9, 15, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[2].TicketId, SaleDate = new DateTime(2021, 9, 2, 10, 0, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[3].TicketId, SaleDate = new DateTime(2021, 9, 2, 11, 30, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[4].TicketId, SaleDate = new DateTime(2021, 9, 3, 12, 45, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        // October 2021
                        new Prodazha { TicketId = tickets[5].TicketId, SaleDate = new DateTime(2021, 10, 1, 8, 0, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[6].TicketId, SaleDate = new DateTime(2021, 10, 1, 9, 30, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[7].TicketId, SaleDate = new DateTime(2021, 10, 2, 10, 15, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[8].TicketId, SaleDate = new DateTime(2021, 10, 2, 11, 45, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[9].TicketId, SaleDate = new DateTime(2021, 10, 3, 13, 0, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        // November 2021
                        new Prodazha { TicketId = tickets[0].TicketId, SaleDate = new DateTime(2021, 11, 1, 8, 15, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[1].TicketId, SaleDate = new DateTime(2021, 11, 1, 9, 45, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[2].TicketId, SaleDate = new DateTime(2021, 11, 2, 10, 30, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[3].TicketId, SaleDate = new DateTime(2021, 11, 2, 12, 0, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[4].TicketId, SaleDate = new DateTime(2021, 11, 3, 13, 15, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        // December 2021
                        new Prodazha { TicketId = tickets[5].TicketId, SaleDate = new DateTime(2021, 12, 1, 8, 45, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[6].TicketId, SaleDate = new DateTime(2021, 12, 1, 10, 0, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[7].TicketId, SaleDate = new DateTime(2021, 12, 2, 11, 15, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[8].TicketId, SaleDate = new DateTime(2021, 12, 2, 12, 30, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[9].TicketId, SaleDate = new DateTime(2021, 12, 3, 14, 0, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        // January 2022
                        new Prodazha { TicketId = tickets[0].TicketId, SaleDate = new DateTime(2022, 1, 3, 8, 0, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[1].TicketId, SaleDate = new DateTime(2022, 1, 3, 9, 30, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[2].TicketId, SaleDate = new DateTime(2022, 1, 4, 10, 45, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[3].TicketId, SaleDate = new DateTime(2022, 1, 4, 12, 15, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[4].TicketId, SaleDate = new DateTime(2022, 1, 5, 13, 30, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        // February 2022
                        new Prodazha { TicketId = tickets[5].TicketId, SaleDate = new DateTime(2022, 2, 1, 8, 30, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[6].TicketId, SaleDate = new DateTime(2022, 2, 1, 10, 0, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[7].TicketId, SaleDate = new DateTime(2022, 2, 2, 11, 30, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[8].TicketId, SaleDate = new DateTime(2022, 2, 2, 13, 0, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[9].TicketId, SaleDate = new DateTime(2022, 2, 3, 14, 30, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        // March 2022
                        new Prodazha { TicketId = tickets[0].TicketId, SaleDate = new DateTime(2022, 3, 1, 8, 15, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[1].TicketId, SaleDate = new DateTime(2022, 3, 1, 9, 45, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[2].TicketId, SaleDate = new DateTime(2022, 3, 2, 11, 0, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[3].TicketId, SaleDate = new DateTime(2022, 3, 2, 12, 30, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[4].TicketId, SaleDate = new DateTime(2022, 3, 3, 14, 0, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        // April 2022
                        new Prodazha { TicketId = tickets[5].TicketId, SaleDate = new DateTime(2022, 4, 1, 8, 45, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[6].TicketId, SaleDate = new DateTime(2022, 4, 1, 10, 15, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[7].TicketId, SaleDate = new DateTime(2022, 4, 2, 11, 45, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[8].TicketId, SaleDate = new DateTime(2022, 4, 2, 13, 15, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        new Prodazha { TicketId = tickets[9].TicketId, SaleDate = new DateTime(2022, 4, 3, 14, 45, 0), TicketSoldToUser = "ФИЗ.ПРОДАЖА" },
                        // Recent sales (last few days) - mix of admin and guest users
                        new Prodazha { TicketId = tickets[0].TicketId, SaleDate = DateTime.Now.AddDays(-5), TicketSoldToUser = "admin" },
                        new Prodazha { TicketId = tickets[1].TicketId, SaleDate = DateTime.Now.AddDays(-4), TicketSoldToUser = "guest" },
                        new Prodazha { TicketId = tickets[2].TicketId, SaleDate = DateTime.Now.AddDays(-3), TicketSoldToUser = "admin" },
                        new Prodazha { TicketId = tickets[3].TicketId, SaleDate = DateTime.Now.AddDays(-2), TicketSoldToUser = "guest" },
                        new Prodazha { TicketId = tickets[4].TicketId, SaleDate = DateTime.Now.AddDays(-1), TicketSoldToUser = "admin" },
                        new Prodazha { TicketId = tickets[5].TicketId, SaleDate = DateTime.Now.AddHours(-12), TicketSoldToUser = "guest" },
                        new Prodazha { TicketId = tickets[6].TicketId, SaleDate = DateTime.Now.AddHours(-8), TicketSoldToUser = "admin" },
                        new Prodazha { TicketId = tickets[7].TicketId, SaleDate = DateTime.Now.AddHours(-6), TicketSoldToUser = "guest" },
                        new Prodazha { TicketId = tickets[8].TicketId, SaleDate = DateTime.Now.AddHours(-4), TicketSoldToUser = "admin" },
                        new Prodazha { TicketId = tickets[9].TicketId, SaleDate = DateTime.Now.AddHours(-2), TicketSoldToUser = "guest" }
                    };
                    await context.Prodazhi.AddRangeAsync(sales);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Sales seeded successfully.");

                    // Seed Maintenance Records
                    var maintenance = new[]
                    {
                        new Obsluzhivanie { BusId = buses[0].BusId, LastServiceDate = DateTime.Now.AddMonths(-1), MileageThreshold ="100000 km",MaintenanceType ="Замена масла, фильтров",ServiceEngineer = "Сидоров А.М.", FoundIssues = "Закончилось масло, грязные фильтры", NextServiceDate = DateTime.Now.AddMonths(2), Roadworthiness = "Исправен" },
                        new Obsluzhivanie { BusId = buses[1].BusId, LastServiceDate = DateTime.Now.AddMonths(-2), MileageThreshold ="100000 km",MaintenanceType ="Регулировка тормозов",ServiceEngineer = "Соловьев А.И.", FoundIssues = "Тормоза", NextServiceDate = DateTime.Now.AddMonths(1), Roadworthiness = "Исправен" },
                        new Obsluzhivanie { BusId = buses[2].BusId, LastServiceDate = DateTime.Now.AddDays(-15), MileageThreshold ="100000 km",MaintenanceType ="Замена тормозных колодок",ServiceEngineer = "Сидоров А.М.", FoundIssues = "Тормозные колодки", NextServiceDate = DateTime.Now.AddMonths(3), Roadworthiness = "Исправен" },
                        new Obsluzhivanie { BusId = buses[3].BusId, LastServiceDate = DateTime.Now.AddDays(-45), MileageThreshold ="100000 km",MaintenanceType ="Диагностика двигателя",ServiceEngineer = "Соловьев А.И.", FoundIssues = "Диагностика двигателя", NextServiceDate = DateTime.Now.AddMonths(2), Roadworthiness = "Требует внимания" },
                        new Obsluzhivanie { BusId = buses[4].BusId, LastServiceDate = DateTime.Now.AddMonths(-1), MileageThreshold ="100000 km",MaintenanceType ="Плановый осмотр",ServiceEngineer = "Сидоров А.М.", FoundIssues = "Плановый осмотр", NextServiceDate = DateTime.Now.AddMonths(2), Roadworthiness = "Исправен" },
                        new Obsluzhivanie { BusId = buses[5].BusId, LastServiceDate = DateTime.Now.AddDays(-20), MileageThreshold ="100000 km",MaintenanceType ="Замена ремня ГРМ",ServiceEngineer = "Соловьев А.И.", FoundIssues = "Ремень ГРМ", NextServiceDate = DateTime.Now.AddMonths(3), Roadworthiness = "Исправен" },
                        new Obsluzhivanie { BusId = buses[6].BusId, LastServiceDate = DateTime.Now.AddMonths(-2), MileageThreshold ="100000 km",MaintenanceType ="Ремонт системы охлаждения",ServiceEngineer = "Сидоров А.М.", FoundIssues = "Ремонт системы охлаждения", NextServiceDate = DateTime.Now.AddMonths(1), Roadworthiness = "Исправен" },
                        new Obsluzhivanie { BusId = buses[7].BusId, LastServiceDate = DateTime.Now.AddDays(-30), MileageThreshold ="100000 km",MaintenanceType ="Замена аккумулятора",ServiceEngineer = "Соловьев А.И.", FoundIssues = "Аккумулятор", NextServiceDate = DateTime.Now.AddMonths(2), Roadworthiness = "Исправен" },
                        new Obsluzhivanie { BusId = buses[8].BusId, LastServiceDate = DateTime.Now.AddDays(-10), MileageThreshold ="100000 km",MaintenanceType ="Диагностика электрики",ServiceEngineer = "Сидоров А.М.", FoundIssues = "Диагностика электрики", NextServiceDate = DateTime.Now.AddMonths(3), Roadworthiness = "Требует внимания" },
                        new Obsluzhivanie { BusId = buses[9].BusId, LastServiceDate = DateTime.Now.AddDays(-5), MileageThreshold ="100000 km",MaintenanceType ="Плановое ТО",ServiceEngineer = "Соловьев А.И.", FoundIssues = "Плановое ТО", NextServiceDate = DateTime.Now.AddMonths(2), Roadworthiness = "Исправен" }
                    };
                    await context.Obsluzhivanies.AddRangeAsync(maintenance);


                    // Generate comprehensive route schedules from 2021 to today
                    var startDate = new DateTime(2021, 1, 1);
                    var endDate = DateTime.Today;
                    var routeSchedules = new List<RouteSchedules>();

                    // Base route configurations
                    var routeConfigs = new[]
                    {
                        // Main routes
                        (start: "Вейнянка", end: "Фатина", stops: new[] {"Вейнянка", "Площадь Орджоникидзе", "Областная больница", "Фатина"}, price: 0.75),
                        (start: "Малая Боровка", end: "Солтановка", stops: new[] {"Малая Боровка", "Машековка", "Центр", "Солтановка"}, price: 0.75),
                        (start: "Железнодорожный вокзал", end: "Спутник", stops: new[] {"Вокзал", "Площадь Ленина", "Универмаг", "Спутник"}, price: 0.75),
                        (start: "Мясокомбинат", end: "Заводская", stops: new[] {"Мясокомбинат", "Димитрова", "Юбилейный", "Заводская"}, price: 0.75),
                        (start: "Броды", end: "Казимировка", stops: new[] {"Броды", "Центр", "Площадь Славы", "Казимировка"}, price: 0.75),
                        (start: "Гребеневский рынок", end: "Холмы", stops: new[] {"Гребеневский рынок", "Площадь Орджоникидзе", "Мир", "Холмы"}, price: 0.75),
                        (start: "Автовокзал", end: "Полыковичи", stops: new[] {"Автовокзал", "Площадь Ленина", "Димитрова", "Полыковичи"}, price: 0.75),
                        (start: "Центр", end: "Сидоровичи", stops: new[] {"Центр", "Площадь Славы", "Заднепровье", "Сидоровичи"}, price: 0.75),
                        (start: "Площадь Славы", end: "Буйничи", stops: new[] {"Площадь Славы", "Областная больница", "Зоосад", "Буйничи"}, price: 0.75),
                        (start: "Заднепровье", end: "Химволокно", stops: new[] {"Заднепровье", "Центр", "Юбилейный", "Химволокно"}, price: 0.75),
                        // Additional routes
                        (start: "Вокзал", end: "Соломинка", stops: new[] {"Вокзал", "Центр", "Димитрова", "Соломинка"}, price: 0.75),
                        (start: "Площадь Ленина", end: "Чаусы", stops: new[] {"Площадь Ленина", "Центр", "Заднепровье", "Чаусы"}, price: 0.75),
                        (start: "Могилев-2", end: "Дашковка", stops: new[] {"Могилев-2", "Центр", "Юбилейный", "Дашковка"}, price: 0.75),
                        (start: "Кожзавод", end: "Сухари", stops: new[] {"Кожзавод", "Центр", "Площадь Славы", "Сухари"}, price: 0.75),
                        (start: "Гребеневский рынок", end: "Любуж", stops: new[] {"Гребеневский рынок", "Центр", "Заднепровье", "Любуж"}, price: 0.75)
                    };

                    // Bus types available
                    var busTypes = new[] { "МАЗ-103", "МАЗ-107", "МАЗ-215", "МАЗ-231" };

                    // Generate schedules for each route configuration
                    for (int routeIndex = 0; routeIndex < routeConfigs.Length; routeIndex++)
                    {
                        var config = routeConfigs[routeIndex];
                        var currentDate = startDate;

                        while (currentDate <= endDate)
                        {
                            // Generate schedules for each day
                            var routeId = routes[routeIndex % routes.Length].RouteId;
                            var busType = busTypes[routeIndex % busTypes.Length];

                            // Morning schedule (every day)
                            routeSchedules.Add(new RouteSchedules
                            {
                                RouteId = routeId,
                                StartPoint = config.start,
                                EndPoint = config.end,
                                RouteStops = config.stops,
                                DepartureTime = currentDate.AddHours(6),
                                ArrivalTime = currentDate.AddHours(7),
                                Price = config.price,
                                AvailableSeats = 42,
                                DaysOfWeek = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" },
                                BusTypes = new[] { busType },
                                EstimatedStopTimes = new[] { "06:00", "06:20", "06:40", "07:00" },
                                StopDistances = new[] { 0.0, 2.5, 4.8, 6.3 },
                                ValidFrom = currentDate,
                                ValidUntil = currentDate.AddDays(1),
                                IsActive = currentDate <= DateTime.Today,
                                StopDurationMinutes = 5,
                                IsRecurring = true,
                                Notes = $"Regular route {config.start} - {config.end}",
                                CreatedAt = DateTime.Now,
                                UpdatedBy = "System"
                            });

                            // Afternoon schedule (weekdays only)
                            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                            {
                                routeSchedules.Add(new RouteSchedules
                                {
                                    RouteId = routeId,
                                    StartPoint = config.start,
                                    EndPoint = config.end,
                                    RouteStops = config.stops,
                                    DepartureTime = currentDate.AddHours(14),
                                    ArrivalTime = currentDate.AddHours(15),
                                    Price = config.price,
                                    AvailableSeats = 42,
                                    DaysOfWeek = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
                                    BusTypes = new[] { busType },
                                    EstimatedStopTimes = new[] { "14:00", "14:20", "14:40", "15:00" },
                                    StopDistances = new[] { 0.0, 2.5, 4.8, 6.3 },
                                    ValidFrom = currentDate,
                                    ValidUntil = currentDate.AddDays(1),
                                    IsActive = currentDate <= DateTime.Today,
                                    StopDurationMinutes = 5,
                                    IsRecurring = true,
                                    Notes = $"Regular route {config.start} - {config.end}",
                                    CreatedAt = DateTime.Now,
                                    UpdatedBy = "System"
                                });
                            }

                            // Evening schedule (every day)
                            routeSchedules.Add(new RouteSchedules
                            {
                                RouteId = routeId,
                                StartPoint = config.start,
                                EndPoint = config.end,
                                RouteStops = config.stops,
                                DepartureTime = currentDate.AddHours(18),
                                ArrivalTime = currentDate.AddHours(19),
                                Price = config.price,
                                AvailableSeats = 42,
                                DaysOfWeek = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" },
                                BusTypes = new[] { busType },
                                EstimatedStopTimes = new[] { "18:00", "18:20", "18:40", "19:00" },
                                StopDistances = new[] { 0.0, 2.5, 4.8, 6.3 },
                                ValidFrom = currentDate,
                                ValidUntil = currentDate.AddDays(1),
                                IsActive = currentDate <= DateTime.Today,
                                StopDurationMinutes = 5,
                                IsRecurring = true,
                                Notes = $"Regular route {config.start} - {config.end}",
                                CreatedAt = DateTime.Now,
                                UpdatedBy = "System"
                            });

                            currentDate = currentDate.AddDays(1); // Move to next day
                        }
                    }

                    await context.RouteSchedules.AddRangeAsync(routeSchedules);
                    logger?.LogInformation($"Added {routeSchedules.Count} route schedules from {startDate:d} to {endDate:d}");
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Maintenance records seeded successfully.");

                    logger?.LogInformation("All initial data seeded successfully.");
                }
                else
                {
                    logger?.LogInformation("Initial data already exists. Validating admin user...");
                    var adminExists = await context.Users.AnyAsync(u => u.Login == "admin" && u.Role == 1);
                    if (!adminExists)
                    {
                        logger?.LogWarning("Admin user not found or invalid. Recreating...");
                        await context.Database.ExecuteSqlRawAsync("DELETE FROM Users WHERE Login = 'admin'");
                var admin = new User
                {
                    Login = "admin",
                    PasswordHash = ComputeHash("admin"),
                            Role = 1,
                            GuidId = Guid.NewGuid(),
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                };
                        await context.Users.AddAsync(admin);
                        await context.SaveChangesAsync();
                        logger?.LogInformation("Admin user recreated successfully.");

                        // Ensure admin has proper role assignment
                        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.LegacyRoleId == 1);
                        if (adminRole != null)
                        {
                            var adminUserRole = new UserRole
                            {
                                UserId = admin.GuidId,
                                RoleId = adminRole.RoleId,
                                AssignedAt = DateTime.UtcNow,
                                AssignedBy = "System"
                            };
                            await context.UserRoles.AddAsync(adminUserRole);
                            await context.SaveChangesAsync();
                            logger?.LogInformation("Admin role reassigned successfully.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to seed data: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        private static string GenerateSqliteCreateTableSql(IEntityType entity)
        {
            ArgumentNullException.ThrowIfNull(entity, nameof(entity));

            var tableName = entity.GetTableName() 
                ?? throw new InvalidOperationException($"Table name not found for entity {entity.Name}");

            var properties = entity.GetProperties();
            var primaryKey = entity.FindPrimaryKey();
            var primaryKeyNames = primaryKey?.Properties.Select(p => p.Name).ToList() ?? new List<string>();
            var isCompositeKey = primaryKeyNames.Count > 1;

            var columns = properties.Select(p => 
            {
                var columnName = p.GetColumnName() 
                    ?? throw new InvalidOperationException($"Column name not found for property {p.Name}");
                var isNullable = p.ClrType.IsValueType ? Nullable.GetUnderlyingType(p.ClrType) != null : true;
                var isPrimaryKey = primaryKeyNames.Contains(p.Name) && !isCompositeKey;

                return $"{columnName} {GetSqliteType(p)} {(isPrimaryKey ? "PRIMARY KEY" : "")} {(!isNullable ? "NOT NULL" : "NULL")}";
            });

            var sql = $"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", columns)}";

            if (isCompositeKey)
            {
                sql += $", PRIMARY KEY ({string.Join(", ", primaryKeyNames)})";
            }

            sql += ");";
            return sql;
        }

        private static string GenerateSqlServerCreateTableSql(IEntityType entity)
        {
            ArgumentNullException.ThrowIfNull(entity, nameof(entity));

            var tableName = entity.GetTableName() 
                ?? throw new InvalidOperationException($"Table name not found for entity {entity.Name}");

            var properties = entity.GetProperties();
            var columns = properties.Select(p => 
            {
                var columnName = p.GetColumnName() 
                    ?? throw new InvalidOperationException($"Column name not found for property {p.Name}");
                var isNullable = p.ClrType.IsValueType ? Nullable.GetUnderlyingType(p.ClrType) != null : true;
                return $"{columnName} {GetSqlServerType(p)} {(p.IsKey() ? "PRIMARY KEY" : "")} {(!isNullable ? "NOT NULL" : "NULL")}";
            });

            return $"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{tableName}') CREATE TABLE {tableName} ({string.Join(", ", columns)});";
        }

        private static string GetSqliteType(IProperty property)
        {
            ArgumentNullException.ThrowIfNull(property, nameof(property));

            var type = property.ClrType;
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            return underlyingType.Name.ToLower() switch
            {
                "int32" or "int64" => "INTEGER",
                "decimal" => "REAL",
                "datetime" => "TEXT",
                "string" => "TEXT",
                "boolean" => "INTEGER",
                "guid" => "TEXT",
                _ => "TEXT"
            };
        }

        private static string GetSqlServerType(IProperty property)
        {
            ArgumentNullException.ThrowIfNull(property, nameof(property));

            return property.ClrType.Name.ToLower() switch
            {
                "int32" => "INT",
                "int64" => "BIGINT",
                "decimal" => "DECIMAL(18,2)",
                "datetime" => "DATETIME2",
                "string" => "NVARCHAR(MAX)",
                "boolean" => "BIT",
                _ => "NVARCHAR(MAX)"
            };
        }

        private static string ComputeHash(string input)
        {
            ArgumentNullException.ThrowIfNull(input, nameof(input));

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private static async Task<List<TableCheckResult>> GetTableStatisticsAsync(AppDbContext context)
        {
            var results = new List<TableCheckResult>();
            foreach (var table in RequiredTables)
            {
                try
                {
                    using var command = context.Database.GetDbConnection().CreateCommand();
                    command.CommandText = $"SELECT COUNT(*) FROM {table.Key}";

                    if (command.Connection!.State != System.Data.ConnectionState.Open)
                        await command.Connection.OpenAsync();

                    var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    results.Add(new TableCheckResult 
                    { 
                        TableName = table.Key, 
                        RowCount = count 
                    });
                }
                catch (Exception)
                {
                    // If table doesn't exist or can't be counted, add it with 0 rows
                    results.Add(new TableCheckResult 
                    { 
                        TableName = table.Key, 
                        RowCount = 0 
                    });
                }
            }
            return results;
        }
    }
}
#endif // MODERN