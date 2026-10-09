using System.Collections.Generic;
using ChessCore.Enums;
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
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Grid[r, c] = null;
                }
            }

            CurrentTurn = PieceColor.Red;

            // =====================================================
            // QUÂN ĐEN
            // =====================================================

            Grid[0, 0] = new Piece(
                PieceType.Rook,
                PieceColor.Black);

            Grid[0, 1] = new Piece(
                PieceType.Horse,
                PieceColor.Black);

            Grid[0, 2] = new Piece(
                PieceType.Elephant,
                PieceColor.Black);

            Grid[0, 3] = new Piece(
                PieceType.Advisor,
                PieceColor.Black);

            Grid[0, 4] = new Piece(
                PieceType.King,
                PieceColor.Black);

            Grid[0, 5] = new Piece(
                PieceType.Advisor,
                PieceColor.Black);

            Grid[0, 6] = new Piece(
                PieceType.Elephant,
                PieceColor.Black);

            Grid[0, 7] = new Piece(
                PieceType.Horse,
                PieceColor.Black);

            Grid[0, 8] = new Piece(
                PieceType.Rook,
                PieceColor.Black);

            Grid[2, 1] = new Piece(
                PieceType.Cannon,
                PieceColor.Black);

            Grid[2, 7] = new Piece(
                PieceType.Cannon,
                PieceColor.Black);

            for (int c = 0; c <= 8; c += 2)
            {
                Grid[3, c] = new Piece(
                    PieceType.Pawn,
                    PieceColor.Black);
            }

            // =====================================================
            // QUÂN ĐỎ
            // =====================================================

            Grid[9, 0] = new Piece(
                PieceType.Rook,
                PieceColor.Red);

            Grid[9, 1] = new Piece(
                PieceType.Horse,
                PieceColor.Red);

            Grid[9, 2] = new Piece(
                PieceType.Elephant,
                PieceColor.Red);

            Grid[9, 3] = new Piece(
                PieceType.Advisor,
                PieceColor.Red);

            Grid[9, 4] = new Piece(
                PieceType.King,
                PieceColor.Red);

            Grid[9, 5] = new Piece(
                PieceType.Advisor,
                PieceColor.Red);

            Grid[9, 6] = new Piece(
                PieceType.Elephant,
                PieceColor.Red);

            Grid[9, 7] = new Piece(
                PieceType.Horse,
                PieceColor.Red);

            Grid[9, 8] = new Piece(
                PieceType.Rook,
                PieceColor.Red);

            Grid[7, 1] = new Piece(
                PieceType.Cannon,
                PieceColor.Red);

            Grid[7, 7] = new Piece(
                PieceType.Cannon,
                PieceColor.Red);

            for (int c = 0; c <= 8; c += 2)
            {
                Grid[6, c] = new Piece(
                    PieceType.Pawn,
                    PieceColor.Red);
            }
        }

        // =====================================================
        // LẤY QUÂN CỜ
        // =====================================================

        public Piece GetPieceAt(int row, int col)
        {
            if (row < 0 ||
                row > 9 ||
                col < 0 ||
                col > 8)
            {
                return null;
            }

            return Grid[row, col];
        }

        // =====================================================
        // ĐẶT LƯỢT HIỆN TẠI
        // =====================================================

        public void SetCurrentTurn(PieceColor color)
        {
            CurrentTurn = color;
        }

        // =====================================================
        // ĐỔI LƯỢT
        // =====================================================

        public void SwitchTurn()
        {
            CurrentTurn =
                CurrentTurn == PieceColor.Red
                    ? PieceColor.Black
                    : PieceColor.Red;
        }

        // =====================================================
        // THỰC HIỆN NƯỚC ĐI
        // =====================================================

        public bool MakeMove(EngineMove move)
        {
            Piece p =
                GetPieceAt(
                    move.From.Row,
                    move.From.Col);

            if (p == null)
                return false;

            if (p.Color != CurrentTurn)
                return false;

            List<EngineMove> validMoves =
                MoveGenerator.GetValidMoves(
                    this,
                    move.From);

            bool isValid =
                validMoves.Exists(
                    m => m.To.Equals(move.To));

            if (!isValid)
                return false;

            Grid[
                move.To.Row,
                move.To.Col
            ] = p;

            Grid[
                move.From.Row,
                move.From.Col
            ] = null;

            SwitchTurn();

            return true;
        }

        // =====================================================
        // DI CHUYỂN KHÔNG KIỂM TRA (dùng cho engine / replay)
        // =====================================================

        public void ApplyMove(Position from, Position to)
        {
            Grid[to.Row, to.Col] = Grid[from.Row, from.Col];
            Grid[from.Row, from.Col] = null;
            SwitchTurn();
        }

        // =====================================================
        // NHÂN BẢN BÀN CỜ
        // =====================================================

        public Board Clone()
        {
            Board copy = new Board(true);

            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Piece? p = Grid[r, c];

                    copy.Grid[r, c] =
                        p == null ? null : new Piece(p.Type, p.Color);
                }
            }

            copy.CurrentTurn = CurrentTurn;

            return copy;
        }

        // =====================================================
        // FEN (CỜ TƯỚNG)
        //
        // Quy ước: chữ HOA = quân Đỏ, chữ thường = quân Đen
        // K=Tướng A=Sĩ B=Tượng N=Mã R=Xe C=Pháo P=Tốt
        // Hàng đầu tiên là hàng của quân Đen (row 0).
        // "w" = Đỏ đi, "b" = Đen đi.
        // =====================================================

        public const string StartFen =
            "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1";

        private Board(bool empty)
        {
            Grid = new Piece[10, 9];
            CurrentTurn = PieceColor.Red;
        }

        public string ToFen()
        {
            var sb = new System.Text.StringBuilder();

            for (int r = 0; r < 10; r++)
            {
                int empty = 0;

                for (int c = 0; c < 9; c++)
                {
                    Piece? p = Grid[r, c];

                    if (p == null)
                    {
                        empty++;
                        continue;
                    }

                    if (empty > 0)
                    {
                        sb.Append(empty);
                        empty = 0;
                    }

                    sb.Append(PieceToChar(p));
                }

                if (empty > 0)
                    sb.Append(empty);

                if (r < 9)
                    sb.Append('/');
            }

            sb.Append(CurrentTurn == PieceColor.Red ? " w" : " b");
            sb.Append(" - - 0 1");

            return sb.ToString();
        }

        private static char PieceToChar(Piece p)
        {
            char ch = p.Type switch
            {
                PieceType.King => 'k',
                PieceType.Advisor => 'a',
                PieceType.Elephant => 'b',
                PieceType.Horse => 'n',
                PieceType.Rook => 'r',
                PieceType.Cannon => 'c',
                _ => 'p'
            };

            return p.Color == PieceColor.Red ? char.ToUpperInvariant(ch) : ch;
        }

        private static bool TryCharToPiece(char ch, out Piece? piece)
        {
            piece = null;

            PieceColor color =
                char.IsUpper(ch) ? PieceColor.Red : PieceColor.Black;

            PieceType type;

            switch (char.ToLowerInvariant(ch))
            {
                case 'k': type = PieceType.King; break;
                case 'a': type = PieceType.Advisor; break;
                case 'b':
                case 'e': type = PieceType.Elephant; break;
                case 'n':
                case 'h': type = PieceType.Horse; break;
                case 'r': type = PieceType.Rook; break;
                case 'c': type = PieceType.Cannon; break;
                case 'p': type = PieceType.Pawn; break;
                default: return false;
            }

            piece = new Piece(type, color);
            return true;
        }

        /// <summary>
        /// Phân tích và kiểm tra chuỗi FEN. Trả về false kèm thông báo lỗi
        /// tiếng Việt nếu FEN sai cú pháp hoặc thế cờ không hợp lệ.
        /// </summary>
        public static bool TryParseFen(
            string? fen,
            out Board? board,
            out string error)
        {
            board = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(fen))
            {
                error = "Chuỗi FEN đang trống.";
                return false;
            }

            if (fen.Length > 200)
            {
                error = "Chuỗi FEN quá dài.";
                return false;
            }

            string[] fields =
                fen.Trim().Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);

            string[] rows = fields[0].Split('/');

            if (rows.Length != 10)
            {
                error = "FEN phải có đúng 10 hàng, ngăn cách bằng dấu '/'.";
                return false;
            }

            Board result = new Board(true);

            for (int r = 0; r < 10; r++)
            {
                int col = 0;

                foreach (char ch in rows[r])
                {
                    if (ch >= '1' && ch <= '9')
                    {
                        col += ch - '0';
                    }
                    else if (TryCharToPiece(ch, out Piece? piece))
                    {
                        if (col > 8)
                        {
                            error = $"Hàng {r + 1} có quá 9 cột.";
                            return false;
                        }

                        result.Grid[r, col] = piece!;
                        col++;
                    }
                    else
                    {
                        error = $"Ký tự '{ch}' không hợp lệ trong FEN.";
                        return false;
                    }

                    if (col > 9)
                    {
                        error = $"Hàng {r + 1} có quá 9 cột.";
                        return false;
                    }
                }

                if (col != 9)
                {
                    error = $"Hàng {r + 1} phải đủ đúng 9 cột.";
                    return false;
                }
            }

            string turn =
                fields.Length > 1
                    ? fields[1].ToLowerInvariant()
                    : "w";

            if (turn == "w" || turn == "r")
            {
                result.CurrentTurn = PieceColor.Red;
            }
            else if (turn == "b")
            {
                result.CurrentTurn = PieceColor.Black;
            }
            else
            {
                error = "Lượt đi phải là 'w' (Đỏ) hoặc 'b' (Đen).";
                return false;
            }

            if (!result.ValidatePosition(out error))
            {
                return false;
            }

            board = result;
            return true;
        }

        private bool ValidatePosition(out string error)
        {
            error = string.Empty;

            int[] redCount = new int[7];
            int[] blackCount = new int[7];

            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Piece? p = Grid[r, c];

                    if (p == null)
                        continue;

                    if (!IsLegalSquare(p, r, c))
                    {
                        error =
                            $"Quân {p.Type} ({p.Color}) không thể đứng ở hàng {r + 1}, cột {c + 1}.";
                        return false;
                    }

                    int[] counter =
                        p.Color == PieceColor.Red ? redCount : blackCount;

                    counter[(int)p.Type]++;
                }
            }

            int[] maxPerType = { 1, 2, 2, 2, 2, 2, 5 };

            for (int t = 0; t < 7; t++)
            {
                if (redCount[t] > maxPerType[t] ||
                    blackCount[t] > maxPerType[t])
                {
                    error = $"Số lượng quân {(PieceType)t} vượt quá quy định.";
                    return false;
                }
            }

            if (redCount[(int)PieceType.King] != 1 ||
                blackCount[(int)PieceType.King] != 1)
            {
                error = "Mỗi bên phải có đúng 1 Tướng.";
                return false;
            }

            PieceColor opponent =
                CurrentTurn == PieceColor.Red
                    ? PieceColor.Black
                    : PieceColor.Red;

            // Nếu bên không có lượt đi mà đang bị chiếu (hoặc hai Tướng
            // đối mặt) thì thế cờ này không thể xảy ra trong ván thật.
            if (MoveGenerator.CheckIfInCheck(this, opponent))
            {
                error =
                    "Thế cờ không hợp lệ: bên không có lượt đi đang bị chiếu " +
                    "(hoặc hai Tướng đối mặt nhau).";
                return false;
            }

            return true;
        }

        private static bool IsLegalSquare(Piece p, int r, int c)
        {
            bool red = p.Color == PieceColor.Red;

            bool inPalace =
                c >= 3 && c <= 5 &&
                (red ? r >= 7 : r <= 2);

            switch (p.Type)
            {
                case PieceType.King:
                    return inPalace;

                case PieceType.Advisor:
                    // Điểm của Sĩ Đỏ có (hàng+cột) chẵn, của Sĩ Đen thì lẻ.
                    return inPalace && ((r + c) % 2 == (red ? 0 : 1));

                case PieceType.Elephant:
                    return IsElephantPoint(r, c, red);

                case PieceType.Pawn:
                    if (red)
                    {
                        if (r > 6) return false;
                        if (r >= 5 && c % 2 != 0) return false;
                        return true;
                    }
                    else
                    {
                        if (r < 3) return false;
                        if (r <= 4 && c % 2 != 0) return false;
                        return true;
                    }

                default:
                    return true;
            }
        }

        private static bool IsElephantPoint(int r, int c, bool red)
        {
            int[,] redPoints =
            {
                { 9, 2 }, { 9, 6 }, { 7, 0 }, { 7, 4 },
                { 7, 8 }, { 5, 2 }, { 5, 6 }
            };

            for (int i = 0; i < redPoints.GetLength(0); i++)
            {
                int pr = red ? redPoints[i, 0] : 9 - redPoints[i, 0];

                if (pr == r && redPoints[i, 1] == c)
                    return true;
            }

            return false;
        }

        public static Board FromFen(string fen)
        {
            if (!TryParseFen(fen, out Board? board, out string error))
                throw new FormatException(error);

            return board!;
        }
    }
}