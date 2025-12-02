using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PomodoroPlant.Models;
using PomodoroPlant.Repositories;

namespace PomodoroPlant.Controllers
{
    public class AnalyticsController : Controller
    {
        private readonly SessionRepository _sessionRepo = new SessionRepository();

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var stats = await _sessionRepo.GetStatsForUserAsync(userId.Value);
            return View(stats);
        }

        [HttpPost]
        public async Task<IActionResult> TrackSession([FromBody] TrackSessionDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Mode) || dto.DurationSeconds <= 0)
                return BadRequest("Invalid session data.");

            await _sessionRepo.LogSessionAsync(userId.Value, dto.Mode, dto.DurationSeconds);
            return Ok("Session logged");
        }
    }
}
