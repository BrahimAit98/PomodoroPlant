namespace PomodoroPlant.Models;

public class User
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Timer Settings
    public int FocusDuration { get; set; } = 25;
    public int ShortBreak { get; set; } = 5;
    public int LongBreak { get; set; } = 15;
    public int SessionsUntilLongBreak { get; set; } = 4;
    public bool AutoStartBreaks { get; set; } = false;
}
