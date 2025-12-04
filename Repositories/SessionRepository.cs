using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PomodoroPlant.Models;

namespace PomodoroPlant.Repositories
{
    public class SessionRepository
    {
        private readonly string _connectionString = "Data Source=PomodoroPlant.db;";

        public async Task LogSessionAsync(int userId, string mode, int durationSeconds)
        {
            const string sql =
                @"
                INSERT INTO Sessions (UserId, Mode, DurationSeconds, CompletedAt)
                VALUES ($UserId, $Mode, $DurationSeconds, $CompletedAt);
            ";

            using var conn = new SqliteConnection(_connectionString);
            using var cmd = new SqliteCommand(sql, conn);

            cmd.Parameters.AddWithValue("$UserId", userId);
            cmd.Parameters.AddWithValue("$Mode", mode);
            cmd.Parameters.AddWithValue("$DurationSeconds", durationSeconds);
            cmd.Parameters.AddWithValue("$CompletedAt", DateTime.UtcNow.ToString("o"));

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<SessionStats> GetStatsForUserAsync(int userId)
        {
            var stats = new SessionStats();

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            // 1) Basic aggregates
            const string aggSql =
                @"
                        SELECT 
                            COUNT(*) AS TotalSessions,
                            IFNULL(SUM(CASE WHEN Mode = 'focus' THEN DurationSeconds ELSE 0 END), 0) AS FocusSeconds,
                            COUNT(DISTINCT date(CompletedAt)) AS ActiveDays,
                            IFNULL(SUM(DurationSeconds), 0) AS TotalSeconds
                        FROM Sessions
                        WHERE UserId = $UserId;
                    ";

            using (var aggCmd = new SqliteCommand(aggSql, conn))
            {
                aggCmd.Parameters.AddWithValue("$UserId", userId);
                using var reader = await aggCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    stats.TotalSessions = reader.GetInt32(0);
                    var focusSeconds = reader.GetInt64(1);
                    stats.ActiveDays = reader.GetInt32(2);
                    var totalSeconds = reader.GetInt64(3);

                    stats.TotalFocusHours = focusSeconds / 3600.0;
                    stats.AverageSessionMinutes =
                        stats.TotalSessions > 0 ? (totalSeconds / 60.0) / stats.TotalSessions : 0.0;
                }
            }

            // 2) Load recent sessions (last 35 days) for streak + charts + achievements
            var since = DateTime.UtcNow.Date.AddDays(-35);
            const string recentSql =
                @"
                        SELECT Mode, DurationSeconds, CompletedAt
                        FROM Sessions
                        WHERE UserId = $UserId
                          AND CompletedAt >= $Since
                        ORDER BY CompletedAt ASC;
                    ";

            var recentSessions =
                new List<(string Mode, int DurationSeconds, DateTime CompletedAt)>();

            using (var recCmd = new SqliteCommand(recentSql, conn))
            {
                recCmd.Parameters.AddWithValue("$UserId", userId);
                recCmd.Parameters.AddWithValue("$Since", since.ToString("o"));

                using var reader = await recCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var mode = reader.GetString(0);
                    var duration = reader.GetInt32(1);
                    var completedAt = DateTime.Parse(reader.GetString(2));
                    recentSessions.Add((mode, duration, completedAt));
                }
            }

            // 3) Day streak calculation (based on any sessions)
            stats.DayStreak = CalculateDayStreak(recentSessions);

            // 4) Chart data (focus sessions only)
            var focusSessions = recentSessions
                .Where(s => string.Equals(s.Mode, "focus", StringComparison.OrdinalIgnoreCase))
                .ToList();

            FillChartData(stats, focusSessions);

            stats.PlantsUnlocked = stats.TotalFocusHours > 0 ? 1 : 0;

            // 5) Build achievements from the sessions we already loaded
            stats.Achievements = BuildAchievements(stats, recentSessions);

            return stats;
        }

        private List<Achievement> BuildAchievements(
            SessionStats stats,
            List<(string Mode, int DurationSeconds, DateTime CompletedAt)> sessions
        )
        {
            var achievements = new List<Achievement>();

            if (sessions.Count == 0)
                return achievements;

            // Streak-based achievements (Fire/Flame SVG)
            if (stats.DayStreak >= 1)
            {
                var today = DateTime.UtcNow.Date;
                var yesterday = today.AddDays(-1);
                var latestSessionDate = sessions.Max(s => s.CompletedAt).Date;
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
                var targetSeconds = milestoneHit * 3600;
                var runningTotal = 0;
                DateTime? milestoneDate = null;

                foreach (var session in sessions.OrderBy(s => s.CompletedAt))
                {
                    runningTotal += session.DurationSeconds;
                    if (runningTotal >= targetSeconds)
                    {
                        milestoneDate = session.CompletedAt;
                        break;
                    }
                }

                var hasSessionToday = sessions.Any(s => s.CompletedAt.Date == DateTime.UtcNow.Date);

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

            // Plant-based achievements
            if (stats.PlantsUnlocked >= 5)
            {
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
                var firstSessionDate = sessions.Min(s => s.CompletedAt);
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

        // ...existing code...
        private int CalculateDayStreak(
            List<(string Mode, int DurationSeconds, DateTime CompletedAt)> sessions
        )
        {
            if (!sessions.Any())
                return 0;

            var daysWithSessions = sessions
                .Select(s => s.CompletedAt.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            // If no session today or yesterday, streak is broken
            if (!daysWithSessions.Contains(today) && !daysWithSessions.Contains(yesterday))
                return 0;

            int streak = 0;
            var current = today;

            // Start counting from today or yesterday (whichever has a session)
            if (!daysWithSessions.Contains(today))
            {
                current = yesterday;
            }

            while (daysWithSessions.Contains(current))
            {
                streak++;
                current = current.AddDays(-1);
            }

            return streak;
        }

        private void FillChartData(
            SessionStats stats,
            List<(string Mode, int DurationSeconds, DateTime CompletedAt)> focusSessions
        )
        {
            var today = DateTime.UtcNow.Date;

            // Week ranges
            var thisWeekStart = StartOfWeek(today);
            var lastWeekStart = thisWeekStart.AddDays(-7);
            var lastWeekEnd = thisWeekStart.AddDays(-1);

            var monthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            // THIS WEEK (Mon–Sun)
            stats.ThisWeek = AggregateByDay(focusSessions, thisWeekStart, thisWeekStart.AddDays(6));

            // LAST WEEK
            stats.LastWeek = AggregateByDay(focusSessions, lastWeekStart, lastWeekEnd);

            // THIS MONTH (by week-of-month)
            stats.ThisMonth = AggregateByWeekOfMonth(
                focusSessions,
                monthStart,
                nextMonthStart.AddDays(-1)
            );
        }

        private static DateTime StartOfWeek(DateTime date)
        {
            // Monday as first day of week
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }

        private List<ChartPoint> AggregateByDay(
            List<(string Mode, int DurationSeconds, DateTime CompletedAt)> sessions,
            DateTime start,
            DateTime end
        )
        {
            var result = new List<ChartPoint>();
            var dayNames = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

            for (var d = start; d <= end; d = d.AddDays(1))
            {
                var totalSeconds = sessions
                    .Where(s => s.CompletedAt.Date == d.Date)
                    .Sum(s => s.DurationSeconds);

                var hours = totalSeconds / 3600.0;

                int dayIndex = (((int)d.DayOfWeek + 6) % 7); // Monday -> 0, ..., Sunday -> 6
                var label = dayNames[dayIndex];

                result.Add(new ChartPoint { Label = label, Hours = hours });
            }

            return result;
        }

        private List<ChartPoint> AggregateByWeekOfMonth(
            List<(string Mode, int DurationSeconds, DateTime CompletedAt)> sessions,
            DateTime monthStart,
            DateTime monthEnd
        )
        {
            // Week index: (day - 1) / 7  -> week 0..4
            var buckets = new double[6];

            foreach (var s in sessions)
            {
                if (s.CompletedAt.Date < monthStart || s.CompletedAt.Date > monthEnd)
                    continue;

                var dayInMonth = s.CompletedAt.Day;
                var weekIndex = (dayInMonth - 1) / 7;
                if (weekIndex < 0)
                    weekIndex = 0;
                if (weekIndex > 5)
                    weekIndex = 5;

                buckets[weekIndex] += s.DurationSeconds;
            }

            var result = new List<ChartPoint>();
            for (int i = 0; i < buckets.Length; i++)
            {
                var hours = buckets[i] / 3600.0;
                if (hours <= 0 && i > 3)
                    break; // stop at first empty after 4 weeks (most months)

                result.Add(new ChartPoint { Label = $"Week {i + 1}", Hours = hours });
            }

            return result;
        }

        public async Task<List<(DateTime SessionDate, int DurationSeconds)>> GetUserSessionsAsync(
            int userId
        )
        {
            var sessions = new List<(DateTime SessionDate, int DurationSeconds)>();

            const string sql =
                @"
                SELECT CompletedAt, DurationSeconds
                FROM Sessions
                WHERE UserId = $UserId
                ORDER BY CompletedAt ASC;
            ";

            using var conn = new SqliteConnection(_connectionString);
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("$UserId", userId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var completedAt = DateTime.Parse(reader.GetString(0));
                var durationSeconds = reader.GetInt32(1);
                sessions.Add((completedAt, durationSeconds));
            }

            return sessions;
        }

        public async Task<List<LeaderboardUser>> GetTopUsersAsync(int limit = 10)
        {
            var leaderboard = new List<LeaderboardUser>();

            const string sql =
                @"
                SELECT 
                    u.UserId,
                    u.Name,
                    IFNULL(SUM(s.DurationSeconds), 0) / 3600.0 AS TotalHours
                FROM Users u
                LEFT JOIN Sessions s ON u.UserId = s.UserId AND s.Mode = 'focus'
                GROUP BY u.UserId, u.Name
                ORDER BY TotalHours DESC, u.Name ASC
                LIMIT $Limit;
            ";

            using var conn = new SqliteConnection(_connectionString);
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("$Limit", limit);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            int rank = 1;
            while (await reader.ReadAsync())
            {
                leaderboard.Add(
                    new LeaderboardUser
                    {
                        UserId = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        TotalHours = reader.GetDouble(2),
                        Rank = rank++,
                    }
                );
            }

            return leaderboard;
        }
    }
}
