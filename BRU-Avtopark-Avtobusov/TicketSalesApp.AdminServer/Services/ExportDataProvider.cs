using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using TicketSalesApp.AdminServer.Services.Interfaces;
using TicketSalesApp.Core.Data;

namespace TicketSalesApp.AdminServer.Services
{
    public class ExportDataProvider : IExportDataProvider
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ExportDataProvider> _logger;

        private static readonly Dictionary<string, Type> SupportedEntities = new()
        {
            { "users", typeof(TicketSalesApp.Core.Models.User) },
            { "employees", typeof(TicketSalesApp.Core.Models.Employee) },
            { "jobs", typeof(TicketSalesApp.Core.Models.Job) },
            { "buses", typeof(TicketSalesApp.Core.Models.Avtobus) },
            { "routes", typeof(TicketSalesApp.Core.Models.Marshut) },
            { "tickets", typeof(TicketSalesApp.Core.Models.Bilet) },
            { "sales", typeof(TicketSalesApp.Core.Models.Prodazha) },
            { "maintenance", typeof(TicketSalesApp.Core.Models.Obsluzhivanie) },
            { "departments", typeof(TicketSalesApp.Core.Models.Department) },
            { "routeschedules", typeof(TicketSalesApp.Core.Models.RouteSchedules) }
        };

        public ExportDataProvider(AppDbContext context, ILogger<ExportDataProvider> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> GetTotalCountAsync(string entityType, Dictionary<string, object>? filters = null)
        {
            if (!IsEntityTypeSupported(entityType))
                throw new ArgumentException($"Entity type '{entityType}' is not supported for export");

            var query = GetBaseQuery(entityType);
            
            if (filters != null && filters.Any())
            {
                query = ApplyFilters(query, filters);
            }

            // Cast to IQueryable<object> to enable Entity Framework async methods
            return await ((IQueryable<object>)query).CountAsync();
        }

        public async IAsyncEnumerable<IEnumerable<object>> GetDataBatchesAsync(
            string entityType, 
            Dictionary<string, object>? filters = null,
            string[]? selectedFields = null,
            int batchSize = 1000,
            int? maxRecords = null)
        {
            if (!IsEntityTypeSupported(entityType))
                throw new ArgumentException($"Entity type '{entityType}' is not supported for export");

            var query = GetBaseQuery(entityType);
            
            if (filters != null && filters.Any())
            {
                query = ApplyFilters(query, filters);
            }

            if (maxRecords.HasValue)
            {
                query = query.Take(maxRecords.Value);
            }

            var totalProcessed = 0;
            var skip = 0;

            while (true)
            {
                var batch = await ((IQueryable<object>)query)
                    .Skip(skip)
                    .Take(batchSize)
                    .ToListAsync();

                if (!batch.Any())
                    break;

                var projectedBatch = selectedFields != null 
                    ? ProjectFields(batch, selectedFields)
                    : batch.Cast<object>();

                yield return projectedBatch;

                totalProcessed += batch.Count;
                skip += batchSize;

                if (maxRecords.HasValue && totalProcessed >= maxRecords.Value)
                    break;

                _logger.LogDebug("Processed {ProcessedRecords} records for {EntityType} export", 
                    totalProcessed, entityType);
            }
        }

        public async Task<IEnumerable<object>> GetAllDataAsync(
            string entityType, 
            Dictionary<string, object>? filters = null,
            string[]? selectedFields = null,
            int? maxRecords = null)
        {
            if (!IsEntityTypeSupported(entityType))
                throw new ArgumentException($"Entity type '{entityType}' is not supported for export");

            var query = GetBaseQuery(entityType);
            
            if (filters != null && filters.Any())
            {
                query = ApplyFilters(query, filters);
            }

            if (maxRecords.HasValue)
            {
                query = query.Take(maxRecords.Value);
            }

            var data = await ((IQueryable<object>)query).ToListAsync();

            return selectedFields != null 
                ? ProjectFields(data, selectedFields)
                : data.Cast<object>();
        }

        public async Task<IEnumerable<string>> GetAvailableFieldsAsync(string entityType)
        {
            if (!IsEntityTypeSupported(entityType))
                throw new ArgumentException($"Entity type '{entityType}' is not supported for export");

            var entityTypeObj = SupportedEntities[entityType.ToLower()];
            var properties = entityTypeObj.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && IsExportableProperty(p))
                .Select(p => p.Name);

            return await Task.FromResult(properties);
        }

        public bool IsEntityTypeSupported(string entityType)
        {
            return SupportedEntities.ContainsKey(entityType.ToLower());
        }

        private IQueryable GetBaseQuery(string entityType)
        {
            return entityType.ToLower() switch
            {
                "users" => _context.Users.AsNoTracking(),
                "employees" => _context.Employees.AsNoTracking()
                    .Include(e => e.Job)
                    .Include(e => e.Department),
                "jobs" => _context.Jobs.AsNoTracking(),
                "buses" => _context.Avtobusy.AsNoTracking(),
                "routes" => _context.Marshuti.AsNoTracking()
                    .Include(m => m.Employee)
                    .Include(m => m.Avtobus),
                "tickets" => _context.Bilety.AsNoTracking()
                    .Include(b => b.Marshut),
                "sales" => _context.Prodazhi.AsNoTracking()
                    .Include(p => p.Bilet)
                    .ThenInclude(b => b.Marshut),
                "maintenance" => _context.Obsluzhivanies.AsNoTracking()
                    .Include(o => o.Avtobus),
                "departments" => _context.Departments.AsNoTracking()
                    .Include(d => d.ParentDepartment),
                "routeschedules" => _context.RouteSchedules.AsNoTracking()
                    .Include(rs => rs.Marshut),
                _ => throw new ArgumentException($"Entity type '{entityType}' is not supported")
            };
        }

        private IQueryable ApplyFilters(IQueryable query, Dictionary<string, object> filters)
        {
            foreach (var filter in filters)
            {
                try
                {
                    var filterExpression = BuildFilterExpression(filter.Key, filter.Value);
                    if (!string.IsNullOrEmpty(filterExpression))
                    {
                        query = query.Where(filterExpression);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to apply filter {FilterKey} with value {FilterValue}", 
                        filter.Key, filter.Value);
                }
            }

            return query;
        }

        private string BuildFilterExpression(string fieldName, object value)
        {
            if (value == null)
                return $"{fieldName} == null";

            return value switch
            {
                string stringValue when stringValue.Contains('*') => 
                    $"{fieldName}.Contains(\"{stringValue.Replace("*", "")}\")",
                string stringValue => 
                    $"{fieldName} == \"{stringValue}\"",
                DateTime dateValue => 
                    $"{fieldName} >= DateTime({dateValue.Year}, {dateValue.Month}, {dateValue.Day})",
                int intValue => 
                    $"{fieldName} == {intValue}",
                long longValue => 
                    $"{fieldName} == {longValue}",
                bool boolValue => 
                    $"{fieldName} == {boolValue.ToString().ToLower()}",
                _ => string.Empty
            };
        }

        private IEnumerable<object> ProjectFields(IEnumerable<object> data, string[] selectedFields)
        {
            return data.Select(item =>
            {
                var projected = new Dictionary<string, object?>();
                var itemType = item.GetType();

                foreach (var field in selectedFields)
                {
                    try
                    {
                        var property = itemType.GetProperty(field, BindingFlags.Public | BindingFlags.Instance);
                        if (property != null && property.CanRead)
                        {
                            var value = property.GetValue(item);
                            projected[field] = value;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to project field {FieldName} from {ItemType}", 
                            field, itemType.Name);
                        projected[field] = null;
                    }
                }

                return (object)projected;
            });
        }

        private static bool IsExportableProperty(PropertyInfo property)
        {
            // Skip navigation properties and complex types that might cause circular references
            var propertyType = property.PropertyType;
            
            // Skip collections
            if (propertyType.IsGenericType && 
                (propertyType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                 propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                 propertyType.GetGenericTypeDefinition() == typeof(List<>)))
            {
                return false;
            }

            // Skip complex navigation properties (but allow simple value types and strings)
            if (!propertyType.IsPrimitive && 
                propertyType != typeof(string) && 
                propertyType != typeof(DateTime) && 
                propertyType != typeof(DateTime?) &&
                propertyType != typeof(Guid) &&
                propertyType != typeof(Guid?) &&
                propertyType != typeof(decimal) &&
                propertyType != typeof(decimal?) &&
                !propertyType.IsEnum)
            {
                return false;
            }

            return true;
        }
    }
}