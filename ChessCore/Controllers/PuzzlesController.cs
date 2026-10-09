using ChessCore.Data;
using ChessCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChessCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PuzzlesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PuzzlesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Puzzle>>> GetPuzzles()
        {
            return await _context.Puzzles.OrderBy(p => p.Level).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Puzzle>> GetPuzzle(int id)
        {
            var puzzle = await _context.Puzzles.FindAsync(id);
            if (puzzle == null) return NotFound();
            return puzzle;
        }
    }
}
