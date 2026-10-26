namespace BankWeb.Models
{
    public class AuditEntry
    {
        public int Id { get; set; }
        public string AdminUsername { get; set; } = "";
        public string Action { get; set; } = "";
        public string Details { get; set; } = "";

        public DateTime CreatedAt { get; set; }
    }
}
