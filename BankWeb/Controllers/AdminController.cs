using BankWeb.Models;
using BankWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankWeb.Controllers
{

    //basically only allow Admins to call /api/admin 
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly BankApiClient _api;

        public AdminController(BankApiClient bankApiClient)
        {
            _api = bankApiClient;
        }

        //admin dashboard 
        //GET /admin
        public IActionResult Index()
        {
            return View();
        }

        //NOTE FOR SELF 
        //for something like /admin/users/{username} (as logn ashas [HttpGet("{username}}")] then put username as parameter will be fine 
        //for something like /admin/users?serach=xxx , just have search inside parameter will be fine


        //GET /admin/users?search=xxxx
        [HttpGet]
        public async Task<IActionResult> Users(string? search)
        {
            //call the service that calls the api from BankAPI 
            var result = await _api.SearchUser(search);
            if (!result.Success)
            {
                //return an empty list 
                return View(new List<UserResponse>());
            }
            //return the result 
            return View(result.Data);
        }


        //GET admin/createUser
        public IActionResult CreateUser()
        {
            return View(new UserCreateProfile());
        }


        //POST  /admin/createUser
        [HttpPost]
        public async Task<IActionResult> CreateUser(UserCreateProfile userCreateProfile)
        {
            //check fi there's any non field fields 
            if 
                ((string.IsNullOrWhiteSpace(userCreateProfile.Username)) ||
                (string.IsNullOrWhiteSpace(userCreateProfile.Name)) ||
                (string.IsNullOrWhiteSpace(userCreateProfile.Email)) ||
                (string.IsNullOrWhiteSpace(userCreateProfile.Address)) ||
                (string.IsNullOrWhiteSpace(userCreateProfile.Phone )) ||
                (string.IsNullOrWhiteSpace(userCreateProfile.Password))){

                return View(userCreateProfile);

            }

            //register user to database 
            var result = await _api.Register(userCreateProfile);
            if (!result.Success)
            {
                return View(userCreateProfile);

            }

            //audit it to database 
            string username = userCreateProfile.Username;
            await Audit("CREATE_USER", "Created customer" + username);
            return RedirectToAction(nameof(User));


        }

        //GET /admin/edituser/{whatevernanme}
        public async Task<IActionResult> EditUser (string id)
        {
            var result = await _api.GetUser(id);
            if(!result.Success || result.Data == null)
            {
                return RedirectToAction(nameof(Users));
            }
            var user = new UserUpdateProfile { Name = result.Data.Name, Email = result.Data.Email, Address = result.Data.Address, Phone = result.Data.Phone };
            return View(user);
        }


        // POST /admin/edituser/{whateverusername} 
        [HttpPost]
        public async Task<IActionResult> EditUser(string id, UserUpdateProfile newProfile)
        {

            //dont let them chagne username and password her e
            newProfile.Username = null;
            newProfile.Password = null;

            var result = await _api.UpdateUser(id, newProfile);
            if (!result.Success)
            {
                return View(newProfile);

            }

            await Audit("EditUser", "Updateed User Profile for " + id);
            return RedirectToAction(nameof(Users));
        }


        [HttpPost]
        public async Task<IActionResult> Deactivate(string id)
        {
            var result = await _api.DeactivateUser(id);

            if (!result.Success)
            {
                return RedirectToAction(nameof(Users));

            }
            return RedirectToAction(nameof(Users));

        }


        [HttpPost]
        public async Task<IActionResult> Activate(string id)
        {
            var result = await _api.ReactivateUser(id);

            if (!result.Success)
            {
                return RedirectToAction(nameof(Users));

            }
            return RedirectToAction(nameof(Users));

        }

        //reset passwrod for user 

        public async Task<IActionResult> ResetPassword(string id , string newPassword)
        {
            if(string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6 )
            {
                return RedirectToAction(nameof(Users));
            }
            var update = new UserUpdateProfile { Password = newPassword };
            var res = await _api.UpdateUser(id, update);
            if (!res.Success)
            {
                return RedirectToAction(nameof(Users));
            }

            await Audit("RESET_PASSWORD", "Password reset for user " + id);
            return RedirectToAction(nameof(Users));

        }


        //basically any admin action will call this fucntion 
        //what this function does is save the given action and details to the database , here -> service -> api -> bankapi -> database 
        private async Task Audit(string action , string details)
        {
            string adminUsername = User.Identity.Name;
            var result = await _api.AddAudit(adminUsername, action, details);
            if (!result.Success)
            {

            }
        }
    }
}
