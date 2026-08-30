using System;
using System.Collections.Generic;
using SchackBot.Engine.Core;
using SchackBot.Engine.Board;
using SchackBot.Engine.Board.Internal;
using SchackBot.Engine.MoveGeneration;

namespace SchackBot.Engine
{
    public class EngineImpl : IEngine
    {
        private Position _internalBoard;

        public EngineImpl()
        {
            _internalBoard = Position.Start();
        }

        public void NewGame()
        {
            _internalBoard = Position.Start();
        }

        public void SetPositionStartpos()
        {
            _internalBoard = Position.Start();
        }

        public void SetPositionFromFen(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen)) { throw new ArgumentNullException(nameof(fen)); }
            _internalBoard = Position.FromFen(fen);
        }

        public string CurrentFen()
        {
            return _internalBoard.ToFen();
        }

        public string BestLegalMove()
        {
            var gen = new MoveGenerator();
            Span<Move> moves = stackalloc Move[MoveGenerator.MaxMoves];
            int count = gen.GenerateLegalMoves(this._internalBoard, moves, capturesOnly: false);
            if (count == 0)
            {
                return "0000"; // nullmove
            }

            #pragma warning disable CA5394
            int i = Random.Shared.Next(count);
            #pragma warning restore CA5394

            return moves[i].ToUCI();
        }

        public void MakeMovesFromUci(IEnumerable<string> uciMoves)
        {
            if (uciMoves == null) { return; }
            foreach (string uci in uciMoves)
            {
                if (string.IsNullOrWhiteSpace(uci)) { continue; }
                ApplyUciMoveToPosition(_internalBoard, uci.Trim());
            }
        }

        // UCI -> Position.MakeMove mapping.
        private void ApplyUciMoveToPosition(Position pos, string uci)
        {
            if (uci.Length < 4)
            {
                throw new ArgumentException($"Invalid UCI move '{uci}'", nameof(uci));
            }

            string from = uci.Substring(0, 2);
            string to = uci.Substring(2, 2);
            int fromIdx = Squares.ToSquareIndex(from);
            int toIdx = Squares.ToSquareIndex(to);
            pos.MakeMove(Move.NormalMove(fromIdx, toIdx));
        }
    }
}
