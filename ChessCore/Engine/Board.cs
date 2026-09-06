using System.Collections.Generic;
using ChessCore.Models;

namespace ChessCore.Engine
{
    public class Board
    {
        public Piece[,] Grid { get; private set; }
        public PieceColor CurrentTurn { get; private set; }

        public Board()
        {
            Grid = new Piece[10, 9];
            CurrentTurn = PieceColor.Red;
            InitializeBoard();
        }

        public void InitializeBoard()
        {
            // Xóa bàn cờ
            for (int r = 0; r < 10; r++)
                for (int c = 0; c < 9; c++)
                    Grid[r, c] = null;

            CurrentTurn = PieceColor.Red;

            // Thiết lập quân Đen (Hàng 0 đến 3)
            Grid[0, 0] = new Piece(PieceType.Rook, PieceColor.Black);
            Grid[0, 1] = new Piece(PieceType.Horse, PieceColor.Black);
            Grid[0, 2] = new Piece(PieceType.Elephant, PieceColor.Black);
            Grid[0, 3] = new Piece(PieceType.Advisor, PieceColor.Black);
            Grid[0, 4] = new Piece(PieceType.King, PieceColor.Black);
            Grid[0, 5] = new Piece(PieceType.Advisor, PieceColor.Black);
            Grid[0, 6] = new Piece(PieceType.Elephant, PieceColor.Black);
            Grid[0, 7] = new Piece(PieceType.Horse, PieceColor.Black);
            Grid[0, 8] = new Piece(PieceType.Rook, PieceColor.Black);

            Grid[2, 1] = new Piece(PieceType.Cannon, PieceColor.Black);
            Grid[2, 7] = new Piece(PieceType.Cannon, PieceColor.Black);

            for (int c = 0; c <= 8; c += 2)
                Grid[3, c] = new Piece(PieceType.Pawn, PieceColor.Black);

            // Thiết lập quân Đỏ (Hàng 6 đến 9)
            Grid[9, 0] = new Piece(PieceType.Rook, PieceColor.Red);
            Grid[9, 1] = new Piece(PieceType.Horse, PieceColor.Red);
            Grid[9, 2] = new Piece(PieceType.Elephant, PieceColor.Red);
            Grid[9, 3] = new Piece(PieceType.Advisor, PieceColor.Red);
            Grid[9, 4] = new Piece(PieceType.King, PieceColor.Red);
            Grid[9, 5] = new Piece(PieceType.Advisor, PieceColor.Red);
            Grid[9, 6] = new Piece(PieceType.Elephant, PieceColor.Red);
            Grid[9, 7] = new Piece(PieceType.Horse, PieceColor.Red);
            Grid[9, 8] = new Piece(PieceType.Rook, PieceColor.Red);

            Grid[7, 1] = new Piece(PieceType.Cannon, PieceColor.Red);
            Grid[7, 7] = new Piece(PieceType.Cannon, PieceColor.Red);

            for (int c = 0; c <= 8; c += 2)
                Grid[6, c] = new Piece(PieceType.Pawn, PieceColor.Red);
        }

        public Piece GetPieceAt(int row, int col)
        {
            if (row < 0 || row > 9 || col < 0 || col > 8) return null;
            return Grid[row, col];
        }

        public bool MakeMove(Move move)
        {
            Piece p = GetPieceAt(move.From.Row, move.From.Col);
            if (p == null || p.Color != CurrentTurn) return false;

            List<Move> validMoves = MoveGenerator.GetValidMoves(this, move.From);
            if (!validMoves.Exists(m => m.To.Equals(move.To))) return false;

            // Thực hiện di chuyển & bắt quân
            Grid[move.To.Row, move.To.Col] = p;
            Grid[move.From.Row, move.From.Col] = null;

            // Đổi lượt
            CurrentTurn = CurrentTurn == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
            return true;
        }
    }
}