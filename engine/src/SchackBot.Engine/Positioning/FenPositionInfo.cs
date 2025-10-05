
using SchackBot.Engine.Core;
namespace SchackBot.Engine.Positioning.Internal;

internal sealed class FenPositionInfo
{
    public byte[] Squares = new byte[64];
    public Color SideToMove;
    public bool WhiteCastleKingside;
    public bool WhiteCastleQueenside;
    public bool BlackCastleKingside;
    public bool BlackCastleQueenside;
    public int EnPassantSquare = -1; // none
    public int HalfmoveClock;
    public int FullmoveNumber;
}
