using System;
using SchackBot.Engine.Core;
using SchackBot.Engine.MoveGeneration;
using SchackBot.Engine.Board;

using static SchackBot.Engine.Core.Squares;

namespace SchackBot.Engine.Utilities;

public static class MoveUtility
{
    public static Move GetMoveFromUCI(string uciMove, Position pos)
    {
        if (uciMove is null) { throw new ArgumentNullException(nameof(uciMove)); }
        if (uciMove.Length < 4) { throw new ArgumentException($"Invalid UCI move '{uciMove}'", nameof(uciMove)); }

        int startSquare = ToSquareIndex(uciMove.Substring(0, 2));
        int targetSquare = ToSquareIndex(uciMove.Substring(2, 2));

        PieceType movedPieceType = Piece.TypeOf(pos.GetPieceAt(startSquare));

        // Determine move flag
        MoveFlag flag = MoveFlag.None;

        if (movedPieceType == PieceType.Pawn)
        {
            // Promotion
            if (uciMove.Length > 4)
            {
                char promoChar = uciMove[4];
                flag = promoChar switch
                {
                    'q' => MoveFlag.PromoteToQueen,
                    'r' => MoveFlag.PromoteToRook,
                    'b' => MoveFlag.PromoteToBishop,
                    'n' => MoveFlag.PromoteToKnight,
                    _ => MoveFlag.None,
                };
            }

            // Double push
            else if (Math.Abs(Rank(startSquare) - Rank(targetSquare)) == 2)
            {
                flag = MoveFlag.PawnTwo;
            }

            // EP
            else if (File(startSquare) != File(targetSquare) && pos.GetPieceAt(targetSquare) == Piece.None)
            {
                flag = MoveFlag.EnPassantCapture;
            }
        }
        // castling
        else if (movedPieceType == PieceType.King)
        {
            if (Math.Abs(File(startSquare) - File(targetSquare)) > 1)
            {
                flag = MoveFlag.Castle;
            }
        }

        return new Move(startSquare, targetSquare, flag);
    }

    public static string GetMoveName(Move move)
    {
        string startSquare = GetSquareName(move.StartSquare);
        string targetSquare = GetSquareName(move.TargetSquare);
        string moveName = startSquare + targetSquare;
        if (move.IsPromotion)
        {
            char promoChar = move.PromotionPieceType switch
            {
                PieceType.Queen => 'q',
                PieceType.Rook => 'r',
                PieceType.Bishop => 'b',
                PieceType.Knight => 'n',
                _ => throw new InvalidOperationException("Invalid promotion piece type"),
            };
            moveName += promoChar;
        }
        return moveName;
    }
}
