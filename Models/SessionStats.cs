using System.Collections.Generic;

namespace PomodoroPlant.Models
{
    public class ChartPoint
    {
        public string Label { get; set; } = "";
        public double Hours { get; set; }
    }

    public class Achievement
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string WhenText { get; set; } = "";
    }

    public class SessionStats
    {
        // Top stats
        public int TotalSessions { get; set; }
        public double TotalFocusHours { get; set; }
        public int ActiveDays { get; set; }
        public int DayStreak { get; set; }
        public double AverageSessionMinutes { get; set; }

        // Plants
        public int PlantsUnlocked { get; set; }

        // Chart data
        public List<ChartPoint> ThisWeek { get; set; } = new();
        public List<ChartPoint> LastWeek { get; set; } = new();
        public List<ChartPoint> ThisMonth { get; set; } = new();

        // Achievements
        public List<Achievement> Achievements { get; set; } = new();
    }
}
