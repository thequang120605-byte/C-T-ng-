using System;
using System.Collections.Generic;
using Microsoft.Maui.Graphics;
using ChessCore.Mobile.Models;

namespace ChessCore.Mobile.Controls
{
    public class ChessBoardDrawable : IDrawable
    {
        public float MoveProgress { get; set; } = 1;

        // Static cached colors to eliminate memory allocation in hot 60fps render loop
        private static readonly Color WoodBgColor = Color.FromArgb("#e5c189");
        private static readonly Color WoodBorderColor = Color.FromArgb("#78350f");
        private static readonly Color GridLineColor = Color.FromArgb("#451a03");
        private static readonly Color RiverTextColor = Color.FromArgb("#78350f");
        private static readonly Color LastMoveFromCol = Color.FromArgb("#6638bdf8");
        private static readonly Color LastMoveToCol = Color.FromArgb("#8838bdf8");
        private static readonly Color SelectedBorder = Color.FromArgb("#f59e0b");
        private static readonly Color CaptureRingCol = Color.FromArgb("#ef4444");
        private static readonly Color ValidDotCol = Color.FromArgb("#8810b981");
        private static readonly Color PieceShadowCol = Color.FromArgb("#26000000");
        private static readonly Color PieceFillCol = Color.FromArgb("#FFF5D8");
        private static readonly Color PieceBevelCol = Color.FromArgb("#D5C198");
        private static readonly Color RedInk = Color.FromArgb("#A43C2E");
        private static readonly Color BlackInk = Color.FromArgb("#2D332E");

        public List<PieceDto>? Pieces { get; set; }
        public int? SelectedRow { get; set; }
        public int? SelectedCol { get; set; }
        public List<ValidMoveDto>? ValidMoves { get; set; }
        public (int Row, int Col)? LastMoveFrom { get; set; }
        public (int Row, int Col)? LastMoveTo { get; set; }
        public bool IsFlipped { get; set; }

        public RectF LastDrawnBoardRect { get; private set; }
        public float CellWidth { get; private set; }
        public float CellHeight { get; private set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0) return;

            // Bàn cờ tướng có 8 khoảng cách ngang x 9 khoảng cách dọc
            // Dự trù lề viền gỗ đủ để quân cờ ở biên không bị tràn ra ngoài
            const float marginUnits = 0.58f;
            float totalHorizUnits = 8f + (2f * marginUnits);
            float totalVertUnits = 9f + (2f * marginUnits);

            float cellSize = Math.Min(dirtyRect.Width / totalHorizUnits, dirtyRect.Height / totalVertUnits);
            CellWidth = cellSize;
            CellHeight = cellSize;

            float boardWidth = CellWidth * 8f;
            float boardHeight = CellHeight * 9f;
            float boardMargin = cellSize * marginUnits;

            float startX = (dirtyRect.Width - boardWidth) / 2f;
            float startY = (dirtyRect.Height - boardHeight) / 2f;

            LastDrawnBoardRect = new RectF(startX, startY, boardWidth, boardHeight);

            // 1. Vẽ nền gỗ bàn cờ (bao trọn vẹn toàn bộ quân cờ ở biên)
            float woodX = startX - boardMargin;
            float woodY = startY - boardMargin;
            float woodWidth = boardWidth + (2f * boardMargin);
            float woodHeight = boardHeight + (2f * boardMargin);

            canvas.FillColor = WoodBgColor;
            canvas.FillRoundedRectangle(woodX, woodY, woodWidth, woodHeight, 10);

            canvas.StrokeColor = WoodBorderColor;
            canvas.StrokeSize = 3;
            canvas.DrawRoundedRectangle(woodX, woodY, woodWidth, woodHeight, 10);

            // 2. Vẽ đường kẻ ô bàn cờ
            canvas.StrokeColor = GridLineColor;
            canvas.StrokeSize = 1.8f;

            // Đường ngang (10 hàng, r = 0..9)
            for (int r = 0; r < 10; r++)
            {
                float y = startY + (r * CellHeight);
                canvas.DrawLine(startX, y, startX + boardWidth, y);
            }

            // Đường dọc (9 cột, c = 0..8)
            // Hai đường biên ngoài cùng (c = 0 và c = 8) nối liền từ hàng 0 đến 9
            canvas.DrawLine(startX, startY, startX, startY + boardHeight);
            canvas.DrawLine(startX + boardWidth, startY, startX + boardWidth, startY + boardHeight);

            // 7 đường dọc ở giữa bị ngắt bởi Sông (hàng 4 đến 5)
            for (int c = 1; c < 8; c++)
            {
                float x = startX + (c * CellWidth);
                // Nửa trên: hàng 0 đến 4
                canvas.DrawLine(x, startY, x, startY + (4 * CellHeight));
                // Nửa dưới: hàng 5 đến 9
                canvas.DrawLine(x, startY + (5 * CellHeight), x, startY + boardHeight);
            }

            // 3. Vẽ Cửu Cung (đường chéo cung tướng)
            // Cung trên: hàng 0-2, cột 3-5
            canvas.DrawLine(startX + (3 * CellWidth), startY, startX + (5 * CellWidth), startY + (2 * CellHeight));
            canvas.DrawLine(startX + (5 * CellWidth), startY, startX + (3 * CellWidth), startY + (2 * CellHeight));

            // Cung dưới: hàng 7-9, cột 3-5
            canvas.DrawLine(startX + (3 * CellWidth), startY + (7 * CellHeight), startX + (5 * CellWidth), startY + (9 * CellHeight));
            canvas.DrawLine(startX + (5 * CellWidth), startY + (7 * CellHeight), startX + (3 * CellWidth), startY + (9 * CellHeight));

            // 4. Vẽ các điểm thập tự định vị Pháo và Tốt (Crosshairs)
            DrawAllCrosshairs(canvas, startX, startY);

            // 5. Vẽ chữ "SỞ HÀ" - "HÁN GIỚI" ở giữa sông (thích ứng góc nhìn IsFlipped)
            canvas.FontColor = RiverTextColor;
            float riverFontSize = Math.Max(12, CellHeight * 0.36f);
            canvas.FontSize = riverFontSize;
            float riverY = startY + (4.5f * CellHeight) + (riverFontSize * 0.35f);

            string leftText = IsFlipped ? "HÁN  GIỚI" : "SỞ  HÀ";
            string rightText = IsFlipped ? "SỞ  HÀ" : "HÁN  GIỚI";
            canvas.DrawString(leftText, startX + (2 * CellWidth), riverY, HorizontalAlignment.Center);
            canvas.DrawString(rightText, startX + (6 * CellWidth), riverY, HorizontalAlignment.Center);

            // 6. Vẽ Highlight nước vừa đi gần nhất
            if (LastMoveFrom.HasValue)
            {
                var pt = GetScreenCoord(LastMoveFrom.Value.Row, LastMoveFrom.Value.Col, startX, startY);
                canvas.FillColor = LastMoveFromCol;
                canvas.FillCircle(pt.X, pt.Y, CellWidth * 0.42f);
            }
            if (LastMoveTo.HasValue)
            {
                var pt = GetScreenCoord(LastMoveTo.Value.Row, LastMoveTo.Value.Col, startX, startY);
                canvas.FillColor = LastMoveToCol;
                canvas.FillCircle(pt.X, pt.Y, CellWidth * 0.42f);
            }

            // 7. Vẽ các quân cờ (kèm hiệu ứng di chuyển nội suy MoveProgress)
            if (Pieces != null)
            {
                foreach (var piece in Pieces)
                {
                    var pt = GetScreenCoord(piece.Row, piece.Col, startX, startY);
                    if (MoveProgress < 1 && LastMoveFrom.HasValue && LastMoveTo == (piece.Row, piece.Col))
                    {
                        var from = GetScreenCoord(LastMoveFrom.Value.Row, LastMoveFrom.Value.Col, startX, startY);
                        pt = new PointF(from.X + (pt.X - from.X) * MoveProgress, from.Y + (pt.Y - from.Y) * MoveProgress);
                    }
                    DrawPiece(canvas, pt.X, pt.Y, piece, CellWidth * 0.43f);
                }
            }

            // 8. Vẽ ô đang được chọn (Viền vàng rực rỡ)
            if (SelectedRow.HasValue && SelectedCol.HasValue)
            {
                var pt = GetScreenCoord(SelectedRow.Value, SelectedCol.Value, startX, startY);
                canvas.StrokeColor = SelectedBorder;
                canvas.StrokeSize = 3.5f;
                canvas.DrawCircle(pt.X, pt.Y, CellWidth * 0.46f);
            }

            // 9. Vẽ các nước đi hợp lệ
            if (ValidMoves != null)
            {
                foreach (var vm in ValidMoves)
                {
                    var pt = GetScreenCoord(vm.Row, vm.Col, startX, startY);
                    if (vm.IsCapture)
                    {
                        // Ô ăn quân: vòng đỏ bao quanh
                        canvas.StrokeColor = CaptureRingCol;
                        canvas.StrokeSize = 3;
                        canvas.DrawCircle(pt.X, pt.Y, CellWidth * 0.45f);
                    }
                    else
                    {
                        // Ô di chuyển trống: chấm tròn xanh lá
                        canvas.FillColor = ValidDotCol;
                        canvas.FillCircle(pt.X, pt.Y, CellWidth * 0.18f);
                    }
                }
            }
        }

        private void DrawAllCrosshairs(ICanvas canvas, float startX, float startY)
        {
            canvas.StrokeColor = GridLineColor;
            canvas.StrokeSize = 1.2f;

            // 4 vị trí Pháo: hàng 2 và hàng 7, cột 1 và cột 7
            DrawSingleCross(canvas, 2, 1, startX, startY, true, true);
            DrawSingleCross(canvas, 2, 7, startX, startY, true, true);
            DrawSingleCross(canvas, 7, 1, startX, startY, true, true);
            DrawSingleCross(canvas, 7, 7, startX, startY, true, true);

            // 10 vị trí Tốt: hàng 3 và hàng 6, cột 0, 2, 4, 6, 8
            for (int col = 0; col <= 8; col += 2)
            {
                DrawSingleCross(canvas, 3, col, startX, startY, col > 0, col < 8);
                DrawSingleCross(canvas, 6, col, startX, startY, col > 0, col < 8);
            }
        }

        private void DrawSingleCross(ICanvas canvas, int row, int col, float startX, float startY, bool hasLeft, bool hasRight)
        {
            var pt = GetScreenCoord(row, col, startX, startY);
            float gap = CellWidth * 0.08f;
            float len = CellWidth * 0.16f;

            if (hasLeft)
            {
                // Góc trên trái & dưới trái
                canvas.DrawLine(pt.X - gap - len, pt.Y - gap, pt.X - gap, pt.Y - gap);
                canvas.DrawLine(pt.X - gap, pt.Y - gap - len, pt.X - gap, pt.Y - gap);
                canvas.DrawLine(pt.X - gap - len, pt.Y + gap, pt.X - gap, pt.Y + gap);
                canvas.DrawLine(pt.X - gap, pt.Y + gap + len, pt.X - gap, pt.Y + gap);
            }
            if (hasRight)
            {
                // Góc trên phải & dưới phải
                canvas.DrawLine(pt.X + gap, pt.Y - gap, pt.X + gap + len, pt.Y - gap);
                canvas.DrawLine(pt.X + gap, pt.Y - gap - len, pt.X + gap, pt.Y - gap);
                canvas.DrawLine(pt.X + gap, pt.Y + gap, pt.X + gap + len, pt.Y + gap);
                canvas.DrawLine(pt.X + gap, pt.Y + gap + len, pt.X + gap, pt.Y + gap);
            }
        }

        private void DrawPiece(ICanvas canvas, float x, float y, PieceDto piece, float radius)
        {
            bool isRed = piece.Color.Equals("Red", StringComparison.OrdinalIgnoreCase);
            Color ink = isRed ? RedInk : BlackInk;

            // 1. Drop shadow đổ bóng nhẹ
            canvas.FillColor = PieceShadowCol;
            canvas.FillCircle(x + 1.5f, y + 2.5f, radius);

            // 2. Thân quân cờ
            canvas.FillColor = PieceFillCol;
            canvas.FillCircle(x, y, radius);

            // 3. Viền Bevel vát cạnh
            canvas.StrokeColor = PieceBevelCol;
            canvas.StrokeSize = 1.2f;
            canvas.DrawCircle(x, y, radius);

            // 4. Vòng tròn viền mực trong
            canvas.StrokeColor = ink;
            canvas.StrokeSize = 1.8f;
            canvas.DrawCircle(x, y, radius - 3.5f);

            // 5. Ký hiệu chữ Hán phồn thể
            string text = GetPieceLabel(piece.Type, isRed);
            canvas.FontColor = ink;
            canvas.FontSize = radius * 0.95f;
            canvas.DrawString(text, x, y + (radius * 0.32f), HorizontalAlignment.Center);
        }

        private static string GetPieceLabel(string type, bool isRed)
        {
            return type.ToLowerInvariant() switch
            {
                "king" => isRed ? "帥" : "將",      // Soái / Tướng
                "advisor" => isRed ? "仕" : "士",   // Sĩ
                "elephant" => isRed ? "相" : "象",  // Tượng
                "horse" => isRed ? "傌" : "馬",     // Mã
                "rook" => isRed ? "俥" : "車",      // Xe
                "cannon" => isRed ? "炮" : "砲",    // Pháo
                "pawn" => isRed ? "兵" : "卒",      // Binh / Tốt
                _ => "?"
            };
        }

        private PointF GetScreenCoord(int row, int col, float startX, float startY)
        {
            int visualRow = IsFlipped ? 9 - row : row;
            int visualCol = IsFlipped ? 8 - col : col;

            float x = startX + (visualCol * CellWidth);
            float y = startY + (visualRow * CellHeight);
            return new PointF(x, y);
        }

        public (int Row, int Col)? TouchToBoardPos(Point touchPoint)
        {
            if (CellWidth <= 0 || CellHeight <= 0 || !double.IsFinite(touchPoint.X) || !double.IsFinite(touchPoint.Y)) return null;

            float relX = (float)touchPoint.X - LastDrawnBoardRect.X;
            float relY = (float)touchPoint.Y - LastDrawnBoardRect.Y;

            int visualCol = (int)Math.Round(relX / CellWidth);
            int visualRow = (int)Math.Round(relY / CellHeight);

            if (visualCol < 0 || visualCol > 8 || visualRow < 0 || visualRow > 9)
            {
                return null;
            }

            int boardRow = IsFlipped ? 9 - visualRow : visualRow;
            int boardCol = IsFlipped ? 8 - visualCol : visualCol;

            return (boardRow, boardCol);
        }
    }
}
