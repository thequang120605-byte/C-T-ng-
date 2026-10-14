using System;
using System.Collections.Generic;
using ChessCore.Models;
using ChessCore.Enums;

namespace ChessCore.ViewModels
{
    public class DashboardViewModel
    {
        public User CurrentUser { get; set; }
        public int PuzzlesSolved { get; set; }
        public double WinRate { get; set; }
        public int RankIndex { get; set; }
        public List<GameHistoryItem> RecentGames { get; set; } = new List<GameHistoryItem>();
        public List<LeaderboardItem> Leaderboard { get; set; } = new List<LeaderboardItem>();
        public List<PuzzleLeaderboardItem> PuzzleLeaderboard { get; set; } = new List<PuzzleLeaderboardItem>();
    }

    public class PuzzleLeaderboardItem
    {
        public int Rank { get; set; }
        public string Username { get; set; }
        public int SolvedCount { get; set; }
        public bool IsCurrentUser { get; set; }
    }

    public class GameHistoryItem
    {
        public int GameId { get; set; }
        public string OpponentName { get; set; }
        public int? OpponentElo { get; set; }
        public int PlayerElo { get; set; }
        public string GameMode { get; set; } = "PvE"; // "PvE" hoặc "PvP"
        public DateTime PlayedAt { get; set; }
        public int TotalMoves { get; set; }
        public string Result { get; set; } // "win", "loss", "draw"
        public string ResultText { get; set; } // "Thắng", "Thua", "Hòa"
    }

    public class LeaderboardItem
    {
        public int Rank { get; set; }
        public string Username { get; set; }
        public int Elo { get; set; }
        public bool IsCurrentUser { get; set; }
    }
}
