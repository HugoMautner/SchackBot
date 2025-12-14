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

    #region Instance fields
    private Position _position;
    private bool _generateQuietMoves;
    private Color _friendlyColor;
    private Color _opponentColor;
    private bool _isWhiteToMove;
    private int _currentMoveIndex;
    #endregion

    #region Public API
    public Move[] GeneratePseudoLegalMoves(Position position, bool capturesOnly)
    {
        var buffer = new Move[MaxMoves];
        int n = GeneratePseudoLegalMoves(position, buffer, capturesOnly);

        var result = new Move[n];
        Array.Copy(buffer, result, n);
        return result;
    }
    public Move[] GenerateLegalMoves(Position position, bool capturesOnly)
    {
        var buffer = new Move[MaxMoves];
        int pseudoCount = GeneratePseudoLegalMoves(position, buffer, capturesOnly);
        int legalCount = FilterLegalMoves(position, buffer, pseudoCount);

        var result = new Move[legalCount];
        Array.Copy(buffer, result, legalCount);
        return result;
    }
    public int GenerateLegalMoves(Position position, Span<Move> buffer, bool capturesOnly) // For perft
    {
        int pseudoCount = GeneratePseudoLegalMoves(position, buffer, capturesOnly);
        return FilterLegalMoves(position, buffer, pseudoCount);
    }

    #endregion

    #region Helpers
    private int GeneratePseudoLegalMoves(Position position, Span<Move> buffer, bool capturesOnly)
    {
        _position = position;
        _generateQuietMoves = !capturesOnly;
        Init();

        for (int startSquare = 0; startSquare < 64; startSquare++)
        {
            byte piece = _position.GetPieceAt(startSquare);
            if (!IsColor(piece, _position.SideToMove)) { continue; }

            if (TypeOf(piece) == PieceType.King) { GenerateKingMoves(buffer, startSquare); }
            if (IsSlider(piece)) { GenerateSlidingMoves(buffer, startSquare, piece); }
            if (TypeOf(piece) == PieceType.Knight) { GenerateKnightMoves(buffer, startSquare); }
            if (TypeOf(piece) == PieceType.Pawn) { GeneratePawnMoves(buffer, startSquare); }
        }

        return _currentMoveIndex;
    }

    private int FilterLegalMoves(Position position, Span<Move> buffer, int count)
    {
        int legalMoveIndex = 0;

        Color mover = position.SideToMove;
        Color opponent = position.OpponentColor;

        for (int i = 0; i < count; i++)
        {
            Move move = buffer[i];

            if (move.IsCastle)
            {
                int kingFrom = move.StartSquare;
                int kingTo = move.TargetSquare;
                int step = (kingTo > kingFrom) ? 1 : -1;
                int kingThrough = kingFrom + step;

                if (position.IsSquareAttacked(kingFrom, opponent) ||
                    position.IsSquareAttacked(kingThrough, opponent) ||
                    position.IsSquareAttacked(kingTo, opponent))
                {
                    continue; // skip illegal castle
                }
            }

            position.MakeMove(move);
            bool illegal = position.CalculateInCheck(mover);
            position.UnmakeMove();

            if (!illegal)
            {
                buffer[legalMoveIndex++] = move;
            }
        }
        return legalMoveIndex;
    }

    private void GenerateSlidingMoves(Span<Move> buffer, int startSquare, byte piece)
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
                { AddMove(buffer, Move.NormalMove(startSquare, targetSquare)); }

                if (isCapture) { break; }
            }
        }
    }
    private void GenerateKingMoves(Span<Move> buffer, int startSquare)
    {
        for (int directionIndex = 0; directionIndex < 8; directionIndex++)
        {
            if (NrSquaresToEdge[startSquare][directionIndex] < 1) { continue; }

            int targetSquare = startSquare + DirectionOffsets[directionIndex];
            byte pieceOnTargetSquare = _position.GetPieceAt(targetSquare);

            if (IsColor(pieceOnTargetSquare, _friendlyColor)) { continue; }

            bool isCapture = IsColor(pieceOnTargetSquare, _opponentColor);
            if (isCapture || _generateQuietMoves)
            { AddMove(buffer, Move.NormalMove(startSquare, targetSquare)); }
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
                    AddMove(buffer, Move.CreateCastle(startSquare, startSquare + 2));
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
                    AddMove(buffer, Move.CreateCastle(startSquare, startSquare - 2));
                }
            }
        }
    }
    private void GenerateKnightMoves(Span<Move> buffer, int startSquare)
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
            { AddMove(buffer, Move.NormalMove(startSquare, targetSquare)); }
        }
    }
    private void GeneratePawnMoves(Span<Move> buffer, int startSquare)
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
                GeneratePromotions(buffer, startSquare, targetSquare);
            }
            else
            {
                if (_generateQuietMoves) { AddMove(buffer, Move.NormalMove(startSquare, targetSquare)); }
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
                if (_generateQuietMoves) { AddMove(buffer, Move.CreatePawnTwo(startSquare, targetSquare)); }
            }
        }

        //captures
        foreach (int df in new[] { -1, 1 })
        {
            int tf = startFile + df;
            int tr = startRank + pushDir;
            if (!InBounds(tf, tr)) { continue; }
            targetSquare = FromFR(tf, tr);

            if (_position.EnPassantSquare >= 0) { GenerateEP(buffer, startSquare, targetSquare); }

            pieceOnTargetSquare = _position.GetPieceAt(targetSquare);
            if (IsColor(pieceOnTargetSquare, _opponentColor))
            {
                if (Rank(targetSquare) == promotionRank) { GeneratePromotions(buffer, startSquare, targetSquare); }
                else { AddMove(buffer, Move.NormalMove(startSquare, targetSquare)); }
            }
        }
    }

    private void GeneratePromotions(Span<Move> buffer, int startSquare, int targetSquare)
    {
        AddMove(buffer, Move.CreatePromotion(startSquare, targetSquare, MoveFlag.PromoteToQueen));

        //Q-search: queen promotions only
        if (!_generateQuietMoves) { return; }

        if (PromotionsToGenerate == PromotionMode.All)
        {
            AddMove(buffer, Move.CreatePromotion(startSquare, targetSquare, MoveFlag.PromoteToKnight));
            AddMove(buffer, Move.CreatePromotion(startSquare, targetSquare, MoveFlag.PromoteToBishop));
            AddMove(buffer, Move.CreatePromotion(startSquare, targetSquare, MoveFlag.PromoteToRook));
        }
        else if (PromotionsToGenerate == PromotionMode.QueenAndKnight)
        {
            AddMove(buffer, Move.CreatePromotion(startSquare, targetSquare, MoveFlag.PromoteToKnight));
        }
    }
    private void GenerateEP(Span<Move> buffer, int startSquare, int targetSquare)
    {
        if (targetSquare != _position.EnPassantSquare) { return; }
        if (_friendlyColor == Color.White && Rank(startSquare) != 4) { return; }
        if (_friendlyColor == Color.Black && Rank(startSquare) != 3) { return; }

        AddMove(buffer, Move.CreateEnPassant(startSquare, targetSquare));
    }
    private void AddMove(Span<Move> buffer, Move move)
    {
        if (_currentMoveIndex >= buffer.Length) { throw new InvalidOperationException("Move buffer overflow"); }
        buffer[_currentMoveIndex++] = move;
    }
    private void Init()
    {
        _currentMoveIndex = 0;
        _friendlyColor = _position.SideToMove;
        _opponentColor = _position.OpponentColor;
        _isWhiteToMove = _friendlyColor == Color.White;
    }
    #endregion
}
