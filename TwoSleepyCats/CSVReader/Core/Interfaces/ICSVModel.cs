namespace TwoSleepyCats.CSVReader.Core
{
    public interface ICsvModel
    {
        string GetCsvFileName();
        string GetCsvFolderPath() => "CSV";
        string GetCsvResourcePath() => $"{GetCsvFolderPath()}/{System.IO.Path.GetFileNameWithoutExtension(GetCsvFileName())}";
        void OnDataLoaded();
        bool ValidateData();
    }
}