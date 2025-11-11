using System;
using SchackBot.Engine.Board;
using SchackBot.Engine.Core;
using SchackBot.Engine.MoveGeneration;

namespace SchackBot.Engine.Utilities;

public static class Perft
{
    public static long CountNodes(Position pos, int depth)
    {
        // if (depth == 0) { return 1; }

        // long nodes = 0;
        // MoveGenerator gen = new();
        // Span<Move> moves = gen.GenerateMoves(pos);
        // foreach (Move m in moves)
        // {
        //     pos.MakeMove(m);


        // }
        // return 1;
        throw new NotImplementedException();
    }
}
