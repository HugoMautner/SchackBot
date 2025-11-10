using SchackBot.Engine.Core;
using SchackBot.Engine.Board;

namespace SchackBot.Engine.Tests.Positioning;

public class PositionFromFen_StartAndBasicsTests
{
    private static int Sq(int file, int rank) => Squares.FromFR(file, rank);

    [Fact]
    public void StartFEN_Metadata_IsCorrect()
    {
        var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var pos = Position.FromFen(fen);

        Assert.Equal(Color.White, pos.SideToMove);
        Assert.Equal(-1, pos.EnPassantSquare);
        Assert.Equal(0, pos.HalfmoveClock);
        Assert.Equal(1, pos.FullmoveNumber);
        Assert.Equal(0b1111, pos.CastlingRights); // KQkq
    }

    [Fact]
    public void StartFEN_PawnRanks_AreCorrect()
    {
        var pos = Position.FromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        for (int f = 0; f < 8; f++)
        {
            var wp = pos.GetPieceAt(Sq(f, 1));
            Assert.Equal(PieceType.Pawn, Piece.TypeOf(wp));
            Assert.Equal(Color.White, Piece.ColorOf(wp));

            var bp = pos.GetPieceAt(Sq(f, 6));
            Assert.Equal(PieceType.Pawn, Piece.TypeOf(bp));
            Assert.Equal(Color.Black, Piece.ColorOf(bp));
        }
    }

    [Fact]
    public void StartFEN_BackRanks_AreCorrect()
    {
        var pos = Position.FromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        // White rank 1 (index 0)
        var expectedWhite = new[]
        {
            PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen,
            PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook
        };
        for (int f = 0; f < 8; f++)
        {
            var p = pos.GetPieceAt(Sq(f, 0));
            Assert.Equal(expectedWhite[f], Piece.TypeOf(p));
            Assert.Equal(Color.White, Piece.ColorOf(p));
        }

        // Black rank 8 (index 7)
        var expectedBlack = expectedWhite;
        for (int f = 0; f < 8; f++)
        {
            var p = pos.GetPieceAt(Sq(f, 7));
            Assert.Equal(expectedBlack[f], Piece.TypeOf(p));
            Assert.Equal(Color.Black, Piece.ColorOf(p));
        }
    }

    [Fact]
    public void EnumeratePieces_StartFEN_Returns32Pieces_NoEmpties()
    {
        var pos = Position.FromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        var pieces = pos.EnumeratePieces().ToList();

        Assert.Equal(32, pieces.Count);
        Assert.All(pieces, t => Assert.False(Piece.IsEmpty(t.piece)));
        Assert.All(pieces, t => Assert.InRange(t.square, 0, 63));
    }
}
