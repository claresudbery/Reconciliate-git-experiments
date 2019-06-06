namespace Interfaces
{
    public interface IDataFile<TRecordType> where TRecordType : ICSVRecord, new()
    {
        ICSVFile<TRecordType> File { get; set; }

        void Load(bool loadFile = true,
            char? overrideSeparator = null,
            bool orderOnLoad = true);

        void FilterForPositiveRecordsOnly();
        void FilterForNegativeRecordsOnly();
        void SwapSignsOfAllAmounts();

        void RefreshFileContents();
    }
}