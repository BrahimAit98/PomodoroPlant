using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public class PomodoroController : Controller
{
    [HttpPost]
    public async Task<IActionResult> Buzz()
    {
        using var http = new HttpClient();
        var espUrl = "http://<ESP_IP_ADDRESS>/buzz";
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

    public IActionResult Timer()
    {
        return View();
    }
}
