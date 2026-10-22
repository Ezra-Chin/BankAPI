using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankApi.Models;
using BankApi.Data;

[Route("api/[controller]")]
[ApiController]
public class AccountsController : ControllerBase
{
    private readonly DBManager _context;

    public AccountsController(DBManager context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountView>>> GetAccounts(string? username)
    {
        var query = _context.Accounts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(username))
        {
            var name = username.Trim().ToLower();
            if (!await _context.Users.AnyAsync(u => u.Username == name))
            {
                return NotFound(new ApiError("USER_NOT_FOUND", $"User '{username}' does not exist."));
            }
            query = query.Where(a => a.User.Username == name);
        }

        var accounts = await query.OrderBy(a => a.AccountNumber).Select(a => new AccountView
        {
            AccountNumber = a.AccountNumber,
            AccountName = a.AccountName,
            Balance = a.Balance,
            IsActive = a.IsActive,
            CreatedAt = a.CreatedAt,
            Username = a.User.Username
        }).ToListAsync();

        return Ok(accounts);
    }

    [HttpGet("{accountNumber}")]
    public async Task<ActionResult<AccountView>> GetAccount(string accountNumber)
    {
        var account = await _context.Accounts.Include(a => a.User)
                                             .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        if (account == null)
        {
            return NotFound(AccountNotFound(accountNumber));
        }
        return Ok(ToView(account));
    }

    [HttpGet("{accountNumber}/transactions")]
    public async Task<ActionResult<PagedResult<TransactionView>>> GetAccountTransactions(
        string accountNumber, DateTime? from, DateTime? to,
        int page = 1, int pageSize = 10, string sortBy = "date", string sortDir = "desc")
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new ApiError("INVALID_PAGING", "page must be 1 or more and pageSize must be between 1 and 100."));
        }
        if (from != null && to != null && from > to)
        {
            return BadRequest(new ApiError("INVALID_DATE_RANGE", "'from' must be on or before 'to'."));
        }
        if (!await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber))
        {
            return NotFound(AccountNotFound(accountNumber));
        }

        var query = _context.Transactions.Where(t => t.Account.AccountNumber == accountNumber);

        if (from != null)
        {
            query = query.Where(t => t.CreatedAt >= from.Value);
        }
        if (to != null)
        {
            var end = to.Value.TimeOfDay == TimeSpan.Zero ? to.Value.AddDays(1) : to.Value;
            query = query.Where(t => t.CreatedAt < end);
        }

        bool ascending = sortDir.ToLower() == "asc";
        switch (sortBy.ToLower())
        {
            case "date":
                query = ascending ? query.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id) : query.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id);
                break;
            case "amount":
                query = ascending ? query.OrderBy(t => (double)t.Amount) : query.OrderByDescending(t => (double)t.Amount);
                break;
            case "type":
                query = ascending ? query.OrderBy(t => t.Type) : query.OrderByDescending(t => t.Type);
                break;
            default:
                return BadRequest(new ApiError("INVALID_SORT", "sortBy must be one of: date, amount, type."));
        }

        var result = new PagedResult<TransactionView>
        {
            TotalCount = await query.CountAsync(),
            Page = page,
            PageSize = pageSize,
            Items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(t => new TransactionView
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

    [HttpPost]
    public async Task<ActionResult<AccountView>> PostAccount([FromBody] CreateAccountRequest request)
    {
        var username = request.Username.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return NotFound(new ApiError("USER_NOT_FOUND", $"User '{request.Username}' does not exist."));
        }
        if (!user.IsActive)
        {
            return UnprocessableEntity(new ApiError("USER_INACTIVE", "Cannot open an account for a deactivated user."));
        }
        if (decimal.Round(request.InitialDeposit, 2) != request.InitialDeposit)
        {
            return BadRequest(TooManyDecimals("InitialDeposit"));
        }

        var account = new Account
        {
            AccountNumber = await NewAccountNumber(),
            AccountName = request.AccountName.Trim(),
            Balance = request.InitialDeposit,
            IsActive = true,
            CreatedAt = DateTime.Now,
            User = user
        };
        _context.Accounts.Add(account);

        if (request.InitialDeposit > 0)
        {
            _context.Transactions.Add(new Transaction
            {
                Account = account,
                Type = "Deposit",
                Amount = request.InitialDeposit,
                Description = "Opening deposit",
                CreatedAt = account.CreatedAt
            });
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAccount), new { accountNumber = account.AccountNumber }, ToView(account));
    }

    [HttpPut("{accountNumber}")]
    public async Task<IActionResult> PutAccount(string accountNumber, [FromBody] UpdateAccountRequest request)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        if (account == null)
        {
            return NotFound(AccountNotFound(accountNumber));
        }

        account.AccountName = request.AccountName.Trim();
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{accountNumber}")]
    public async Task<IActionResult> DeactivateAccount(string accountNumber)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        if (account == null)
        {
            return NotFound(AccountNotFound(accountNumber));
        }

        account.IsActive = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{accountNumber}/activate")]
    public async Task<IActionResult> ActivateAccount(string accountNumber)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        if (account == null)
        {
            return NotFound(AccountNotFound(accountNumber));
        }

        account.IsActive = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<string> NewAccountNumber()
    {
        var random = new Random();
        string number;
        do
        {
            number = random.Next(10000000, 99999999).ToString();
        }
        while (await _context.Accounts.AnyAsync(a => a.AccountNumber == number));
        return number;
    }

    private static AccountView ToView(Account a)
    {
        return new AccountView
        {
            AccountNumber = a.AccountNumber,
            AccountName = a.AccountName,
            Balance = a.Balance,
            IsActive = a.IsActive,
            CreatedAt = a.CreatedAt,
            Username = a.User.Username
        };
    }

    private static ApiError AccountNotFound(string accountNumber)
    {
        return new ApiError("ACCOUNT_NOT_FOUND", $"Account '{accountNumber}' does not exist.");
    }

    private static ApiError TooManyDecimals(string field)
    {
        return new ApiError("VALIDATION_FAILED", "One or more fields failed validation.",
            new Dictionary<string, string[]> { { field, new[] { "Amount can have at most 2 decimal places." } } });
    }
}