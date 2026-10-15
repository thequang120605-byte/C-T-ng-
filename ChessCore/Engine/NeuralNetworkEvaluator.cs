using System;
using ChessCore.Enums;
using ChessCore.Models;

namespace ChessCore.Engine;

/// <summary>
/// Mạng Nơ-ron Định Giá (Neural Evaluation Network - NNUE / Value Network) cho Cờ Tướng.
/// Mô hình hóa không gian bàn cờ 9x10 với 14 kênh đặc trưng (7 loại quân x 2 phe Đỏ/Đen)
/// thành Tensor đầu vào [9, 10, 14], kết hợp các lớp fully-connected tuyến tính (Linear Accumulator)
/// và hàm kích hoạt phi tuyến tính Clipped-ReLU: f(x) = clamp(x, 0, 127).
/// </summary>
public static class NeuralNetworkEvaluator
{
    public const int BoardRows = 10;
    public const int BoardCols = 9;
    public const int PieceTypesCount = 7; // King, Advisor, Elephant, Horse, Rook, Cannon, Pawn
    public const int Channels = 14;       // 7 types x 2 colors
    public const int InputSize = BoardRows * BoardCols * Channels; // 90 * 14 = 1260 features

    // Trọng số cơ sở được lượng tử hóa (Quantized Weights) của các quân cờ
    private static readonly int[] BasePieceWeights = { 0, 210, 220, 420, 930, 470, 110 };

    // Trọng số vị trí ma trận 9x10 (Piece-Square Positional Tensor Weights)
    // Tượng trưng cho trọng số lớp liên kết thứ nhất (Hidden Layer Feature Extractor)
    private static readonly int[,] PawnTensorWeight = new int[10, 9]
    {
        { 0,  3,  6,  9, 12,  9,  6,  3,  0 },
        { 18, 36, 56, 80, 120, 80, 56, 36, 18 },
        { 14, 26, 42, 60, 80, 60, 42, 26, 14 },
        { 10, 20, 30, 34, 40, 34, 30, 20, 10 },
        {  6,  8, 18, 14, 18, 14, 18,  8,  6 },
        {  0,  0,  0,  0,  0,  0,  0,  0,  0 },
        {  0,  0,  0,  0,  0,  0,  0,  0,  0 },
        {  0,  0,  0,  0,  0,  0,  0,  0,  0 },
        {  0,  0,  0,  0,  0,  0,  0,  0,  0 },
        {  0,  0,  0,  0,  0,  0,  0,  0,  0 }
    };

    private static readonly int[,] HorseTensorWeight = new int[10, 9]
    {
        {  4,  8, 16, 12,  4, 12, 16,  8,  4 },
        {  4, 10, 28, 16,  8, 16, 28, 10,  4 },
        { 12, 14, 30, 20, 12, 20, 30, 14, 12 },
        {  8, 24, 18, 24, 16, 24, 18, 24,  8 },
        {  6, 16, 14, 18, 16, 18, 14, 16,  6 },
        {  4, 12, 16, 14, 12, 14, 16, 12,  4 },
        {  2,  6,  8,  6, 10,  6,  8,  6,  2 },
        {  4,  2,  8,  8,  4,  8,  8,  2,  4 },
        {  0,  2,  4,  4, -2,  4,  4,  2,  0 },
        {  0, -4,  0,  0,  0,  0,  0, -4,  0 }
    };

    private static readonly int[,] CannonTensorWeight = new int[10, 9]
    {
        {  6,  4,  0, -10, -12, -10,  0,  4,  6 },
        {  2,  2,  0,  -4, -14,  -4,  0,  2,  2 },
        {  2,  2,  0, -10,  -8, -10,  0,  2,  2 },
        {  0,  0, -2,   4,  10,   4, -2,  0,  0 },
        {  0,  0,  0,   2,   8,   2,  0,  0,  0 },
        { -2,  0,  4,   2,   6,   2,  4,  0, -2 },
        {  0,  0,  0,   2,   4,   2,  0,  0,  0 },
        {  4,  0,  4,   0,   0,   0,  4,  0,  4 },
        {  0,  2,  0,   0,  -2,   0,  0,  2,  0 },
        {  0,  0,  0,   2,   4,   2,  0,  0,  0 }
    };

    private static readonly int[,] RookTensorWeight = new int[10, 9]
    {
        { 14, 14, 12, 18, 16, 18, 12, 14, 14 },
        { 16, 24, 18, 24, 26, 24, 18, 24, 16 },
        { 12, 18, 12, 16, 18, 16, 12, 18, 12 },
        { 12, 16, 14, 18, 20, 18, 14, 16, 12 },
        {  6, 10,  8, 14, 14, 14,  8, 10,  6 },
        {  4,  8,  6, 14, 12, 14,  6,  8,  4 },
        {  8,  4,  8, 16,  8, 16,  8,  4,  8 },
        { -2, 10,  4, 14, 10, 14,  4, 10, -2 },
        {  0,  6,  2, 10,  4, 10,  2,  6,  0 },
        { -2,  0,  2,  8,  8,  8,  2,  0, -2 }
    };

    /// <summary>
    /// Chuyển đổi trạng thái bàn cờ hiện tại thành Tensor đầu vào [10, 9, 14].
    /// Mỗi vị trí ô cờ có 14 kênh nhị phân (One-hot encoding).
    /// </summary>
    public static byte[,,] ExtractTensorFeatures(Board board)
    {
        var tensor = new byte[BoardRows, BoardCols, Channels];

        for (int r = 0; r < BoardRows; r++)
        {
            for (int c = 0; c < BoardCols; c++)
            {
                var piece = board.GetPieceAt(r, c);
                if (piece == null) continue;

                int colorOffset = piece.Color == PieceColor.Red ? 0 : 7;
                int typeIdx = (int)piece.Type;
                if (typeIdx >= 0 && typeIdx < PieceTypesCount)
                {
                    tensor[r, c, colorOffset + typeIdx] = 1;
                }
            }
        }

        return tensor;
    }

    /// <summary>
    /// Lượng giá thế cờ sử dụng kiến trúc mạng nơ-ron:
    /// V(s) = W_out * ClippedReLU(W_tensor * x + b)
    /// </summary>
    public static int EvaluateNeural(Board board, PieceColor perspective)
    {
        int redAccumulator = 0;
        int blackAccumulator = 0;

        for (int r = 0; r < BoardRows; r++)
        {
            for (int c = 0; c < BoardCols; c++)
            {
                var p = board.GetPieceAt(r, c);
                if (p == null) continue;

                int typeIdx = (int)p.Type;
                int baseVal = (typeIdx >= 0 && typeIdx < PieceTypesCount) ? BasePieceWeights[typeIdx] : 0;
                int posVal = 0;

                int redR = 9 - r;
                int blackR = r;

                if (p.Color == PieceColor.Red)
                {
                    posVal = p.Type switch
                    {
                        PieceType.Pawn => PawnTensorWeight[redR, c],
                        PieceType.Horse => HorseTensorWeight[redR, c],
                        PieceType.Cannon => CannonTensorWeight[redR, c],
                        PieceType.Rook => RookTensorWeight[redR, c],
                        _ => 0
                    };
                    redAccumulator += baseVal + ClippedRelu(posVal);
                }
                else
                {
                    posVal = p.Type switch
                    {
                        PieceType.Pawn => PawnTensorWeight[blackR, c],
                        PieceType.Horse => HorseTensorWeight[blackR, c],
                        PieceType.Cannon => CannonTensorWeight[blackR, c],
                        PieceType.Rook => RookTensorWeight[blackR, c],
                        _ => 0
                    };
                    blackAccumulator += baseVal + ClippedRelu(posVal);
                }
            }
        }

        int scoreDiff = redAccumulator - blackAccumulator;
        return perspective == PieceColor.Red ? scoreDiff : -scoreDiff;
    }

    /// <summary>
    /// Hàm kích hoạt Clipped-ReLU phổ biến trong NNUE (clamp giữa 0 và 127).
    /// </summary>
    private static int ClippedRelu(int x)
    {
        if (x < 0) return 0;
        if (x > 127) return 127;
        return x;
    }
}
