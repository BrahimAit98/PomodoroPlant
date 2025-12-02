using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PomodoroPlant.Models;

namespace PomodoroPlant.Controllers;

public class AccountController : Controller
{
    private readonly UserRepository _repo = new UserRepository();

    public IActionResult Index()
    {
        return View();
    }

    [Route("register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount(string name, string email, string password)
    {
        if (
            string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password)
        )
        {
            ModelState.AddModelError(string.Empty, "All fields are required.");
            return View("Register");
        }

        // if email already exists
        var existingUser = await _repo.GetByEmailAsync(email);
        if (existingUser != null)
        {
            ModelState.AddModelError(string.Empty, "Email is already registered.");
            return View("Register");
        }

        var hashed = HashPassword(password);

        var user = new UserModel
        {
            Name = name,
            Email = email,
            HashedPassword = hashed,
            ArduinoId = 0,
            CreatedAt = DateTime.UtcNow,
            Role = "User",
        };

        await _repo.CreateAsync(user);
        return RedirectToAction("Index", "Pomodoro");
    }

    [Route("login")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> LoginUser(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Email and password are required.");
            return View("Login");
        }

        var user = await _repo.GetByEmailAsync(email);
        if (user == null)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View("Login");
        }

        if (user.HashedPassword != HashPassword(password))
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View("Login");
        }

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("UserName", user.Name);

        return RedirectToAction("Index", "Pomodoro");
    }

    private string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    public bool IsLoggedIn()
    {
        return HttpContext.Session.GetInt32("UserId") != null;
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Pomodoro");
    }
}
