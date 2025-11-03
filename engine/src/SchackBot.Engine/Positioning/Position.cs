
using System;
using System.Collections.Generic;
using SchackBot.Engine.Core;
using SchackBot.Engine.MoveGeneration;
using SchackBot.Engine.Positioning.Internal;

using static SchackBot.Engine.Core.ColorExtensions;

namespace SchackBot.Engine.Positioning;

public sealed class Position
{
    public Color SideToMove { get; private set; } = Color.White;
    public Color OpponentColor { get; private set; }
    public int EnPassantSquare { get; private set; } = -1;
    public int HalfmoveClock { get; private set; }
    public int FullmoveNumber { get; private set; }
    public int CastlingRights { get; private set; }

    public bool WhiteCanCastleKingside => (CastlingRights & 0b0001) != 0;
    public bool WhiteCanCastleQueenside => (CastlingRights & 0b0010) != 0;
    public bool BlackCanCastleKingside => (CastlingRights & 0b0100) != 0;
    public bool BlackCanCastleQueenside => (CastlingRights & 0b1000) != 0;

    // Instance data
    private struct Undo
    {
        public int From;
        public int To;
        public byte MovedPiece;
        public byte CapturedPiece;
        public Color PrevSideToMove;
        public int PrevEnPassantSquare;
        public int PrevHalfMoveClock;
        public int PrevFullMoveNumber;
        public int PrevCastlingRights;
    }
    private readonly BoardArray _board = new();
    private readonly Stack<Undo> _history = new();

    private Position()
    {
        this.OpponentColor = OtherColor(SideToMove);
    }

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

    public byte GetPieceAt(int square) => this._board.Get(square);

    public IEnumerable<(int square, byte piece)> EnumeratePieces() => this._board.Enumerate();

    public void MakeMove(int startSquare, int targetSquare, int flags = 0)
    {
        byte moving = _board.Get(startSquare);
        byte target = _board.Get(targetSquare);

        var undo = new Undo
        {
            From = startSquare,
            To = targetSquare,
            MovedPiece = moving,
            CapturedPiece = target,
            PrevSideToMove = this.SideToMove,
            PrevEnPassantSquare = this.EnPassantSquare,
            PrevHalfMoveClock = this.HalfmoveClock,
            PrevFullMoveNumber = this.FullmoveNumber,
            PrevCastlingRights = this.CastlingRights
        };
        _history.Push(undo);

        _board.Set(startSquare, Piece.None);
        _board.Set(targetSquare, moving);

        //default reset en-passant
        this.EnPassantSquare = -1;

        //Halfmove clock
        if (Piece.TypeOf(moving) == PieceType.Pawn || target != Piece.None)
        {
            HalfmoveClock = 0;
        }
        else
        {
            HalfmoveClock++;
        }

        //pawn double-move
        if (Piece.TypeOf(moving) == PieceType.Pawn && Math.Abs(targetSquare - startSquare) == 16)
        {
            this.EnPassantSquare = (startSquare + targetSquare) / 2;
        }

        //full move incr
        if (this.SideToMove == Color.Black)
        {
            this.FullmoveNumber++;
        }

        //switch sides
        this.SideToMove = OtherColor(this.SideToMove);
        this.OpponentColor = OtherColor(this.SideToMove);

        //TODO castling rights, promotions, en-passant captures
    }
    public void MakeMove(Move move, int flags = 0) => MakeMove(move.StartSquare, move.TargetSquare, flags);

    public void UnmakeMove()
    {
        if (_history.Count == 0) { throw new InvalidOperationException("No move to unmake."); }

        var undo = _history.Pop();

        _board.Set(undo.To, undo.CapturedPiece);
        _board.Set(undo.From, undo.MovedPiece);

        this.SideToMove = undo.PrevSideToMove;
        this.OpponentColor = OtherColor(this.SideToMove);
        this.EnPassantSquare = undo.PrevEnPassantSquare;
        this.HalfmoveClock = undo.PrevHalfMoveClock;
        this.FullmoveNumber = undo.PrevFullMoveNumber;
        this.CastlingRights = undo.PrevCastlingRights;
    }

    public int GetKingSquareForSide(Color side)
    {
        foreach ((int square, byte piece) in EnumeratePieces())
        {
            if (Piece.TypeOf(piece) == PieceType.King && Piece.ColorOf(piece) == side)
            {
                return square;
            }
        }
        return -1;
    }

    private void InitializeStartPosition()
    {
        this.LoadFrom(Fen.Parse(Fen.startFEN));
    }
    internal void LoadFrom(FenPositionInfo info)
    {
        this._board.Clear();
        for (int sq = 0; sq < 64; sq++)
        {
            this._board.Set(sq, info.Squares[sq]);
        }
        this.SideToMove = info.SideToMove;
        this.OpponentColor = OtherColor(this.SideToMove);
        this.EnPassantSquare = info.EnPassantSquare;
        this.HalfmoveClock = info.HalfmoveClock;
        this.FullmoveNumber = info.FullmoveNumber;
        // compact mask (bit 0: K, bit 1: Q, bit 2: k, bit 3: q)
        this.CastlingRights =
            ((info.WhiteCastleKingside ? 1 : 0) << 0) |
            ((info.WhiteCastleQueenside ? 1 : 0) << 1) |
            ((info.BlackCastleKingside ? 1 : 0) << 2) |
            ((info.BlackCastleQueenside ? 1 : 0) << 3);

        // TODO: Reset any internal state that isn't represented in FEN.
        // Caches, move history, Zobrist/hash values, attack tables, etc.,
        // clear or recompute them here so the Position matches the FEN exactly.
    }
}
