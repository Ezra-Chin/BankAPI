using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankApi.Models;
using BankApi.Data;
//set base url to /api/accounts 
[Route("api/[controller]")]
[ApiController]
public class AccountsController : ControllerBase
{

    private readonly DBManager _context;

    public AccountsController(DBManager context)
    {
        _context = context;
    }

    //GET /api/accounts?username=
    // retrieve all accounts or filter for specific accounts 
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountView>>> GetAccounts(string? username)
    {
        // start query for all accounts 
        var query = _context.Accounts.AsQueryable();
        //filter by username if given 
        if (!string.IsNullOrWhiteSpace(username))
        {
            var name = username.Trim().ToLower();
            if (!await _context.Users.AnyAsync(u => u.Username == name))
            {
                return NotFound(new ApiError("USER_NOT_FOUND", $"User '{username}' does not exist."));
            }
            query = query.Where(a => a.User.Username == name);
        }
        //sort accounts by account number 
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

    // GET /api/accounts/{accountsNumber}
    // Retrieve one accounts by its account number 

    [HttpGet("{accountNumber}")]
    public async Task<ActionResult<AccountView>> GetAccount(string accountNumber)
    {
        //fetch account from db based on the 
        var account = await _context.Accounts.Include(a => a.User)
                                             .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        if (account == null)
        {
            return NotFound(AccountNotFound(accountNumber));
        }
        return Ok(ToView(account));
    }
    // GET /api/accounts/{accountnumber}/transactions 
    //retrieve an account's transaction with filtering and sorting and paging 
    [HttpGet("{accountNumber}/transactions")]
    public async Task<ActionResult<PagedResult<TransactionView>>> GetAccountTransactions(
        string accountNumber, DateTime? from, DateTime? to,
        int page = 1, int pageSize = 10, string sortBy = "date", string sortDir = "desc")
    {

        //validate based on page size 
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new ApiError("INVALID_PAGING", "page must be 1 or more and pageSize must be between 1 and 100."));
        }
        //reject the date if the start if after the end 
        if (from != null && to != null && from > to)
        {
            return BadRequest(new ApiError("INVALID_DATE_RANGE", "'from' must be on or before 'to'."));
        }
        //check if account exists 
        if (!await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber))
        {
            return NotFound(AccountNotFound(accountNumber));
        }
        //start query consisting onlly transaction for this acc 
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
        // determine if reuslt should be ascending or descending 
        bool ascending = sortDir.ToLower() == "asc";

        //apply requested sorting method 
        switch (sortBy.ToLower())
        {
            //sort by date 
            case "date":
                query = ascending ? query.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id) : query.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id);
                break;
                //sort by amount 
            case "amount":
                query = ascending ? query.OrderBy(t => (double)t.Amount) : query.OrderByDescending(t => (double)t.Amount);
                break;
                //sort by type of transaction
            case "type":
                query = ascending ? query.OrderBy(t => t.Type) : query.OrderByDescending(t => t.Type);
                break;
            default:
                //reject unsupported sorting fields 
                return BadRequest(new ApiError("INVALID_SORT", "sortBy must be one of: date, amount, type."));
        }

        //build the page based on the respones 
        var result = new PagedResult<TransactionView>
        {
            TotalCount = await query.CountAsync(),
            Page = page,
            PageSize = pageSize,

            //skip earlier pages that isnt taken (for example if page is 11 , screw page 1 - 10) 
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

    // post /api/accounts 
    // create a new account for an existing active user 
    [HttpPost]
    public async Task<ActionResult<AccountView>> PostAccount([FromBody] CreateAccountRequest request)
    {
        var username = request.Username.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        //return 404 if user doesnt exists 
        if (user == null)
        {
            return NotFound(new ApiError("USER_NOT_FOUND", $"User '{request.Username}' does not exist."));
        }
        //deactivated user cant open accounts ,so throw 402 
        if (!user.IsActive)
        {
            return UnprocessableEntity(new ApiError("USER_INACTIVE", "Cannot open an account for a deactivated user."));
        }
        //reject irregular initial deposit 
        if (decimal.Round(request.InitialDeposit, 2) != request.InitialDeposit)
        {
            return BadRequest(TooManyDecimals("InitialDeposit"));
        }
        //crate new account 
        var account = new Account
        {
            AccountNumber = await NewAccountNumber(),
            AccountName = request.AccountName.Trim(),
            Balance = request.InitialDeposit,
            IsActive = true,
            CreatedAt = DateTime.Now,
            User = user
        };
        //add the account into databse 
        _context.Accounts.Add(account);
        //if there's a initial depsoit ,then set that as a transaction 
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
        //pushing the changes into database (the ones before are like git commits while this is like git push) 
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetAccount), new { accountNumber = account.AccountNumber }, ToView(account));
    }
    // PUT /api/accounts/{accountNumber}
    // update an account's name 
    [HttpPut("{accountNumber}")]
    public async Task<IActionResult> PutAccount(string accountNumber, [FromBody] UpdateAccountRequest request)
    {
        //find which account is it 
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        //if cant find that acc, throw 404 
        if (account == null)
        {
            return NotFound(AccountNotFound(accountNumber));
        }
        
        account.AccountName = request.AccountName.Trim();
        //save the changes to database 
        await _context.SaveChangesAsync();
        return NoContent();
    }
    //DELTE /api/accounts/{accountNumber}
    // Deactive an account wihtout deleting the database record 
    [HttpDelete("{accountNumber}")]
    public async Task<IActionResult> DeactivateAccount(string accountNumber)
    {
        //Find the account to deactive 
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        //If account cant be found ,throw 404
        if (account == null)
        {
            return NotFound(AccountNotFound(accountNumber));
        }
        //set the account to not active 
        account.IsActive = false;
        //save to databse 
        await _context.SaveChangesAsync();
        return NoContent();
    }
    //PUT /api/accounts/{accountsNumber}/activaet 
    // Reactivate existing accoutns 
    [HttpPut("{accountNumber}/activate")]
    public async Task<IActionResult> ActivateAccount(string accountNumber)
    {
        //Find account to reactive 
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        //if exists , reactive 
        if (account == null)
        {
            return NotFound(AccountNotFound(accountNumber));
        }
        //set account to active
        account.IsActive = true;
        //save it to database 
        await _context.SaveChangesAsync();
        return NoContent();
    }

    //generate account number that is already not in use 
    private async Task<string> NewAccountNumber()
    {
        //generate random number until its a number not in database 
        var random = new Random();
        string number;
        do
        {
            number = random.Next(10000000, 99999999).ToString();
        }
        while (await _context.Accounts.AnyAsync(a => a.AccountNumber == number));
        return number;
    }

    //Convert account entity into account into a format i want to send through api  ( so no id and shit) 
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

    //to standardized the error that is account not found 
    private static ApiError AccountNotFound(string accountNumber)
    {
        return new ApiError("ACCOUNT_NOT_FOUND", $"Account '{accountNumber}' does not exist.");
    }

    //to standardized the error thatis too many decimals 
    private static ApiError TooManyDecimals(string field)
    {
        return new ApiError("VALIDATION_FAILED", "One or more fields failed validation.",
            new Dictionary<string, string[]> { { field, new[] { "Amount can have at most 2 decimal places." } } });
    }
}