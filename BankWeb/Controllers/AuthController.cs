using BankWeb.Models;
using BankWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BankWeb.Controllers
{
    public class AuthController : Controller
    {
        private readonly BankApiClient _bankApiClient;

        //get api client so this controller can communicate with PArt A
        public AuthController (BankApiClient bankApiClient)
        {
            _bankApiClient = bankApiClient;
        }

        //display registration page
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        //recevie registration form and send to Part A
        [HttpPost]
        public async Task<IActionResult> Register(UserCreateProfile userCreateProfile)
        {
            //check if required contents are entered
            if (string.IsNullOrEmpty(userCreateProfile.Username) ||
            string.IsNullOrEmpty(userCreateProfile.Name) || 
            string.IsNullOrEmpty(userCreateProfile.Email) ||
            string.IsNullOrEmpty(userCreateProfile.Address) ||
            string.IsNullOrEmpty(userCreateProfile.Phone) ||
            string.IsNullOrEmpty(userCreateProfile.Password))
            {
                ViewBag.Error = "Please fill in all the required fields";
                return View(userCreateProfile);
            }

            //send registration details to BankAPI
            ApiResult<UserResponse> result = await _bankApiClient.Register(userCreateProfile);

            //if fail, display error
            if (!result.Success)
            {
                if (result.ServiceUnavailable)
                {
                    ViewBag.Error = "The bank service is currently unavailable. Please try again later";
                }
                else
                {
                    //use error msg if hv, or else use the default msg
                    ViewBag.Error = result.Error?.Message ?? "Registration Failed";
                }
                
                return View(userCreateProfile);
            }
            //UPDATE BY EZRA 
            //Create a account so user automatically has an account to work with on registration (makes more sense) 
            //basically what this line means if by default ill use the value before ?? , if its null, then ill use the one on the right 
            string newUsername = result.Data?.Username ?? userCreateProfile.Username.Trim().ToLower();
            //create account through the service 
            var account = await _bankApiClient.CreateAccount(newUsername, "Everyday Account", 0);


            //registration succeeded, go to login page
            TempData["Success"] = "Registration Successful. You can login now";
            return RedirectToAction("Login");
        }

        //show login page, if user edy logged in, direct them to the logged in page instead
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        //receive login form and verify credentials through Part A
        [HttpPost]
        public async Task<IActionResult> Login(UserLogin userLogin)
        {
            //check if username & pw are entered
            if (string.IsNullOrEmpty(userLogin.Username) || string.IsNullOrEmpty(userLogin.Password))
            {
                ViewBag.Error  = "Please enter your username and password";
                return View(userLogin);
            }

            //send it to BankAPI
            ApiResult<UserResponse> result = await _bankApiClient.Login(userLogin);

            //if fail, display error
            if (!result.Success || result.Data == null)
            {
                if (result.ServiceUnavailable)
                {
                    ViewBag.Error = "The bank service is currently unavailable. Please try again later";
                }
                else
                {
                    //use error msg if hv, or else use the default msg
                    ViewBag.Error = result.Error?.Message ?? "Username or Password is incorrect. Please try again";
                }
                
                return View(userLogin);
            }

            //login succeeded, generate a login cookie
            //ref source: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0
            //store user's username n role as claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, result.Data.Username),
                new Claim("FullName", result.Data.Name),
                new Claim(ClaimTypes.Role, result.Data.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            //sign the user in and generate authentication cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity)
            );

            //redirect customer  to home page after login in
            return RedirectToAction("Index", "Home");
        }

        //log out n remove cookie
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            //direct to Login page
            return RedirectToAction("Login");
        }

        //prevent user trying to open a page they shouldn't be allowed to access
        [HttpGet]
        public IActionResult AccessDenied()
        {
            //403 Forbidden
            Response.StatusCode = 403;
            ViewBag.Code = 403;
            return View("StatusCode");
        }
    }
}