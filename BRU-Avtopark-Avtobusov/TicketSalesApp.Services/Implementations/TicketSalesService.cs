using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Globalization;

using TicketSalesApp.Core.Data;
using TicketSalesApp.Services.Interfaces;
using System.Text;

namespace TicketSalesApp.Services.Implementations
{
    public class TicketSalesService : ITicketSalesService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TicketSalesService> _logger;
        private readonly IExportService? _exportService; // Make it optional

        public TicketSalesService(
            AppDbContext context,
            ILogger<TicketSalesService> logger,
            IExportService? exportService = null) // Make it optional with default null
        {
            _context = context;
            _logger = logger;
            _exportService = exportService; // Can be null
        }


        public async Task<decimal> GetTotalIncomeAsync(int year, int month)
        {
            var totalIncome = await _context.Prodazhi
                .Where(s => s.SaleDate.Year == year && s.SaleDate.Month == month)
                .Join(_context.Bilety,
                      sale => sale.TicketId,
                      ticket => ticket.TicketId,
                      (sale, ticket) => ticket.TicketPrice)
                .SumAsync(price => price);
            return totalIncome;
        }

        public async Task<List<TransportStatistic>> GetTopTransportsAsync(int year, int month)
        {
            var query = from sale in _context.Prodazhi
                        where sale.SaleDate.Year == year && sale.SaleDate.Month == month
                        join ticket in _context.Bilety on sale.TicketId equals ticket.TicketId
                        join route in _context.Marshuti on ticket.RouteId equals route.RouteId
                        join bus in _context.Avtobusy on route.BusId equals bus.BusId
                        group sale by bus.Model into g
                        select new TransportStatistic
                        {
                            TransportModel = g.Key,
                            TicketsSold = g.Count()
                        };

            return await query.OrderByDescending(ts => ts.TicketsSold)
                              .Take(38)
                              .ToListAsync();
        }

        public async Task<SalesReport> GetMonthlyReportAsync(int year, int month)
        {
            try
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var sales = await _context.Prodazhi
                    .Where(p => p.SaleDate >= startDate && p.SaleDate <= endDate)
                    .Include(p => p.Bilet)
                    .ThenInclude(b => b.Marshut)
                    .ThenInclude(m => m.Avtobus)
                    .ToListAsync();

                var report = new SalesReport
                {
                    Period = startDate,
                    TotalIncome = sales.Sum(s => s.Bilet.TicketPrice),
                    TotalTicketsSold = sales.Count,
                    AverageTicketPrice = sales.Any() ? sales.Average(s => s.Bilet.TicketPrice) : 0,
                    TopRoutes = await GetRoutePerformanceAsync(startDate, endDate),
                    TransportStats = await GetTransportUtilizationAsync(startDate, endDate)
                };

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly report for {Year}-{Month}", year, month);
                throw;
            }
        }

        public async Task<List<SalesReport>> GetYearlyReportAsync(int year)
        {
            var reports = new List<SalesReport>();
            for (int month = 1; month <= 12; month++)
            {
                reports.Add(await GetMonthlyReportAsync(year, month));
            }
            return reports;
        }

        public async Task<List<RoutePerformance>> GetRoutePerformanceAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.Prodazhi
                    .Where(p => p.SaleDate >= startDate && p.SaleDate <= endDate)
                    .Include(p => p.Bilet)
                    .ThenInclude(b => b.Marshut)
                    .GroupBy(p => p.Bilet.Marshut)
                    .Select(g => new RoutePerformance
                    {
                        RouteName = g.Key.StartPoint + " - " + g.Key.EndPoint,
                        StartPoint = g.Key.StartPoint,
                        EndPoint = g.Key.EndPoint,
                        TicketsSold = g.Count(),
                        TotalIncome = g.Sum(s => s.Bilet.TicketPrice),
                        OccupancyRate = g.Count() / (decimal)g.Key.Tickets.Count * 100
                    })
                    .OrderByDescending(r => r.TicketsSold)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting route performance");
                throw;
            }
        }

        public async Task<List<TransportUtilization>> GetTransportUtilizationAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.Prodazhi
                    .Where(p => p.SaleDate >= startDate && p.SaleDate <= endDate)
                    .Include(p => p.Bilet)
                    .ThenInclude(b => b.Marshut)
                    .ThenInclude(m => m.Avtobus)
                    .GroupBy(p => p.Bilet.Marshut.Avtobus)
                    .Select(g => new TransportUtilization
                    {
                        TransportModel = g.Key.Model,
                        TotalRoutes = g.Key.Routes.Count,
                        TicketsSold = g.Count(),
                        TotalIncome = g.Sum(s => s.Bilet.TicketPrice),
                        UtilizationRate = g.Count() / (double)(g.Key.Routes.Sum(r => r.Tickets.Count)) * 100
                    })
                    .OrderByDescending(t => t.UtilizationRate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transport utilization");
                throw;
            }
        }

        public async Task<byte[]> ExportToCsvAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var report = await GetMonthlyReportAsync(startDate.Year, startDate.Month);
                var sb = new StringBuilder();

                // Add headers
                sb.AppendLine("Period,TotalIncome,TotalTicketsSold,AverageTicketPrice");

                // Add main report data
                sb.AppendLine($"{report.Period:yyyy-MM-dd},{report.TotalIncome},{report.TotalTicketsSold},{report.AverageTicketPrice}");

                // Add route performance
                sb.AppendLine("\nRoute Performance");
                sb.AppendLine("RouteName,StartPoint,EndPoint,TicketsSold,TotalIncome,OccupancyRate");
                foreach (var route in report.TopRoutes)
                {
                    sb.AppendLine($"{route.RouteName},{route.StartPoint},{route.EndPoint},{route.TicketsSold},{route.TotalIncome},{route.OccupancyRate}");
                }

                // Add transport stats
                sb.AppendLine("\nTransport Statistics");
                sb.AppendLine("TransportModel,TotalRoutes,TicketsSold,TotalIncome,UtilizationRate");
                foreach (var transport in report.TransportStats)
                {
                    sb.AppendLine($"{transport.TransportModel},{transport.TotalRoutes},{transport.TicketsSold},{transport.TotalIncome},{transport.UtilizationRate}");
                }

                return Encoding.UTF8.GetBytes(sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to CSV");
                throw;
            }
        }

        public async Task<byte[]> ExportToExcelAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // For Excel export, we'll return the same CSV format since it can be opened directly in Excel
                return await ExportToCsvAsync(startDate, endDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to Excel");
                throw;
            }
        }

        public async Task<byte[]> ExportToPdfAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var report = await GetMonthlyReportAsync(startDate.Year, startDate.Month);
                var sb = new StringBuilder();

                // Create a simple HTML document that can be converted to PDF
                sb.AppendLine("<!DOCTYPE html>");
                sb.AppendLine("<html>");
                sb.AppendLine("<head>");
                sb.AppendLine("<style>");
                sb.AppendLine("body { font-family: Arial, sans-serif; }");
                sb.AppendLine("table { border-collapse: collapse; width: 100%; margin-bottom: 20px; }");
                sb.AppendLine("th, td { border: 1px solid black; padding: 8px; text-align: left; }");
                sb.AppendLine("h2 { color: #333; }");
                sb.AppendLine("</style>");
                sb.AppendLine("</head>");
                sb.AppendLine("<body>");

                // Main report section
                sb.AppendLine($"<h1>Sales Report for {report.Period:MMMM yyyy}</h1>");
                sb.AppendLine("<table>");
                sb.AppendLine("<tr><th>Total Income</th><th>Total Tickets Sold</th><th>Average Ticket Price</th></tr>");
                sb.AppendLine($"<tr><td>{report.TotalIncome:C}</td><td>{report.TotalTicketsSold}</td><td>{report.AverageTicketPrice:C}</td></tr>");
                sb.AppendLine("</table>");

                // Route performance section
                sb.AppendLine("<h2>Route Performance</h2>");
                sb.AppendLine("<table>");
                sb.AppendLine("<tr><th>Route</th><th>Tickets Sold</th><th>Total Income</th><th>Occupancy Rate</th></tr>");
                foreach (var route in report.TopRoutes)
                {
                    sb.AppendLine($"<tr><td>{route.RouteName}</td><td>{route.TicketsSold}</td><td>{route.TotalIncome:C}</td><td>{route.OccupancyRate:F1}%</td></tr>");
                }
                sb.AppendLine("</table>");

                // Transport statistics section
                sb.AppendLine("<h2>Transport Statistics</h2>");
                sb.AppendLine("<table>");
                sb.AppendLine("<tr><th>Transport Model</th><th>Total Routes</th><th>Tickets Sold</th><th>Utilization Rate</th></tr>");
                foreach (var transport in report.TransportStats)
                {
                    sb.AppendLine($"<tr><td>{transport.TransportModel}</td><td>{transport.TotalRoutes}</td><td>{transport.TicketsSold}</td><td>{transport.UtilizationRate:F1}%</td></tr>");
                }
                sb.AppendLine("</table>");

                sb.AppendLine("</body>");
                sb.AppendLine("</html>");

                return Encoding.UTF8.GetBytes(sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to PDF format");
                throw;
            }
        }
    }
}