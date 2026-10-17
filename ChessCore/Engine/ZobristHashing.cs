using System;
using ChessCore.Enums;
using ChessCore.Models;

namespace ChessCore.Engine
{
    /// <summary>
    /// Zobrist Hashing cho Cờ Tướng:
    /// Sinh số ngẫu nhiên 64-bit cho từng vị trí bàn cờ (10x9), từng loại quân (7 loại) và từng màu (2 màu).
    /// Giúp tính mã băm cực nhanh chỉ bằng phép XOR.
    /// </summary>
    public static class ZobristHashing
    {
        // 10 hàng x 9 cột = 90 ô
        // 7 loại quân (King..Pawn) x 2 màu (Red/Black) = 14 trạng thái quân
        // 1 chỉ số cho lượt đi (Red hoặc Black)
        private static readonly ulong[,,] PieceKeys = new ulong[10, 9, 14];
        private static readonly ulong BlackTurnKey;

        static ZobristHashing()
        {
            // Dùng seed cố định để mọi phiên tính toán đều đồng nhất mã băm
            var random = new Random(20261014);

            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    for (int p = 0; p < 14; p++)
                    {
                        PieceKeys[r, c, p] = NextUInt64(random);
                    }
                }
            }

            BlackTurnKey = NextUInt64(random);
        }

        private static ulong NextUInt64(Random rnd)
        {
            byte[] buffer = new byte[8];
            rnd.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }

        private static int GetPieceIndex(PieceType type, PieceColor color)
        {
            int baseIdx = type switch
            {
                PieceType.King => 0,
                PieceType.Advisor => 1,
                PieceType.Elephant => 2,
                PieceType.Horse => 3,
                PieceType.Rook => 4,
                PieceType.Cannon => 5,
                PieceType.Pawn => 6,
                _ => 0
            };

            return color == PieceColor.Red ? baseIdx : baseIdx + 7;
        }

        /// <summary>
        /// Tính toán Zobrist Hash 64-bit cho toàn bộ bàn cờ
        /// </summary>
        public static ulong ComputeHash(Board board)
        {
            ulong hash = 0;

            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    var piece = board.GetPieceAt(r, c);
                    if (piece != null)
                    {
                        int pIdx = GetPieceIndex(piece.Type, piece.Color);
                        hash ^= PieceKeys[r, c, pIdx];
                    }
                }
            }

            if (board.CurrentTurn == PieceColor.Black)
            {
                hash ^= BlackTurnKey;
            }

            return hash;
        }
    }
}
