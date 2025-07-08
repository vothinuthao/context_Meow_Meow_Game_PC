using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TwoSleepyCats.CSVReader.Utils;
using UnityEngine;

namespace TwoSleepyCats.CSVReader.Core
{
    /// <summary>
    /// Unified CSV Reader with advanced type conversion, validation, and flexible path management
    /// Combines basic and advanced functionality in a single implementation
    /// </summary>
    public static class CsvReader<T> where T : ICsvModel, new()
    {
        /// <summary>
        /// Load CSV data asynchronously using the model's specified path configuration
        /// Supports flexible folder structures through ICsvModel.GetCsvResourcePath()
        /// </summary>
        public static async Task<(List<T> data, Models.CsvErrorCollection errors)> LoadAsync()
        {
            var model = new T();
            var resourcePath = model.GetCsvResourcePath();
            
            return await LoadFromResourceAsync(resourcePath);
        }

        /// <summary>
        /// Load CSV data from a specific resource path
        /// Provides backward compatibility for custom path specifications
        /// </summary>
        public static async Task<(List<T> data, Models.CsvErrorCollection errors)> LoadFromResourceAsync(string resourcePath)
        {
            var errors = new Models.CsvErrorCollection();
            var data = new List<T>();

            try
            {
                var textAsset = Resources.Load<TextAsset>(resourcePath);
                if (textAsset == null)
                {
                    errors.AddError(new Models.CsvError
                    {
                        Row = 0,
                        Column = "File",
                        Value = resourcePath,
                        ErrorMessage = $"CSV file not found at Resources/{resourcePath}",
                        Severity = Models.ErrorSeverity.Critical
                    });
                    return (data, errors);
                }
                
                Debug.Log($"[CsvReader] Loading CSV from: Resources/{resourcePath}");
                return await ParseCsvContentAsync(textAsset.text, errors);
            }
            catch (Exception ex)
            {
                errors.AddError(new Models.CsvError
                {
                    Row = 0,
                    Column = "System",
                    ErrorMessage = $"Failed to load CSV from {resourcePath}: {ex.Message}",
                    Severity = Models.ErrorSeverity.Critical
                });
                return (data, errors);
            }
        }

        /// <summary>
        /// Parse CSV content asynchronously with comprehensive error handling
        /// </summary>
        private static async Task<(List<T> data, Models.CsvErrorCollection errors)> ParseCsvContentAsync(
            string csvContent, Models.CsvErrorCollection errors)
        {
            var data = new List<T>();
            await Task.Run(() =>
            {
                ParseCsvContent(csvContent, data, errors);
            });

            return (data, errors);
        }

        /// <summary>
        /// Core CSV content parsing with advanced type conversion and validation
        /// </summary>
        private static void ParseCsvContent(string csvContent, List<T> data, Models.CsvErrorCollection errors)
        {
            var lines = csvContent.Split('\n');
            if (lines.Length == 0)
            {
                errors.AddError(new Models.CsvError
                {
                    Row = 0,
                    Column = "File",
                    ErrorMessage = "CSV file is empty",
                    Severity = Models.ErrorSeverity.Critical
                });
                return;
            }

            // Parse header with improved error handling
            string[] headers = null;
            int startRow = 0;
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (Utils.CsvParser.IsLineEmpty(line) || Utils.CsvParser.IsLineComment(line))
                    continue;
                    
                headers = Utils.CsvParser.ParseHeader(line);
                startRow = i + 1;
                break;
            }

            if (headers == null)
            {
                errors.AddError(new Models.CsvError
                {
                    Row = 0,
                    Column = "Header",
                    ErrorMessage = "No valid header found in CSV",
                    Severity = Models.ErrorSeverity.Critical
                });
                return;
            }

            Debug.Log($"[CsvReader] Found headers: {string.Join(", ", headers)}");

            // Get property mappings with enhanced error reporting
            var propertyMappings = GetPropertyMappings<T>(headers, errors);
            if (propertyMappings.Count == 0)
            {
                errors.AddError(new Models.CsvError
                {
                    Row = 0,
                    Column = "Mapping",
                    ErrorMessage = "No property mappings found. Check column names and attributes.",
                    Severity = Models.ErrorSeverity.Critical
                });
                return;
            }

            // Parse data rows with advanced processing
            int validRowCount = 0;
            for (int i = startRow; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (Utils.CsvParser.IsLineEmpty(line) || Utils.CsvParser.IsLineComment(line))
                    continue;

                var rowData = Utils.CsvParser.ParseLine(line);
                var model = ParseRowAdvanced<T>(rowData, headers, propertyMappings, i + 1, errors);
                
                if (model != null)
                {
                    try
                    {
                        model.OnDataLoaded();
                        if (model.ValidateData())
                        {
                            data.Add(model);
                            validRowCount++;
                        }
                        else
                        {
                            errors.AddError(new Models.CsvError
                            {
                                Row = i + 1,
                                Column = "Validation",
                                ErrorMessage = "Model validation failed",
                                Severity = Models.ErrorSeverity.Error
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.AddError(new Models.CsvError
                        {
                            Row = i + 1,
                            Column = "Processing",
                            ErrorMessage = $"Error in OnDataLoaded or ValidateData: {ex.Message}",
                            Severity = Models.ErrorSeverity.Error
                        });
                    }
                }
            }

            Debug.Log($"[CsvReader] Successfully parsed {validRowCount} valid rows from {typeof(T).Name}");
        }

        /// <summary>
        /// Enhanced property mapping with comprehensive attribute support
        /// </summary>
        private static Dictionary<PropertyInfo, (int index, Attributes.CsvColumnAttribute attribute)> GetPropertyMappings<TModel>(
            string[] headers, Models.CsvErrorCollection errors)
        {
            var mappings = new Dictionary<PropertyInfo, (int, Attributes.CsvColumnAttribute)>();
            var properties = typeof(TModel).GetProperties()
                .Where(p => p.CanWrite && !p.HasAttribute<Attributes.CsvIgnoreAttribute>())
                .ToArray();

            foreach (var property in properties)
            {
                var csvAttr = property.GetCustomAttribute<Attributes.CsvColumnAttribute>();
                int columnIndex = -1;

                if (csvAttr != null)
                {
                    if (csvAttr.UseIndex)
                    {
                        // Map by index with bounds checking
                        if (csvAttr.ColumnIndex >= 0 && csvAttr.ColumnIndex < headers.Length)
                        {
                            columnIndex = csvAttr.ColumnIndex;
                        }
                    }
                    else
                    {
                        // Map by name with case-insensitive matching
                        columnIndex = Array.FindIndex(headers, h => 
                            string.Equals(h, csvAttr.ColumnName, StringComparison.OrdinalIgnoreCase));
                    }
                }
                else
                {
                    // Auto-map by property name with case-insensitive matching
                    columnIndex = Array.FindIndex(headers, h => 
                        string.Equals(h, property.Name, StringComparison.OrdinalIgnoreCase));
                }

                if (columnIndex >= 0)
                {
                    mappings[property] = (columnIndex, csvAttr);
                    Debug.Log($"[CsvReader] Mapped property '{property.Name}' to column '{headers[columnIndex]}' (index {columnIndex})");
                }
                else if (csvAttr == null || !csvAttr.IsOptional)
                {
                    // Required column not found
                    errors.AddError(new Models.CsvError
                    {
                        Row = 0,
                        Column = csvAttr?.ColumnName ?? property.Name,
                        ErrorMessage = $"Required column not found for property '{property.Name}'",
                        Severity = Models.ErrorSeverity.Error
                    });
                }
            }

            return mappings;
        }

        /// <summary>
        /// Advanced row parsing with custom converters and comprehensive validation
        /// </summary>
        private static TModel ParseRowAdvanced<TModel>(string[] rowData, string[] headers, 
            Dictionary<PropertyInfo, (int index, Attributes.CsvColumnAttribute attribute)> mappings, 
            int rowNumber, Models.CsvErrorCollection errors) where TModel : new()
        {
            var model = new TModel();

            foreach (var mapping in mappings)
            {
                var property = mapping.Key;
                var (columnIndex, attribute) = mapping.Value;

                try
                {
                    string value = "";
                    if (columnIndex < rowData.Length)
                    {
                        value = rowData[columnIndex];
                    }

                    // Check for custom converter
                    Models.ICsvConverter customConverter = null;
                    var converterAttr = property.GetCustomAttribute<Attributes.CsvConverterAttribute>();
                    if (converterAttr != null)
                    {
                        customConverter = (Models.ICsvConverter)Activator.CreateInstance(converterAttr.ConverterType);
                    }

                    // Use advanced type converter
                    var convertedValue = Utils.AdvancedTypeConverter.ConvertValue(
                        value, property.PropertyType, attribute?.AutoConvert ?? false, customConverter);
                    
                    property.SetValue(model, convertedValue);
                    
                    // Perform validation if specified
                    var validationAttr = property.GetCustomAttribute<Attributes.CsvValidationAttribute>();
                    if (validationAttr != null)
                    {
                        ValidateProperty(property, convertedValue, validationAttr, rowNumber, errors);
                    }
                }
                catch (Exception ex)
                {
                    var columnName = attribute?.ColumnName ?? property.Name;
                    var rawValue = columnIndex < rowData.Length ? rowData[columnIndex] : "";
                    
                    errors.AddError(new Models.CsvError
                    {
                        Row = rowNumber,
                        Column = columnName,
                        Value = rawValue,
                        ExpectedType = property.PropertyType.Name,
                        ErrorMessage = $"Failed to convert value: {ex.Message}",
                        Severity = attribute?.IsOptional == true ? Models.ErrorSeverity.Warning : Models.ErrorSeverity.Error
                    });

                    // Set default value for optional properties
                    if (attribute?.IsOptional == true)
                    {
                        var defaultValue = Utils.AdvancedTypeConverter.ConvertValue("", property.PropertyType);
                        property.SetValue(model, defaultValue);
                    }
                }
            }

            return model;
        }
        
        /// <summary>
        /// Comprehensive property validation with business rules support
        /// </summary>
        private static void ValidateProperty(PropertyInfo property, object value, Attributes.CsvValidationAttribute validation, 
            int rowNumber, Models.CsvErrorCollection errors)
        {
            // Required validation
            if (validation.Required && value == null)
            {
                errors.AddError(new Models.CsvError
                {
                    Row = rowNumber,
                    Column = property.Name,
                    Value = value?.ToString() ?? "null",
                    ErrorMessage = validation.ErrorMessage ?? $"Required property '{property.Name}' is null",
                    Severity = Models.ErrorSeverity.Error
                });
                return;
            }
            
            if (value == null) return;
            
            // Range validation for comparable types
            if (validation.MinValue != null && value is IComparable comparable)
            {
                if (comparable.CompareTo(validation.MinValue) < 0)
                {
                    errors.AddError(new Models.CsvError
                    {
                        Row = rowNumber,
                        Column = property.Name,
                        Value = value.ToString(),
                        ErrorMessage = validation.ErrorMessage ?? $"Value {value} is less than minimum {validation.MinValue}",
                        Severity = Models.ErrorSeverity.Warning
                    });
                }
            }
            
            if (validation.MaxValue != null && value is IComparable comparable2)
            {
                if (comparable2.CompareTo(validation.MaxValue) > 0)
                {
                    errors.AddError(new Models.CsvError
                    {
                        Row = rowNumber,
                        Column = property.Name,
                        Value = value.ToString(),
                        ErrorMessage = validation.ErrorMessage ?? $"Value {value} is greater than maximum {validation.MaxValue}",
                        Severity = Models.ErrorSeverity.Warning
                    });
                }
            }
            
            // Regex validation for strings
            if (!string.IsNullOrEmpty(validation.RegexPattern) && value is string stringValue)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(stringValue, validation.RegexPattern))
                {
                    errors.AddError(new Models.CsvError
                    {
                        Row = rowNumber,
                        Column = property.Name,
                        Value = stringValue,
                        ErrorMessage = validation.ErrorMessage ?? $"Value '{stringValue}' does not match pattern '{validation.RegexPattern}'",
                        Severity = Models.ErrorSeverity.Warning
                    });
                }
            }
        }
    }
}