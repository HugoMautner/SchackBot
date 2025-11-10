
namespace SchackBot.Engine.Core;

public enum MoveFlag : byte
{
    None = 0,
    EnPassantCapture = 1,
    Castle = 2,
    PawnTwo = 3,
    PromoteToQueen = 4,
    PromoteToKnight = 5,
    PromoteToRook = 6,
    PromoteToBishop = 7
}
