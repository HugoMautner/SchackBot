using SchackBot.Engine.Core;
using SchackBot.Engine.Positioning;

namespace SchackBot.Engine.Tests.Positioning;

public class PositionFromFen_FieldsAndErrorsTests
{
    private static int Sq(int file, int rank) => Squares.FromFR(file, rank);

    [Fact]
    public void EnPassant_IsParsedToSquareIndex()
    {
        // EP at e6 (file=4, rank=5)
        var fen = "8/8/8/3pP3/8/8/8/8 b KQkq e6 12 34";
        var pos = Position.FromFen(fen);

        Assert.Equal(Sq(4, 5), pos.EnPassantSquare);
        Assert.Equal(12, pos.HalfmoveClock);
        Assert.Equal(34, pos.FullmoveNumber);
        Assert.Equal(Color.Black, pos.SideToMove);
        Assert.Equal(0b1111, pos.CastlingRights);
    }

    [Theory]
    [InlineData("-", 0b0000)]
    [InlineData("K", 0b0001)]
    [InlineData("Q", 0b0010)]
    [InlineData("k", 0b0100)]
    [InlineData("q", 0b1000)]
    [InlineData("KQ", 0b0011)]
    [InlineData("kq", 0b1100)]
    [InlineData("KQkq", 0b1111)]
    public void CastlingRights_AreParsedToMask(string castling, int expectedMask)
    {
        var fen = $"8/8/8/8/8/8/8/8 w {castling} - 0 1";
        var pos = Position.FromFen(fen);
        Assert.Equal(expectedMask, pos.CastlingRights);
    }

    [Fact]
    public void DigitsInPlacement_AreHandledCorrectly()
    {
        // Single white king on e4 (file=4, rank=3), rest empty
        var fen = "8/8/8/8/4K3/8/8/8 w - - 0 1";
        var pos = Position.FromFen(fen);

        for (int r = 0; r < 8; r++)
            for (int f = 0; f < 8; f++)
            {
                var p = pos.GetPieceAt(Sq(f, r));
                if (r == 3 && f == 4)
                {
                    Assert.Equal(PieceType.King, Piece.TypeOf(p));
                    Assert.Equal(Color.White, Piece.ColorOf(p));
                }
                else
                {
                    Assert.True(Piece.IsEmpty(p));
                }
            }
    }

    // --- Invalid FENs should throw ArgumentException ---

    [Theory]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR")]                        // too few fields
    [InlineData("8/8/8/8/8/8/8/8 w KQkq - 0")]                                        // too few fields
    [InlineData("8/8/8/8/8/8/8/8 w X - 0 1")]                                         // invalid castling string
    [InlineData("8/8/8/8/8/8/8/8 x - - 0 1")]                                         // invalid side to move
    [InlineData("8/8/8/8/8/8/8/9 w - - 0 1")]                                         // rank sums to 9
    [InlineData("8/8/8/8/8/8/8/7 w - - 0 1")]                                         // rank sums to 7
    [InlineData("8/8/8/8/8/8/8/8 w - i9 0 1")]                                        // invalid EP square
    [InlineData("8/8/8/8/8/8/8/8 w - e9 0 1")]                                        // invalid EP rank
    [InlineData("8/8/8/8/8/8/8/8 w - - -1 1")]                                        // negative halfmove
    [InlineData("8/8/8/8/8/8/8/8 w - - 0 0")]                                         // fullmove < 1
    [InlineData("8/8/8/8/8/8/8/8 w - - X 1")]                                         // non-numeric halfmove
    [InlineData("8/8/8/8/8/8/8/8 w - - 0 Y")]                                         // non-numeric fullmove
    [InlineData("8/8/8/8/8/8/8/8 w - - 0 1 extra")]                                   // too many fields
    [InlineData("rnbqkbnr/pppppppz/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]          // invalid piece char 'z'
    public void InvalidFENs_ThrowArgumentException(string fen)
    {
        Assert.Throws<ArgumentException>(() => Position.FromFen(fen));
    }
}
