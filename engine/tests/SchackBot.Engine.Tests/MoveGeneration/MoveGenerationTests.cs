using System.Linq;
using SchackBot.Engine.Core;
using SchackBot.Engine.MoveGeneration;
using SchackBot.Engine.Positioning;

namespace SchackBot.Engine.Tests.MoveGeneration;

public class MoveGenerationTests
{
    [Fact]
    public void MoveEncoding_And_UCI_Works_ForNormalAndPromotions()
    {
        var m = Move.NormalMove(0, 8); // a1 -> a2
        Assert.Equal(0, m.StartSquare);
        Assert.Equal(8, m.TargetSquare);
        Assert.False(m.IsNullMove);
        Assert.False(m.IsPromotion);
        Assert.Equal("a1a2", m.ToUCI());

        var promo = Move.CreatePromotion(6, 14, MoveFlag.PromoteToQueen); // g1 -> g2 promote
        Assert.True(promo.IsPromotion);
        Assert.Equal(PieceType.Queen, promo.PromotionPieceType);
        Assert.Equal("g1g2q", promo.ToUCI());
    }

    [Fact]
    public void Precomputed_NrSquaresToEdge_ProducesExpectedCounts()
    {
        // a1 (file 0, rank 0)
        int a1 = Squares.FromFR(0, 0);
        Assert.Equal(7, PrecomputedMoveData.NrSquaresToEdge[a1][0]); // north
        Assert.Equal(0, PrecomputedMoveData.NrSquaresToEdge[a1][1]); // south
        Assert.Equal(0, PrecomputedMoveData.NrSquaresToEdge[a1][2]); // west
        Assert.Equal(7, PrecomputedMoveData.NrSquaresToEdge[a1][3]); // east

        // d4 (file 3, rank 3) should have 4 north, 3 south, 3 west, 4 east
        int d4 = Squares.FromFR(3, 3);
        Assert.Equal(4, PrecomputedMoveData.NrSquaresToEdge[d4][0]);
        Assert.Equal(3, PrecomputedMoveData.NrSquaresToEdge[d4][1]);
        Assert.Equal(3, PrecomputedMoveData.NrSquaresToEdge[d4][2]);
        Assert.Equal(4, PrecomputedMoveData.NrSquaresToEdge[d4][3]);
    }

    [Fact]
    public void MoveGenerator_Generates_SlidingCapture_For_Rook()
    {
        // Place a white rook at a1 and a black pawn at a2. No knights/pawns for white side
        var fen = "8/8/8/8/8/8/p7/R7 w - - 0 1";
        var pos = Position.FromFen(fen);

        var gen = new MoveGenerator();
        var moves = gen.GenerateMoves(pos);

        // Expect at least the capture a1 -> a2 (0 -> 8)
        var expected = Move.NormalMove(Squares.FromFR(0, 0), Squares.FromFR(0, 1));
        Assert.Contains(moves.ToArray(), m => m == expected);

        // Some moves should be returned (rook can move along rank/file)
        Assert.True(moves.Length > 0);
    }

    [Fact]
    public void GenerateMoves_With_EmptyBoard_ReturnsNoMovesForSideWithNoPieces()
    {
        var pos = Position.FromFen("8/8/8/8/8/8/8/8 w - - 0 1");
        var gen = new MoveGenerator();
        var moves = gen.GenerateMoves(pos);
        Assert.Equal(0, moves.Length);
    }
}
