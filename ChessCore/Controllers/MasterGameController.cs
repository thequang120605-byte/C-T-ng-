using System;
using System.Collections.Generic;
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
            try
            {
                await EnsureMasterGamesPopulatedAsync();

                var games = await _context.MasterGames
                    .OrderByDescending(g => g.EventDate ?? g.CreatedAt)
                    .ToListAsync();

                return View(games);
            }
            catch (Exception)
            {
                // Fallback nếu database vừa khởi động hoặc đang cập nhật bảng
                return View(new List<MasterGame>());
            }
        }

        // GET: /MasterGame/Detail/{id}
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                await EnsureMasterGamesPopulatedAsync();

                var game = await _context.MasterGames.FindAsync(id);
                if (game == null)
                {
                    return NotFound();
                }

                return View(game);
            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        // ==========================================
        // REST API CHO MOBILE APP & WEB INTEGRATION
        // ==========================================
        [HttpGet("/api/master-games")]
        public async Task<IActionResult> GetMasterGamesApi()
        {
            try
            {
                await EnsureMasterGamesPopulatedAsync();

                var games = await _context.MasterGames
                    .OrderByDescending(g => g.EventDate ?? g.CreatedAt)
                    .Select(g => new
                    {
                        id = g.Id,
                        title = g.Title,
                        redPlayer = g.RedPlayer,
                        blackPlayer = g.BlackPlayer,
                        eventName = g.Event,
                        eventDate = g.EventDate.HasValue ? g.EventDate.Value.ToString("dd/MM/yyyy") : "",
                        result = g.Result,
                        openingName = g.OpeningName,
                        initialFen = g.InitialFen,
                        pgnContent = g.PgnContent,
                        totalMoves = 10
                    })
                    .ToListAsync();

                return Ok(new { total = games.Count, games });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi nạp danh sách kỳ phổ danh thủ: " + ex.Message });
            }
        }

        [HttpGet("/api/master-games/{id}")]
        public async Task<IActionResult> GetMasterGameDetailApi(int id)
        {
            try
            {
                await EnsureMasterGamesPopulatedAsync();

                var g = await _context.MasterGames.FindAsync(id);
                if (g == null) return NotFound(new { message = "Không tìm thấy ván cờ danh thủ." });

                return Ok(new
                {
                    id = g.Id,
                    title = g.Title,
                    redPlayer = g.RedPlayer,
                    blackPlayer = g.BlackPlayer,
                    eventName = g.Event,
                    eventDate = g.EventDate.HasValue ? g.EventDate.Value.ToString("dd/MM/yyyy") : "",
                    result = g.Result,
                    openingName = g.OpeningName,
                    initialFen = g.InitialFen,
                    pgnContent = g.PgnContent
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi nạp chi tiết kỳ phổ: " + ex.Message });
            }
        }

        private async Task EnsureMasterGamesPopulatedAsync()
        {
            await DatabaseBootstrap.EnsureMasterGamesAsync(_context);
        }
    }
}
