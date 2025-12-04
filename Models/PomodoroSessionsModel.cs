using System;

namespace PomodoroPlant.Models
{
    public class PomodoroSessionModel
    {
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public DateTime StartTime { get; set; }
        public int Duration { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
        public int PlantGrowth { get; set; }
        public bool NotificationArduino { get; set; }
    }
}
