using System;

namespace jouwprojectnaam.Models
{
    public class ArduinoModel
    {
        public int ArduinoId { get; set; } 
        public string ActivationCode { get; set; }
        public bool Status { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime LastConnection { get; set; }
    }
}