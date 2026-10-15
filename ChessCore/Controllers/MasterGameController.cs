using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChessCore.Data;
using ChessCore.Models;

namespace ChessCore.Controllers
{
    public class MasterGameController : Controller
    {
        private readonly AppDbContext _context;

        public MasterGameController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /MasterGame
        public async Task<IActionResult> Index()
        {
            var games = await _context.MasterGames
                .OrderByDescending(g => g.EventDate ?? g.CreatedAt)
                .ToListAsync();

            return View(games);
        }

        // GET: /MasterGame/Detail/{id}
        public async Task<IActionResult> Detail(int id)
        {
            var game = await _context.MasterGames.FindAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }
    }
}
