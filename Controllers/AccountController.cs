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

        var user = new User
        {
            Name = name,
            Email = email,
            PasswordHash = hashed,
            CreatedAt = DateTime.UtcNow,
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

        if (user.PasswordHash != HashPassword(password))
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

    [HttpPost]
    public async Task<IActionResult> UpdateAccount(
        string name,
        string email,
        string currentPassword,
        string newPassword,
        string confirmPassword
    )
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login");
        }

        var user = await _repo.GetByIdAsync(userId.Value);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        // Validate name and email
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError("", "Name and email are required.");
            ViewBag.CurrentName = user.Name;
            ViewBag.CurrentEmail = user.Email;
            return View("Profile");
        }

        // Check if email is already taken
        var existingUser = await _repo.GetByEmailAsync(email);
        if (existingUser != null && existingUser.UserId != userId.Value)
        {
            ModelState.AddModelError("", "Email is already in use.");
            ViewBag.CurrentName = user.Name;
            ViewBag.CurrentEmail = user.Email;
            return View("Profile");
        }

        bool updatedProfile = false;
        bool updatedPassword = false;

        // Update profile if changed
        if (user.Name != name || user.Email != email)
        {
            user.Name = name;
            user.Email = email;
            updatedProfile = true;
        }

        // Check if user wants to change password
        bool wantsPasswordChange =
            !string.IsNullOrWhiteSpace(currentPassword)
            || !string.IsNullOrWhiteSpace(newPassword)
            || !string.IsNullOrWhiteSpace(confirmPassword);

        if (wantsPasswordChange)
        {
            // Validate all password fields are filled
            if (
                string.IsNullOrWhiteSpace(currentPassword)
                || string.IsNullOrWhiteSpace(newPassword)
                || string.IsNullOrWhiteSpace(confirmPassword)
            )
            {
                ModelState.AddModelError(
                    "",
                    "All password fields are required to change password."
                );
                ViewBag.CurrentName = name;
                ViewBag.CurrentEmail = email;
                return View("Profile");
            }

            // Validate passwords match
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "New passwords do not match.");
                ViewBag.CurrentName = name;
                ViewBag.CurrentEmail = email;
                return View("Profile");
            }

            // Validate password length
            if (newPassword.Length < 1)
            {
                ModelState.AddModelError("", "Password must be at least 1 character.");
                ViewBag.CurrentName = name;
                ViewBag.CurrentEmail = email;
                return View("Profile");
            }

            // Verify current password
            var currentHashedPassword = HashPassword(currentPassword);
            if (user.PasswordHash != currentHashedPassword)
            {
                ModelState.AddModelError("", "Current password is incorrect.");
                ViewBag.CurrentName = name;
                ViewBag.CurrentEmail = email;
                return View("Profile");
            }

            // Update password
            user.PasswordHash = HashPassword(newPassword);
            updatedPassword = true;
        }

        // Save changes if any
        if (updatedProfile || updatedPassword)
        {
            await _repo.UpdateAsync(user);

            if (updatedProfile && updatedPassword)
            {
                TempData["Success"] = "Profile and password updated successfully!";
            }
            else if (updatedProfile)
            {
                TempData["Success"] = "Profile updated successfully!";
            }
            else if (updatedPassword)
            {
                TempData["Success"] = "Password changed successfully!";
            }
        }
        else
        {
            TempData["Success"] = "No changes made.";
        }

        return RedirectToAction("Profile");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTimerSettings(
        int focusDuration,
        int shortBreak,
        int longBreak,
        int sessionsUntilLongBreak,
        bool autoStartBreaks = false
    )
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login");
        }

        Console.WriteLine("UpdateTimerSettings called:");
        Console.WriteLine($"  focusDuration: {focusDuration}");
        Console.WriteLine($"  shortBreak: {shortBreak}");
        Console.WriteLine($"  longBreak: {longBreak}");
        Console.WriteLine($"  sessionsUntilLongBreak: {sessionsUntilLongBreak}");
        Console.WriteLine($"  autoStartBreaks: {autoStartBreaks}");

        await _repo.UpdateTimerSettingsAsync(
            userId.Value,
            focusDuration,
            shortBreak,
            longBreak,
            sessionsUntilLongBreak,
            autoStartBreaks
        );

        TempData["Success"] = "Timer settings updated successfully!";
        return RedirectToAction("Profile");
    }

    [Route("profile")]
    public async Task<IActionResult> Profile()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login");
        }

        var user = await _repo.GetByIdAsync(userId.Value);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        ViewBag.CurrentName = user.Name;
        ViewBag.CurrentEmail = user.Email;

        // Load timer settings from database
        ViewBag.FocusDuration = user.FocusDuration;
        ViewBag.ShortBreak = user.ShortBreak;
        ViewBag.LongBreak = user.LongBreak;
        ViewBag.SessionsUntilLongBreak = user.SessionsUntilLongBreak;
        ViewBag.AutoStartBreaks = user.AutoStartBreaks;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetTimerSettings()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return Json(
                new
                {
                    focusDuration = 25,
                    shortBreak = 5,
                    longBreak = 15,
                    sessionsUntilLongBreak = 4,
                    autoStartBreaks = false,
                }
            );
        }

        var user = await _repo.GetByIdAsync(userId.Value);

        return Json(
            new
            {
                focusDuration = user.FocusDuration,
                shortBreak = user.ShortBreak,
                longBreak = user.LongBreak,
                sessionsUntilLongBreak = user.SessionsUntilLongBreak,
                autoStartBreaks = user.AutoStartBreaks,
            }
        );
    }
}
