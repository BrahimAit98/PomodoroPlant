using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PomodoroPlant.Models;

namespace PomodoroPlant.Controllers;

public class RegisterController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
