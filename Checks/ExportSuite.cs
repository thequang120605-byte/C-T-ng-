using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using ChessCore.Mobile.Offline;

namespace ExportSuiteNamespace
{
    public class SuiteEntry
    {
        public string Name { get; set; } = "";
        public string Fen { get; set; } = "";
        public int RedLegalMovesCount { get; set; }
        public int BlackLegalMovesCount { get; set; }
        public int RedEvalTotal { get; set; }
        public int BlackEvalTotal { get; set; }
        public int RedH1 { get; set; }
        public int RedH2 { get; set; }
        public int RedH3 { get; set; }
        public int RedH4 { get; set; }
        public bool RedInCheck { get; set; }
        public bool BlackInCheck { get; set; }
        public string ZobristHex { get; set; } = "";
    }

    public class GroundTruthData
    {
        public List<string> ZobristPieceKeysHex { get; set; } = new();
        public string ZobristBlackTurnKeyHex { get; set; } = "";
        public List<SuiteEntry> Positions { get; set; } = new();
    }

    public static class Program
    {
        public static void Main()
        {
            var data = new GroundTruthData();

            // 1. Export Zobrist keys using Reflection from ZobristHashing
            var pieceKeysField = typeof(ZobristHashing).GetField("PieceKeys", BindingFlags.Static | BindingFlags.NonPublic)!;
            var blackTurnKeyField = typeof(ZobristHashing).GetField("BlackTurnKey", BindingFlags.Static | BindingFlags.NonPublic)!;
            
            var pieceKeys = (ulong[,,])pieceKeysField.GetValue(null)!;
            var blackTurnKey = (ulong)blackTurnKeyField.GetValue(null)!;

            data.ZobristBlackTurnKeyHex = blackTurnKey.ToString("X16");
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    for (int p = 0; p < 14; p++)
                    {
                        data.ZobristPieceKeysHex.Add(pieceKeys[r, c, p].ToString("X16"));
                    }
                }
            }

            // 2. Generate test positions
            var positions = new List<(string Name, Board Board)>();

            // Position 1: Initial Board
            var initialBoard = new Board();
            positions.Add(("Initial Position", initialBoard));

            // Position 2: After Red Cannon 2 to center (7, 1) -> (7, 4)
            var c25Board = new Board();
            c25Board.ApplyMove(new Position(7, 1), new Position(7, 4));
            positions.Add(("Midgame C2=5 (Pháo 2 bình 5)", c25Board));

            // Position 3: Horse blocked by pawn & elephant
            var horseTest = new Board();
            horseTest.ApplyMove(new Position(9, 1), new Position(7, 2)); // Horse to (7,2)
            positions.Add(("Horse Move to River (Mã 8 tiến 7)", horseTest));

            // Position 4: Endgame checkmate scenario
            var mateBoard = new Board();
            for (int r = 0; r < 10; r++) for (int c = 0; c < 9; c++) mateBoard.Grid[r, c] = null;
            mateBoard.Grid[0, 4] = new Piece(PieceType.King, PieceColor.Black);
            mateBoard.Grid[9, 4] = new Piece(PieceType.King, PieceColor.Red);
            mateBoard.Grid[1, 4] = new Piece(PieceType.Rook, PieceColor.Red); // Rook in face of king
            mateBoard.Grid[2, 4] = new Piece(PieceType.Rook, PieceColor.Red); // Double rook check
            mateBoard.SetCurrentTurn(PieceColor.Black);
            positions.Add(("Endgame Checkmate Facing", mateBoard));

            // Position 5: Tactical Flank Coordination (H4 = 60 or 100)
            var flankBoard = new Board();
            flankBoard.ApplyMove(new Position(9, 0), new Position(3, 0)); // Red rook deep in enemy territory
            flankBoard.ApplyMove(new Position(9, 1), new Position(3, 1)); // Red horse deep in enemy territory
            flankBoard.ApplyMove(new Position(7, 1), new Position(2, 1)); // Red cannon deep in enemy territory
            positions.Add(("Flank Attack Wing Coordination (H4)", flankBoard));

            foreach (var (name, b) in positions)
            {
                var redScores = BoardEvaluator.GetSideScores(b, PieceColor.Red);
                var blackScores = BoardEvaluator.GetSideScores(b, PieceColor.Black);
                var redMoves = GameRules.GetAllLegalMoves(b, PieceColor.Red);
                var blackMoves = GameRules.GetAllLegalMoves(b, PieceColor.Black);
                ulong hash = ZobristHashing.ComputeHash(b);

                data.Positions.Add(new SuiteEntry
                {
                    Name = name,
                    Fen = b.ToFen(),
                    RedLegalMovesCount = redMoves.Count,
                    BlackLegalMovesCount = blackMoves.Count,
                    RedEvalTotal = redScores.Total,
                    BlackEvalTotal = blackScores.Total,
                    RedH1 = redScores.H1,
                    RedH2 = redScores.H2,
                    RedH3 = redScores.H3,
                    RedH4 = redScores.H4,
                    RedInCheck = MoveGenerator.CheckIfInCheck(b, PieceColor.Red),
                    BlackInCheck = MoveGenerator.CheckIfInCheck(b, PieceColor.Black),
                    ZobristHex = hash.ToString("X16")
                });
            }

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            string outPath = Path.GetFullPath("Checks/ground_truth_suite.json");
            File.WriteAllText(outPath, json);
            Console.WriteLine($"Exported {data.Positions.Count} positions and {data.ZobristPieceKeysHex.Count} Zobrist keys to Checks/ground_truth_suite.json");
        }
    }
}
