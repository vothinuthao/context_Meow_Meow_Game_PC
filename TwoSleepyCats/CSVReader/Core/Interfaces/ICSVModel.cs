namespace TwoSleepyCats.CSVReader.Core
{
    /// <summary>
    /// Enhanced ICsvModel interface with flexible path management
    /// Supports organized folder structures for different CSV categories
    /// </summary>
    public interface ICsvModel
    {
        /// <summary>
        /// Return CSV filename without path. Example: "characters.csv"
        /// </summary>
        string GetCsvFileName();
        
        /// <summary>
        /// Return CSV folder path relative to Resources folder. 
        /// Examples: "CSV", "CSV/InputConfig", "CSV/Character", "CSV/Data/Gameplay"
        /// Default implementation returns "CSV" for backward compatibility
        /// </summary>
        string GetCsvFolderPath() => "CSV";
        
        /// <summary>
        /// Get complete resource path for this CSV file
        /// Combines folder path and filename automatically
        /// Override this method if you need custom path logic
        /// </summary>
        string GetCsvResourcePath() => $"{GetCsvFolderPath()}/{System.IO.Path.GetFileNameWithoutExtension(GetCsvFileName())}";
        
        /// <summary>
        /// Called after all data is loaded and mapped. Use for post-processing.
        /// </summary>
        void OnDataLoaded();
        
        /// <summary>
        /// Validate the loaded data. Return false to mark as invalid.
        /// </summary>
        bool ValidateData();
    }
}