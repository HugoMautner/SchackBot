
using System.Collections.Generic;
using SchackBot.Engine.Core;
using static SchackBot.Engine.Core.Squares;
using static System.Math;

namespace SchackBot.Engine.MoveGeneration;

public static class PrecomputedMoveData
{
    internal enum Dir : int
    {
        N = 0, S = 1, W = 2, E = 3, NW = 4, SE = 5, NE = 6, SW = 7
    }

    // N, S, W, E, NW, SE, NE, SW
    public static readonly int[] DirectionOffsets = [8, -8, -1, 1, 7, -7, 9, -9];

    /// <summary>
    /// [square][direction] => number of squares to edge in that direction.
    /// Direction indices: N, S, W, E, NW, SE, NE, SW
    /// </summary>
    public static readonly int[][] NrSquaresToEdge;

    public static readonly int[][] WhitePawnAttacks, BlackPawnAttacks; // [64][2]
    public static readonly int[][] KnightMoves, KingMoves; // [64][8]
    public static readonly int[][][] RookRays, BishopRays; // [64][4][len]


    private static readonly Dir[] RookDirs = { Dir.N, Dir.S, Dir.W, Dir.E };
    private static readonly Dir[] BishopDirs = { Dir.NW, Dir.SE, Dir.NE, Dir.SW };


    //clock-wise relative knight jumps
    public static readonly (int df, int dr)[] KnightOffsets = [
        (1, 2), (2, 1), (2, -1), (1, -2),
        (-1, -2), (-2, -1), (-2, 1), (-1, 2)
    ];

    // clock-wise relative king jumps
    public static readonly (int df, int dr)[] KingOffsets = [
        (0, 1), (1, 1), (1, 0), (1, -1),
        (0, -1), (-1, -1), (-1, 0), (-1, 1)
    ];


    // initialize lookup data
    static PrecomputedMoveData()
    {
        NrSquaresToEdge = new int[64][];
        WhitePawnAttacks = new int[64][];
        BlackPawnAttacks = new int[64][];
        KnightMoves = new int[64][];
        KingMoves = new int[64][];
        RookRays = new int[64][][];
        BishopRays = new int[64][][];

        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                int squareIndex = FromFR(file, rank);

                // Edge distances
                int n = 7 - rank, s = rank, w = file, e = 7 - file;
                int nw = Min(n, w), se = Min(s, e), ne = Min(n, e), sw = Min(s, w);
                NrSquaresToEdge[squareIndex] = [n, s, w, e, nw, se, ne, sw];

                // Pawn attacks
                List<int> whitePawnAttacksList = [];
                if (file > 0 && rank < 7)
                {
                    whitePawnAttacksList.Add(FromFR(file - 1, rank + 1));
                }
                if (file < 7 && rank < 7)
                {
                    whitePawnAttacksList.Add(FromFR(file + 1, rank + 1));
                }
                WhitePawnAttacks[squareIndex] = whitePawnAttacksList.ToArray();

                List<int> blackPawnAttacksList = [];
                if (file > 0 && rank > 0)
                {
                    blackPawnAttacksList.Add(FromFR(file - 1, rank - 1));
                }
                if (file < 7 && rank > 0)
                {
                    blackPawnAttacksList.Add(FromFR(file + 1, rank - 1));
                }
                BlackPawnAttacks[squareIndex] = blackPawnAttacksList.ToArray();

                // Knight moves
                List<int> knightMovesList = [];
                foreach ((int df, int dr) in KnightOffsets)
                {
                    int newFile = file + df, newRank = rank + dr;
                    if (InBounds(newFile, newRank)) { knightMovesList.Add(FromFR(newFile, newRank)); }
                }
                KnightMoves[squareIndex] = knightMovesList.ToArray();

                // King moves
                List<int> kingMovesList = [];
                foreach ((int df, int dr) in KingOffsets)
                {
                    int newFile = file + df, newRank = rank + dr;
                    if (InBounds(newFile, newRank)) { kingMovesList.Add(FromFR(newFile, newRank)); }
                }
                KingMoves[squareIndex] = kingMovesList.ToArray();

                // Sliding rays
                RookRays[squareIndex] = new int[4][];
                foreach (Dir dir in RookDirs)
                {
                    int len = NrSquaresToEdge[squareIndex][(int)dir];
                    RookRays[squareIndex][(int)dir] = BuildRay(squareIndex, dir, len);
                }
                BishopRays[squareIndex] = new int[4][];
                foreach (Dir dir in BishopDirs)
                {
                    int len = NrSquaresToEdge[squareIndex][(int)dir];
                    BishopRays[squareIndex][(int)dir - (int)Dir.NW] = BuildRay(squareIndex, dir, len);
                }
            }
        }

        static int[] BuildRay(int sq, Dir dir, int len)
        {
            int[] ray = new int[len];
            int offset = DirectionOffsets[(int)dir];
            for (int i = 0; i < len; i++)
            {
                ray[i] = sq + ((i + 1) * offset);
            }
            return ray;
        }
    }
}
