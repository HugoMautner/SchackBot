using System.Linq;
using SchackBot.Engine.Core;
using SchackBot.Engine.Board;

namespace SchackBot.Engine.Tests.Positioning;

public class PositionMakeMoveTests
{
    private static int Sq(int file, int rank) => Squares.FromFR(file, rank);

    [Fact]
    public void PawnDoubleMove_SetsEnPassantAndResetsHalfmoveClock()
    {
        var pos = Position.FromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        // e2 -> e4
        int e2 = Sq(4, 1);
        int e4 = Sq(4, 3);
        int e3 = Sq(4, 2);

        pos.MakeMove(Move.NormalMove(e2, e4));

        Assert.Equal(e3, pos.EnPassantSquare);
        Assert.Equal(0, pos.HalfmoveClock); // pawn move resets halfmove clock
        Assert.Equal(Color.Black, pos.SideToMove);
        Assert.Equal(1, pos.FullmoveNumber); // still fullmove 1 after white's move
    }

    [Fact]
    public void BlackMove_IncrementsFullmoveNumber()
    {
        var pos = Position.FromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        // e2 -> e4
        pos.MakeMove(Move.NormalMove(Sq(4, 1), Sq(4, 3)));
        // e7 -> e5 (black)
        pos.MakeMove(Move.NormalMove(Sq(4, 6), Sq(4, 4)));

        Assert.Equal(2, pos.FullmoveNumber);
        Assert.Equal(Color.White, pos.SideToMove);
    }

    [Fact]
    public void CaptureResetsHalfmoveClock_And_UnmakeMoveRestoresState()
    {
        var pos = Position.FromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        // white: e2 -> e4
        pos.MakeMove(Move.NormalMove(Sq(4, 1), Sq(4, 3)));
        // black: d7 -> d5
        pos.MakeMove(Move.NormalMove(Sq(3, 6), Sq(3, 4)));
        // white: e4 -> d5 (capture)
        pos.MakeMove(Move.NormalMove(Sq(4, 3), Sq(3, 4)));

        // After a capture, halfmove clock should be reset
        Assert.Equal(0, pos.HalfmoveClock);
        // White pawn should now be on d5
        var pieceAtD5 = pos.GetPieceAt(Sq(3, 4));
        Assert.Equal(PieceType.Pawn, Piece.TypeOf(pieceAtD5));
        Assert.Equal(Color.White, Piece.ColorOf(pieceAtD5));

        // Unmake the capture and verify state restored
        pos.UnmakeMove(); // undo capture

        // After undo, black pawn should be back at d5 and white pawn back at e4
        var blackPawn = pos.GetPieceAt(Sq(3, 4));
        var whitePawn = pos.GetPieceAt(Sq(4, 3));

        Assert.Equal(PieceType.Pawn, Piece.TypeOf(blackPawn));
        Assert.Equal(Color.Black, Piece.ColorOf(blackPawn));

        Assert.Equal(PieceType.Pawn, Piece.TypeOf(whitePawn));
        Assert.Equal(Color.White, Piece.ColorOf(whitePawn));
    }

    [Fact]
    public void GetKingSquareForSide_StartPosition_ReturnsExpected()
    {
        var pos = Position.Start();
        int wk = pos.GetKingSquare(Color.White);
        int bk = pos.GetKingSquare(Color.Black);

        Assert.Equal(Sq(4, 0), wk); // e1
        Assert.Equal(Sq(4, 7), bk); // e8
    }

    [Fact]
    public void Start_Equals_FromFenStartFEN_Basically()
    {
        var a = Position.Start();
        var b = Position.FromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        // Compare a few core facts
        Assert.Equal(a.CastlingRights, b.CastlingRights);
        Assert.Equal(a.SideToMove, b.SideToMove);
        var ap = a.EnumeratePieces().ToList();
        var bp = b.EnumeratePieces().ToList();
        Assert.Equal(ap.Count, bp.Count);
    }
}
