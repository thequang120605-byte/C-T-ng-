using System.Collections.Generic;
using ChessCore.Enums;
using ChessCore.Models;

namespace ChessCore.Engine
{
    /// <summary>
    /// Bộ giải "chiếu bí cưỡng bức trong N nước" bằng vét cạn.
    /// Chỉ phù hợp với thế cờ ít quân (Cờ thế), N &lt;= 2~3.
    /// Nước cuối cùng của bên tấn công bắt buộc phải là nước chiếu.
    /// </summary>
    public static class MateSolver
    {
        public const int MaxSupportedDepth = 3;

        /// <summary>
        /// board.CurrentTurn phải là bên tấn công. Trả về số nước chiếu bí
        /// ngắn nhất (&lt;= maxN) hoặc -1 nếu không có.
        /// </summary>
        public static int MateDistance(
            Board board,
            PieceColor attacker,
            int maxN)
        {
            for (int n = 1; n <= maxN; n++)
            {
                if (FindMateMove(board, attacker, n) != null)
                    return n;
            }

            return -1;
        }

        /// <summary>
        /// Tìm một nước đi của bên tấn công sao cho chiếu bí cưỡng bức
        /// trong đúng tối đa n nước (tính cả nước này).
        /// </summary>
        public static EngineMove? FindMateMove(
            Board board,
            PieceColor attacker,
            int n)
        {
            PieceColor defender = GameRules.Opponent(attacker);

            foreach (EngineMove move in GameRules.GetAllLegalMoves(board, attacker))
            {
                Board next = board.Clone();
                next.ApplyMove(move.From, move.To);

                if (n == 1)
                {
                    // Nước chiếu bí cuối: phải là nước chiếu rồi hết nước đỡ.
                    if (MoveGenerator.CheckIfInCheck(next, defender) &&
                        !GameRules.HasLegalMove(next, defender))
                    {
                        return move;
                    }

                    continue;
                }

                if (IsForcedMate(next, attacker, n - 1))
                    return move;
            }

            return null;
        }

        /// <summary>
        /// Sau khi bên tấn công vừa đi (board.CurrentTurn = bên phòng thủ),
        /// bên tấn công còn tối đa <paramref name="remaining"/> nước nữa
        /// để chiếu bí. Trả về true nếu MỌI nước đỡ đều bị chiếu bí kịp.
        /// </summary>
        public static bool IsForcedMate(
            Board board,
            PieceColor attacker,
            int remaining)
        {
            PieceColor defender = GameRules.Opponent(attacker);

            List<EngineMove> replies =
                GameRules.GetAllLegalMoves(board, defender);

            // Bên phòng thủ hết nước đi => đã bị chiếu bí.
            if (replies.Count == 0)
                return true;

            if (remaining <= 0)
                return false;

            foreach (EngineMove reply in replies)
            {
                Board afterReply = board.Clone();
                afterReply.ApplyMove(reply.From, reply.To);

                if (FindMateMove(afterReply, attacker, remaining) == null)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Chọn nước đỡ "cứng đầu" nhất của bên phòng thủ (kéo dài ván nhất).
        /// </summary>
        public static EngineMove? PickBestDefense(
            Board board,
            PieceColor attacker,
            int remaining)
        {
            PieceColor defender = GameRules.Opponent(attacker);

            EngineMove? best = null;
            int bestDistance = -2;

            foreach (EngineMove reply in GameRules.GetAllLegalMoves(board, defender))
            {
                Board afterReply = board.Clone();
                afterReply.ApplyMove(reply.From, reply.To);

                int distance =
                    MateDistance(afterReply, attacker, remaining);

                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best = reply;
                }
            }

            return best;
        }
    }
}
