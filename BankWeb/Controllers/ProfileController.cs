using BankWeb.Models;
using BankWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Authorize(Roles = "Customer")]
namespace BankWeb.Controllers
{
    public class ProfileController : Controller
    {
    }
}