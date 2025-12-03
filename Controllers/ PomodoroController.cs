using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PomodoroPlant.Controllers
{
    public class PomodoroController : Controller
    {
        private const string EspBaseUrl = "http://10.110.206.211";

        // POST /Pomodoro/UpdateMode
        [HttpPost]
        public async Task<IActionResult> UpdateMode(string mode, int seconds)
        {
            if (string.IsNullOrWhiteSpace(mode))
                return BadRequest("Mode is required");

            using var http = new HttpClient();
            var encodedMode = WebUtility.UrlEncode(mode);

            var espUrl = $"{EspBaseUrl}/mode?name={encodedMode}&seconds={seconds}";

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

        [HttpGet]
        public async Task<IActionResult> Buzz()
        {
            using var http = new HttpClient();
            var espUrl = $"{EspBaseUrl}/buzz";

            try
            {
                var response = await http.GetAsync(espUrl);
                var content = await response.Content.ReadAsStringAsync();
                // Optionally forward status code so you see 404 if it ever comes from ESP
                return StatusCode((int)response.StatusCode, content);
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
