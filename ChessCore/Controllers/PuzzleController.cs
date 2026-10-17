using Microsoft.AspNetCore.Mvc;

namespace ChessCore.Controllers
{
    public class PuzzleController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
