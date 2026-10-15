
namespace BankApi.Models
{
    public class Account
    {
        public int Id { get; set; }
        public String AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public ICollection<Transaction> Transactions { get; set; }
    }
}   
