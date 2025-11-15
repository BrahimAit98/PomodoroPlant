using Microsoft.AspNetCore.Mvc;

namespace PomodoroPlant.Controllers
{
    // Controller responsible for Pomodoro-related pages
    public class PomodoroController : Controller
    {
        // GET: /Pomodoro/Timer
        // Returns the Timer view (Views/Pomodoro/Timer.cshtml)
        public IActionResult Timer()
        {
            return View();
        }
    }
}
