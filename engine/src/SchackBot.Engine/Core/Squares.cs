
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
}
