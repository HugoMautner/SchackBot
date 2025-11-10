using System;

namespace SchackBot.Engine.Core;

public static class Squares
{
    public static int FromFR(int file, int rank) => rank * 8 + file;
    public static int File(int square) => square % 8;
    public static int Rank(int square) => square / 8;
    public static bool InBounds(int file, int rank) =>
        file >= 0 && file < 8 && rank >= 0 && rank < 8;
    public static bool InBounds(int square) =>
        square >= 0 && square < 64;
    public static int ToSquareIndex(string square)
    {
        if (square is null) { throw new ArgumentNullException(nameof(square)); }
        if (square.Length != 2) { throw new ArgumentException($"Invalid square '{square}'", nameof(square)); }
        char file = char.ToLowerInvariant(square[0]);
        char rank = square[1];
        if (file < 'a' || file > 'h') { throw new ArgumentException($"Invalid file in square '{square}'"); }
        if (rank < '1' || rank > '8') { throw new ArgumentException($"Invalid rank in square '{square}'"); }
        int fileIndex = file - 'a';
        int rankIndex = rank - '1';
        return FromFR(fileIndex, rankIndex);
    }
    public static string GetSquareName(int square)
    {
        if (!InBounds(square)) { throw new ArgumentOutOfRangeException(nameof(square), $"Square index {square} is out of bounds"); }
        char fileChar = (char)('a' + File(square));
        char rankChar = (char)('1' + Rank(square));
        return $"{fileChar}{rankChar}";
    }
}
