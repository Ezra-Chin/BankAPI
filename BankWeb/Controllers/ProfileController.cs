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

    }
}