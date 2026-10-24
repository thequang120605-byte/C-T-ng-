#nullable enable annotations
#nullable disable warnings



namespace ChessCore.Mobile.Offline;

public struct SideScores
{
    public int H1;
    public int H2;
    public int H3;
    public int H4;
    public int Total => H1 + H2 + H3 + H4;
}

/// <summary>
/// H1 = vat chat; H2 = vi tri/co dong; H3 = phong thu/chien thuat; H4 = tam/tu tu dong bien (heuristic do an).
/// Heuristic nay la tham so cua do an, khong phai bang diem bat buoc cua lien doan.
/// Tat ca dac trung chi phu thuoc vi tri hien tai, phu hop voi transposition table.
/// </summary>
public static class BoardEvaluator
{
    public const int ThreePieceWingBonus = 60;
    public const int FourPieceWingBonus = 100;
    public const int AdvisorPairBonus = 70;
    public const int ElephantPairBonus = 55;
    public const int MissingGuardPenalty = 35;
    public const int GuardHomeBonus = 12;
    public const int ElephantAdvancePenalty = 35;
    public const int PalaceDangerPenalty = 24;
    public const int HangingPiecePercent = 55;
    public const int PrematureRaidPenalty = 55;
    public const int RookMobilityWeight = 3;
    public const int HorseMobilityWeight = 6;
    public const int CannonMobilityWeight = 2;
    public const int ImportantFileBonus = 18; // Cot mang 3/5 = lo 4/6.
    public const int CentralRookBonus = 28;
    public const int RiverRookBonus = 22;
    public const int BottomCannonBonus = 32;

    public static int GetPieceValue(PieceType type) => type switch
    {
        PieceType.Rook => 900, PieceType.Cannon => 450, PieceType.Horse => 400,
        PieceType.Advisor or PieceType.Elephant => 200, PieceType.Pawn => 100,
        _ => 0 // Tuong duoc xu ly bang ket qua thang/thua, khong trao doi bang diem.
    };

    public static int GetPieceValue(PieceType type, PieceColor color, int row) =>
        type == PieceType.Pawn && RelativeRow(color, row) >= 5 ? 200 : GetPieceValue(type);

    private static int RelativeRow(PieceColor color, int row) => color == PieceColor.Red ? 9 - row : row;
    private static int Index(int row, int col) => row * 9 + col;
    private static int Side(PieceColor color) => color == PieceColor.Red ? 0 : 1;
    private static bool Major(PieceType type) => type is PieceType.Rook or PieceType.Horse or PieceType.Cannon;

    private sealed class Features
    {
        public readonly int[,] Attacks = new int[2, 90];
        public readonly int[,] CheapestAttacker = new int[2, 90];
        public readonly int[] Advisors = new int[2];
        public readonly int[] Elephants = new int[2];
        public readonly Position?[] Kings = new Position?[2];
        public int Opening;
        public bool ControlsEnemyPalace;
        public readonly int[,] WingCount = new int[2, 2];
        public readonly int[,] WingPressure = new int[2, 2];
    }

    // Build geometric control maps once per evaluation, including friendly targets.
    // They describe pseudo attacks/defence, not fully legal moves of pinned pieces.
    private static Features BuildFeatures(Board board)
    {
        var f = new Features();
        int undeveloped = 0;
        for (int r = 0; r < 10; r++) for (int c = 0; c < 9; c++)
        {
            var p = board.GetPieceAt(r, c);
            if (p == null) continue;
            int side = Side(p.Color), rank = RelativeRow(p.Color, r);
            if (p.Type == PieceType.Advisor) f.Advisors[side]++;
            if (p.Type == PieceType.Elephant) f.Elephants[side]++;
            if (p.Type == PieceType.King) f.Kings[side] = new(r, c);
            if ((p.Type == PieceType.Rook && rank == 0 && (c == 0 || c == 8)) ||
                (p.Type == PieceType.Horse && rank == 0 && (c == 1 || c == 7)) ||
                (p.Type == PieceType.Cannon && rank == 2 && (c == 1 || c == 7))) undeveloped++;
            AddControls(board, f, p, r, c);
            if (Major(p.Type) && rank >= 5 && c != 4)
            {
                int wing = c < 4 ? 0 : 1;
                f.WingCount[side, wing]++;
                if (f.ControlsEnemyPalace) f.WingPressure[side, wing]++;
            }
        }
        // Position-based opening estimate: fades as majors develop or leave the board.
        f.Opening = Math.Clamp(undeveloped * 100 / 12, 0, 100);
        return f;
    }

    private static void Mark(Features f, Piece p, int row, int col)
    {
        if (row < 0 || row > 9 || col < 0 || col > 8) return;
        int side = Side(p.Color), square = Index(row, col);
        f.Attacks[side, square]++;
        if (Palace(GameRules.Opponent(p.Color), row, col)) f.ControlsEnemyPalace = true;
        int value = p.Type == PieceType.King ? 10000 : GetPieceValue(p.Type);
        int previous = f.CheapestAttacker[side, square];
        if (previous == 0 || value < previous) f.CheapestAttacker[side, square] = value;
    }

    private static bool Palace(PieceColor color, int row, int col) =>
        col >= 3 && col <= 5 && RelativeRow(color, row) <= 2;

    private static void AddControls(Board board, Features f, Piece p, int row, int col)
    {
        f.ControlsEnemyPalace = false;
        if (p.Type is PieceType.Rook or PieceType.Cannon)
        {
            foreach (var (dr, dc) in Directions)
            {
                bool screen = false;
                for (int r = row + dr, c = col + dc; r >= 0 && r < 10 && c >= 0 && c < 9; r += dr, c += dc)
                {
                    var target = board.GetPieceAt(r, c);
                    if (p.Type == PieceType.Rook) { Mark(f, p, r, c); if (target != null) break; }
                    else if (!screen) { if (target != null) screen = true; }
                    else { Mark(f, p, r, c); if (target != null) break; }
                }
            }
        }
        else if (p.Type == PieceType.Horse)
        {
            foreach (var (dr, dc) in HorseSteps)
            {
                int legR = row + (Math.Abs(dr) == 2 ? Math.Sign(dr) : 0);
                int legC = col + (Math.Abs(dc) == 2 ? Math.Sign(dc) : 0);
                if (board.GetPieceAt(legR, legC) == null) Mark(f, p, row + dr, col + dc);
            }
        }
        else if (p.Type == PieceType.Elephant)
        {
            foreach (int dr in DiagonalSigns) foreach (int dc in DiagonalSigns)
            {
                int r = row + dr * 2, c = col + dc * 2;
                if (r >= 0 && r < 10 && RelativeRow(p.Color, r) <= 4 && board.GetPieceAt(row + dr, col + dc) == null) Mark(f, p, r, c);
            }
        }
        else if (p.Type == PieceType.Advisor)
        {
            foreach (int dr in DiagonalSigns) foreach (int dc in DiagonalSigns)
                if (Palace(p.Color, row + dr, col + dc)) Mark(f, p, row + dr, col + dc);
        }
        else if (p.Type == PieceType.King)
        {
            foreach (var (dr, dc) in Directions)
                if (Palace(p.Color, row + dr, col + dc)) Mark(f, p, row + dr, col + dc);
        }
        else if (p.Type == PieceType.Pawn)
        {
            Mark(f, p, row + (p.Color == PieceColor.Red ? -1 : 1), col);
            if (RelativeRow(p.Color, row) >= 5) { Mark(f, p, row, col - 1); Mark(f, p, row, col + 1); }
        }
    }

    private static readonly (int, int)[] Directions = { (-1, 0), (1, 0), (0, -1), (0, 1) };
    private static readonly (int, int)[] HorseSteps = { (-2,-1),(-2,1),(2,-1),(2,1),(-1,-2),(1,-2),(-1,2),(1,2) };
    private static readonly int[] DiagonalSigns = { -1, 1 };

    public static SideScores GetSideScores(Board board, PieceColor color) => GetSideScores(board, color, BuildFeatures(board));

    public static int Evaluate(Board board, PieceColor perspective)
    {
        var f = BuildFeatures(board);
        int net = GetSideScores(board, PieceColor.Red, f).Total - GetSideScores(board, PieceColor.Black, f).Total;
        return perspective == PieceColor.Red ? net : -net;
    }

    private static SideScores GetSideScores(Board board, PieceColor color, Features f)
    {
        var result = new SideScores();
        int side = Side(color), enemy = 1 - side;
        for (int r = 0; r < 10; r++) for (int c = 0; c < 9; c++)
        {
            var p = board.GetPieceAt(r, c);
            if (p == null || p.Color != color) continue;
            int square = Index(r, c), rank = RelativeRow(color, r);
            int value = GetPieceValue(p.Type, color, r);
            bool attacked = f.Attacks[enemy, square] > 0, defended = f.Attacks[side, square] > 0;
            result.H1 += value;
            if (Major(p.Type))
            {
                var moves = MoveGenerator.GetPseudoLegalMoves(board, new(r, c));
                int safeMobility = 0;
                foreach (var move in moves)
                    if (f.Attacks[enemy, Index(move.To.Row, move.To.Col)] == 0) safeMobility++;
                int weight = p.Type == PieceType.Horse ? HorseMobilityWeight : p.Type == PieceType.Rook ? RookMobilityWeight : CannonMobilityWeight;
                result.H2 += moves.Count * weight + safeMobility * 2;
                if (c == 3 || c == 5) result.H2 += ImportantFileBonus;
                result.H2 += Math.Max(0, 4 - Math.Abs(4 - c)) * 3;
                if (p.Type == PieceType.Rook)
                {
                    if (c == 4) result.H2 += CentralRookBonus;
                    if (rank == 4 || rank == 5) result.H2 += RiverRookBonus;
                    if (rank >= 1 && rank <= 3) result.H2 += 18; // Xuat Xe, khong can lao sang song.
                }
                if (p.Type == PieceType.Cannon)
                {
                    if (rank >= 8 && (!attacked || defended)) result.H2 += BottomCannonBonus;
                    // Mot ngoi va quan dich cung truc: ap luc Phao, khong chi thuong chieu.
                    result.H3 += CannonPressure(board, color, r, c);
                }
                if (rank >= 5 && !defended) result.H3 -= PrematureRaidPenalty * f.Opening / 100;
                if (moves.Count <= 1) result.H2 -= 35; // Quan bi ket khong phai vi tri tot.
            }
            else if (p.Type == PieceType.Pawn)
                result.H2 += Math.Max(0, 4 - Math.Abs(4 - c)) * 4 + (rank >= 7 ? 12 : 0);
            else if (p.Type == PieceType.Advisor && rank == 1 && c == 4)
                result.H3 += 22 * (50 + f.Opening) / 100; // Len Si che mat Tuong.
            else if (p.Type == PieceType.Elephant && rank == 2 && c == 4)
                result.H3 += 18 * (50 + f.Opening) / 100; // Gac Tuong trung lo.

            if (p.Type is PieceType.Advisor or PieceType.Elephant)
            {
                if (rank <= 2) result.H3 += GuardHomeBonus;
                else if (p.Type == PieceType.Elephant)
                    result.H3 -= ElephantAdvancePenalty * f.Opening / 100;
            }
            if (p.Type != PieceType.King)
            {
                if (defended) result.H3 += 8;
                if (attacked && !defended) result.H3 -= value * HangingPiecePercent / 100;
                else if (attacked)
                    result.H3 -= Math.Max(0, value - f.CheapestAttacker[enemy, square]) / 5;
            }
        }
        result.H4 = WingScore(f, color);
        result.H3 += KingSafety(board, color, f);
        return result;
    }

    /// <summary>
    /// Project approximation: 3 or >=4 attacking Rook/Horse/Cannon pieces on
    /// the same flank, all across the river. Central file is excluded; at least
    /// two of those pieces must geometrically control the enemy palace zone.
    /// Count each piece once. H4 is capped at 100, not stacked with 60.
    /// Does not prove a forced attack or override terminal game results.
    /// </summary>
    public static int EvaluateWingCoordination(Board board, PieceColor color)
    {
        return WingScore(BuildFeatures(board), color);
    }

    private static int WingScore(Features f, PieceColor color)
    {
        int side = Side(color), best = 0;
        for (int wing = 0; wing < 2; wing++)
            if (f.WingPressure[side, wing] >= 2)
                best = Math.Max(best, f.WingCount[side, wing] >= 4 ? FourPieceWingBonus
                    : f.WingCount[side, wing] == 3 ? ThreePieceWingBonus : 0);
        return best;
    }

    private static int KingSafety(Board board, PieceColor color, Features f)
    {
        int side = Side(color), enemy = 1 - side;
        if (!f.Kings[side].HasValue) return -20000;
        var king = f.Kings[side]!.Value;
        int score = f.Advisors[side] >= 2 ? AdvisorPairBonus : 0;
        score += f.Elephants[side] >= 2 ? ElephantPairBonus : 0;
        // Form phong thu co gia tri, nhung khong du suc buoc AI bo qua mat Xe/bi sat.
        int missing = Math.Max(0, 2 - f.Advisors[side]) + Math.Max(0, 2 - f.Elephants[side]);
        score -= missing * MissingGuardPenalty * (50 + f.Opening) / 100;
        if (RelativeRow(color, king.Row) > 0) score -= 28 * f.Opening / 100;
        for (int r = Math.Max(0, king.Row - 1); r <= Math.Min(9, king.Row + 1); r++)
            for (int c = Math.Max(0, king.Col - 1); c <= Math.Min(8, king.Col + 1); c++)
                if (Palace(color, r, c) && f.Attacks[enemy, Index(r, c)] > 0)
                    score -= PalaceDangerPenalty * Math.Min(3, f.Attacks[enemy, Index(r, c)]);
        if (MoveGenerator.CheckIfInCheck(board, color)) score -= 90;
        return score;
    }

    private static int CannonPressure(Board board, PieceColor color, int row, int col)
    {
        int score = 0;
        foreach (var (dr, dc) in Directions)
        {
            bool screen = false;
            for (int r = row + dr, c = col + dc; r >= 0 && r < 10 && c >= 0 && c < 9; r += dr, c += dc)
            {
                var p = board.GetPieceAt(r, c);
                if (p == null) continue;
                if (!screen) { screen = true; continue; }
                if (p.Color != color) score += p.Type == PieceType.King ? 35 : 12;
                break;
            }
        }
        return score;
    }

    public static bool IsCheckmate(Board board, PieceColor color) => MoveGenerator.CheckIfInCheck(board, color) && !GameRules.HasLegalMove(board, color);
    public static bool HasNoLegalMoves(Board board, PieceColor color) => !GameRules.HasLegalMove(board, color);
}
