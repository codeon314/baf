using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserAutomationFramework.Models
{
    /// <summary>
    /// Represents the structured records extracted by a <see cref="Scraping.DataScraper"/> operation,
    /// with built-in support for JSON/CSV export, <see cref="DataTable"/> conversion, and strongly-typed object binding.
    /// </summary>
    public class ScrapeResult
    {
        /// <summary>
        /// Gets the file dialog filter used when exporting scrape results as JSON.
        /// </summary>
        public const string JsonFileDialogFilter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";

        /// <summary>
        /// Gets the file dialog filter used when exporting scrape results as CSV.
        /// </summary>
        public const string CsvFileDialogFilter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";

        /// <summary>
        /// Gets the default file extension used when exporting scrape results as JSON.
        /// </summary>
        public const string JsonFileExtension = "json";

        /// <summary>
        /// Gets the default file extension used when exporting scrape results as CSV.
        /// </summary>
        public const string CsvFileExtension = "csv";

        /// <summary>
        /// Gets or sets the ID of the <see cref="ScrapeDefinition"/> that produced this result.
        /// </summary>
        [JsonPropertyName("scrapeId")]
        public string ScrapeId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the scrape definition that produced this result.
        /// </summary>
        [JsonPropertyName("scrapeName")]
        public string ScrapeName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the scrape operation completed without error.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        /// <summary>
        /// Gets or sets the error message if <see cref="Success"/> is <c>false</c>.
        /// </summary>
        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the scrape operation completed.
        /// </summary>
        [JsonPropertyName("timestampUtc")]
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the total number of pages scraped.
        /// </summary>
        [JsonPropertyName("pagesScraped")]
        public int PagesScraped { get; set; } = 1;

        /// <summary>
        /// Gets or sets the list of extracted rows, where each row maps field names to extracted string values.
        /// </summary>
        [JsonPropertyName("records")]
        public List<Dictionary<string, string?>> Records { get; set; } = new();

        /// <summary>
        /// Gets the total number of records in <see cref="Records"/>.
        /// </summary>
        [JsonIgnore]
        public int RecordCount => Records.Count;

        /// <summary>
        /// Gets whether this scrape result contains at least one extracted record.
        /// </summary>
        [JsonIgnore]
        public bool HasRecords => Records != null && Records.Count > 0;

        /// <summary>
        /// Returns a human-readable status summary of this scrape result.
        /// </summary>
        public string GetStatusSummary()
        {
            return $"Results: {RecordCount} record(s) | Pages: {PagesScraped} | Source: {ScrapeName}";
        }

        /// <summary>
        /// Returns all distinct column/field names across all scraped records.
        /// </summary>
        public IReadOnlyList<string> GetColumnNames()
        {
            var columns = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var record in Records)
            {
                foreach (var key in record.Keys)
                {
                    if (seen.Add(key))
                    {
                        columns.Add(key);
                    }
                }
            }

            return columns;
        }

        /// <summary>
        /// Retrieves all values for a specific field across all scraped records.
        /// </summary>
        public IEnumerable<string?> GetColumnValues(string fieldName)
        {
            foreach (var record in Records)
            {
                if (record.TryGetValue(fieldName, out var val))
                {
                    yield return val;
                }
            }
        }

        /// <summary>
        /// Retrieves the first scraped value for the specified field name, or null if not present.
        /// </summary>
        public string? GetFirstValue(string fieldName)
        {
            var firstRecord = Records.FirstOrDefault();
            if (firstRecord != null && firstRecord.TryGetValue(fieldName, out var val))
            {
                return val;
            }
            return null;
        }

        /// <summary>
        /// Serializes the scraped records to a JSON string.
        /// </summary>
        public string ToJson(bool writeIndented = true, bool recordsOnly = false)
        {
            var options = new JsonSerializerOptions { WriteIndented = writeIndented };
            return recordsOnly
                ? JsonSerializer.Serialize(Records, options)
                : JsonSerializer.Serialize(this, options);
        }

        /// <summary>
        /// Formats the scraped records as a CSV string.
        /// </summary>
        public string ToCsv(char delimiter = ',')
        {
            var columns = GetColumnNames();
            if (columns.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(delimiter, columns.Select(c => EscapeCsvField(c, delimiter))));

            foreach (var record in Records)
            {
                var rowValues = columns.Select(col =>
                {
                    record.TryGetValue(col, out var val);
                    return EscapeCsvField(val ?? string.Empty, delimiter);
                });
                sb.AppendLine(string.Join(delimiter, rowValues));
            }

            return sb.ToString();
        }

        private static string EscapeCsvField(string field, char delimiter)
        {
            if (field.Contains(delimiter) || field.Contains('"') || field.Contains('\r') || field.Contains('\n'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }

        /// <summary>
        /// Converts the scraped records into a <see cref="DataTable"/> suitable for UI binding or data processing.
        /// </summary>
        public DataTable ToDataTable()
        {
            var table = new DataTable(string.IsNullOrWhiteSpace(ScrapeName) ? "ScrapeResults" : ScrapeName);
            var columns = GetColumnNames();

            foreach (var col in columns)
            {
                table.Columns.Add(col, typeof(string));
            }

            foreach (var record in Records)
            {
                var row = table.NewRow();
                foreach (var col in columns)
                {
                    if (record.TryGetValue(col, out var val) && val != null)
                    {
                        row[col] = val;
                    }
                    else
                    {
                        row[col] = string.Empty;
                    }
                }
                table.Rows.Add(row);
            }

            return table;
        }

        /// <summary>
        /// Maps the scraped records onto strongly-typed C# objects of type <typeparamref name="T"/>.
        /// </summary>
        public List<T> BindTo<T>() where T : new()
        {
            var results = new List<T>(Records.Count);
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var record in Records)
            {
                var instance = new T();
                foreach (var kvp in record)
                {
                    string normalizedKey = kvp.Key.Replace(" ", "").Replace("_", "").Replace("-", "");
                    if (!props.TryGetValue(kvp.Key, out var prop))
                    {
                        prop = props.Values.FirstOrDefault(p =>
                            string.Equals(p.Name.Replace("_", ""), normalizedKey, StringComparison.OrdinalIgnoreCase));
                    }

                    if (prop != null && kvp.Value != null)
                    {
                        try
                        {
                            Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                            object? converted = targetType == typeof(string)
                                ? kvp.Value
                                : Convert.ChangeType(kvp.Value, targetType);
                            prop.SetValue(instance, converted);
                        }
                        catch
                        {
                            // Skip unconvertible fields
                        }
                    }
                }
                results.Add(instance);
            }

            return results;
        }

        /// <summary>
        /// Combines multiple <see cref="ScrapeResult"/> instances into a single aggregated <see cref="ScrapeResult"/>.
        /// </summary>
        public static ScrapeResult Combine(IEnumerable<ScrapeResult> results, string? combinedName = null, bool deduplicate = false)
        {
            var list = results?.Where(r => r != null).ToList() ?? new List<ScrapeResult>();
            if (list.Count == 1 && string.IsNullOrWhiteSpace(combinedName) && !deduplicate)
            {
                return list[0];
            }

            var combined = new ScrapeResult
            {
                ScrapeName = !string.IsNullOrWhiteSpace(combinedName)
                    ? combinedName!
                    : (list.Count == 1 ? list[0].ScrapeName : "Combined Active Scrapes"),
                PagesScraped = list.Sum(r => r.PagesScraped),
                Success = list.All(r => r.Success)
            };

            foreach (var r in list)
            {
                combined.Merge(r, deduplicate);
            }

            return combined;
        }

        /// <summary>
        /// Merges records from another <see cref="ScrapeResult"/> into this instance.
        /// </summary>
        public void Merge(ScrapeResult other, bool deduplicate = false)
        {
            if (other == null) return;

            if (!deduplicate)
            {
                Records.AddRange(other.Records);
                return;
            }

            var existingFingerprints = new HashSet<string>(Records.Select(GetRecordFingerprint));
            foreach (var rec in other.Records)
            {
                if (existingFingerprints.Add(GetRecordFingerprint(rec)))
                {
                    Records.Add(rec);
                }
            }
        }

        internal static string GetRecordFingerprint(Dictionary<string, string?> record)
        {
            return string.Join("|", record.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => $"{k.Key}={k.Value}"));
        }

        /// <summary>
        /// Generates a sanitized default file name for exporting this <see cref="ScrapeResult"/> (e.g. <c>"Product_List.json"</c>).
        /// </summary>
        public string GetSuggestedFileName(string extension = JsonFileExtension)
        {
            string ext = (extension ?? JsonFileExtension).Trim().TrimStart('.');
            if (string.IsNullOrEmpty(ext)) ext = JsonFileExtension;

            string baseName = string.IsNullOrWhiteSpace(ScrapeName) ? "scrape_results" : ScrapeName.Trim();
            var invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(baseName.Length);
            foreach (char c in baseName)
            {
                if (c == ' ' || Array.IndexOf(invalidChars, c) >= 0)
                    sb.Append('_');
                else
                    sb.Append(c);
            }

            string cleaned = sb.ToString().Trim('_');
            if (string.IsNullOrWhiteSpace(cleaned))
                cleaned = "scrape_results";

            return $"{cleaned}.{ext}";
        }

        /// <summary>
        /// Saves the scraped records as formatted JSON to the specified file path.
        /// </summary>
        public void SaveToJsonFile(string filePath, bool recordsOnly = false)
        {
            File.WriteAllText(filePath, ToJson(true, recordsOnly));
        }

        /// <summary>
        /// Asynchronously saves the scraped records as formatted JSON to the specified file path.
        /// </summary>
        public Task SaveToJsonFileAsync(string filePath, bool recordsOnly = false, CancellationToken cancellationToken = default)
        {
            return File.WriteAllTextAsync(filePath, ToJson(true, recordsOnly), cancellationToken);
        }

        /// <summary>
        /// Saves the scraped records as UTF-8 CSV to the specified file path.
        /// </summary>
        public void SaveToCsvFile(string filePath, char delimiter = ',')
        {
            File.WriteAllText(filePath, ToCsv(delimiter), Encoding.UTF8);
        }

        /// <summary>
        /// Asynchronously saves the scraped records as UTF-8 CSV to the specified file path.
        /// </summary>
        public Task SaveToCsvFileAsync(string filePath, char delimiter = ',', CancellationToken cancellationToken = default)
        {
            return File.WriteAllTextAsync(filePath, ToCsv(delimiter), Encoding.UTF8, cancellationToken);
        }
    }
}