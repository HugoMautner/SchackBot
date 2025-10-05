using SchackBot.Engine.Core;

namespace SchackBot.Engine.Tests;

public class PieceTests
{
    [Fact]
    public void MakeShouldCombineTypeAndColor()
    {
        var piece = Piece.Make(PieceType.Pawn, Color.Black);

        Assert.Equal(PieceType.Pawn, Piece.TypeOf(piece));
        Assert.Equal(Color.Black, Piece.ColorOf(piece));
    }

    [Fact]
    public void IsEmptyShouldDetectEmpty()
    {
        byte empty = 0;
        Assert.True(Piece.IsEmpty(empty));

        var pawn = Piece.Make(PieceType.Pawn, Color.White);
        Assert.False(Piece.IsEmpty(pawn));
    }

    [Fact]
    public void IsWhiteAndIsBlackShouldRespectColor()
    {
        var whiteKing = Piece.Make(PieceType.King, Color.White);
        var blackQueen = Piece.Make(PieceType.Queen, Color.Black);

        Assert.True(Piece.IsWhite(whiteKing));
        Assert.False(Piece.IsBlack(whiteKing));

        Assert.True(Piece.IsBlack(blackQueen));
        Assert.False(Piece.IsWhite(blackQueen));

        Assert.False(Piece.IsWhite(0));
        Assert.False(Piece.IsBlack(0));
    }

    [Theory]
    [InlineData(PieceType.Bishop, true, false, true)]
    [InlineData(PieceType.Queen, true, true, true)]
    [InlineData(PieceType.Rook, true, true, false)]
    [InlineData(PieceType.Knight, false, false, false)]
    [InlineData(PieceType.Pawn, false, false, false)]
    [InlineData(PieceType.King, false, false, false)]
    public void SliderChecksWorkAsExpected(PieceType type, bool isSlider, bool isOrthogonal, bool isDiagonal)
    {
        var white = Color.White;
        var piece = Piece.Make(type, white);

        Assert.Equal(isSlider, Piece.IsSlider(piece));
        Assert.Equal(isOrthogonal, Piece.IsOrthogonalSlider(piece));
        Assert.Equal(isDiagonal, Piece.IsDiagonalSlider(piece));
    }

}
