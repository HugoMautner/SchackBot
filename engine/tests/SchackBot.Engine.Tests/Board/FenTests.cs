using SchackBot.Engine.Board;
using SchackBot.Engine.MoveGeneration;

namespace SchackBot.Engine.Tests.Board;

public sealed class FenTests
{
    public static TheoryData<string> RoundTripFens => new()
    {
        // startpos
        "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
        // all four castling flags, empty middle board
        "r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQkq - 0 1",
        // no castling rights
        "8/P6k/8/8/8/8/8/K7 w - - 0 1",
        // partial castling rights, run-length at rank start
        "r3k3/1P6/8/8/8/8/8/4K3 w - - 0 1",
        // en passant square present
        "rnbqkbnr/ppp1pppp/8/3pP3/8/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 3",
        // black to move, non-trivial clocks
        "r1bqkb1r/pppp1ppp/2n2n2/1B2p3/4P3/5N2/PPPP1PPP/RNBQ1RK1 b kq - 5 4",
        // Kiwipete
        "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1",
    };

    [Theory]
    [MemberData(nameof(RoundTripFens))]
    public void Fen_RoundTrips(string fen)
    {
        var position = Position.FromFen(fen);
        string actual = position.ToFen();
        Assert.Equal(fen, actual);
    }

    [Fact]
    public void Start_ToFen_MatchesStarposLiteral()
    {
        Assert.Equal(
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            Position.Start().ToFen()
        );
    }
}
