namespace BankApi.Models
{
    // What the account and transaction endpoints send back (JSON).

    public class AccountView
    {
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Username { get; set; }
    }

    public class TransactionView
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AccountNumber { get; set; }
        public string Username { get; set; }
        public string? RelatedAccountNumber { get; set; }
    }
}