using Interfaces.Constants;

namespace Interfaces.DTOs
{
    public class FilePaths
    {
        public ReconciliationType Reconciliation_type;
        public string Main_path { get; set; }
        public string Third_party_file_name { get; set; }
        public string Owned_file_name { get; set; }
    }
}