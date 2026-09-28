using System;
using System.Collections.Generic;
using ChessCore.Enums;
using ChessCore.Models;

namespace ChessCore.Engine
{
    // =====================================================
    // CẤU TRÚC LƯU TRỮ BÓC TÁCH CHI TIẾT H1, H2, H3
    // =====================================================
    public struct SideScores
    {
        public int H1; // Điểm chất quân (Material)
        public int H2; // Điểm vị trí & độ cơ động (Position & Mobility)
        public int H3; // Điểm chiến thuật (Tactics)
        public int Total => H1 + H2 + H3;
    }

    public static class BoardEvaluator
    {
        // =====================================================
        // H1: GIÁ TRỊ CƠ BẢN CỦA QUÂN CỜ (MATERIAL)
        // =====================================================
        public static int GetPieceValue(PieceType type)
        {
            return type switch
            {
                PieceType.King => 100000,
                PieceType.Rook => 900,
                PieceType.Cannon => 450,
                PieceType.Horse => 400,
                PieceType.Elephant => 200,
                PieceType.Advisor => 200,
                PieceType.Pawn => 100,
                _ => 0
            };
        }

        // =====================================================
        // HÀM TÍNH TÁCH BIỆT H1, H2, H3 CHO MỘT PHE (RED / BLACK)
        // Dùng để so sánh độ chênh lệch nước đi vừa đánh
        // =====================================================
        public static SideScores GetSideScores(Board board, PieceColor color)
        {
            int h1 = 0;
            int h2 = 0;

            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Piece? p = board.GetPieceAt(r, c);
                    if (p == null || p.Color != color) continue;

                    h1 += GetPieceValue(p.Type);
                    h2 += EvaluatePositionAdvanced(board, p, r, c);
                }
            }

            int h3 = EvaluateTactics(board, color);

            return new SideScores
            {
                H1 = h1,
                H2 = h2,
                H3 = h3
            };
        }

        // =====================================================
        // HÀM ĐÁNH GIÁ TỔNG THỂ BÀN CỜ DÙNG CHO MINIMAX
        // Trả về chênh lệch điểm theo góc nhìn của perspective
        // =====================================================
        public static int Evaluate(Board board, PieceColor perspective)
        {
            SideScores redScores = GetSideScores(board, PieceColor.Red);
            SideScores blackScores = GetSideScores(board, PieceColor.Black);

            int netScore = redScores.Total - blackScores.Total;

            return perspective == PieceColor.Red ? netScore : -netScore;
        }

        // =====================================================
        // H2: ĐÁNH GIÁ VỊ TRÍ CHI TIẾT THEO TỪNG LOẠI QUÂN
        // =====================================================
        private static int EvaluatePositionAdvanced(Board board, Piece piece, int row, int col)
        {
            int score = 0;

            switch (piece.Type)
            {
                case PieceType.Horse:
                    score += GetCenterBonus(row, col);
                    score += GetHorseMobility(board, row, col); // Mã linh hoạt, tránh cản chân
                    break;

                case PieceType.Cannon:
                    score += GetCenterBonus(row, col);
                    score += HasCannonScreen(board, row, col, piece.Color); // Pháo có mồi
                    break;

                case PieceType.Pawn:
                    score += GetPawnBonus(piece.Color, row, col); // Tốt qua sông, tốt trung lộ
                    break;

                case PieceType.Rook:
                    score += GetCenterBonus(row, col);
                    score += GetRookBonus(board, row, col); // Thưởng Xe lộ thoáng
                    break;

                default:
                    score += GetCenterBonus(row, col) / 2;
                    break;
            }

            return score;
        }

        // Điểm thưởng vị trí trung tâm
        private static int GetCenterBonus(int row, int col)
        {
            int rowDistance = Math.Abs(4 - row);
            int colDistance = Math.Abs(4 - col);
            return Math.Max(0, 20 - (rowDistance + colDistance) * 2);
        }

        // H2.1: Độ cơ động của Mã (Mobility - tránh cản chân)
        private static int GetHorseMobility(Board board, int row, int col)
        {
            Position pos = new Position(row, col);
            int moveCount = MoveGenerator.GetPseudoLegalMoves(board, pos).Count;
            return moveCount * 3; // 2 nước = +6đ, 8 nước = +24đ
        }

        // H2.2: Pháo có mồi (ngòi nổ tấn công)
        private static int HasCannonScreen(Board board, int row, int col, PieceColor color)
        {
            int bonus = 0;
            int[] dR = { -1, 1, 0, 0 };
            int[] dC = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int r = row + dR[i];
                int c = col + dC[i];
                bool foundScreen = false;

                while (r >= 0 && r <= 9 && c >= 0 && c <= 8)
                {
                    Piece? target = board.GetPieceAt(r, c);
                    if (!foundScreen)
                    {
                        if (target != null) foundScreen = true; // Tìm thấy quân làm mồi (screen)
                    }
                    else
                    {
                        if (target != null)
                        {
                            if (target.Color != color)
                            {
                                bonus += 20; // Pháo có mồi và đang đe dọa quân đối phương
                            }
                            break;
                        }
                    }
                    r += dR[i];
                    c += dC[i];
                }
            }
            return bonus;
        }

        // H2.3: Tốt qua sông và kiểm soát trung lộ
        private static int GetPawnBonus(PieceColor color, int row, int col)
        {
            int score = 0;

            // Thưởng qua sông
            if (color == PieceColor.Red && row <= 4) score += 20;
            if (color == PieceColor.Black && row >= 5) score += 20;

            // Thưởng tiến sâu vào cung đối phương
            if (color == PieceColor.Red && row <= 2) score += 15;
            if (color == PieceColor.Black && row >= 7) score += 15;

            // Thưởng trung lộ (cột 4 là cột trung tâm)
            int centerDistance = Math.Abs(4 - col);
            score += Math.Max(0, (4 - centerDistance) * 5);

            return score;
        }

        // Thưởng điểm Xe thông thoáng
        private static int GetRookBonus(Board board, int row, int col)
        {
            int freeSquares = 0;
            int[] dR = { -1, 1, 0, 0 };
            int[] dC = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int r = row + dR[i];
                int c = col + dC[i];
                while (r >= 0 && r <= 9 && c >= 0 && c <= 8)
                {
                    if (board.GetPieceAt(r, c) != null) break;
                    freeSquares++;
                    r += dR[i];
                    c += dC[i];
                }
            }
            return freeSquares * 2;
        }

        // =====================================================
        // H3: CHIẾN THUẬT (TACTICS EVALUATION)
        // =====================================================
        private static int EvaluateTactics(Board board, PieceColor color)
        {
            int score = 0;
            PieceColor opponentColor = color == PieceColor.Red ? PieceColor.Black : PieceColor.Red;

            // H3.1 & H3.2: Bảo vệ quân & Quân bị treo
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Piece? p = board.GetPieceAt(r, c);
                    if (p == null || p.Color != color) continue;

                    bool attacked = IsSquareAttacked(board, r, c, opponentColor);
                    bool defended = IsSquareAttacked(board, r, c, color);

                    if (defended)
                    {
                        score += 10; // Có quân phe mình giữ
                    }

                    // Quân bị treo (đang bị địch nhắm mà không có quân giữ)
                    if (attacked && !defended)
                    {
                        score -= GetPieceValue(p.Type) / 4; // Bị phạt 25% giá trị quân
                    }
                }
            }

            // H3.3: Thưởng điểm Chiếu tướng
            if (MoveGenerator.CheckIfInCheck(board, opponentColor))
            {
                score += 80;
            }

            // H3.4: Thưởng thế Tam tử đồng biên (3 quân phối hợp cùng 1 cánh)
            score += EvaluateSideConcentration(board, color);

            return score;
        }

        // Kiểm tra xem ô (targetRow, targetCol) có đang bị quân bên attackerColor kiểm soát/tấn công không
        private static bool IsSquareAttacked(Board board, int targetRow, int targetCol, PieceColor attackerColor)
        {
            Position targetPos = new Position(targetRow, targetCol);

            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Piece? p = board.GetPieceAt(r, c);
                    if (p == null || p.Color != attackerColor) continue;

                    List<EngineMove> moves = MoveGenerator.GetPseudoLegalMoves(board, new Position(r, c));
                    if (moves.Exists(m => m.To.Equals(targetPos)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // H3.4: Tam tử đồng biên (Cánh trái: cột 0-2, Cánh phải: cột 6-8)
        private static int EvaluateSideConcentration(Board board, PieceColor color)
        {
            int leftFlankPieces = 0;
            int rightFlankPieces = 0;

            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Piece? p = board.GetPieceAt(r, c);
                    if (p == null || p.Color != color) continue;

                    if (p.Type == PieceType.Rook || p.Type == PieceType.Cannon || p.Type == PieceType.Horse)
                    {
                        if (c <= 2) leftFlankPieces++;
                        else if (c >= 6) rightFlankPieces++;
                    }
                }
            }

            int score = 0;
            if (leftFlankPieces >= 3) score += 25;  // Tam tử cánh trái
            if (rightFlankPieces >= 3) score += 25; // Tam tử cánh phải

            return score;
        }

        // =====================================================
        // KIỂM TRA CHIẾU BÍ (CHECKMATE)
        // =====================================================
        public static bool IsCheckmate(Board board, PieceColor color)
        {
            if (!MoveGenerator.CheckIfInCheck(board, color))
            {
                return false;
            }

            return !HasAnyLegalMoves(board, color);
        }

        // =====================================================
        // KIỂM TRA HẾT NƯỚC ĐI / HÒA CỜ (STALEMATE)
        // =====================================================
        public static bool IsStalemate(Board board, PieceColor color)
        {
            if (MoveGenerator.CheckIfInCheck(board, color))
            {
                return false;
            }

            return !HasAnyLegalMoves(board, color);
        }

        // Hàm phụ trợ: kiểm tra xem phe 'color' còn ít nhất 1 nước đi hợp lệ không
        private static bool HasAnyLegalMoves(Board board, PieceColor color)
        {
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Piece? piece = board.GetPieceAt(r, c);
                    if (piece != null && piece.Color == color)
                    {
                        var validMoves = MoveGenerator.GetValidMoves(board, new Position(r, c));
                        if (validMoves.Count > 0)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}