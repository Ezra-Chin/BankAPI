using BankWeb.Models;
using BankWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankWeb.Controllers
{
    [Authorize(Roles = "Customer")]
    public class AccountsController : Controller
    {
        private readonly BankApiClient _api;
        private static readonly string[] SortFields = { "date", "amount", "type" };

        public AccountsController(BankApiClient api)
        {
            _api = api;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var result = await _api.GetAccounts(User.Identity!.Name!);
            if (!result.Success || result.Data == null)
            {
                ViewBag.Error = result.ErrorMessage;
                return PartialView("_AccountList", new List<AccountModel>());
            }
            return PartialView("_AccountList", result.Data);
        }

        public async Task<IActionResult> History(string id)
        {
            var account = await GetMyAccount(id);
            if (account.Unavailable)
            {
                TempData["Error"] = "The bank service is currently unavailable. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
            if (account.Data == null)
            {
                return NotFound();
            }
            return View(account.Data);
        }

        [HttpGet]
        public async Task<IActionResult> HistoryTable(string id, DateTime? from, DateTime? to, int page = 1, string sortBy = "date", string sortDir = "desc")
        {
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

            if (from != null && to != null && from > to)
            {
                ViewBag.Error = "The 'From' date must be on or before the 'To' date.";
                return PartialView("_HistoryTable", model);
            }

            var result = await _api.GetAccountHistory(model.Filter);
            if (!result.Success || result.Data == null)
            {
                ViewBag.Error = result.ErrorMessage;
                return PartialView("_HistoryTable", model);
            }

            model.Result = result.Data;
            return PartialView("_HistoryTable", model);
        }

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