namespace BankApi.Models
{
    public class AuditEntryRequest
    {
        public string AdminUsername { get; set; } = "";
        public string Action { get; set; } = "";
        public string? Details { get; set; }
    }
}
