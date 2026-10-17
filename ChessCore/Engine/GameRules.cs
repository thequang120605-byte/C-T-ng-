using System.Collections.Generic;
using System.Linq;
using ChessCore.Enums;
using ChessCore.Models;

namespace ChessCore.Engine
{
    /// <summary>
    /// Các luật dùng chung cho API, Hub online, Cờ thế và thanh phân tích.
    /// </summary>
    public static class GameRules
    {
        public static PieceColor Opponent(PieceColor color)
        {
            return color == PieceColor.Red
                ? PieceColor.Black
                : PieceColor.Red;
        }

        public static List<EngineMove> GetAllLegalMoves(
            Board board,
            PieceColor color)
        {
            var moves = new List<EngineMove>();

            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    Piece? piece = board.GetPieceAt(row, col);

                    if (piece == null || piece.Color != color)
                        continue;

                    moves.AddRange(
                        MoveGenerator.GetValidMoves(
                            board,
                            new Position(row, col)));
                }
            }

            return moves;
        }

        public static bool HasLegalMove(Board board, PieceColor color)
        {
            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    Piece? piece = board.GetPieceAt(row, col);

                    if (piece == null || piece.Color != color)
                        continue;

                    if (MoveGenerator
                        .GetValidMoves(board, new Position(row, col))
                        .Count > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Trạng thái ván cờ SAU KHI một nước đã được đi
        /// (board.CurrentTurn là bên sắp đi). Theo luật Cờ Tướng,
        /// bên hết nước đi hợp lệ (bị chiếu bí hoặc hết nước) là bên thua.
        /// </summary>
        public static GameStatus StatusAfterMove(Board boardAfterMove)
        {
            PieceColor sideToMove = boardAfterMove.CurrentTurn;

            if (HasLegalMove(boardAfterMove, sideToMove))
                return GameStatus.InProgress;

            return sideToMove == PieceColor.Red
                ? GameStatus.BlackWins
                : GameStatus.RedWins;
        }

        /// <summary>
        /// Dựng lại bàn cờ từ thế cờ ban đầu (FEN hoặc thế chuẩn)
        /// và danh sách nước đã đi.
        /// </summary>
        public static Board RebuildBoard(
            string? initialFen,
            IEnumerable<Move>? moves,
            PieceColor currentTurn)
        {
            Board board =
                !string.IsNullOrWhiteSpace(initialFen) &&
                Board.TryParseFen(initialFen, out Board? parsed, out _)
                    ? parsed!
                    : new Board();

            if (moves != null)
            {
                foreach (Move move in moves.OrderBy(m => m.MoveOrder))
                {
                    Piece? piece = board.GetPieceAt(move.FromX, move.FromY);

                    if (piece == null)
                        continue;

                    board.Grid[move.ToX, move.ToY] = piece;
                    board.Grid[move.FromX, move.FromY] = null;
                    board.SwitchTurn();
                }
            }

            while (board.CurrentTurn != currentTurn)
            {
                board.SwitchTurn();
            }

            return board;
        }

        /// <summary>
        /// Kiểm tra luật lặp lại thế cờ và luật cấm Chiếu dai (Perpetual Check) theo luật Cờ tướng chuẩn.
        /// - Nếu một bên chiếu tướng liên tục trong các lần lặp thế cờ (>= 3 lần): bên chiếu phạm luật xử THUA.
        /// - Nếu lặp lại thế cờ thông thường (>= 3 lần) mà không chiếu dai: xử HÒA (Tam quy chiếu lặp).
        /// </summary>
        public static (bool HasViolation, GameStatus ResultStatus, string Reason) CheckRepetitionRule(
            string? initialFen,
            IEnumerable<Move>? moves)
        {
            if (moves == null) return (false, GameStatus.InProgress, "");
            var orderedMoves = moves.OrderBy(m => m.MoveOrder).ToList();
            if (orderedMoves.Count < 6) return (false, GameStatus.InProgress, "");

            Board simBoard = !string.IsNullOrWhiteSpace(initialFen) && Board.TryParseFen(initialFen, out Board? parsed, out _)
                ? parsed!
                : new Board();

            var fenHistory = new List<(string Fen, bool IsCheck, PieceColor ColorJustMoved)>();
            fenHistory.Add((simBoard.ToFen().Split(' ')[0], false, PieceColor.Black));

            foreach (var m in orderedMoves)
            {
                var piece = simBoard.GetPieceAt(m.FromX, m.FromY);
                if (piece != null)
                {
                    simBoard.Grid[m.ToX, m.ToY] = piece;
                    simBoard.Grid[m.FromX, m.FromY] = null;
                    simBoard.SwitchTurn();
                    bool check = MoveGenerator.CheckIfInCheck(simBoard, simBoard.CurrentTurn);
                    fenHistory.Add((simBoard.ToFen().Split(' ')[0], check, m.PieceColor));
                }
            }

            if (fenHistory.Count == 0) return (false, GameStatus.InProgress, "");

            string currentFenKey = fenHistory[^1].Fen;
            var matchIndices = new List<int>();
            for (int i = 0; i < fenHistory.Count; i++)
            {
                if (fenHistory[i].Fen == currentFenKey)
                {
                    matchIndices.Add(i);
                }
            }

            // Nếu lặp lại từ 3 lần trở lên
            if (matchIndices.Count >= 3)
            {
                // Kiểm tra xem bên vừa đi có phải đang chiếu tướng liên tục để ép lặp không
                var lastMover = orderedMoves[^1].PieceColor;
                int consecutiveChecks = 0;
                for (int i = orderedMoves.Count - 1; i >= Math.Max(0, orderedMoves.Count - 6); i--)
                {
                    if (orderedMoves[i].PieceColor == lastMover)
                    {
                        if (fenHistory[i + 1].IsCheck)
                        {
                            consecutiveChecks++;
                        }
                    }
                }

                if (consecutiveChecks >= 3)
                {
                    // Phạm luật Chiếu dai: Bên chiếu bị xử thua
                    GameStatus loserStatus = lastMover == PieceColor.Red ? GameStatus.BlackWins : GameStatus.RedWins;
                    string moverName = lastMover == PieceColor.Red ? "Đỏ" : "Đen";
                    return (true, loserStatus, $"Phạm luật Cờ Tướng: Bên {moverName} chiếu dai liên tục 3 lần không đổi nước nên bị xử THUA.");
                }

                // Lặp bình thường 3 lần: xử Hòa
                return (true, GameStatus.Draw, "Thế cờ lặp lại 3 lần (Tam quy chiếu lặp). Trận đấu kết thúc với kết quả HÒA.");
            }

            return (false, GameStatus.InProgress, "");
        }
    }
}

