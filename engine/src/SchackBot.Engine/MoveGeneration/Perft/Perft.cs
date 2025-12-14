using System;
using System.Collections.Generic;
using System.Diagnostics;
using SchackBot.Engine.Board;
using SchackBot.Engine.Core;

namespace SchackBot.Engine.MoveGeneration.Perft;

public readonly struct PerftResult
{
    public long Nodes { get; }
    public TimeSpan Elapsed { get; }

    public PerftResult(long nodes, TimeSpan elapsed)
    {
        Nodes = nodes;
        Elapsed = elapsed;
    }

    public double Nps => Elapsed.TotalSeconds <= 0 ? 0 : Nodes / Elapsed.TotalSeconds;
    public override string ToString() => $"{Nodes:n0} nodes in {Elapsed.TotalMilliseconds:n1} ms ({Nps:n0} nps)";
}

public static class PerftRunner
{

    public static PerftResult Run(Position pos, int depth, MoveGenerator gen)
    {
        if (depth <= 0) throw new ArgumentOutOfRangeException(nameof(depth), "Depth must be greater than zero.");
        var sw = Stopwatch.StartNew();
        long nodes = Nodes(pos, depth, gen);
        sw.Stop();
        return new PerftResult(nodes, sw.Elapsed);
    }

    // Root breakdown: useful when a perft count fails.
    public static IReadOnlyList<(Move move, long nodes)> Divide(Position pos, int depth, MoveGenerator gen)
    {
        if (depth < 1) throw new ArgumentOutOfRangeException(nameof(depth), "Divide requires depth >= 1.");

        Span<Move> moves = stackalloc Move[MoveGenerator.MaxMoves];
        int count = gen.GenerateLegalMoves(pos, moves, capturesOnly: false);

        var list = new List<(Move move, long nodes)>(count);
        for (int i = 0; i < count; i++)
        {
            Move m = moves[i];
            pos.MakeMove(m);
            long n = Nodes(pos, depth - 1, gen);
            pos.UnmakeMove();
            list.Add((m, n));
        }
        return list;
    }

    private static long Nodes(Position pos, int depth, MoveGenerator gen)
    {
        if (depth == 0) return 1;

        Span<Move> moves = stackalloc Move[MoveGenerator.MaxMoves];
        int count = gen.GenerateLegalMoves(pos, moves, capturesOnly: false);

        long nodes = 0;
        for (int i = 0; i < count; i++)
        {
            pos.MakeMove(moves[i]);
            nodes += Nodes(pos, depth - 1, gen);
            pos.UnmakeMove();
        }
        return nodes;
    }
}
