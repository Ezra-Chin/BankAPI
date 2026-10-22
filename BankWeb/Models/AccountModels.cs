using System.ComponentModel.DataAnnotations;

namespace BankWeb.Models
{
    public class AccountModel
    {
        public string AccountNumber { get; set; } = "";
        public string AccountName { get; set; } = "";
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Username { get; set; } = "";
    }

    public class TransactionModel
    {
        public int Id { get; set; }
        public string Type { get; set; } = "";
        public decimal Amount { get; set; }
        public string Description { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string AccountNumber { get; set; } = "";
        public string Username { get; set; } = "";
        public string? RelatedAccountNumber { get; set; }

        public bool IsCredit => Type == "Deposit" || Type == "TransferIn";
    }

    public class TransactionFilter
    {
        public string? AccountNumber { get; set; }
        public string? Search { get; set; }
        public string? Type { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "date";
        public string SortDir { get; set; } = "desc";
    }

    public class TransferViewModel
    {
        [Required(ErrorMessage = "Choose the account to transfer from.")]
        [Display(Name = "From account")]
        public string FromAccountNumber { get; set; } = "";

        [Required(ErrorMessage = "Enter the recipient account number.")]
        [Display(Name = "Recipient account number")]
        public string ToAccountNumber { get; set; } = "";

        [Required]
        [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Amount must be between $0.01 and $1,000,000.")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Amount can have at most 2 decimal places.")]
        public decimal? Amount { get; set; }

        [StringLength(100)]
        public string? Description { get; set; }
        public List<AccountModel> MyAccounts { get; set; } = new List<AccountModel>();
    }

    public class TransactionHistoryViewModel
    {
        public AccountModel? Account { get; set; }
        public TransactionFilter Filter { get; set; } = new TransactionFilter();
        public PagedResult<TransactionModel> Result { get; set; } = new PagedResult<TransactionModel>();
    }

    public class PagerModel
    {
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }
}
