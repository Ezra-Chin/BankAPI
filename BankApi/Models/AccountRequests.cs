using System.ComponentModel.DataAnnotations;

namespace BankApi.Models
{
    public class CreateAccountRequest
    {
        [Required]
        public string Username { get; set; }

        [Required, StringLength(50)]
        public string AccountName { get; set; }

        [Range(typeof(decimal), "0", "1000000", ErrorMessage = "Initial deposit must be between 0 and 1,000,000.")]
        public decimal InitialDeposit { get; set; }
    }

    public class UpdateAccountRequest
    {
        [Required, StringLength(50)]
        public string AccountName { get; set; }
    }

    public class DepositWithdrawRequest
    {
        [Required]
        public string AccountNumber { get; set; }

        [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Amount must be between 0.01 and 1,000,000.")]
        public decimal Amount { get; set; }

        [StringLength(100)]
        public string? Description { get; set; }
    }

    public class TransferRequest
    {
        [Required]
        public string FromAccountNumber { get; set; }

        [Required]
        public string ToAccountNumber { get; set; }

        [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Amount must be between 0.01 and 1,000,000.")]
        public decimal Amount { get; set; }

        [StringLength(100)]
        public string? Description { get; set; }
    }
}