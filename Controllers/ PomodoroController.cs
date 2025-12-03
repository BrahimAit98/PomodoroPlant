using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PomodoroPlant.Controllers
{
    public class PomodoroController : Controller
    {
        private readonly string EspBaseUrl;
        private readonly IHttpClientFactory _httpClientFactory;

        public PomodoroController(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory
        )
        {
            EspBaseUrl = configuration["EspSettings:BaseUrl"] ?? "http://10.110.206.211";
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMode([FromBody] ModeRequest request)
        {
            // Check if user is authenticated
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return Unauthorized("User not authenticated");
            }

            // Validate mode
            var validModes = new[] { "focus", "short", "long" };
            if (
                string.IsNullOrWhiteSpace(request.Mode)
                || !validModes.Contains(request.Mode.ToLower())
            )
                return BadRequest("Invalid mode");

            // Validate seconds (reasonable limits)
            if (request.Seconds < 0 || request.Seconds > 3600)
                return BadRequest("Invalid duration");

            var http = _httpClientFactory.CreateClient();
            var encodedMode = WebUtility.UrlEncode(request.Mode);
            var espUrl = $"{EspBaseUrl}/mode?name={encodedMode}&seconds={request.Seconds}";

            try
            {
                var response = await http.GetAsync(espUrl);
                var content = await response.Content.ReadAsStringAsync();
                return Ok(content);
            }
            catch (Exception ex)
            {
                // Log the exception (add ILogger if needed)
                return StatusCode(500, "ESP not reachable");
            }
        }

        public class ModeRequest
        {
            public string Mode { get; set; } = string.Empty;
            public int Seconds { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> Buzz()
        {
            // Check if user is authenticated
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return Unauthorized("User not authenticated");
            }

            var http = _httpClientFactory.CreateClient();
            var espUrl = $"{EspBaseUrl}/buzz";

            try
            {
                var response = await http.GetAsync(espUrl);
                var content = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, content);
            }
            catch (Exception ex)
            {
                // Log the exception (add ILogger if needed)
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
