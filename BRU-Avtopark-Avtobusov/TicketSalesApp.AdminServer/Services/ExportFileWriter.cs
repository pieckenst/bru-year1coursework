using CsvHelper;
using OfficeOpenXml;
using System.Globalization;
using System.Text.Json;
using TicketSalesApp.AdminServer.Models.Export;
using TicketSalesApp.AdminServer.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Services
{
    public class ExportFileWriter : IExportFileWriter
    {
        private readonly ILogger<ExportFileWriter> _logger;

        public ExportFileWriter(ILogger<ExportFileWriter> logger)
        {
            _logger = logger;
            
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<string> WriteFileAsync(
            IAsyncEnumerable<IEnumerable<object>> dataBatches,
            ExportFormat format,
            string filePath,
            string[]? fieldNames = null,
            bool includeHeaders = true,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            return format switch
            {
                ExportFormat.CSV => await WriteCsvFileAsync(dataBatches, filePath, fieldNames, includeHeaders, cancellationToken),
                ExportFormat.Excel => await WriteExcelFileAsync(dataBatches, filePath, fieldNames, includeHeaders, cancellationToken),
                ExportFormat.JSON => await WriteJsonFileAsync(dataBatches, filePath, cancellationToken),
                _ => throw new ArgumentException($"Unsupported export format: {format}")
            };
        }

        public string GetContentType(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.CSV => "text/csv",
                ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ExportFormat.JSON => "application/json",
                _ => "application/octet-stream"
            };
        }

        public string GetFileExtension(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.CSV => ".csv",
                ExportFormat.Excel => ".xlsx",
                ExportFormat.JSON => ".json",
                _ => ".bin"
            };
        }

        public bool SupportsStreaming(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.CSV => true,
                ExportFormat.JSON => true,
                ExportFormat.Excel => false, // Excel requires all data in memory
                _ => false
            };
        }

        private async Task<string> WriteCsvFileAsync(
            IAsyncEnumerable<IEnumerable<object>> dataBatches,
            string filePath,
            string[]? fieldNames,
            bool includeHeaders,
            CancellationToken cancellationToken)
        {
            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            var isFirstBatch = true;
            var totalRecords = 0;

            await foreach (var batch in dataBatches.WithCancellation(cancellationToken))
            {
                var batchList = batch.ToList();
                if (!batchList.Any()) continue;

                if (isFirstBatch)
                {
                    if (includeHeaders)
                    {
                        var headers = fieldNames ?? GetFieldNames(batchList.First());
                        foreach (var header in headers)
                        {
                            csv.WriteField(header);
                        }
                        await csv.NextRecordAsync();
                    }
                    isFirstBatch = false;
                }

                foreach (var record in batchList)
                {
                    WriteRecordToCsv(csv, record, fieldNames);
                    await csv.NextRecordAsync();
                    totalRecords++;
                }

                await csv.FlushAsync();
            }

            _logger.LogInformation("CSV export completed: {TotalRecords} records written to {FilePath}", 
                totalRecords, filePath);

            return filePath;
        }

        private async Task<string> WriteExcelFileAsync(
            IAsyncEnumerable<IEnumerable<object>> dataBatches,
            string filePath,
            string[]? fieldNames,
            bool includeHeaders,
            CancellationToken cancellationToken)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Export");

            var allData = new List<object>();
            await foreach (var batch in dataBatches.WithCancellation(cancellationToken))
            {
                allData.AddRange(batch);
            }

            if (!allData.Any())
            {
                // Create empty file
                await package.SaveAsAsync(new FileInfo(filePath), cancellationToken);
                return filePath;
            }

            var headers = fieldNames ?? GetFieldNames(allData.First());
            var currentRow = 1;

            // Write headers
            if (includeHeaders)
            {
                for (int col = 0; col < headers.Length; col++)
                {
                    worksheet.Cells[currentRow, col + 1].Value = headers[col];
                }
                currentRow++;
            }

            // Write data
            foreach (var record in allData)
            {
                WriteRecordToExcel(worksheet, record, headers, currentRow);
                currentRow++;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            await package.SaveAsAsync(new FileInfo(filePath), cancellationToken);

            _logger.LogInformation("Excel export completed: {TotalRecords} records written to {FilePath}", 
                allData.Count, filePath);

            return filePath;
        }

        private async Task<string> WriteJsonFileAsync(
            IAsyncEnumerable<IEnumerable<object>> dataBatches,
            string filePath,
            CancellationToken cancellationToken)
        {
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions { Indented = true });

            writer.WriteStartArray();

            var totalRecords = 0;
            await foreach (var batch in dataBatches.WithCancellation(cancellationToken))
            {
                foreach (var record in batch)
                {
                    JsonSerializer.Serialize(writer, record, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    });
                    totalRecords++;
                }
            }

            writer.WriteEndArray();
            await writer.FlushAsync(cancellationToken);

            _logger.LogInformation("JSON export completed: {TotalRecords} records written to {FilePath}", 
                totalRecords, filePath);

            return filePath;
        }

        private string[] GetFieldNames(object record)
        {
            return record switch
            {
                Dictionary<string, object?> dict => dict.Keys.ToArray(),
                _ => record.GetType().GetProperties().Select(p => p.Name).ToArray()
            };
        }

        private void WriteRecordToCsv(CsvWriter csv, object record, string[]? fieldNames)
        {
            switch (record)
            {
                case Dictionary<string, object?> dict:
                    var fields = fieldNames ?? dict.Keys.ToArray();
                    foreach (var field in fields)
                    {
                        var value = dict.TryGetValue(field, out var val) ? val : null;
                        csv.WriteField(FormatValueForCsv(value));
                    }
                    break;
                default:
                    var properties = record.GetType().GetProperties();
                    var fieldsToWrite = fieldNames ?? properties.Select(p => p.Name).ToArray();
                    
                    foreach (var field in fieldsToWrite)
                    {
                        var property = properties.FirstOrDefault(p => p.Name == field);
                        var value = property?.GetValue(record);
                        csv.WriteField(FormatValueForCsv(value));
                    }
                    break;
            }
        }

        private void WriteRecordToExcel(ExcelWorksheet worksheet, object record, string[] fieldNames, int row)
        {
            switch (record)
            {
                case Dictionary<string, object?> dict:
                    for (int col = 0; col < fieldNames.Length; col++)
                    {
                        var value = dict.TryGetValue(fieldNames[col], out var val) ? val : null;
                        worksheet.Cells[row, col + 1].Value = FormatValueForExcel(value);
                    }
                    break;
                default:
                    var properties = record.GetType().GetProperties();
                    for (int col = 0; col < fieldNames.Length; col++)
                    {
                        var property = properties.FirstOrDefault(p => p.Name == fieldNames[col]);
                        var value = property?.GetValue(record);
                        worksheet.Cells[row, col + 1].Value = FormatValueForExcel(value);
                    }
                    break;
            }
        }

        private object? FormatValueForCsv(object? value)
        {
            return value switch
            {
                DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss zzz"),
                null => string.Empty,
                _ => value.ToString()
            };
        }

        private object? FormatValueForExcel(object? value)
        {
            return value switch
            {
                DateTime => value,
                DateTimeOffset dateTimeOffset => dateTimeOffset.DateTime,
                null => null,
                _ => value
            };
        }
    }
}