using System.Globalization;
using BankWeb.Models;
using BankWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankWeb.Controllers
{
    [Authorize(Roles = "Customer")]
    public class TransferController : Controller
    {
        private readonly BankApiClient _api;

        public TransferController(BankApiClient api)
        {
            _api = api;
        }

        [HttpGet]
        public IActionResult Index(string? from)
        {
            ViewBag.From = from;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Form(string? from)
        {
            var model = new TransferViewModel { FromAccountNumber = from ?? "" };
            await LoadMyAccounts(model);
            return PartialView("_TransferForm", model);
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] TransferViewModel model)
        {
            if (model == null)
            {
                return BadRequest();
            }
            await LoadMyAccounts(model);
            if (ViewBag.Error != null)
            {
                return PartialView("_TransferForm", model);
            }

            var fromAccount = model.MyAccounts.FirstOrDefault(a => a.AccountNumber == model.FromAccountNumber);
            if (!string.IsNullOrEmpty(model.FromAccountNumber) && fromAccount == null)
            {
                ModelState.AddModelError("FromAccountNumber", "You can only transfer from your own accounts.");
            }
            if (fromAccount != null && !fromAccount.IsActive)
            {
                ModelState.AddModelError("FromAccountNumber", "This account is deactivated.");
            }
            if (model.FromAccountNumber == model.ToAccountNumber && !string.IsNullOrEmpty(model.ToAccountNumber))
            {
                ModelState.AddModelError("ToAccountNumber", "The recipient account must be different from the 'from' account.");
            }
            if (fromAccount != null && model.Amount != null && model.Amount > fromAccount.Balance)
            {
                ModelState.AddModelError("Amount", $"Insufficient funds. Available balance is {fromAccount.Balance.ToString("C", CultureInfo.CurrentCulture)}.");
            }
            if (!ModelState.IsValid)
            {
                return PartialView("_TransferForm", model);
            }

            var result = await _api.Transfer(model.FromAccountNumber, model.ToAccountNumber, model.Amount!.Value, model.Description);
            if (!result.Success)
            {
                switch (result.Error?.Error)
                {
                    case "ACCOUNT_NOT_FOUND":
                        ModelState.AddModelError("ToAccountNumber", "No account with this number exists.");
                        break;
                    case "INSUFFICIENT_FUNDS":
                        ModelState.AddModelError("Amount", "Insufficient funds. " + result.Error.Message);
                        break;
                    case "ACCOUNT_INACTIVE":
                        ModelState.AddModelError("ToAccountNumber", "The recipient account is not active.");
                        break;
                    case "SAME_ACCOUNT":
                        ModelState.AddModelError("ToAccountNumber", "The recipient account must be different from the 'from' account.");
                        break;
                    default:
                        ViewBag.Error = result.ErrorMessage;
                        break;
                }
                return PartialView("_TransferForm", model);
            }

            ModelState.Clear();
            var fresh = new TransferViewModel { FromAccountNumber = model.FromAccountNumber };
            await LoadMyAccounts(fresh);
            ViewBag.Success = $"Transferred {model.Amount.Value.ToString("C", CultureInfo.CurrentCulture)} from {model.FromAccountNumber} to {model.ToAccountNumber}.";
            return PartialView("_TransferForm", fresh);
        }

        private async Task LoadMyAccounts(TransferViewModel model)
        {
            var result = await _api.GetAccounts(User.Identity!.Name!);
            if (!result.Success || result.Data == null)
            {
                ViewBag.Error = result.ErrorMessage;
                return;
            }
            model.MyAccounts = result.Data;
        }
    }
}