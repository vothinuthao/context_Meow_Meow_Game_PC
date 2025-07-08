// =============================================================================
// Two Sleepy Cats Studio - Input System Module
// 
// Author: Two Sleepy Cats Development Team
// Created: 2025
// 
// Sweet dreams and happy coding! 😸💤
// =============================================================================

using TwoSleepyCats.CSVReader.Attributes;
using TwoSleepyCats.CSVReader.Core;

namespace TwoSleepyCats.TSCInputSystem
{
    public class InputConfiguration : ICsvModel
    {
        [CsvColumn("setting_name")]
        public string SettingName { get; set; }
        
        [CsvColumn("value")]
        public string Value { get; set; }
        
        [CsvColumn("description", isOptional: true)]
        public string Description { get; set; }

        public string GetCsvFileName() => "inputConfig.csv";
        public string GetCsvFolderPath() => "InputConfig";
        public void OnDataLoaded() { }
        public bool ValidateData() => !string.IsNullOrEmpty(SettingName);
    }
}