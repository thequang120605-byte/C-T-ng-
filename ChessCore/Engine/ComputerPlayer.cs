using ChessCore.Enums;
using ChessCore.Models;

namespace ChessCore.Engine
{
    public class ComputerPlayer
    {
        private readonly MinimaxEngine _engine;

        public PieceColor Color { get; }

        public ComputerPlayer(
            PieceColor color,
            int difficulty = 2)
        {
            Color = color;

            _engine =
                new MinimaxEngine(
                    difficulty);
        }

        public EngineMove? GetMove(
            Board board)
        {
            if (board == null)
                return null;

            if (board.CurrentTurn != Color)
                return null;

            return _engine.FindBestMove(
                board,
                Color);
        }

        public bool MakeMove(
            Board board)
        {
            if (board == null)
                return false;

            EngineMove? move =
                GetMove(board);

            if (move == null)
                return false;

            return board.MakeMove(move);
        }
    }
}