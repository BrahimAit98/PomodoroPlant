using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PomodoroPlant.Models;

namespace PomodoroPlant.Controllers;

public class AccountController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }
}
