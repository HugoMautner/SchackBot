namespace SchackBot.Engine.Board;

public readonly struct UndoRecord(
    ulong prevzobristKey,
    byte capturedPiece,
    int capturedSquare,
    int prevCastlingRights,
    int prevEnPassantSquare,
    int prevHalfMoveClock,
    int prevFullMoveNumber)
{
    public readonly ulong PrevzobristKey = prevzobristKey;
    public readonly byte CapturedPiece = capturedPiece;
    public readonly int CapturedSquare = capturedSquare;
    public readonly int PrevCastlingRights = prevCastlingRights;
    public readonly int PrevEnPassantSquare = prevEnPassantSquare;
    public readonly int PrevHalfMoveClock = prevHalfMoveClock;
    public readonly int PrevFullMoveNumber = prevFullMoveNumber;
}
