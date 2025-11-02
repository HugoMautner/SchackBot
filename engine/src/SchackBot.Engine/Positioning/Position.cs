
using System;
using System.Collections.Generic;
using SchackBot.Engine.Core;
using SchackBot.Engine.Positioning.Internal;

namespace SchackBot.Engine.Positioning;

public sealed class Position
{
    private readonly BoardArray board = new();
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

    private Position() { }

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

    public byte GetPieceAt(int square) => this.board.Get(square);

    public IEnumerable<(int square, byte piece)> EnumeratePieces() => this.board.Enumerate();

    private void InitializeStartPosition()
    {
        this.LoadFrom(Fen.Parse(Fen.startFEN));
    }
    internal void LoadFrom(FenPositionInfo info)
    {
        this.board.Clear();
        for (int sq = 0; sq < 64; sq++)
        {
            this.board.Set(sq, info.Squares[sq]);
        }
        this.SideToMove = info.SideToMove;
        this.OpponentColor = this.SideToMove == Color.White ? Color.Black : Color.White;
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
