
using System;
using System.Collections.Generic;
using SchackBot.Engine.Core;
using SchackBot.Engine.Board.Internal;

using static SchackBot.Engine.Core.ColorExtensions;
using static SchackBot.Engine.Core.Squares;
using static SchackBot.Engine.Core.Piece;
using static SchackBot.Engine.Utilities.BitMasks;

namespace SchackBot.Engine.Board;

public sealed class Position
{
    public const int WhiteIndex = 0;
    public const int BlackIndex = 1;

    // [0] = white king sq
    // [1] = black king sq
    public int[] KingSquare;

    public Color SideToMove { get; private set; } = Color.White;
    public Color OpponentColor { get; private set; }
    public bool IsWhiteToMove => SideToMove == Color.White;
    public int SideToMoveIndex => IsWhiteToMove ? WhiteIndex : BlackIndex;
    public int OpponentColorIndex => IsWhiteToMove ? BlackIndex : WhiteIndex;


    public int CastlingRights { get; private set; }
    public int EnPassantSquare { get; private set; } = -1;
    public int HalfmoveClock { get; private set; }
    public int FullmoveNumber { get; private set; }

    public bool WhiteCanCastleKingside => (CastlingRights & 0b0001) != 0;
    public bool WhiteCanCastleQueenside => (CastlingRights & 0b0010) != 0;
    public bool BlackCanCastleKingside => (CastlingRights & 0b0100) != 0;
    public bool BlackCanCastleQueenside => (CastlingRights & 0b1000) != 0;


    #region Instance members
    private readonly BoardArray _board = new();
    private readonly Stack<UndoRecord> _history = new();
    private readonly Stack<Move> _moves = new();
    private Position()
    {
        OpponentColor = OtherColor(SideToMove);
    }
    #endregion

    #region Factories
    public static Position Start()
    {
        var pos = new Position();
        pos.InitializeStartPosition();
        return pos;
    }
    public static Position FromFen(string fen)
    {
        if (fen is null) { throw new ArgumentNullException(nameof(fen)); }
        FenPositionInfo info = Fen.Parse(fen);
        var pos = new Position();
        pos.LoadFrom(info);
        return pos;
    }
    #endregion

    public byte GetPieceAt(int square) => _board.Get(square);

    public IEnumerable<(int square, byte piece)> EnumeratePieces() => _board.Enumerate();

    public void MakeMove(Move move)
    {
        #region Info about move
        int startSquare = move.StartSquare;
        int targetSquare = move.TargetSquare;

        byte movingPiece = _board.Get(startSquare);
        PieceType movingType = TypeOf(movingPiece);

        int capturedSquare = targetSquare;
        //special EP case: capture sq != target sq
        if (move.IsEnPassant) { capturedSquare += IsWhiteToMove ? -8 : 8; }

        byte capturedPiece = _board.Get(capturedSquare);
        #endregion

        #region Record History (move + undo)
        var undo = new UndoRecord
        (
            0, //TODO real zobrist
            capturedPiece,
            capturedSquare,
            CastlingRights,
            EnPassantSquare,
            HalfmoveClock,
            FullmoveNumber
        );
        _moves.Push(move);
        _history.Push(undo);
        #endregion

        #region PHASE A: Board Mutation
        //kill
        _board.Set(capturedSquare, None);

        //advance
        if (move.IsPromotion)
        {
            _board.Set(targetSquare, Make(move.PromotionPieceType, SideToMove));
        }
        else
        {
            _board.Set(targetSquare, movingPiece);
        }
        _board.Set(startSquare, None);

        //shuffle rook
        if (move.IsCastle)
        {
            bool isKingSide = File(targetSquare) == 6;

            int workingRank = IsWhiteToMove ? 0 : 7;
            int oldRookFile = isKingSide ? 7 : 0;
            int newRookFile = isKingSide ? 5 : 3;

            int oldRookSquare = FromFR(oldRookFile, workingRank);
            int newRookSquare = FromFR(newRookFile, workingRank);

            _board.Set(oldRookSquare, None);
            _board.Set(newRookSquare, Rook(SideToMove));
        }
        #endregion

        #region PHASE B: Ephemeral States

        EnPassantSquare = -1;
        if (move.IsPawnTwo)
        {
            EnPassantSquare = (startSquare + targetSquare) / 2;
        }

        if (movingType is PieceType.Pawn || capturedPiece != None)
        {
            HalfmoveClock = 0;
        }
        else
        {
            HalfmoveClock++;
        }

        if (SideToMove == Color.Black)
        {
            FullmoveNumber++;
        }

        if (CastlingRights != 0)
        {
            if (movingType is PieceType.King)
            {
                // Clear both k-side + q-side rights
                CastlingRights &= IsWhiteToMove ? ClearWhiteRights : ClearBlackRights;
            }

            // Clear individual rights
            // Either move to or from corner square invalidates that side's right
            if (startSquare == ToSquareIndex("h1") || targetSquare == ToSquareIndex("h1"))
            { CastlingRights &= ClearWhiteKingside; }

            if (startSquare == ToSquareIndex("a1") || targetSquare == ToSquareIndex("a1"))
            { CastlingRights &= ClearWhiteQueenside; }

            if (startSquare == ToSquareIndex("h8") || targetSquare == ToSquareIndex("h8"))
            { CastlingRights &= ClearBlackKingside; }

            if (startSquare == ToSquareIndex("a8") || targetSquare == ToSquareIndex("a8"))
            { CastlingRights &= ClearBlackQueenside; }
        }
        #endregion

        #region PHASE C: Side to Move
        SideToMove = OtherColor(SideToMove);
        OpponentColor = OtherColor(SideToMove);
        #endregion

        #region PHASE D: Zobrist + cache
        #endregion
    }

    public void UnmakeMove()
    {
        if (_history.Count == 0 || _moves.Count == 0) { throw new InvalidOperationException("No move to unmake."); }

        #region Info about to-be-unmade move
        UndoRecord undoRec = _history.Pop();
        Move move = _moves.Pop();
        Color mover = OtherColor(SideToMove);

        int startSquare = move.StartSquare;
        int targetSquare = move.TargetSquare;
        byte moverPiece = _board.Get(targetSquare);
        #endregion

        #region PHASE A: Board Mutation

        // Un-shuffle Rook
        if (move.IsCastle)
        {
            bool isKingSide = File(targetSquare) == 6;

            int workingRank = mover is Color.White ? 0 : 7;
            int currentRookFile = isKingSide ? 5 : 3;
            int targetRookFile = isKingSide ? 7 : 0;

            int currentRookSquare = FromFR(currentRookFile, workingRank);
            int targetRookSquare = FromFR(targetRookFile, workingRank);

            _board.Set(currentRookSquare, None);
            _board.Set(targetRookSquare, Rook(mover));
        }

        // Retreat
        _board.Set(targetSquare, None);
        if (move.IsPromotion)
        {
            _board.Set(startSquare, Pawn(mover));
        }
        else
        {
            _board.Set(startSquare, moverPiece);
        }

        // Revive
        if (undoRec.CapturedPiece != None)
        {
            _board.Set(undoRec.CapturedSquare, undoRec.CapturedPiece);
        }
        #endregion

        #region PHASE B: Ephemeral States
        CastlingRights = undoRec.PrevCastlingRights;
        EnPassantSquare = undoRec.PrevEnPassantSquare;
        HalfmoveClock = undoRec.PrevHalfMoveClock;
        FullmoveNumber = undoRec.PrevFullMoveNumber;
        #endregion

        #region PHASE C: Side to Move
        SideToMove = mover;
        OpponentColor = OtherColor(SideToMove);
        #endregion

        #region Zobrist + cache
        #endregion
    }

    public int GetKingSquare(Color side)
    {
        foreach ((int square, byte piece) in EnumeratePieces())
        {
            if (TypeOf(piece) == PieceType.King && ColorOf(piece) == side)
            {
                return square;
            }
        }
        return -1;
    }

    public bool IsSquareAttacked(int square, Color attacker)
    {
        // int rank = Rank(square);
        // int file = File(square);
        throw new NotImplementedException();
    }

    public bool IsKingInCheck(Color side)
    {
        return IsSquareAttacked(GetKingSquare(side), OtherColor(side));
    }

    #region Private methods
    private void InitializeStartPosition()
    {
        LoadFrom(Fen.Parse(Fen.startFEN));
    }
    internal void LoadFrom(FenPositionInfo info)
    {
        _board.Clear();
        for (int sq = 0; sq < 64; sq++)
        {
            _board.Set(sq, info.Squares[sq]);
        }
        SideToMove = info.SideToMove;
        OpponentColor = OtherColor(SideToMove);
        EnPassantSquare = info.EnPassantSquare;
        HalfmoveClock = info.HalfmoveClock;
        FullmoveNumber = info.FullmoveNumber;
        // compact mask (bit 0: K, bit 1: Q, bit 2: k, bit 3: q)
        CastlingRights =
            ((info.WhiteCastleKingside ? 1 : 0) << 0) |
            ((info.WhiteCastleQueenside ? 1 : 0) << 1) |
            ((info.BlackCastleKingside ? 1 : 0) << 2) |
            ((info.BlackCastleQueenside ? 1 : 0) << 3);

        // TODO: Reset any internal state that isn't represented in FEN.
        // Caches, move history, Zobrist/hash values, attack tables, etc.,
        // clear or recompute them here so the Position matches the FEN exactly.
    }
    #endregion
}
