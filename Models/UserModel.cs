using System;

namespace PomodoroPlant.Models
{
    public class UserModel
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string HashedPassword { get; set; }
        public int ArduinoId { get; set; } 
        public DateTime CreatedAt { get; set; }
        public string Role { get; set; }
    }
}