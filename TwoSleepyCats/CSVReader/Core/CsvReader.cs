using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TwoSleepyCats.CSVReader.Utils;
using UnityEngine;

namespace TwoSleepyCats.CSVReader.Core
{
    public static class CsvReader<T> where T : ICsvModel, new()
    {
        public static (List<T> data, Models.CsvErrorCollection errors) LoadSync()
        {
            var model = new T();
            var resourcePath = model.GetCsvResourcePath();
            
            return LoadFromResourceSync(resourcePath);
        }
        public static (List<T> data, Models.CsvErrorCollection errors) LoadFromResourceSync(string resourcePath)
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
                
                Debug.Log($"[SyncCsvReader] Loading CSV synchronously from: Resources/{resourcePath}");
                
                // Parse content synchronously
                ParseCsvContentSync(textAsset.text, data, errors);
                
                return (data, errors);
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

        private static void ParseCsvContentSync(string csvContent, List<T> data, Models.CsvErrorCollection errors)
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

            Debug.Log($"[SyncCsvReader] Found headers: {string.Join(", ", headers)}");

            // Get property mappings
            var propertyMappings = GetPropertyMappingsSync<T>(headers, errors);
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

            // Parse data rows
            int validRowCount = 0;
            for (int i = startRow; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (Utils.CsvParser.IsLineEmpty(line) || Utils.CsvParser.IsLineComment(line))
                    continue;

                var rowData = Utils.CsvParser.ParseLine(line);
                var model = ParseRowSync<T>(rowData, headers, propertyMappings, i + 1, errors);
                
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

            Debug.Log($"[SyncCsvReader] Successfully parsed {validRowCount} valid rows from {typeof(T).Name}");
        }

        /// <summary>
        /// Get property mappings synchronously
        /// </summary>
        private static Dictionary<PropertyInfo, (int index, Attributes.CsvColumnAttribute attribute)> GetPropertyMappingsSync<TModel>(
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
                        if (csvAttr.ColumnIndex >= 0 && csvAttr.ColumnIndex < headers.Length)
                        {
                            columnIndex = csvAttr.ColumnIndex;
                        }
                    }
                    else
                    {
                        columnIndex = Array.FindIndex(headers, h => 
                            string.Equals(h, csvAttr.ColumnName, StringComparison.OrdinalIgnoreCase));
                    }
                }
                else
                {
                    columnIndex = Array.FindIndex(headers, h => 
                        string.Equals(h, property.Name, StringComparison.OrdinalIgnoreCase));
                }

                if (columnIndex >= 0)
                {
                    mappings[property] = (columnIndex, csvAttr);
                    Debug.Log($"[SyncCsvReader] Mapped property '{property.Name}' to column '{headers[columnIndex]}' (index {columnIndex})");
                }
                else if (csvAttr == null || !csvAttr.IsOptional)
                {
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
        /// Parse row synchronously
        /// </summary>
        private static TModel ParseRowSync<TModel>(string[] rowData, string[] headers, 
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

                    // Use advanced type converter
                    var convertedValue = Utils.AdvancedTypeConverter.ConvertValue(
                        value, property.PropertyType, attribute?.AutoConvert ?? false, null);
                    
                    property.SetValue(model, convertedValue);
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

                    if (attribute?.IsOptional == true)
                    {
                        var defaultValue = Utils.AdvancedTypeConverter.ConvertValue("", property.PropertyType);
                        property.SetValue(model, defaultValue);
                    }
                }
            }

            return model;
        }
    }
}