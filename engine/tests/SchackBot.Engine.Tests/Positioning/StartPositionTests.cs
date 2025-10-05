using SchackBot.Engine.Core;
using SchackBot.Engine.Positioning;

namespace SchackBot.Engine.Tests.Positioning;

public class StartPositionTests
{
    // Helper to keep tests readable
    private static int Sq(int file, int rank) => Squares.FromFR(file, rank);

    [Fact]
    public void StartPosition_Has32Pieces()
    {
        var pos = Position.Start();
        var count = pos.EnumeratePieces().Count();
        Assert.Equal(32, count);
    }

    [Fact]
    public void StartPosition_EmptySquares_AreEmpty()
    {
        var pos = Position.Start();

        // Middle ranks 2..5 (i.e., rank indices 2..5) should be empty
        for (int rank = 2; rank <= 5; rank++)
            for (int file = 0; file < 8; file++)
                Assert.True(Piece.IsEmpty(pos.GetPieceAt(Sq(file, rank))));
    }

    [Fact]
    public void StartPosition_WhitePawns_OnRank1()
    {
        var pos = Position.Start();
        for (int file = 0; file < 8; file++)
        {
            var p = pos.GetPieceAt(Sq(file, 1));
            Assert.Equal(PieceType.Pawn, Piece.TypeOf(p));
            Assert.Equal(Color.White, Piece.ColorOf(p));
        }
    }

    [Fact]
    public void StartPosition_BlackPawns_OnRank6()
    {
        var pos = Position.Start();
        for (int file = 0; file < 8; file++)
        {
            var p = pos.GetPieceAt(Sq(file, 6));
            Assert.Equal(PieceType.Pawn, Piece.TypeOf(p));
            Assert.Equal(Color.Black, Piece.ColorOf(p));
        }
    }

    [Fact]
    public void StartPosition_WhiteBackRank_Correct()
    {
        var pos = Position.Start();

        // a1 rook, b1 knight, c1 bishop, d1 queen, e1 king, f1 bishop, g1 knight, h1 rook
        var expected = new (int file, PieceType type)[]
        {
            (0, PieceType.Rook), (1, PieceType.Knight), (2, PieceType.Bishop),
            (3, PieceType.Queen), (4, PieceType.King), (5, PieceType.Bishop),
            (6, PieceType.Knight), (7, PieceType.Rook)
        };

        foreach (var (file, type) in expected)
        {
            var p = pos.GetPieceAt(Sq(file, 0));
            Assert.Equal(type, Piece.TypeOf(p));
            Assert.Equal(Color.White, Piece.ColorOf(p));
        }
    }

    [Fact]
    public void StartPosition_BlackBackRank_Correct()
    {
        var pos = Position.Start();

        // a8 rook, b8 knight, c8 bishop, d8 queen, e8 king, f8 bishop, g8 knight, h8 rook
        var expected = new (int file, PieceType type)[]
        {
            (0, PieceType.Rook), (1, PieceType.Knight), (2, PieceType.Bishop),
            (3, PieceType.Queen), (4, PieceType.King), (5, PieceType.Bishop),
            (6, PieceType.Knight), (7, PieceType.Rook)
        };

        foreach (var (file, type) in expected)
        {
            var p = pos.GetPieceAt(Sq(file, 7));
            Assert.Equal(type, Piece.TypeOf(p));
            Assert.Equal(Color.Black, Piece.ColorOf(p));
        }
    }

    [Fact]
    public void EnumeratePieces_ReturnsNoEmpties_AndSquaresWithinBounds()
    {
        var pos = Position.Start();
        foreach (var (sq, piece) in pos.EnumeratePieces())
        {
            Assert.InRange(sq, 0, 63);
            Assert.False(Piece.IsEmpty(piece));
        }
    }

    [Fact]
    public void GetPieceAt_ReturnsSameAsEnumerate_ForAllSquares()
    {
        var pos = Position.Start();

        var map = pos.EnumeratePieces().ToDictionary(x => x.square, x => x.piece);
        for (int sq = 0; sq < 64; sq++)
        {
            var p = pos.GetPieceAt(sq);
            if (map.TryGetValue(sq, out var fromEnum))
                Assert.Equal(fromEnum, p);
            else
                Assert.True(Piece.IsEmpty(p));
        }
    }
}
