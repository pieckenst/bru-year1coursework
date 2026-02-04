using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Data.MongoDB;
using TicketSalesApp.Core.Data.MongoDB.Documents;
using TicketSalesApp.Core.Models;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.Services.Implementations
{
    /// <summary>
    /// Service for synchronizing data between SQL and MongoDB databases
    /// </summary>
    public class DataSynchronizationService : IDataSynchronizationService
    {
        private readonly AppDbContext _sqlContext;
        private readonly IMongoContext _mongoContext;
        private readonly ILogger<DataSynchronizationService> _logger;
        private bool _autoSyncEnabled = false;
        private readonly SynchronizationStatus _status = new();

        public DataSynchronizationService(
            AppDbContext sqlContext,
            IMongoContext mongoContext,
            ILogger<DataSynchronizationService> logger)
        {
            _sqlContext = sqlContext;
            _mongoContext = mongoContext;
            _logger = logger;
        }

        public bool IsAutoSyncEnabled => _autoSyncEnabled;

        public async Task SynchronizeAllAsync()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting full data synchronization from SQL to MongoDB");

            try
            {
                // Clear previous errors
                _status.Errors.Clear();

                // Synchronize all entity types
                await SynchronizeUsersAsync();
                await SynchronizeBusesAsync();
                await SynchronizeRoutesAsync();
                await SynchronizeTicketsAsync();
                await SynchronizeEmployeesAsync();

                _status.LastSyncTime = DateTime.UtcNow;
                _status.TotalSyncTime = DateTime.UtcNow - startTime;
                _status.IsEnabled = true;

                _logger.LogInformation("Full data synchronization completed in {Duration}ms", 
                    _status.TotalSyncTime.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _status.Errors.Add($"Full sync failed: {ex.Message}");
                _logger.LogError(ex, "Full data synchronization failed");
                throw;
            }
        }

        public async Task SynchronizeEntityAsync<T>() where T : class
        {
            var entityType = typeof(T);
            _logger.LogInformation("Synchronizing entity type: {EntityType}", entityType.Name);

            try
            {
                switch (entityType.Name)
                {
                    case nameof(User):
                        await SynchronizeUsersAsync();
                        break;
                    case nameof(Avtobus):
                        await SynchronizeBusesAsync();
                        break;
                    case nameof(Marshut):
                        await SynchronizeRoutesAsync();
                        break;
                    case nameof(Bilet):
                        await SynchronizeTicketsAsync();
                        break;
                    case nameof(Employee):
                        await SynchronizeEmployeesAsync();
                        break;
                    default:
                        _logger.LogWarning("Entity type {EntityType} is not supported for synchronization", entityType.Name);
                        break;
                }

                _status.LastEntitySync[entityType.Name] = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                var error = $"Entity sync failed for {entityType.Name}: {ex.Message}";
                _status.Errors.Add(error);
                _logger.LogError(ex, "Entity synchronization failed for {EntityType}", entityType.Name);
                throw;
            }
        }

        public async Task SynchronizeEntityAsync<T>(T entity) where T : class
        {
            if (entity == null) return;

            var entityType = typeof(T);
            _logger.LogDebug("Synchronizing single entity: {EntityType}", entityType.Name);

            try
            {
                switch (entity)
                {
                    case User user:
                        await SynchronizeUserAsync(user);
                        break;
                    case Avtobus bus:
                        await SynchronizeBusAsync(bus);
                        break;
                    case Marshut route:
                        await SynchronizeRouteAsync(route);
                        break;
                    case Bilet ticket:
                        await SynchronizeTicketAsync(ticket);
                        break;
                    case Employee employee:
                        await SynchronizeEmployeeAsync(employee);
                        break;
                    default:
                        _logger.LogWarning("Entity type {EntityType} is not supported for synchronization", entityType.Name);
                        break;
                }
            }
            catch (Exception ex)
            {
                var error = $"Single entity sync failed for {entityType.Name}: {ex.Message}";
                _status.Errors.Add(error);
                _logger.LogError(ex, "Single entity synchronization failed for {EntityType}", entityType.Name);
                throw;
            }
        }

        public async Task<SynchronizationStatus> GetSynchronizationStatusAsync()
        {
            // Update entity counts
            try
            {
                _status.EntityCounts["Users"] = await _sqlContext.Users.CountAsync();
                _status.EntityCounts["Buses"] = await _sqlContext.Avtobusy.CountAsync();
                _status.EntityCounts["Routes"] = await _sqlContext.Marshuti.CountAsync();
                _status.EntityCounts["Tickets"] = await _sqlContext.Bilety.CountAsync();
                _status.EntityCounts["Employees"] = await _sqlContext.Employees.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update entity counts in synchronization status");
            }

            return _status;
        }

        public async Task SetAutoSyncAsync(bool enabled)
        {
            _autoSyncEnabled = enabled;
            _logger.LogInformation("Auto-synchronization {Status}", enabled ? "enabled" : "disabled");
            
            if (enabled)
            {
                // Perform initial sync when enabling
                await SynchronizeAllAsync();
            }
        }

        private async Task SynchronizeUsersAsync()
        {
            var users = await _sqlContext.Users.ToListAsync();
            var userDocuments = users.Select(u => new UserDocument
            {
                Id = u.GuidId.ToString(),
                UserId = u.UserId,
                Login = u.Login,
                PasswordHash = u.PasswordHash,
                Role = u.Role,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            var collection = _mongoContext.GetCollection<UserDocument>();
            
            // Clear existing documents
            await collection.DeleteManyAsync(Builders<UserDocument>.Filter.Empty);
            
            // Insert new documents
            if (userDocuments.Any())
            {
                await collection.InsertManyAsync(userDocuments);
            }

            _logger.LogInformation("Synchronized {Count} users to MongoDB", userDocuments.Count);
        }

        private async Task SynchronizeBusesAsync()
        {
            var buses = await _sqlContext.Avtobusy.ToListAsync();
            var busDocuments = buses.Select(b => new BusDocument
            {
                Id = b.BusId.ToString(),
                BusId = b.BusId,
                BusNumber = b.BusId.ToString(), // Use BusId as number since BusNumber doesn't exist
                Model = b.Model,
                YearManufactured = DateTime.Now.Year, // Default year since not available
                Capacity = 50, // Default capacity since not available
                IsActive = true, // Default status since not available
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            var collection = _mongoContext.GetCollection<BusDocument>();
            
            // Clear existing documents
            await collection.DeleteManyAsync(Builders<BusDocument>.Filter.Empty);
            
            // Insert new documents
            if (busDocuments.Any())
            {
                await collection.InsertManyAsync(busDocuments);
            }

            _logger.LogInformation("Synchronized {Count} buses to MongoDB", busDocuments.Count);
        }

        private async Task SynchronizeRoutesAsync()
        {
            var routes = await _sqlContext.Marshuti.ToListAsync();
            var routeDocuments = routes.Select(r => new RouteDocument
            {
                Id = r.RouteId.ToString(),
                RouteId = r.RouteId,
                RouteName = $"{r.StartPoint} - {r.EndPoint}", // Create name from start/end points
                StartPoint = r.StartPoint,
                EndPoint = r.EndPoint,
                Distance = 0, // Default since not available
                EstimatedDuration = TimeSpan.Parse(r.TravelTime ?? "01:00:00"), // Use TravelTime if available
                BusId = r.BusId,
                DriverId = r.DriverId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            var collection = _mongoContext.GetCollection<RouteDocument>();
            
            // Clear existing documents
            await collection.DeleteManyAsync(Builders<RouteDocument>.Filter.Empty);
            
            // Insert new documents
            if (routeDocuments.Any())
            {
                await collection.InsertManyAsync(routeDocuments);
            }

            _logger.LogInformation("Synchronized {Count} routes to MongoDB", routeDocuments.Count);
        }

        private async Task SynchronizeTicketsAsync()
        {
            var tickets = await _sqlContext.Bilety.ToListAsync();
            var ticketDocuments = tickets.Select(t => new TicketDocument
            {
                Id = t.TicketId.ToString(),
                TicketId = t.TicketId,
                RouteId = t.RouteId,
                TicketPrice = t.TicketPrice,
                IsActive = true, // Default since not available
                TicketType = "Standard", // Default since not available
                ValidFrom = DateTime.UtcNow, // Default since not available
                ValidTo = DateTime.UtcNow.AddDays(1), // Default since not available
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            var collection = _mongoContext.GetCollection<TicketDocument>();
            
            // Clear existing documents
            await collection.DeleteManyAsync(Builders<TicketDocument>.Filter.Empty);
            
            // Insert new documents
            if (ticketDocuments.Any())
            {
                await collection.InsertManyAsync(ticketDocuments);
            }

            _logger.LogInformation("Synchronized {Count} tickets to MongoDB", ticketDocuments.Count);
        }

        private async Task SynchronizeEmployeesAsync()
        {
            var employees = await _sqlContext.Employees.ToListAsync();
            var employeeDocuments = employees.Select(e => new TicketSalesApp.Core.Data.MongoDB.Documents.EmployeeDocument
            {
                Id = e.EmpId.ToString(),
                EmployeeId = e.EmpId,
                FirstName = e.Name,
                LastName = e.Surname,
                MiddleName = e.Patronym, // Use Patronym as middle name
                HireDate = e.EmployedSince,
                JobId = e.JobId,
                DepartmentId = e.DepartmentId,
                DateOfBirth = e.DateOfBirth,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            var collection = _mongoContext.GetCollection<TicketSalesApp.Core.Data.MongoDB.Documents.EmployeeDocument>();
            
            // Clear existing documents
            await collection.DeleteManyAsync(Builders<TicketSalesApp.Core.Data.MongoDB.Documents.EmployeeDocument>.Filter.Empty);
            
            // Insert new documents
            if (employeeDocuments.Any())
            {
                await collection.InsertManyAsync(employeeDocuments);
            }

            _logger.LogInformation("Synchronized {Count} employees to MongoDB", employeeDocuments.Count);
        }

        // Single entity synchronization methods
        private async Task SynchronizeUserAsync(User user)
        {
            var userDocument = new UserDocument
            {
                Id = user.GuidId.ToString(),
                UserId = user.UserId,
                Login = user.Login,
                PasswordHash = user.PasswordHash,
                Role = user.Role,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var collection = _mongoContext.GetCollection<UserDocument>();
            var filter = Builders<UserDocument>.Filter.Eq(d => d.Id, userDocument.Id);
            
            await collection.ReplaceOneAsync(filter, userDocument, new ReplaceOptions { IsUpsert = true });
        }

        private async Task SynchronizeBusAsync(Avtobus bus)
        {
            var busDocument = new BusDocument
            {
                Id = bus.BusId.ToString(),
                BusId = bus.BusId,
                BusNumber = bus.BusId.ToString(),
                Model = bus.Model,
                YearManufactured = DateTime.Now.Year,
                Capacity = 50,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var collection = _mongoContext.GetCollection<BusDocument>();
            var filter = Builders<BusDocument>.Filter.Eq(d => d.Id, busDocument.Id);
            
            await collection.ReplaceOneAsync(filter, busDocument, new ReplaceOptions { IsUpsert = true });
        }

        private async Task SynchronizeRouteAsync(Marshut route)
        {
            var routeDocument = new RouteDocument
            {
                Id = route.RouteId.ToString(),
                RouteId = route.RouteId,
                RouteName = $"{route.StartPoint} - {route.EndPoint}",
                StartPoint = route.StartPoint,
                EndPoint = route.EndPoint,
                Distance = 0,
                EstimatedDuration = TimeSpan.Parse(route.TravelTime ?? "01:00:00"),
                BusId = route.BusId,
                DriverId = route.DriverId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var collection = _mongoContext.GetCollection<RouteDocument>();
            var filter = Builders<RouteDocument>.Filter.Eq(d => d.Id, routeDocument.Id);
            
            await collection.ReplaceOneAsync(filter, routeDocument, new ReplaceOptions { IsUpsert = true });
        }

        private async Task SynchronizeTicketAsync(Bilet ticket)
        {
            var ticketDocument = new TicketDocument
            {
                Id = ticket.TicketId.ToString(),
                TicketId = ticket.TicketId,
                RouteId = ticket.RouteId,
                TicketPrice = ticket.TicketPrice,
                IsActive = true,
                TicketType = "Standard",
                ValidFrom = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var collection = _mongoContext.GetCollection<TicketDocument>();
            var filter = Builders<TicketDocument>.Filter.Eq(d => d.Id, ticketDocument.Id);
            
            await collection.ReplaceOneAsync(filter, ticketDocument, new ReplaceOptions { IsUpsert = true });
        }

        private async Task SynchronizeEmployeeAsync(Employee employee)
        {
            var employeeDocument = new TicketSalesApp.Core.Data.MongoDB.Documents.EmployeeDocument
            {
                Id = employee.EmpId.ToString(),
                EmployeeId = employee.EmpId,
                FirstName = employee.Name,
                LastName = employee.Surname,
                MiddleName = employee.Patronym,
                HireDate = employee.EmployedSince,
                JobId = employee.JobId,
                DepartmentId = employee.DepartmentId,
                DateOfBirth = employee.DateOfBirth,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var collection = _mongoContext.GetCollection<TicketSalesApp.Core.Data.MongoDB.Documents.EmployeeDocument>();
            var filter = Builders<TicketSalesApp.Core.Data.MongoDB.Documents.EmployeeDocument>.Filter.Eq(d => d.Id, employeeDocument.Id);
            
            await collection.ReplaceOneAsync(filter, employeeDocument, new ReplaceOptions { IsUpsert = true });
        }
    }
}