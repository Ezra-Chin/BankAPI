using BankWeb.Models;
using BankWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BankWeb.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly BankApiClient _bankApiClient;

        public ProfileController (BankApiClient bankApiClient)
        {
            _bankApiClient = bankApiClient;
        }

        private async Task LoadProfile()
        {
            //get username of the current user
            if (User.Identity == null || User.Identity.Name == null)
            {
                return;
            }
            string username = User.Identity.Name;

            //req for curr user profile info from Bank API
            ApiResult<UserResponse> result = await _bankApiClient.GetUser(username);

            //req failed or no data returned
            if (!result.Success || result.Data == null)
            {
                ViewBag.Error = result.ErrorMessage;
                return;
            }
            else
            {
                //pass user data to view
                ViewBag.Profile = result.Data;
            }
        }

        //show logged in user profile
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await LoadProfile();
            return View();
        }

        //show edit form, filled with curr detaikls
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            await LoadProfile();
            return View();
        }

        //reveive edit form and send changes made to Part A
        [HttpPost]
        public async Task<IActionResult> Edit (UserUpdateProfile userUpdateProfile)
        {
            if (User.Identity == null || User.Identity.Name == null)
            {
                return Unauthorized();
            }
            string username = User.Identity.Name;

            //spec mentioned change picture, contact details n pw
            //ignore username, name 
            userUpdateProfile.Username = null;
            userUpdateProfile.Name = null;

            //make sure these fields not empty
            string error = "";
            if (string.IsNullOrEmpty(userUpdateProfile.Email) || string.IsNullOrEmpty(userUpdateProfile.Address) || string.IsNullOrEmpty(userUpdateProfile.Phone))
            {
                error = "Email, Address and Phone must not be empty";
            }
            else if (!userUpdateProfile.Email.Contains("@"))
            {
                error = "Please enter a valid email address";
            }

            if (string.IsNullOrEmpty(error))
            {
                ApiResult<object> result = await _bankApiClient.UpdateUser(username, userUpdateProfile);

                if (result.Success)
                {
                    //in _Alert.cshtml
                    TempData["Success"] = "Your profile has been updated";
                    return RedirectToAction("Index");
                }

                error  = result.ErrorMessage;
            }

            //show form with error if stg went wrong
            ViewBag.Error = error;
            ApiResult<UserResponse> current = await _bankApiClient.GetUser(username);

            if (current.Success && current.Data != null)
            {
                //keep what user wrote
                current.Data.Email = userUpdateProfile.Email ?? "";
                current.Data.Address = userUpdateProfile.Address ?? "";
                current.Data.Phone = userUpdateProfile.Phone ?? "";

                ViewBag.Profile = current.Data;
            }
            return View();
        }

        //send login user pfp
        [HttpGet]
        public async Task<IActionResult> ProfilePicture()
        {
            if (User.Identity == null || User.Identity.Name == null)
            {
                return Unauthorized();
            }
            string username = User.Identity.Name;

            ApiResult<ProfilePictureFile> result = await _bankApiClient.GetUserProfilePicture(username);

            if (!result.Success || result.Data == null)
            {
                return NotFound();
            }
            
            return File(result.Data.Bytes, result.Data.ContentType);
        }
    }   
}