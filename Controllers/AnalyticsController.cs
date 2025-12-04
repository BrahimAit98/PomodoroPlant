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

            // Add leaderboard
            stats.TopUsers = await _sessionRepo.GetTopUsersAsync(10);

            return View(stats);
        }

        private async Task<List<Achievement>> GetAchievementsFromStatsAsync(
            int userId,
            SessionStats stats
        )
        {
            var achievements = new List<Achievement>();

            // Get user's sessions to determine actual dates
            var sessions = await _sessionRepo.GetUserSessionsAsync(userId);

            // Streak-based achievements (Fire/Flame SVG)
            if (stats.DayStreak >= 1)
            {
                var today = DateTime.UtcNow.Date;
                var yesterday = today.AddDays(-1);
                var latestSessionDate = sessions.Max(s => s.SessionDate).Date;
                var isActiveStreak = latestSessionDate == today || latestSessionDate == yesterday;

                string streakTitle;
                if (stats.DayStreak >= 30)
                {
                    streakTitle = "30 Day Streak";
                }
                else if (stats.DayStreak >= 14)
                {
                    streakTitle = "14 Day Streak";
                }
                else if (stats.DayStreak >= 7)
                {
                    streakTitle = "7 Day Streak";
                }
                else if (stats.DayStreak >= 3)
                {
                    streakTitle = "3 Day Streak";
                }
                else
                {
                    streakTitle = $"{stats.DayStreak} Day Streak";
                }

                achievements.Add(
                    new Achievement
                    {
                        Title = streakTitle,
                        WhenText = isActiveStreak
                            ? "Active now"
                            : GetRelativeTime(latestSessionDate),
                        IconType = "fire",
                    }
                );
            }

            // Hours-based achievements (Clock SVG)
            var totalSeconds = sessions.Sum(s => s.DurationSeconds);
            var milestoneHit = 0;

            if (totalSeconds >= 360000) // 100 hours
            {
                milestoneHit = 100;
            }
            else if (totalSeconds >= 180000) // 50 hours
            {
                milestoneHit = 50;
            }
            else if (totalSeconds >= 90000) // 25 hours
            {
                milestoneHit = 25;
            }
            else if (totalSeconds >= 18000) // 5 hours
            {
                milestoneHit = 5;
            }

            if (milestoneHit > 0)
            {
                // Find when they crossed this milestone
                var targetSeconds = milestoneHit * 3600;
                var runningTotal = 0;
                DateTime? milestoneDate = null;

                foreach (var session in sessions.OrderBy(s => s.SessionDate))
                {
                    runningTotal += session.DurationSeconds;
                    if (runningTotal >= targetSeconds)
                    {
                        milestoneDate = session.SessionDate;
                        break;
                    }
                }

                // If user has been active today, show "Today" instead of when milestone was hit
                var hasSessionToday = sessions.Any(s => s.SessionDate.Date == DateTime.UtcNow.Date);

                achievements.Add(
                    new Achievement
                    {
                        Title = $"{milestoneHit} Hours Milestone",
                        WhenText = hasSessionToday
                            ? "Today"
                            : (
                                milestoneDate.HasValue
                                    ? GetRelativeTime(milestoneDate.Value)
                                    : "Recently"
                            ),
                        IconType = "clock",
                    }
                );
            }

            // Plant-based achievements (Plant/Palette SVG)
            if (stats.PlantsUnlocked >= 5)
            {
                // Find when 5th plant was unlocked - you'll need to check UserPlants table
                achievements.Add(
                    new Achievement
                    {
                        Title = "5 Plants Unlocked!",
                        WhenText = "Recently",
                        IconType = "plant",
                    }
                );
            }
            else if (stats.PlantsUnlocked >= 1)
            {
                // Find first session date as approximation for first plant
                var firstSessionDate = sessions.Min(s => s.SessionDate);
                achievements.Add(
                    new Achievement
                    {
                        Title = "First Plant Unlocked!",
                        WhenText = GetRelativeTime(firstSessionDate),
                        IconType = "plant",
                    }
                );
            }

            return achievements.Take(5).ToList();
        }

        private string GetRelativeTime(DateTime date)
        {
            var diff = DateTime.UtcNow.Date - date.Date;
            if (diff.Days == 0)
                return "Today";
            if (diff.Days == 1)
                return "Yesterday";
            if (diff.Days < 7)
                return $"{diff.Days} days ago";
            if (diff.Days < 30)
            {
                var weeks = diff.Days / 7;
                return $"{weeks} week{(weeks == 1 ? "" : "s")} ago";
            }
            var months = diff.Days / 30;
            return $"{months} month{(months == 1 ? "" : "s")} ago";
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
