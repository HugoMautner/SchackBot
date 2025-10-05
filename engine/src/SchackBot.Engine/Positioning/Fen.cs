using System;
using SchackBot.Engine.Core;

namespace SchackBot.Engine.Positioning.Internal;


internal static class Fen
{
    internal const string startFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    internal static FenPositionInfo Parse(string fen)
    {
        var info = new FenPositionInfo();
        string[] fields = fen.Split(' ');
        if (fields.Length != 6) { throw new ArgumentException("FEN must have 6 fields"); }

        // 1: Piece Placements
        int file = 0, rank = 7;
        foreach (char c in fields[0])
        {
            if (c == '/')
            {
                if (file != 8) { throw new ArgumentException("Rank doesn't sum to 8"); }
                rank--; file = 0;
                continue;
            }
            if (c >= '1' && c <= '8')
            {
                file += c - '1' + 1;
                continue;
            }
            Color color = char.IsUpper(c) ? Color.White : Color.Black;
            char lower = char.ToLowerInvariant(c);
            PieceType pieceType = lower switch
            {
                'p' => PieceType.Pawn,
                'n' => PieceType.Knight,
                'b' => PieceType.Bishop,
                'r' => PieceType.Rook,
                'q' => PieceType.Queen,
                'k' => PieceType.King,
                _ => throw new ArgumentException($"Invalid piece char: {c}")
            };
            if (file >= 8 || rank < 0) { throw new ArgumentException("Out of board"); }
            info.Squares[Squares.FromFR(file, rank)] = Piece.Make(pieceType, color);
            file++;
        }
        if (rank != 0 || file != 8) { throw new ArgumentException("Must have 8 ranks of 8 files"); }

        // 2: Turn
        info.SideToMove = fields[1] switch
        {
            "w" => Color.White,
            "b" => Color.Black,
            _ => throw new ArgumentException("Invalid player")
        };

        // 3: Castling
        string castling = fields[2];
        if (castling == "-")
        {
            info.WhiteCastleKingside = info.WhiteCastleQueenside =
            info.BlackCastleKingside = info.BlackCastleQueenside = false;
        }
        else
        {
            int mask = 0;
            foreach (char c in castling)
            {
                switch (c)
                {
                    case 'K': info.WhiteCastleKingside = true; mask |= 1 << 0; break;
                    case 'Q': info.WhiteCastleQueenside = true; mask |= 1 << 1; break;
                    case 'k': info.BlackCastleKingside = true; mask |= 1 << 2; break;
                    case 'q': info.BlackCastleQueenside = true; mask |= 1 << 3; break;
                    default: throw new ArgumentException("Invalid castling string");
                }
            }
        }

        // 4: En Passant
        string ep = fields[3];
        if (ep == "-") { info.EnPassantSquare = -1; }
        else
        {
            if (ep.Length != 2) { throw new ArgumentException("Invalid EP field"); }
            char f = ep[0], r = ep[1];
            if (f < 'a' || f > 'h' || r < '1' || r > '8') { throw new ArgumentException("Invalid EP square"); }
            int fileEp = f - 'a';
            int rankEp = r - '1'; // convert to 0-based rank
            info.EnPassantSquare = Squares.FromFR(fileEp, rankEp);
        }

        // 5: Halfmove
        if (!int.TryParse(fields[4], out info.HalfmoveClock) || info.HalfmoveClock < 0)
            throw new ArgumentException("Invalid halfmove clock");

        // 6: Fullmove
        if (!int.TryParse(fields[5], out info.FullmoveNumber) || info.FullmoveNumber < 1)
            throw new ArgumentException("Invalid fullmove number");

        return info;
    }

    internal static string Format(FenPositionInfo info)
    {
        return "";
    }
}
