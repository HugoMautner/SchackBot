// EngineImpl.cs  — minimal, zero-friction version for UCI plumbing & GUI testing
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SchackBot.Engine.Positioning;
using Color = SchackBot.Engine.Core.Color;

namespace SchackBot.Engine
{
    public class EngineImpl : IEngine
    {
        private Position _internalBoard;

        public EngineImpl()
        {
            _internalBoard = Position.Start();
        }

        public IEnumerable<(string name, string type, string defaultValue)> GetOptions()
        {
            yield break;
        }

        public void SetOption(string name, string value) { }

        public void NewGame()
        {
            _internalBoard = Position.Start();
        }

        public void SetPositionFromFen(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen)) { throw new ArgumentNullException(nameof(fen)); }
            _internalBoard = Position.FromFen(fen);
        }

        public void SetPositionStartpos()
        {
            _internalBoard = Position.Start();
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

        public void PonderHit() { }

        // Replace with real searcher later
        public async Task<SearchResult> StartSearchAsync(SearchParams pars, CancellationToken token, IProgress<SearchInfo> progress)
        {
            // Respect cancellation if already requested
            if (token.IsCancellationRequested) { return await Task.FromCanceled<SearchResult>(token).ConfigureAwait(false); }

            Color side = _internalBoard.SideToMove;

            // dummy info line for GUI
            progress?.Report(new SearchInfo
            {
                Depth = 1,
                SelDepth = 1,
                ScoreCp = 0,
                Nodes = 1,
                Pv = (side == Color.White) ? "e2e4" : "e7e5"
            });

            // emulate thinking
            await Task.Delay(50, token).ConfigureAwait(false);

            string best = (side == Color.White) ? "e2e4" : "e7e5";
            return new SearchResult { BestMoveUci = best };
        }

        // ----------------- helpers -----------------

        // UCI -> Position.MakeMove mapping.
        private void ApplyUciMoveToPosition(Position pos, string uci)
        {
            if (uci.Length < 4)
            {
                throw new ArgumentException($"Invalid UCI move '{uci}'", nameof(uci));
            }

            string from = uci.Substring(0, 2);
            string to = uci.Substring(2, 2);
            int fromIdx = SquareNameToIndex(from);
            int toIdx = SquareNameToIndex(to);
            int flags = 0;
            pos.MakeMove(fromIdx, toIdx, flags);
        }

        // a1 -> 0, b1 -> 1, ..., a2 -> 8, ..., h8 -> 63
        private static int SquareNameToIndex(string sq)
        {
            if (sq.Length != 2) throw new ArgumentException($"Invalid square '{sq}'", nameof(sq));
            char file = char.ToLowerInvariant(sq[0]);
            char rank = sq[1];
            if (file < 'a' || file > 'h') throw new ArgumentException($"Invalid file in square '{sq}'");
            if (rank < '1' || rank > '8') throw new ArgumentException($"Invalid rank in square '{sq}'");
            int fileIndex = file - 'a';
            int rankIndex = rank - '1';
            return rankIndex * 8 + fileIndex;
        }
    }
}
