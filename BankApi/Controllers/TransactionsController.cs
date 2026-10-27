using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankApi.Models;
using BankApi.Data;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Data.SqlClient;

[Route("api/[controller]")]
[ApiController]
public class TransactionsController : ControllerBase
{
    private readonly DBManager _context;

    public TransactionsController(DBManager context)
    {
        _context = context;
    }


    [HttpGet]
    public async Task<ActionResult<PagedResult<TransactionView>>> GetTransactions([FromQuery] DateTime? from,[FromQuery] string? search,   [FromQuery] DateTime? to,  [FromQuery] string? type, [FromQuery] int page = 1 , [FromQuery] int pageSize = 10, [FromQuery] string sortBy = "date" , [FromQuery] string sortOrder = "descending")
    {
        if (page < 1 )
        {
            page = 1;
        }
        if (pageSize < 1)
        {
            pageSize = 10;
        }
        var transactions = _context.Transactions.AsQueryable();

        if (from != null)
        {
            transactions = transactions.Where(t => t.CreatedAt >= from.Value);
        }
        if (to != null)
        {
            transactions = transactions.Where(t => t.CreatedAt <= to.Value);

        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            transactions = transactions.Where(t => t.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            transactions = transactions.Where(t => t.Description.ToLower().Contains(s) || t.Account.AccountNumber.ToLower().Contains(s) || (t.RelatedAccount != null && t.RelatedAccount.AccountNumber.ToLower().Contains(s)) || t.Account.User.Username.ToLower().Contains(s));
        }


        if (sortBy == "amount")
        {
            if (sortOrder == "ascending")
            {
                transactions = transactions.OrderBy(t => (double)t.Amount);

            }
            else
            {
                transactions = transactions.OrderByDescending(t => (double)t.Amount);
            }
        }
        else  
        {
            if (sortOrder == "ascending")
            {
                transactions = transactions.OrderBy(t => t.CreatedAt);

            }
            else
            {
                transactions = transactions.OrderByDescending(t => t.CreatedAt);

            }
        }

        var result = new PagedResult<TransactionView>
        {
            TotalCount = await transactions.CountAsync(),
            Page = page,
            PageSize = pageSize,

            Items = await transactions.Skip((page - 1 ) * pageSize ).Take(pageSize).Select(t => new TransactionView
            {
                Id = t.Id,
                Type = t.Type,
                Amount = t.Amount,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                AccountNumber = t.Account.AccountNumber,
                Username = t.Account.User.Username,
                RelatedAccountNumber = t.RelatedAccount != null ? t.RelatedAccount.AccountNumber : null

            }).ToListAsync()
        };
        return Ok(result);
    
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionView>> GetTransaction(int id)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Account).ThenInclude(a => a.User)
            .Include(t => t.RelatedAccount)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null)
        {
            return NotFound(new ApiError("TRANSACTION_NOT_FOUND", $"Transaction {id} does not exist."));
        }
        return Ok(ToView(transaction));
    }

    [HttpPost("deposit")]
    public async Task<ActionResult<TransactionView>> Deposit([FromBody] DepositWithdrawRequest request)
    {
        if (request.Amount <= 0)
        {

            return BadRequest(new ApiError("BAD_REQUEST", "Transaction cannot be 0 or under"));
        }
        if (decimal.Round(request.Amount, 2) != request.Amount)
        {
            return BadRequest(TooManyDecimals());
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        var account = await FindAccount(request.AccountNumber);
        if (account == null)
        {
            return NotFound(AccountNotFound(request.AccountNumber));
        }
        if (!account.IsActive || !account.User.IsActive)
        {

            return UnprocessableEntity(AccountInactive(account.AccountNumber));
        }

        account.Balance += request.Amount;
        var transaction = new Transaction
        {
            Account = account,
            Type = "Deposit",
            Amount = request.Amount,
            Description = request.Description ?? "Deposit",
            CreatedAt = DateTime.Now
        };
        _context.Transactions.Add(transaction);

        await _context.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, ToView(transaction));
    }

    [HttpPost("withdraw")]
    public async Task<ActionResult<TransactionView>> Withdraw([FromBody] DepositWithdrawRequest request)
    {
        if (decimal.Round(request.Amount, 2) != request.Amount)
        {
            return BadRequest(TooManyDecimals());
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        var account = await FindAccount(request.AccountNumber);
        if (account == null)
        {
            return NotFound(AccountNotFound(request.AccountNumber));
        }
        if (!account.IsActive || !account.User.IsActive)
        {
            return UnprocessableEntity(AccountInactive(account.AccountNumber));
        }
        if (account.Balance < request.Amount)
        {
            return UnprocessableEntity(InsufficientFunds(account.Balance, request.Amount));
        }

        account.Balance -= request.Amount;
        var transaction = new Transaction
        {
            Account = account,
            Type = "Withdrawal",
            Amount = request.Amount,
            Description = request.Description ?? "Withdrawal",
            CreatedAt = DateTime.Now
        };
        _context.Transactions.Add(transaction);

        await _context.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, ToView(transaction));
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        if (decimal.Round(request.Amount, 2) != request.Amount)
        {
            return BadRequest(TooManyDecimals());
        }
        if (request.FromAccountNumber == request.ToAccountNumber)
        {
            return UnprocessableEntity(new ApiError("SAME_ACCOUNT", "Cannot transfer money to the same account."));
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        var fromAccount = await FindAccount(request.FromAccountNumber);
        if (fromAccount == null)
        {
            return NotFound(AccountNotFound(request.FromAccountNumber));
        }
        var toAccount = await FindAccount(request.ToAccountNumber);
        if (toAccount == null)
        {
            return NotFound(AccountNotFound(request.ToAccountNumber));
        }
        if (!fromAccount.IsActive || !fromAccount.User.IsActive)
        {
            return UnprocessableEntity(AccountInactive(fromAccount.AccountNumber));
        }
        if (!toAccount.IsActive || !toAccount.User.IsActive)
        {
            return UnprocessableEntity(AccountInactive(toAccount.AccountNumber));
        }
        if (fromAccount.Balance < request.Amount)
        {
            return UnprocessableEntity(InsufficientFunds(fromAccount.Balance, request.Amount));
        }

        var now = DateTime.Now;
        var description = string.IsNullOrWhiteSpace(request.Description) ? "Transfer" : request.Description.Trim();

        fromAccount.Balance -= request.Amount;
        toAccount.Balance += request.Amount;

        var debit = new Transaction
        {
            Account = fromAccount,
            RelatedAccount = toAccount,
            Type = "TransferOut",
            Amount = request.Amount,
            Description = description,
            CreatedAt = now
        };
        var credit = new Transaction
        {
            Account = toAccount,
            RelatedAccount = fromAccount,
            Type = "TransferIn",
            Amount = request.Amount,
            Description = description,
            CreatedAt = now
        };
        _context.Transactions.Add(debit);
        _context.Transactions.Add(credit);

        await _context.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        var response = new { Debit = ToView(debit), Credit = ToView(credit) };
        return CreatedAtAction(nameof(GetTransaction), new { id = debit.Id }, response);
    }

    private Task<Account?> FindAccount(string accountNumber)
    {
        return _context.Accounts.Include(a => a.User).FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
    }

    private static TransactionView ToView(Transaction t)
    {
        return new TransactionView
        {
            Id = t.Id,
            Type = t.Type,
            Amount = t.Amount,
            Description = t.Description,
            CreatedAt = t.CreatedAt,
            AccountNumber = t.Account.AccountNumber,
            Username = t.Account.User.Username,
            RelatedAccountNumber = t.RelatedAccount?.AccountNumber
        };
    }

    private static ApiError AccountNotFound(string accountNumber)
    {
        return new ApiError("ACCOUNT_NOT_FOUND", $"Account '{accountNumber}' does not exist.");
    }

    private static ApiError AccountInactive(string accountNumber)
    {
        return new ApiError("ACCOUNT_INACTIVE", $"Account '{accountNumber}' is deactivated.");
    }

    private static ApiError InsufficientFunds(decimal balance, decimal amount)
    {
        var culture = CultureInfo.InvariantCulture;
        return new ApiError("INSUFFICIENT_FUNDS", $"Balance is {balance.ToString("F2", culture)}; requested {amount.ToString("F2", culture)}.");
    }

    private static ApiError TooManyDecimals()
    {
        return new ApiError("VALIDATION_FAILED", "One or more fields failed validation.",
            new Dictionary<string, string[]> { { "Amount", new[] { "Amount can have at most 2 decimal places." } } });
    }
}