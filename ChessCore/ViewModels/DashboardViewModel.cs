using System;
using System.Collections.Generic;
using ChessCore.Models;
using ChessCore.Enums;

namespace ChessCore.ViewModels
{
    public class DashboardViewModel
    {
        public User CurrentUser { get; set; }
        public List<ChessCore.Services.ModeStatistics> ModeStats { get; set; } = new();
        public ChessCore.Services.LearningStatistics Learning { get; set; } = new();
        public int FinishedGames => ModeStats.Where(r => r.Mode == "Online" || r.Mode == "PvE").Sum(r => r.Finished);
        public int PuzzlesSolved { get; set; }
        public double WinRate { get; set; }
        public int RankIndex { get; set; }
        public List<GameHistoryItem> RecentGames { get; set; } = new List<GameHistoryItem>();
        public List<LeaderboardItem> Leaderboard { get; set; } = new List<LeaderboardItem>();
        public List<PuzzleLeaderboardItem> PuzzleLeaderboard { get; set; } = new List<PuzzleLeaderboardItem>();
        public List<EloPointDto> EloHistory { get; set; } = new List<EloPointDto>();
        public List<UserAccountOptionDto> AllAccounts { get; set; } = new();
    }

    public class EloPointDto
    {
        public string DateLabel { get; set; } = string.Empty;
        public int Elo { get; set; }
        public int OldElo { get; set; }
        public int Change { get; set; }
        public int? GameId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class PuzzleLeaderboardItem
    {
        public int Rank { get; set; }
        public string Username { get; set; }
        public int SolvedCount { get; set; }
        public int Score { get; set; }
        public string LevelTitle { get; set; } = string.Empty;
        public bool IsCurrentUser { get; set; }
    }

    public class GameHistoryItem
    {
        public int GameId { get; set; }
        public string OpponentName { get; set; }
        public int? OpponentElo { get; set; }
        public int? PlayerElo { get; set; }
        public int? OldElo { get; set; }
        public int? NewElo { get; set; }
        public int? EloChange { get; set; }
        public bool IsRated { get; set; }
        public string GameMode { get; set; } = "PvE";
        public string SubModeBadge { get; set; } = string.Empty;
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

    public class UserAccountOptionDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;
        public int Elo { get; set; }
        public string Title { get; set; } = string.Empty;
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public double WinRate { get; set; }
        public int PuzzlesSolved { get; set; }
        public int LessonsCompleted { get; set; }
        public bool IsCurrentUser { get; set; }
        public int Rank { get; set; }
    }

    public class AdvancedStatsRequest
    {
        public List<int> UserIds { get; set; } = new();
    }

    public class AdvancedStatsResponseDto
    {
        public AdvancedKpiDto Kpis { get; set; } = new();
        public List<ChessCore.Services.ModeStatistics> ModeStats { get; set; } = new();
        public List<UserAccountOptionDto> Users { get; set; } = new();
        public ChessCore.Services.LearningStatistics Learning { get; set; } = new();
        public AdvancedDonutDto Donut { get; set; } = new();
        public Dictionary<string, int> ModeDistribution { get; set; } = new();
    }

    public class AdvancedKpiDto
    {
        public int TotalGames { get; set; }
        public double WinRate { get; set; }
        public double AvgElo { get; set; }
        public int MaxElo { get; set; }
        public int PuzzlesSolved { get; set; }
        public int SelectedCount { get; set; }
        public string DisplayEloText { get; set; } = string.Empty;
    }

    public class AdvancedDonutDto
    {
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public double WinRate { get; set; }
    }
}
