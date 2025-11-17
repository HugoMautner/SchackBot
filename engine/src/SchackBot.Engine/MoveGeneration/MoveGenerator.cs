using System;
using SchackBot.Engine.Core;
using SchackBot.Engine.Board;

using static SchackBot.Engine.MoveGeneration.PrecomputedMoveData;
using static SchackBot.Engine.Core.Squares;
using static SchackBot.Engine.Core.Piece;

namespace SchackBot.Engine.MoveGeneration;

public class MoveGenerator
{
    public const int MaxMoves = 218; // theoretical limit
    public enum PromotionMode { All, QueenOnly, QueenAndKnight }
    public PromotionMode PromotionsToGenerate { get; set; } = PromotionMode.All;

    // Instance Fields
    private Position _position;
    private bool _generateQuietMoves;
    private Color _friendlyColor;
    private Color _opponentColor;
    private bool _isWhiteToMove;
    private int _currMoveIndex;

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
            if (IsColor(piece, _position.SideToMove))
            {
                if (TypeOf(piece) == PieceType.King) { GenerateKingMoves(moves, startSquare); }
                if (IsSlider(piece)) { GenerateSlidingMoves(moves, startSquare, piece); }
                if (TypeOf(piece) == PieceType.Knight) { GenerateKnightMoves(moves, startSquare); }
                if (TypeOf(piece) == PieceType.Pawn) { GeneratePawnMoves(moves, startSquare); }
            }
        }

        moves = moves[.._currMoveIndex]; // save a little memory space
        return moves.Length;
    }

    private void GenerateSlidingMoves(Span<Move> moves, int startSquare, byte piece)
    {
        int startDirectionIndex = (TypeOf(piece) == PieceType.Bishop) ? 4 : 0;
        int endDirectionIndex = (TypeOf(piece) == PieceType.Rook) ? 4 : 8;

        for (int directionIndex = startDirectionIndex; directionIndex < endDirectionIndex; directionIndex++)
        {
            for (int n = 0; n < NrSquaresToEdge[startSquare][directionIndex]; n++)
            {
                int targetSquare = startSquare + (DirectionOffsets[directionIndex] * (n + 1));
                byte pieceOnTargetSquare = _position.GetPieceAt(targetSquare);

                if (IsColor(pieceOnTargetSquare, _friendlyColor)) { break; }

                bool isCapture = IsColor(pieceOnTargetSquare, _opponentColor);

                if (isCapture || _generateQuietMoves)
                { AddMove(moves, Move.NormalMove(startSquare, targetSquare)); }

                if (isCapture) { break; }
            }
        }
    }
    private void GenerateKingMoves(Span<Move> moves, int startSquare)
    {
        for (int directionIndex = 0; directionIndex < 8; directionIndex++)
        {
            if (NrSquaresToEdge[startSquare][directionIndex] < 1) { continue; }

            int targetSquare = startSquare + DirectionOffsets[directionIndex];
            byte pieceOnTargetSquare = _position.GetPieceAt(targetSquare);

            if (IsColor(pieceOnTargetSquare, _friendlyColor)) { continue; }

            bool isCapture = IsColor(pieceOnTargetSquare, _opponentColor);
            if (isCapture || _generateQuietMoves)
            { AddMove(moves, Move.NormalMove(startSquare, targetSquare)); }
        }

        //castling is quiet
        if (!_generateQuietMoves) { return; }

        //relative castling squares
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
                    IsEmpty(_position.GetPieceAt(kingSideBetween1)) &&
                    IsEmpty(_position.GetPieceAt(kingSideBetween2));
                // sanity check rook pos
                byte rook = _position.GetPieceAt(kingSideRookSquare);
                bool isRookOk =
                    TypeOf(rook) == PieceType.Rook &&
                    IsColor(rook, _friendlyColor);
                if (isEmptyPath && isRookOk)
                {
                    AddMove(moves, new Move(startSquare, startSquare + 2));
                }
            }
        }

        //queenside
        if (_friendlyColor == Color.White ? _position.WhiteCanCastleQueenside : _position.BlackCanCastleQueenside)
        {
            if (InBounds(queenSideBetween1) && InBounds(queenSideBetween2) && InBounds(queenSideBetween3) && InBounds(queenSideRookSquare))
            {
                bool isEmptyPath =
                    IsEmpty(_position.GetPieceAt(queenSideBetween1)) &&
                    IsEmpty(_position.GetPieceAt(queenSideBetween2)) &&
                    IsEmpty(_position.GetPieceAt(queenSideBetween3));
                // sanity check rook pos
                byte rook = _position.GetPieceAt(queenSideRookSquare);
                bool isRookOk =
                    TypeOf(rook) == PieceType.Rook &&
                    IsColor(rook, _friendlyColor);
                if (isEmptyPath && isRookOk)
                {
                    AddMove(moves, new Move(startSquare, startSquare - 2));
                }
            }
        }
    }
    private void GenerateKnightMoves(Span<Move> moves, int startSquare)
    {
        int startFile = File(startSquare);
        int startRank = Rank(startSquare);
        foreach ((int df, int dr) in KnightOffsets)
        {
            int file = startFile + df;
            int rank = startRank + dr;

            if (!InBounds(file, rank)) { continue; }

            int targetSquare = FromFR(file, rank);
            byte pieceOnTargetSquare = _position.GetPieceAt(targetSquare);

            if (IsColor(pieceOnTargetSquare, _friendlyColor)) { continue; }

            bool isCapture = IsColor(pieceOnTargetSquare, _opponentColor);

            if (isCapture || _generateQuietMoves)
            { AddMove(moves, Move.NormalMove(startSquare, targetSquare)); }
        }
    }
    private void GeneratePawnMoves(Span<Move> moves, int startSquare)
    {
        int pushDir = _isWhiteToMove ? 1 : -1;
        int pushOffset = pushDir * 8;
        int startFile = File(startSquare);
        int startRank = Rank(startSquare);
        int promotionRank = _isWhiteToMove ? 7 : 0;
        int pawnStartRank = _isWhiteToMove ? 1 : 6;

        int targetSquare;
        byte pieceOnTargetSquare;

        //single-push
        targetSquare = startSquare + pushOffset;
        if (InBounds(targetSquare) && IsEmpty(_position.GetPieceAt(targetSquare)))
        {
            if (Rank(targetSquare) == promotionRank)
            {
                GeneratePromotions(moves, startSquare, targetSquare);
            }
            else
            {
                if (_generateQuietMoves) { AddMove(moves, Move.NormalMove(startSquare, targetSquare)); }
            }
        }

        //double-push
        if (startRank == pawnStartRank)
        {
            targetSquare = startSquare + (pushOffset * 2);
            int middleSquare = startSquare + pushOffset;
            pieceOnTargetSquare = _position.GetPieceAt(targetSquare);
            byte pieceOnMiddleSquare = _position.GetPieceAt(middleSquare);

            //rank is checked - no need for inbound checking
            if (IsEmpty(pieceOnTargetSquare) && IsEmpty(pieceOnMiddleSquare))
            {
                if (_generateQuietMoves) { AddMove(moves, Move.CreatePawnTwo(startSquare, targetSquare)); }
            }
        }

        //captures
        foreach (int df in new[] { -1, 1 })
        {
            int tf = startFile + df;
            int tr = startRank + pushDir;
            if (!InBounds(tf, tr)) { continue; }
            targetSquare = FromFR(tf, tr);

            if (_position.EnPassantSquare >= 0) { GenerateEP(moves, startSquare, targetSquare); }

            pieceOnTargetSquare = _position.GetPieceAt(targetSquare);
            if (IsColor(pieceOnTargetSquare, _opponentColor))
            {
                if (Rank(targetSquare) == promotionRank) { GeneratePromotions(moves, startSquare, targetSquare); }
                else { AddMove(moves, Move.NormalMove(startSquare, targetSquare)); }
            }
        }
    }

    private void GeneratePromotions(Span<Move> moves, int startSquare, int targetSquare)
    {
        AddMove(moves, Move.CreatePromotion(startSquare, targetSquare, MoveFlag.PromoteToQueen));

        //Q-search: queen promotions only
        if (!_generateQuietMoves) { return; }

        if (PromotionsToGenerate == PromotionMode.All)
        {
            AddMove(moves, Move.CreatePromotion(startSquare, targetSquare, MoveFlag.PromoteToKnight));
            AddMove(moves, Move.CreatePromotion(startSquare, targetSquare, MoveFlag.PromoteToBishop));
            AddMove(moves, Move.CreatePromotion(startSquare, targetSquare, MoveFlag.PromoteToRook));
        }
        else if (PromotionsToGenerate == PromotionMode.QueenAndKnight)
        {
            AddMove(moves, Move.CreatePromotion(startSquare, targetSquare, MoveFlag.PromoteToKnight));
        }
    }
    private void GenerateEP(Span<Move> moves, int startSquare, int targetSquare)
    {
        if (targetSquare != _position.EnPassantSquare) { return; }
        if (_friendlyColor == Color.White && Rank(startSquare) != 4) { return; }
        if (_friendlyColor == Color.Black && Rank(startSquare) != 3) { return; }

        AddMove(moves, Move.CreateEnPassant(startSquare, targetSquare));
    }
    private void AddMove(Span<Move> moves, Move move)
    {
        if (_currMoveIndex >= moves.Length) { throw new InvalidOperationException("Move buffer overflow"); }
        moves[_currMoveIndex++] = move;
    }
    private void Init()
    {
        _currMoveIndex = 0;
        _friendlyColor = _position.SideToMove;
        _opponentColor = _position.OpponentColor;
        _isWhiteToMove = _friendlyColor == Color.White;
    }
}
