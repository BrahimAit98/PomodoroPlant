using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PomodoroPlant.Controllers
{
    public class GardenController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }
    }
}