using Microsoft.AspNetCore.Mvc;

namespace ChessCore.Controllers
{
    public class GameController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}