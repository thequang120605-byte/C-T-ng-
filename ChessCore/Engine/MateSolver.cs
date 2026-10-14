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

        /// <summary>
        /// Tìm nước phản đòn hiệu quả nhất của bên phòng thủ khi người chơi đi sai nước sát cục.
        /// </summary>
        public static EngineMove? PickBestCounterAttack(
            Board board,
            PieceColor defender,
            Position? playerTargetPos = null)
        {
            var moves = GameRules.GetAllLegalMoves(board, defender);
            if (moves.Count == 0) return null;

            EngineMove? best = null;
            int bestScore = int.MinValue;

            foreach (var m in moves)
            {
                int score = 0;
                Board next = board.Clone();
                next.ApplyMove(m.From, m.To);

                Piece? target = board.GetPieceAt(m.To.Row, m.To.Col);
                if (target != null)
                {
                    // Ưu tiên ăn quân
                    int targetVal = target.Type switch
                    {
                        PieceType.Rook => 900,
                        PieceType.Cannon => 450,
                        PieceType.Horse => 400,
                        PieceType.Elephant => 200,
                        PieceType.Advisor => 200,
                        PieceType.Pawn => 100,
                        _ => 50
                    };
                    score += targetVal * 10;

                    // Đặc biệt ưu tiên ăn đúng quân người chơi vừa di chuyển hớ hênh
                    if (playerTargetPos.HasValue && m.To.Row == playerTargetPos.Value.Row && m.To.Col == playerTargetPos.Value.Col)
                    {
                        score += 3000;
                    }
                }

                // Kiểm tra nếu nước đi này phản chiếu lại Tướng của bên tấn công
                PieceColor attacker = GameRules.Opponent(defender);
                if (MoveGenerator.CheckIfInCheck(next, attacker))
                {
                    score += 2000;
                }

                // Điểm đánh giá bàn cờ sau nước đi theo góc nhìn của bên phòng thủ
                score += BoardEvaluator.Evaluate(next, defender);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = m;
                }
            }

            return best ?? moves[0];
        }
    }
}
