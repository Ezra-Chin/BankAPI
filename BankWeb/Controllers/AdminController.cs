using BankWeb.Models;
using BankWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankWeb.Controllers
{
    //only Admins can reach ANY action here. This is checked on the server,
    //so a customer editing the URL to /Admin/... is still blocked
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly BankApiClient _api;
        //only alow these fields when sorting 
        private static readonly string[] SortFields = { "date", "amount", "type" };
        //only allow these fields for trnsaction type 
        private static readonly string[] TransactionTypes = { "Deposit", "Withdrawal", "TransferIn", "TransferOut" };


        public AdminController(BankApiClient bankApiClient)
        {
            _api = bankApiClient;
        }

        
        //Admin Dashboard (has user coounts and latest audit entries) 
        //GET /Admin
        public async Task<IActionResult> Index()
        {
            //call the service to fetch user count and get audit 
            var users = await _api.SearchUser(null);
            var audit = await _api.GetAudit(1, 5);


            if (!users.Success || !audit.Success)
            {
                ViewBag.Error = users.Success ? audit.ErrorMessage : users.ErrorMessage;
            }

            if (users.Success && users.Data != null)
            {
                ViewBag.TotalUsers = users.Data.Count;
                ViewBag.ActiveUsers = users.Data.Count(u => u.IsActive);
            }

            return View(audit.Data?.Items ?? new List<AuditEntry>());
        }

        //GET /Admin/Users?search=xxxx  (name, username or account number)
        [HttpGet]
        public async Task<IActionResult> Users(string? search)
        {
            ViewBag.Search = search;
            var result = await _api.SearchUser(search);
            if (!result.Success || result.Data == null)
            {
                //e.g. service is down -> show a message instead of an empty table
                ViewBag.Error = result.ErrorMessage;
                return View(new List<UserResponse>());
            }
            return View(result.Data);
        }

        //GET /Admin/CreateUser
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View(new UserCreateProfile());
        }

        //POST /Admin/CreateUser
        [HttpPost]
        public async Task<IActionResult> CreateUser(UserCreateProfile userCreateProfile)
        {
            if (string.IsNullOrWhiteSpace(userCreateProfile.Username) ||
                string.IsNullOrWhiteSpace(userCreateProfile.Name) ||
                string.IsNullOrWhiteSpace(userCreateProfile.Email) ||
                string.IsNullOrWhiteSpace(userCreateProfile.Address) ||
                string.IsNullOrWhiteSpace(userCreateProfile.Phone) ||
                string.IsNullOrWhiteSpace(userCreateProfile.Password))
            {
                ViewBag.Error = "Please fill in all the required fields.";
                return View(userCreateProfile);
            }

            var result = await _api.Register(userCreateProfile);
            if (!result.Success)
            {
                //e.g. "This username is already taken" from Part A
                ViewBag.Error = result.ErrorMessage;
                return View(userCreateProfile);
            }

            string username = result.Data?.Username ?? userCreateProfile.Username;
            await _api.CreateAccount(username, "Everyday Account", 0);
            await Audit("CREATE_USER", "Created customer " + username);
            TempData["Success"] = "User " + username + " was created.";
            return RedirectToAction(nameof(Users));
        }

        //GET /Admin/EditUser/{username}
        [HttpGet]
        public async Task<IActionResult> EditUser(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(nameof(Users));
            }

            var result = await _api.GetUser(id);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.ServiceUnavailable ? result.ErrorMessage : "User '" + id + "' could not be found.";
                return RedirectToAction(nameof(Users));
            }

            ViewBag.Username = result.Data.Username;
            var user = new UserUpdateProfile
            {
                Name = result.Data.Name,
                Email = result.Data.Email,
                Address = result.Data.Address,
                Phone = result.Data.Phone
            };
            return View(user);
        }

        //POST /Admin/EditUser/{username}
        [HttpPost]
        public async Task<IActionResult> EditUser(string? id, UserUpdateProfile newProfile)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(nameof(Users));
            }
            ViewBag.Username = id;

            if (string.IsNullOrWhiteSpace(newProfile.Name) ||
                string.IsNullOrWhiteSpace(newProfile.Email) ||
                string.IsNullOrWhiteSpace(newProfile.Address) ||
                string.IsNullOrWhiteSpace(newProfile.Phone))
            {
                ViewBag.Error = "Please fill in all the fields.";
                return View(newProfile);
            }

            //username and password are not changed on this form (password has its own reset form)
            newProfile.Username = null;
            newProfile.Password = null;

            var result = await _api.UpdateUser(id, newProfile);
            if (!result.Success)
            {
                ViewBag.Error = result.ErrorMessage;
                return View(newProfile);
            }

            await Audit("EDIT_USER", "Updated profile details for " + id);
            TempData["Success"] = "Details for " + id + " were updated.";
            return RedirectToAction(nameof(Users));
        }

        //POST /Admin/Deactivate/{username}
        [HttpPost]
        public async Task<IActionResult> Deactivate(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(nameof(Users));
            }
            //stop an admin locking themselves out
            if (id == User.Identity?.Name)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Users));
            }

            var result = await _api.DeactivateUser(id);
            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Users));
            }

            await Audit("DEACTIVATE_USER", "Deactivated user " + id);
            TempData["Success"] = "User " + id + " was deactivated.";
            return RedirectToAction(nameof(Users));
        }

        //POST /Admin/Activate/{username}
        [HttpPost]
        public async Task<IActionResult> Activate(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(nameof(Users));
            }

            var result = await _api.ReactivateUser(id);
            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Users));
            }

            await Audit("ACTIVATE_USER", "Reactivated user " + id);
            TempData["Success"] = "User " + id + " was reactivated.";
            return RedirectToAction(nameof(Users));
        }

        //POST /Admin/ResetPassword/{username}
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string? id, string? newPassword)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(nameof(Users));
            }
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["Error"] = "The new password must be at least 6 characters.";
                return RedirectToAction(nameof(EditUser), new { id });
            }

            var result = await _api.UpdateUser(id, new UserUpdateProfile { Password = newPassword });
            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(EditUser), new { id });
            }

            await Audit("RESET_PASSWORD", "Reset the password for " + id);
            TempData["Success"] = "Password for " + id + " was reset.";
            return RedirectToAction(nameof(Users));
        }

        //GET /Admin/Transactions  (page shell, the table is loaded by admin.js)
        [HttpGet]
        public IActionResult Transactions()
        {
            return View();
        }

        //GET /Admin/TransactionsTable?search=&type=&from=&to=&page=&sortBy=&sortDir=
        //returns only the table (partial view), same idea as AccountsController.HistoryTable
        [HttpGet]
        public async Task<IActionResult> TransactionsTable(string? search, string? type, DateTime? from, DateTime? to,
            int page = 1, string sortBy = "date", string sortDir = "desc")
        {
            var model = new TransactionHistoryViewModel
            {
                Filter = new TransactionFilter
                {
                    Search = search,
                    Type = TransactionTypes.Contains(type) ? type : null,
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
                return PartialView("TransactionsTableView", model);
            }

            var result = await _api.GetAllTransactions(model.Filter);
            if (!result.Success || result.Data == null)
            {
                ViewBag.Error = result.ErrorMessage;
                return PartialView("TransactionsTableView", model);
            }

            model.Result = result.Data;
            return PartialView("TransactionsTableView", model);
        }

        //GET /Admin/AuditTrail  (page shell, the table is loaded by admin.js)
        [HttpGet]
        public IActionResult AuditTrail()
        {
            return View();
        }

        //GET /Admin/AuditTable?page=
        [HttpGet]
        public async Task<IActionResult> AuditTable(int page = 1)
        {
            var result = await _api.GetAudit(page < 1 ? 1 : page, 10);
            if (!result.Success || result.Data == null)
            {
                ViewBag.Error = result.ErrorMessage;
                return PartialView("AuditTableView", new PagedResult<AuditEntry>());
            }
            return PartialView("AuditTableView", result.Data);
        }

        //every admin action calls this: BankWeb -> BankApiClient -> POST api/audit -> database
        private async Task Audit(string action, string details)
        {
            string adminUsername = User.Identity?.Name ?? "unknown";
            var result = await _api.AddAudit(adminUsername, action, details);
            if (!result.Success)
            {
                TempData["Error"] = "The action was completed, but it could not be recorded in the audit trail. " + result.ErrorMessage;
            }
        }
    }
}