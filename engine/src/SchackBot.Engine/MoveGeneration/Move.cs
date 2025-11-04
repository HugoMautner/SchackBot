
using System;
using SchackBot.Engine.Core;

namespace SchackBot.Engine.MoveGeneration;

public readonly struct Move : IEquatable<Move>
{
    private const int StartShift = 0;
    private const int TargetShift = 6;
    private const int FlagShift = 12;
    private const ushort StartSquareMask = 0b0000000000111111; //0x003F
    private const ushort TargetSquareMask = 0b0000111111000000; //0x0FC0
    private const ushort FlagMask = 0b1111000000000000; //0xF000
    private readonly ushort _moveValue;

    public Move(ushort moveValue)
    {
        _moveValue = moveValue;
    }
    public Move(int startSquare, int targetSquare)
    {
        if ((uint)startSquare > 63u) { throw new ArgumentOutOfRangeException(nameof(startSquare)); }
        if ((uint)targetSquare > 63u) { throw new ArgumentOutOfRangeException(nameof(targetSquare)); }

        ushort s = (ushort)((startSquare << StartShift) & StartSquareMask);
        ushort t = (ushort)((targetSquare << TargetShift) & TargetSquareMask);

        _moveValue = (ushort)(s | t);
    }

    public Move(int startSquare, int targetSquare, MoveFlag flags)
    {
        if ((uint)startSquare > 63u) { throw new ArgumentOutOfRangeException(nameof(startSquare)); }
        if ((uint)targetSquare > 63u) { throw new ArgumentOutOfRangeException(nameof(targetSquare)); }

        ushort s = (ushort)((startSquare << StartShift) & StartSquareMask);
        ushort t = (ushort)((targetSquare << TargetShift) & TargetSquareMask);
        ushort f = (ushort)(((int)flags << FlagShift) & FlagMask);

        _moveValue = (ushort)(s | t | f);
    }

    public ushort Value => _moveValue;

    public int StartSquare => _moveValue & StartSquareMask;
    public int TargetSquare => (_moveValue & TargetSquareMask) >> TargetShift;
    public MoveFlag Flag => (MoveFlag)((_moveValue & FlagMask) >> FlagShift);

    public bool IsNullMove => _moveValue == 0;
    public bool IsPromotion => Flag >= MoveFlag.PromoteToQueen;
    public bool IsEnPassant => Flag == MoveFlag.EnPassantCapture;
    public bool IsCastle => Flag == MoveFlag.Castle;
    public bool IsPawnTwo => Flag == MoveFlag.PawnTwo;

    public PieceType PromotionPieceType
    {
        get
        {
            switch (Flag)
            {
                case MoveFlag.PromoteToQueen:
                    return PieceType.Queen;
                case MoveFlag.PromoteToKnight:
                    return PieceType.Knight;
                case MoveFlag.PromoteToRook:
                    return PieceType.Rook;
                case MoveFlag.PromoteToBishop:
                    return PieceType.Bishop;
                default:
                    return PieceType.None;
            }
        }
    }

    public string ToUCI()
    {
        char fromFile = (char)('a' + Squares.File(StartSquare));
        int fromRank = Squares.Rank(StartSquare) + 1;
        char toFile = (char)('a' + Squares.File(TargetSquare));
        int toRank = Squares.Rank(TargetSquare) + 1;

        string uci = $"{fromFile}{fromRank}{toFile}{toRank}";
        if (IsPromotion)
        {
            char p = PromotionPieceType switch
            {
                PieceType.Queen => 'q',
                PieceType.Rook => 'r',
                PieceType.Bishop => 'b',
                PieceType.Knight => 'n',
                _ => '?'
            };
            uci += p;
        }
        return uci;
    }
    public bool Equals(Move other) => _moveValue == other.Value;
    public override bool Equals(object? obj) => obj is Move m && Equals(m);
    public override int GetHashCode() => _moveValue.GetHashCode();
    public override string ToString() => ToUCI();


    public static bool operator ==(Move left, Move right) =>
        left.Equals(right);

    public static bool operator !=(Move left, Move right) =>
        !(left == right);

    public static Move NullMove => new(0);
    public static Move NormalMove(int from, int to) => new(from, to, MoveFlag.None);
    public static Move CreatePawnTwo(int from, int to) => new(from, to, MoveFlag.PawnTwo);
    public static Move CreateEnPassant(int from, int to) => new(from, to, MoveFlag.EnPassantCapture);
    public static Move CreatePromotion(int from, int to, MoveFlag promoFlag) => new(from, to, promoFlag);
    public static Move CreateCastle(int from, int to) => new(from, to, MoveFlag.Castle);
}
