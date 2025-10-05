using SchackBot.Engine.Core;

namespace SchackBot.Engine.Tests.Core;

public class SquaresTests
{
    [Theory]
    [InlineData(0, 0, 0)]   // a1
    [InlineData(7, 0, 7)]   // h1
    [InlineData(0, 7, 56)]  // a8
    [InlineData(7, 7, 63)]  // h8
    [InlineData(4, 0, 4)]   // e1
    [InlineData(4, 7, 60)]  // e8
    [InlineData(3, 3, 27)]  // d4
    public void FromFR_IsRowMajor_And_Bijective(int file, int rank, int expectedIndex)
    {
        var sq = Squares.FromFR(file, rank);
        Assert.Equal(expectedIndex, sq);
        Assert.Equal(file, Squares.File(sq));
        Assert.Equal(rank, Squares.Rank(sq));
    }

    [Theory]
    [InlineData(-1, 0, false)]
    [InlineData(8, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(0, 8, false)]
    [InlineData(0, 0, true)]
    [InlineData(7, 7, true)]
    public void InBounds_Works(int file, int rank, bool expected)
    {
        Assert.Equal(expected, Squares.InBounds(file, rank));
    }
}
