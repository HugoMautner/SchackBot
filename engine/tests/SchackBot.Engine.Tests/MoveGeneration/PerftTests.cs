using System;
using Xunit;
using SchackBot.Engine.Board;
using SchackBot.Engine.MoveGeneration;
using SchackBot.Engine.MoveGeneration.Perft;

namespace SchackBot.Engine.Tests.MoveGeneration;

public sealed class PerftTests
{
    // Startpos
    public static TheoryData<int, long> StartPosCases => new()
    {
        { 1, 20L },
        { 2, 400L },
        { 3, 8902L },
        { 4, 197281L },
        // { 5, 4865609L }, // enable if CI is fast enough
    };

    // Kiwipete: catches castling/EP/pins
    public static TheoryData<int, long> KiwipeteCases => new()
    {
        { 1, 48L },
        { 2, 2039L },
        { 3, 97862L },
        { 4, 4085603L },
    };

    [Theory]
    [MemberData(nameof(StartPosCases))]
    public void Perft_StartPos(int depth, long expectedNodes)
    {
        var gen = new MoveGenerator();
        var pos = Position.Start();

        PerftResult res = PerftRunner.Run(pos, depth, gen);

        Assert.True(res.Nodes == expectedNodes,
            $"StartPos depth {depth} failed. Got {res.Nodes:n0}, expected {expectedNodes:n0}. ({res})");
    }

    [Theory]
    [MemberData(nameof(KiwipeteCases))]
    public void Perft_Kiwipete(int depth, long expectedNodes)
    {
        const string kiwipete =
            "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";

        var gen = new MoveGenerator();
        var pos = Position.FromFen(kiwipete);

        var res = PerftRunner.Run(pos, depth, gen);

        Assert.True(res.Nodes == expectedNodes,
            $"Kiwipete depth {depth} failed. Got {res.Nodes:n0}, expected {expectedNodes:n0}. ({res})");
    }

    // Debug helper: enable temporarily if a perft test fails
    // [Fact]
    // public void Divide_Debug_StartPos_Depth3()
    // {
    //     var gen = new MoveGenerator();
    //     var pos = Position.FromFen(Fen.startFEN);
    //     var list = Perft.Divide(pos, 3, gen);
    //     foreach (var (m, n) in list)
    //         Console.WriteLine($"{m.ToUCI(),6}  {n,12:n0}");
    // }
}
