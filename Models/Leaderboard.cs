namespace PomodoroPlant.Models
{
    public class LeaderboardUser
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public double TotalHours { get; set; }
        public int Rank { get; set; }
    }
}
