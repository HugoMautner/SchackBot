

namespace SchackBot.Engine.Tests.Uci;

public sealed class UciTests
{
    public static TheoryData<string, string, string> UciMoveFens => new()
    {
        {
            // castling, white, kingside
            "r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQkq - 0 1",
            "e1g1",
            "r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R4RK1 b kq - 1 1"
        },
        {
            // castling, white, queenside
            "r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQkq - 0 1",
            "e1c1",
            "r3k2r/pppppppp/8/8/8/8/PPPPPPPP/2KR3R b kq - 1 1"
        },
        {
            // castling, black, kingside
            "r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R b KQkq - 0 1",
            "e8g8",
            "r4rk1/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQ - 1 2"
        },
        {
            // castling, black, queenside
            "r3k2r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R b KQkq - 0 1",
            "e8c8",
            "2kr3r/pppppppp/8/8/8/8/PPPPPPPP/R3K2R w KQ - 1 2"
        },
        {
            // promotion to queen
            "8/P6k/8/8/8/8/8/K7 w - - 0 1",
            "a7a8q",
            "Q7/7k/8/8/8/8/8/K7 b - - 0 1"
        },
        {
            // promotion to knight
            "8/P6k/8/8/8/8/8/K7 w - - 0 1",
            "a7a8n",
            "N7/7k/8/8/8/8/8/K7 b - - 0 1"
        },
        {
            // promotion to queen w/ capture
            "r3k3/1P6/8/8/8/8/8/4K3 w - - 0 1",
            "b7a8q",
            "Q3k3/8/8/8/8/8/8/4K3 b - - 0 1"
        },
        {
            // En passant
            "rnbqkbnr/ppp1pppp/8/3pP3/8/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 3",
            "e5d6",
            "rnbqkbnr/ppp1pppp/3P4/8/8/8/PPPP1PPP/RNBQKBNR b KQkq - 0 3"
        },
        {
            // castling rights lost to capture
            "r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1",
            "h1h8",
            "r3k2R/8/8/8/8/8/8/R3K3 b Qq - 0 1"
        },
        {
            // multi-move
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            "e2e4 e7e5 g1f3 b8c6 f1b5 g8f6 e1g1",
            "r1bqkb1r/pppp1ppp/2n2n2/1B2p3/4P3/5N2/PPPP1PPP/RNBQ1RK1 b kq - 5 4"
        }
    };

    [Theory]
    [MemberData(nameof(UciMoveFens))]
    public void MakeMovesFromUci_Produces_Expected_Position(string startFen, string moves, string expectedFen)
    {
        var engine = new EngineImpl();
        engine.SetPositionFromFen(startFen);
        engine.MakeMovesFromUci(moves.Split(' '));
        Assert.Equal(expectedFen, engine.CurrentFen());
    }
}
