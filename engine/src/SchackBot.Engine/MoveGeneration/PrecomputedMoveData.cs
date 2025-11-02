
using SchackBot.Engine.Core;
using static System.Math;

namespace SchackBot.Engine.MoveGeneration;

public static class PrecomputedMoveData
{
    // N, S, W, E, NW, SE, NE, SW
    public static readonly int[] DirectionOffsets = [8, -8, -1, 1, 7, -7, 9, -9];

    // example: if availableSquares[0][1] == 7
    // there are 7 squares north of b1 (index 1)
    public static readonly int[][] NrSquaresToEdge;

    // initialize lookup data
    static PrecomputedMoveData()
    {
        NrSquaresToEdge = new int[64][];

        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                int nrNorth = 7 - rank;
                int nrSouth = rank;
                int nrWest = file;
                int nrEast = 7 - file;

                int squareIndex = Squares.FromFR(file, rank);

                NrSquaresToEdge[squareIndex] = [
                    nrNorth,
                    nrSouth,
                    nrWest,
                    nrEast,
                    Min(nrNorth, nrWest),
                    Min(nrSouth, nrEast),
                    Min(nrNorth, nrEast),
                    Min(nrSouth, nrWest)
                ];
            }
        }
    }
}
