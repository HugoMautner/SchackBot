namespace SchackBot.Engine.Core;


public static class Piece
{
    public const byte None = 0;
    private const byte TypeMask = 0b0111;
    private const byte ColorMask = 0b1000;

    public static byte Make(PieceType type, Color color) =>
        (byte)((byte)type | ((byte)color << 3));

    public static PieceType TypeOf(byte piece) =>
        (PieceType)(piece & TypeMask);
    public static Color ColorOf(byte piece) =>
        (Color)((piece & ColorMask) >> 3);

    public static bool IsEmpty(byte piece) =>
        piece == 0;


    public static bool IsWhite(byte piece) =>
        !IsEmpty(piece) && ColorOf(piece) == Color.White;
    public static bool IsBlack(byte piece) =>
        !IsEmpty(piece) && ColorOf(piece) == Color.Black;
    public static bool IsColor(byte piece, Color color) =>
        !IsEmpty(piece) && ColorOf(piece) == color;

    public static bool IsSlider(byte piece) =>
        TypeOf(piece) is PieceType.Bishop or PieceType.Queen or PieceType.Rook;
    public static bool IsOrthogonalSlider(byte piece) =>
        TypeOf(piece) is PieceType.Rook or PieceType.Queen;
    public static bool IsDiagonalSlider(byte piece) =>
        TypeOf(piece) is PieceType.Bishop or PieceType.Queen;

    // Helper factories
    public static byte Rook(Color color) =>
        Make(PieceType.Rook, color);
    public static byte Knight(Color color) =>
        Make(PieceType.Knight, color);
    public static byte Bishop(Color color) =>
        Make(PieceType.Bishop, color);
    public static byte Queen(Color color) =>
        Make(PieceType.Queen, color);
    public static byte King(Color color) =>
        Make(PieceType.King, color);
    public static byte Pawn(Color color) =>
        Make(PieceType.Pawn, color);
}
