using System;

namespace SchackBot.Engine.Utilities;

public static class BitMasks
{
    public const byte FullCastlingRights = 0b1111;
    public const byte ClearAllCastlingRights = 0b0000;

    // very sus
    public const byte ClearWhiteRights = 0b1100;
    public const byte ClearBlackRights = 0b0011;

    public const byte ClearWhiteKingside = 0b1110;
    public const byte ClearWhiteQueenside = 0b1101;
    public const byte ClearBlackKingside = 0b1011;
    public const byte ClearBlackQueenside = 0b0111;
}
