
namespace SchackBot.Engine.Core;

public enum Color : byte
{
    White = 0,
    Black = 1,
}

public static class ColorExtensions
{
    public static Color OtherColor(Color color)
    {
        return color == Color.White ? Color.Black : Color.White;
    }
}
