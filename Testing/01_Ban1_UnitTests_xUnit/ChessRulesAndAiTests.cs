using System;
using System.Collections.Generic;
using System.Linq;
using ChessCore.Engine;
using ChessCore.Enums;
using ChessCore.Models;
using Xunit;

namespace ChessCore.Tests
{
    public class ChessRulesAndAiTests
    {
        #region RULE-01: Mã bị chặn chân (Horse Leg Blocking)

        [Fact]
        public void RULE01_Horse_Blocked_At_Leg_Cannot_Move_To_Destination()
        {
            // Tướng Đen ở (0,3), Tướng Đỏ ở (9,5). Mã Đỏ ở (9,1).
            // Đặt Pháo Đỏ ở (8,1) để chặn chân Mã tiến:
            // Hàng 8: 1C7 (Pháo Đỏ chiếm ô (8,1))
            // Hàng 9: 1N3K3 (Mã Đỏ ở col 1, Tướng Đỏ ở col 5)
            string fenBlocked = "3k5/9/9/9/9/9/9/9/1C7/1N3K3 w - - 0 1";
            var board = Board.FromFen(fenBlocked);

            var validMoves = MoveGenerator.GetValidMoves(board, new Position(9, 1));

            // Mã Đỏ tại (9,1) bị chặn chân ở (8,1) -> KHÔNG THỂ đi đến (7,0) hoặc (7,2)
            Assert.DoesNotContain(validMoves, m => m.To.Row == 7 && m.To.Col == 0);
            Assert.DoesNotContain(validMoves, m => m.To.Row == 7 && m.To.Col == 2);
        }

        [Fact]
        public void RULE01_Horse_Unblocked_Can_Move_Normally()
        {
            // Không có quân cản ở hàng 8: Mã Đỏ ở (9,1) nhảy bình thường tới (7,0) và (7,2)
            string fenUnblocked = "3k5/9/9/9/9/9/9/9/9/1N3K3 w - - 0 1";
            var board = Board.FromFen(fenUnblocked);

            var validMoves = MoveGenerator.GetValidMoves(board, new Position(9, 1));

            Assert.Contains(validMoves, m => m.To.Row == 7 && m.To.Col == 0);
            Assert.Contains(validMoves, m => m.To.Row == 7 && m.To.Col == 2);
        }

        #endregion

        #region RULE-02: Pháo ăn quân khi không có ngòi vs có 1 ngòi vs 2 ngòi

        [Fact]
        public void RULE02_Cannon_Cannot_Capture_Without_Screen()
        {
            // Pháo Đỏ ở (9,1), Xe Đen ở (0,1). Tướng Đen (0,3), Tướng Đỏ (9,5).
            // Không có quân nào ở giữa trên cột 1 -> Pháo KHÔNG ĐƯỢC ăn Xe Đen tại (0,1).
            string fenNoScreen = "1r1k5/9/9/9/9/9/9/9/9/1C3K3 w - - 0 1";
            var board = Board.FromFen(fenNoScreen);

            var validMoves = MoveGenerator.GetValidMoves(board, new Position(9, 1));

            Assert.DoesNotContain(validMoves, m => m.To.Row == 0 && m.To.Col == 1);
        }

        [Fact]
        public void RULE02_Cannon_Can_Capture_With_Exactly_One_Screen()
        {
            // Đặt 1 quân Mã Đỏ tại (4,1) làm ngòi -> Pháo Đỏ tại (9,1) ĂN ĐƯỢC Xe Đen tại (0,1).
            string fenOneScreen = "1r1k5/9/9/9/1N7/9/9/9/9/1C3K3 w - - 0 1";
            var board = Board.FromFen(fenOneScreen);

            var validMoves = MoveGenerator.GetValidMoves(board, new Position(9, 1));

            Assert.Contains(validMoves, m => m.To.Row == 0 && m.To.Col == 1);
        }

        [Fact]
        public void RULE02_Cannon_Cannot_Capture_With_Two_Screens()
        {
            // Đặt 2 quân trên cột 1: Mã Đen (3,1) và Mã Đỏ (5,1) -> Có 2 ngòi, Pháo KHÔNG THỂ ăn.
            string fenTwoScreens = "1r1k5/9/9/1n7/9/1N7/9/9/9/1C3K3 w - - 0 1";
            var board = Board.FromFen(fenTwoScreens);

            var validMoves = MoveGenerator.GetValidMoves(board, new Position(9, 1));

            Assert.DoesNotContain(validMoves, m => m.To.Row == 0 && m.To.Col == 1);
        }

        #endregion

        #region RULE-03: Hai Tướng đối mặt (Flying General)

        [Fact]
        public void RULE03_Moving_Shield_Piece_Exposing_Kings_Is_Illegal()
        {
            // Tướng Đen ở (0,4), Tướng Đỏ ở (9,4) (cùng cột 4).
            // Ở giữa chỉ có 1 quân Mã Đỏ tại (5,4) che mặt 2 Tướng.
            // Nếu Mã Đỏ nhảy đi nơi khác, 2 Tướng sẽ lộ mặt đối đầu trực tiếp -> Nước đi của Mã bị từ chối!
            string fenFacing = "4k4/9/9/9/9/4N4/9/9/9/4K4 w - - 0 1";
            var board = Board.FromFen(fenFacing);

            var validMoves = MoveGenerator.GetValidMoves(board, new Position(5, 4));

            // Mã không thể rời khỏi cột 4 vì sẽ vi phạm luật 2 Tướng đối mặt
            Assert.Empty(validMoves);
        }

        #endregion

        #region RULE-04: Đang bị chiếu, đi quân không giải chiếu bị từ chối

        [Fact]
        public void RULE04_Move_That_Does_Not_Resolve_Check_Is_Illegal()
        {
            // Tướng Đỏ ở (9,4). Xe Đen ở (1,4) chiếu dọc cột 4.
            // Tướng Đen ở (0,3). Bên Đỏ có thêm Mã Đỏ ở (9,1).
            // Đỏ đến lượt đi (w) và đang bị chiếu.
            string fenUnderCheck = "3k5/4r4/9/9/9/9/9/9/9/1N2K4 w - - 0 1";
            var board = Board.FromFen(fenUnderCheck);

            // Kiểm tra Đỏ đang bị chiếu
            Assert.True(MoveGenerator.CheckIfInCheck(board, PieceColor.Red));

            // Mã Đỏ ở (9,1) nhảy không giải được chiếu -> Bắt buộc danh sách nước đi hợp lệ phải rỗng
            var knightMoves = MoveGenerator.GetValidMoves(board, new Position(9, 1));
            Assert.Empty(knightMoves);

            // Tướng Đỏ ở (9,4) có thể né sang (9,3) hoặc (9,5) để thoát chiếu
            var kingMoves = MoveGenerator.GetValidMoves(board, new Position(9, 4));
            Assert.NotEmpty(kingMoves);
        }

        #endregion

        #region END-01: Hết nước đi hợp lệ (Checkmate / Stalemate)

        [Fact]
        public void END01_Checkmate_Position_Leaves_Zero_Legal_Moves()
        {
            // Thế cờ chiếu bí kinh điển:
            // Tướng Đen ở (0,3). Xe Đỏ ở (0,4) được Mã Đỏ ở (2,5) bảo kê (Mã nhảy 2 lên 1 trái -> (0,4)).
            // Xe Đỏ thứ 2 ở (9,3) khóa chặt toàn bộ cột 3. Tướng Đỏ ở (9,5).
            // Đen đến lượt đi (b) nhưng Tướng Đen không còn bất kỳ ô hợp lệ nào để đi:
            // - Sang (0,4): bị Mã (2,5) bảo vệ
            // - Xuống (1,3): bị Xe (9,3) kiểm soát
            // - Sang trái (0,2): ngoài cung cấm
            // -> Kết quả: 0 nước đi hợp lệ -> Bị xử thua theo đúng luật cờ tướng!
            string fenCheckmate = "3kR4/9/5N3/9/9/9/9/9/9/3R1K3 b - - 0 1";
            var board = Board.FromFen(fenCheckmate);

            bool hasMoves = MoveGenerator.HasAnyLegalMoves(board, PieceColor.Black);
            var allBlackMoves = MoveGenerator.GetAllValidMoves(board, PieceColor.Black);

            Assert.False(hasMoves);
            Assert.Empty(allBlackMoves);
        }

        #endregion

        #region SCORE-01: Heuristic H1/H2/H3/H4 tính điểm chính xác

        [Fact]
        public void SCORE01_Heuristic_H1_Material_Calculated_Accurately()
        {
            // Trọng số vật chất theo BoardEvaluator:
            // Xe = 900, Pháo = 450, Mã = 400. Tướng không tính vào H1.
            // Bên Đỏ gồm: 1 Xe Đỏ (900) + 1 Mã Đỏ (400) + 1 Pháo Đỏ (450) = 1750.
            string fen = "4k4/9/9/9/4C4/9/9/9/9/RN2K4 w - - 0 1";
            var board = Board.FromFen(fen);

            var redScores = BoardEvaluator.GetSideScores(board, PieceColor.Red);

            Assert.Equal(1750, redScores.H1);
            Assert.Equal(redScores.Total, redScores.H1 + redScores.H2 + redScores.H3 + redScores.H4);
        }

        #endregion

        #region AI-01: AI đang bị chiếu chọn nước đi hợp lệ giải chiếu

        [Fact]
        public void AI01_ComputerPlayer_Under_Check_Selects_Resolving_Move()
        {
            // Đỏ đến lượt (w), Tướng Đỏ (9,4) đang bị Xe Đen (1,4) chiếu thẳng cột.
            // Tướng Đen ở (0,3).
            string fen = "3k5/4r4/9/9/9/9/9/9/9/4K4 w - - 0 1";
            var board = Board.FromFen(fen);

            Assert.True(MoveGenerator.CheckIfInCheck(board, PieceColor.Red));

            var ai = new ComputerPlayer(PieceColor.Red, difficulty: 2);
            var bestMove = ai.GetMove(board);

            Assert.NotNull(bestMove);

            // Thực hiện nước đi của AI
            bool executed = board.MakeMove(bestMove);
            Assert.True(executed);

            // Sau khi AI đi xong, Tướng Đỏ không còn bị chiếu
            Assert.False(MoveGenerator.CheckIfInCheck(board, PieceColor.Red));
        }

        #endregion

        #region FEN Validation & Robustness

        [Fact]
        public void FEN_Valid_Start_Position_Loads_Correctly()
        {
            bool ok = Board.TryParseFen(Board.StartFen, out Board? board, out string error);

            Assert.True(ok);
            Assert.NotNull(board);
            Assert.Empty(error);
            Assert.Equal(PieceColor.Red, board.CurrentTurn);
        }

        [Fact]
        public void FEN_Invalid_Turn_Char_Returns_Error()
        {
            string invalidFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR x - - 0 1";
            bool ok = Board.TryParseFen(invalidFen, out Board? board, out string error);

            Assert.False(ok);
            Assert.Null(board);
            Assert.Contains("Lượt đi phải là 'w' (Đỏ) hoặc 'b' (Đen)", error);
        }

        #endregion
    }
}
