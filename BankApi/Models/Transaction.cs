using System.Text.Json.Serialization;

namespace BankApi.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        public decimal Amount { get; set; }
        public String Type { get; set; }
        public String Description { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        public int AccountId { get; set; }
        [JsonIgnore]
        public Account Account { get; set; }
        public int? RelatedAccountId { get; set; }
        [JsonIgnore]
        public Account? RelatedAccount { get; set; }
    }
}