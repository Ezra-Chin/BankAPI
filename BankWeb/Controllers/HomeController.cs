using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BankWeb.Models;

namespace BankWeb.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return User.IsInRole("Admin") ? RedirectToAction("Index", "Admin") : RedirectToAction("Index", "Accounts");
        }
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [Route("Home/StatusCode/{code}")]
    public IActionResult StatusCodePage(int code)
    {
        ViewBag.Code = code;
        return View("StatusCode");
    }
}