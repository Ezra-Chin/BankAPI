using BankWeb.Models;
using BankWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankWeb.Controllers
{
    // accounts summary page and transaction history page
    // the pages load first, then accounts.js uses fetch() to get the parts that change
    // only logged in customers can use this controller
    [Authorize(Roles = "Customer")]
    public class AccountsController : Controller
    {
        // used to call the part A web service (restsharp)
        private readonly BankApiClient _api;
        // the only columns we allow sorting by
        private static readonly string[] SortFields = { "date", "amount", "type" };

        public AccountsController(BankApiClient api)
        {
            _api = api;
        }
        // GET /Accounts
        // the "my accounts" page, the account cards are loaded after by accounts.js
        public IActionResult Index()
        {
            return View();
        }

        // GET /Accounts/List
        // called by fetch(), returns the account cards (account number + balance) as html
        [HttpGet]
        public async Task<IActionResult> List()
        {
            // get the accounts of the user who is logged in
            var result = await _api.GetAccounts(User.Identity!.Name!);
            if (!result.Success || result.Data == null)
            {
                ViewBag.Error = result.ErrorMessage;
                return PartialView("_AccountList", new List<AccountModel>());
            }
            return PartialView("_AccountList", result.Data);
        }

        // GET /Accounts/History/{accountNumber}
        // the history page with the date filter, the table is loaded after by accounts.js
        public async Task<IActionResult> History(string id)
        {
            var account = await GetMyAccount(id);
            // service is down, go back to my accounts page and show an error
            if (account.Unavailable)
            {
                TempData["Error"] = "The bank service is currently unavailable. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
            // 404 returned if account does not exist
            if (account.Data == null)
            {
                return NotFound();
            }
            return View(account.Data);
        }

        // GET /Accounts/HistoryTable/{accountNumber}?from=&to=&page=&sortBy=&sortDir=
        // called by fetch(), returns the history table + page buttons as html
        [HttpGet]
        public async Task<IActionResult> HistoryTable(string id, DateTime? from, DateTime? to, int page = 1, string sortBy = "date", string sortDir = "desc")
        {
            // check again that the account belongs to this user
            var account = await GetMyAccount(id);
            if (account.Unavailable)
            {
                ViewBag.Error = "The bank service is currently unavailable. Please try again later.";
                return PartialView("_HistoryTable", new TransactionHistoryViewModel());
            }
            if (account.Data == null)
            {
                return NotFound();
            }

            var model = new TransactionHistoryViewModel
            {
                Account = account.Data,
                Filter = new TransactionFilter
                {
                    AccountNumber = id,
                    From = from,
                    To = to,
                    Page = page < 1 ? 1 : page,
                    PageSize = 10,
                    SortBy = SortFields.Contains(sortBy) ? sortBy : "date",
                    SortDir = sortDir == "asc" ? "asc" : "desc"
                }
            };

            // from date cant be after the to date
            if (from != null && to != null && from > to)
            {
                ViewBag.Error = "The 'From' date must be on or before the 'To' date.";
                return PartialView("_HistoryTable", model);
            }

            // get one page of the history from the web service
            var result = await _api.GetAccountHistory(model.Filter);
            if (!result.Success || result.Data == null)
            {
                ViewBag.Error = result.ErrorMessage;
                return PartialView("_HistoryTable", model);
            }

            model.Result = result.Data;
            return PartialView("_HistoryTable", model);
        }

        // get an accountbut only if it belongs to the logged in user
        // someone else's account gives the same "not found" as an account that doesnt exist
        // Unavailable = true means the web service could not be reached
        private async Task<(AccountModel? Data, bool Unavailable)> GetMyAccount(string? accountNumber)
        {
            var result = await _api.GetAccount(accountNumber ?? "");
            if (result.ServiceUnavailable)
            {
                return (null, true);
            }
            if (!result.Success || result.Data == null || result.Data.Username != User.Identity!.Name)
            {
                return (null, false);
            }
            return (result.Data, false);
        }
    }
}