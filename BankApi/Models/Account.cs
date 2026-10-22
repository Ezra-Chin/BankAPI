using System.Text.Json.Serialization;

namespace BankApi.Models
{
    public class Account
    {
        public int Id { get; set; }
        public String AccountNumber { get; set; }
        public String AccountName { get; set; } = "Everyday Account";
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UserId { get; set; }

        [JsonIgnore]
        public User User { get; set; }
        [JsonIgnore]
        public ICollection<Transaction> Transactions { get; set; }
    }
}