using System;
using SchackBot.Engine.Core;
using SchackBot.Engine.Positioning;

using static SchackBot.Engine.MoveGeneration.PrecomputedMoveData;
using static SchackBot.Engine.Core.Squares;

namespace SchackBot.Engine.MoveGeneration;

public class MoveGenerator
{
    public const int MaxMoves = 218; // theoretical limit

    Position _position;
    bool _generateQuietMoves;
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
                if (Piece.TypeOf(piece) == PieceType.King) GenerateKingMoves(moves, startSquare);
                if (Piece.IsSlider(piece)) GenerateSlidingMoves(moves, startSquare, piece);
                if (Piece.TypeOf(piece) == PieceType.Knight) GenerateKnightMoves(moves, startSquare);
                if (Piece.TypeOf(piece) == PieceType.Pawn) GeneratePawnMoves(moves, startSquare);
            }
        }

        moves = moves.Slice(0, _currMoveIndex); // save a little memory space
        return moves.Length;
    }

    //pseudo-legal only (for now)
    private void GenerateSlidingMoves(Span<Move> moves, int startSquare, byte piece)
    {
        int startDirectionIndex = (Piece.TypeOf(piece) == PieceType.Bishop) ? 4 : 0;
        int endDirectionIndex = (Piece.TypeOf(piece) == PieceType.Rook) ? 4 : 8;

        for (int directionIndex = startDirectionIndex; directionIndex < endDirectionIndex; directionIndex++)
        {
            for (int n = 0; n < NrSquaresToEdge[startSquare][directionIndex]; n++)
            {
                int targetSquare = startSquare + (DirectionOffsets[directionIndex] * (n + 1));
                byte pieceOnTargetSquare = _position.GetPieceAt(targetSquare);

                if (Piece.ColorOf(pieceOnTargetSquare) == _friendlyColor) { break; }

                moves[_currMoveIndex++] = new Move(startSquare, targetSquare);

                if (Piece.ColorOf(pieceOnTargetSquare) == _opponentColor) { break; }
            }
        }
    }
    private void GenerateKingMoves(Span<Move> moves, int startSquare)
    {
        for (int directionIndex = 0; directionIndex < 8; directionIndex++)
        {
            if (NrSquaresToEdge[directionIndex][startSquare] < 1) { continue; }

            int targetSquare = startSquare + DirectionOffsets[directionIndex];
            byte pieceOnTargetSquare = _position.GetPieceAt(targetSquare);

            if (Piece.ColorOf(pieceOnTargetSquare) == _friendlyColor) { break; }

            moves[_currMoveIndex++] = new Move(startSquare, targetSquare);
        }

        int kingSideBetween1 = startSquare + 1;
        int kingSideBetween2 = startSquare + 2;
        int kingSideRookSquare = startSquare + 3;

        int queenSideBetween1 = startSquare - 1;
        int queenSideBetween2 = startSquare - 2;
        int queenSideBetween3 = startSquare - 3;
        int queenSideRookSquare = startSquare - 4;


        //kingside
        if (_friendlyColor == Color.White ? _position.WhiteCanCastleKingside : _position.BlackCanCastleKingside)
        {
            if (InBounds(kingSideBetween1) && InBounds(kingSideBetween2) && InBounds(kingSideRookSquare))
            {
                bool isEmptyPath =
                    Piece.IsEmpty(_position.GetPieceAt(kingSideBetween1)) &&
                    Piece.IsEmpty(_position.GetPieceAt(kingSideBetween2));
                // sanity check rook pos
                byte rook = _position.GetPieceAt(kingSideRookSquare);
                bool isRookOk =
                    !Piece.IsEmpty(rook) &&
                    Piece.TypeOf(rook) == PieceType.Rook &&
                    Piece.ColorOf(rook) == _friendlyColor;
                if (isEmptyPath && isRookOk)
                {
                    //TODO Buffer overflow here?
                    moves[_currMoveIndex++] = new Move(startSquare, startSquare + 2);
                }
            }
        }

        //queenside
        if (_friendlyColor == Color.White ? _position.WhiteCanCastleQueenside : _position.BlackCanCastleQueenside)
        {
            if (InBounds(queenSideBetween1) && InBounds(queenSideBetween2) && InBounds(queenSideBetween3) && InBounds(queenSideRookSquare))
            {
                bool isEmptyPath =
                    Piece.IsEmpty(_position.GetPieceAt(queenSideBetween1)) &&
                    Piece.IsEmpty(_position.GetPieceAt(queenSideBetween2)) &&
                    Piece.IsEmpty(_position.GetPieceAt(queenSideBetween3));
                // sanity check rook pos
                byte rook = _position.GetPieceAt(queenSideRookSquare);
                bool isRookOk =
                    !Piece.IsEmpty(rook) &&
                    Piece.TypeOf(rook) == PieceType.Rook &&
                    Piece.ColorOf(rook) == _friendlyColor;
                if (isEmptyPath && isRookOk)
                {
                    //TODO Buffer overflow here?
                    moves[_currMoveIndex++] = new Move(startSquare, startSquare - 2);
                }
            }
        }
    }
    private void GenerateKnightMoves(Span<Move> moves, int startSquare)
    {
        // int[] knightOffsets = [8];
        // for (int directionIndex = 0; directionIndex < 8; directionIndex++)
        // {
        //     knightOffsets[directionIndex] =
        // }
        throw new NotImplementedException();
    }
    private void GeneratePawnMoves(Span<Move> moves, int startSquare)
    {
        throw new NotImplementedException();
    }

    private void Init()
    {
        _friendlyColor = _position.SideToMove;
        _opponentColor = _position.OpponentColor;
    }
}
