using Microsoft.AspNetCore.Mvc;

namespace ChessCore.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
