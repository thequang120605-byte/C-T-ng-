using System;
using ChessCore.Mobile.Offline;

namespace ChessCore.Mobile.Checks;

public static class TestPuzzles
{
    public static void Run()
    {
        string[] fens = new[]
        {
            // 1. Mã Hậu Pháo
            "3k5/4a4/4N4/4C4/9/9/4P4/9/9/4K4 w - - 0 1",
            // 2. Thiết Môn Thuyên
            "3k5/4a4/9/9/4C4/9/4P4/4R4/9/4K4 w - - 0 1",
            // 3. Đơn Xa Sát Cục
            "3k1a3/4a4/9/R8/9/9/4P4/9/9/4K4 w - - 0 1",
            // 4. Trọng Pháo Sát
            "3k5/9/4C4/4C4/9/9/4P4/9/9/4K4 w - - 0 1",
            // 5. Bạch Mã Hiện Hình
            "4k4/3Na4/9/9/9/9/4P4/9/9/4K4 w - - 0 1",
            // 6. Song Xa Phá Thành
            "3k5/4a4/9/R8/1R7/9/4P4/9/9/4K4 w - - 0 1",
            // 7. Mã Đội Pháo
            "3k5/4a4/4N4/9/4C4/9/4P4/9/9/4K4 w - - 0 1",
            // 8. Điểm Huyệt Tuyệt Chiêu: Red Pawn at (1,4), so row 1, col 4 (crossed river, so legal!)
            "3k5/4P4/9/9/9/9/9/9/9/4K4 w - - 0 1",
            // 9. Hải Để Lao Nguyệt: Rook at col 2
            "3k5/4a4/9/9/9/9/4P4/9/2R6/3CK4 w - - 0 1",
            // 10. Kim Câu Điếu Ngư
            "3k5/4a4/3N5/9/9/9/4P4/9/4R4/4K4 w - - 0 1",
            // 11. Đại Đao Trảm Quan
            "4k4/4a4/4R4/9/9/9/4P4/9/9/4K4 w - - 0 1",
            // 12. Song Mã Khâm Đao
            "3k5/4a4/3NN4/9/9/9/4P4/9/9/4K4 w - - 0 1",
            // 13. Tiến Tốt Chiếu Bí: Pawns at row 2
            "3k5/9/3PP4/9/9/9/9/9/9/4K4 w - - 0 1",
            // 14. Pháo Lồng Cửu Cung
            "3k5/4a4/9/3C1C3/9/9/4P4/9/9/4K4 w - - 0 1",
            // 15. Khởi Mã Hoành Xa
            "3k5/4a4/5N3/4R4/9/9/4P4/9/9/4K4 w - - 0 1",
            // 16. Ngọa Long Xuất Trận
            "3k5/4a4/9/3N5/4C4/9/4P4/9/9/4K4 w - - 0 1",
            // 17. Lưỡng Long Triều Nhật
            "3k5/9/9/R3R4/9/9/4P4/9/9/4K4 w - - 0 1",
            // 18. Bạt Sơn Khí Cái
            "3k5/4a4/2C6/3C5/9/9/4P4/9/9/4K4 w - - 0 1",
            // 19. Tam Xa Tề Hội
            "3k5/4a4/3P5/R3R4/9/9/9/9/9/4K4 w - - 0 1",
            // 20. Cửu Cung Tuyệt Sát
            "3k5/4a4/3N5/4R4/4C4/9/4P4/9/9/4K4 w - - 0 1"
        };

        for (int i = 0; i < fens.Length; i++)
        {
            if (!Board.TryParseFen(fens[i], out var b, out var err))
            {
                Console.WriteLine($"FAILED [{i + 1}]: {err} -> {fens[i]}");
                return;
            }
            var moves = MoveGenerator.GetAllValidMoves(b!, b!.CurrentTurn);
            Console.WriteLine($"OK [{i + 1}]: {moves.Count} moves");
        }
        Console.WriteLine("ALL 20 PUZZLES VALIDATED SUCCESSFULLY!");
    }
}
