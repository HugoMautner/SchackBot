
using System;
using SchackBot.Engine.Core;

namespace SchackBot.Engine.MoveGeneration;

public readonly struct Move : IEquatable<Move>
{
    public int StartSquare { get; }
    public int TargetSquare { get; }

    public Move(int startSquare, int targetSquare)
    {
        StartSquare = startSquare;
        TargetSquare = targetSquare;
    }

    public string toUCI()
    {
        char fromFile = (char)('a' + Squares.File(StartSquare));
        int fromRank = Squares.Rank(StartSquare) + 1;
        char toFile = (char)('a' + Squares.File(TargetSquare));
        int toRank = Squares.Rank(TargetSquare) + 1;
        return $"{fromFile}{fromRank}{toFile}{toRank}";
    }
    public bool Equals(Move other) =>
        StartSquare == other.StartSquare && TargetSquare == other.TargetSquare;
    public override bool Equals(object? obj) => obj is Move m && Equals(m);
    public override int GetHashCode() => HashCode.Combine(StartSquare, TargetSquare);
    public override string ToString() => toUCI();


    public static bool operator ==(Move left, Move right) =>
        left.Equals(right);

    public static bool operator !=(Move left, Move right) =>
        !(left == right);
}
