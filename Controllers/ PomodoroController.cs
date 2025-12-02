using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PomodoroPlant.Controllers
{
    public class PomodoroController : Controller
    {
        // Put the ESP32 base URL in ONE place
        private const string EspBaseUrl = "http://192.168.1.42"; // <-- change to their ESP32 IP

        [HttpPost]
        public async Task<IActionResult> Buzz()
        {
            using var http = new HttpClient();
            var espUrl = $"{EspBaseUrl}/buzz";

            try
            {
                var response = await http.GetAsync(espUrl);
                var content = await response.Content.ReadAsStringAsync();
                return Ok(content);
            }
            catch
            {
                return StatusCode(500, "ESP not reachable");
            }
        }

        // POST /Pomodoro/UpdateMode?mode=focus
        [HttpPost]
        public async Task<IActionResult> UpdateMode(string mode)
        {
            if (string.IsNullOrWhiteSpace(mode))
                return BadRequest("Mode is required");

            using var http = new HttpClient();
            var espUrl = $"{EspBaseUrl}/mode?name={mode}";

            try
            {
                var response = await http.GetAsync(espUrl);
                var content = await response.Content.ReadAsStringAsync();
                return Ok(content);
            }
            catch
            {
                return StatusCode(500, "ESP not reachable");
            }
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }
    }
}
