using System;
using SchackBot.Engine.Core;
using SchackBot.Engine.Positioning;

using static SchackBot.Engine.MoveGeneration.PrecomputedMoveData;

namespace SchackBot.Engine.MoveGeneration;

public class MoveGenerator
{
    public const int MaxMoves = 218; // theoretical limit

    bool _generateQuietMoves;
    Position _position;

    Color _friendlyColor;
    Color _opponentColor;
    int _currMoveIndex;

    public Span<Move> GenerateMoves(Position position, bool capturesOnly = false)
    {
        var buffer = new Move[MaxMoves];
        Span<Move> moves = buffer;
        GenerateMoves(position, ref moves, capturesOnly);

        return moves;
    }

    public int GenerateMoves(Position position, ref Span<Move> moves, bool capturesOnly)
    {
        _position = position;
        _generateQuietMoves = !capturesOnly;

        Init();

        for (int startSquare = 0; startSquare < 64; startSquare++)
        {
            byte piece = _position.GetPieceAt(startSquare);
            if (Piece.ColorOf(piece) == _position.SideToMove)
            {
                if (Piece.IsSlider(piece)) GenerateSlidingMoves(moves, startSquare, piece);

            }
        }

        moves = moves.Slice(0, _currMoveIndex);
        return moves.Length;
    }

    private void Init()
    {
        _friendlyColor = _position.SideToMove;
        _opponentColor = _position.OpponentColor;
    }

    void GenerateSlidingMoves(Span<Move> moves, int startSquare, byte piece)
    {
        int startDirectionIndex = (Piece.TypeOf(piece) == PieceType.Bishop) ? 4 : 0;
        int endDirectionIndex = (Piece.TypeOf(piece) == PieceType.Rook) ? 4 : 8;

        for (int directionIndex = startDirectionIndex; directionIndex < endDirectionIndex; directionIndex++)
        {
            for (int n = 0; n < NrSquaresToEdge[startSquare][directionIndex]; n++)
            {
                int targetSquare = startSquare + (DirectionOffsets[directionIndex] * (n + 1));
                byte pieceOnTargetSquare = _position.GetPieceAt(targetSquare);

                if (Piece.ColorOf(pieceOnTargetSquare) == _friendlyColor) break;

                moves[_currMoveIndex++] = new Move(startSquare, targetSquare);

                if (Piece.ColorOf(pieceOnTargetSquare) == _opponentColor) break;
            }
        }
    }
}
