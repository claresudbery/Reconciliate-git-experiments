using ConsoleCatchall.Console.Reconciliation.Records;
using Interfaces;

namespace ConsoleCatchall.Console.Reconciliation.Files
{
    internal class ActualBankOutFile : IDataFile<ActualBankRecord>
    {
        public ICSVFile<ActualBankRecord> File { get; set; }

        public ActualBankOutFile(ICSVFile<ActualBankRecord> csv_file)
        {
            File = csv_file;
        }

        public void Load(bool load_file = true,
            char? override_separator = null,
            bool order_on_load = true)
        {
            File.Load(load_file, override_separator, order_on_load);
            Refresh_file_contents();
        }

        public void Refresh_file_contents()
        {
            File.Filter_for_negative_records_only();
        }
    }
}