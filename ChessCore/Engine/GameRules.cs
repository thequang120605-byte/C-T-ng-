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
    }
}
