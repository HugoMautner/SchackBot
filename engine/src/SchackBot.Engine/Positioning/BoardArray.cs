
using System;
using System.Collections.Generic;
using SchackBot.Engine.Core;

namespace SchackBot.Engine.Positioning;

internal sealed class BoardArray
{
    private readonly byte[] squares = new byte[64];

    public byte Get(int square) => squares[square];
    public void Set(int square, byte piece) => squares[square] = piece;
    public void Clear()
    {
        Array.Clear(squares, 0, squares.Length);
    }

    // Helper for iteration
    public IEnumerable<(int square, byte piece)> Enumerate()
    {
        for (int sq = 0; sq < 64; sq++)
        {
            var piece = squares[sq];
            if (!Piece.IsEmpty(piece))
                yield return (sq, piece);
        }
    }
}
