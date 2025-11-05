using System.Linq;
using Xunit;
using SchackBot.Engine.Core;
using SchackBot.Engine.MoveGeneration;
using SchackBot.Engine.Positioning;
using static SchackBot.Engine.Core.Squares;

namespace SchackBot.Engine.Tests.MoveGeneration
{
    public class MoveGenerator_ContractTests
    {
        private static int Sq(int file, int rank) => FromFR(file, rank);

        [Fact]
        public void Generates_CapturesOnly_When_Requested_ContainsOnlyCaptures()
        {
            // White rook on a1, black pawn at a2 and an extra quiet rook move on rank
            var fen = "8/8/8/8/8/8/p7/R7 w - - 0 1";
            var pos = Position.FromFen(fen);

            var gen = new MoveGenerator();
            var captures = gen.GenerateMoves(pos, capturesOnly: true);
            var all = gen.GenerateMoves(pos, capturesOnly: false);

            // capture set should be subset of all moves
            Assert.True(captures.Length <= all.Length);

            // The capture a1 -> a2 must exist in captures-only
            Assert.Contains(captures.ToArray(), m => m == Move.NormalMove(Sq(0, 0), Sq(0, 1)));

            // And an example of a quiet move should exist in 'all' but not in 'captures'
            var allArr = all.ToArray();
            var quietCandidates = allArr.Where(m => m != Move.NormalMove(Sq(0, 0), Sq(0, 1))).ToArray();
            if (quietCandidates.Length > 0)
            {
                Assert.True(quietCandidates.Length >= 1);
                // at least one of those quiet moves should not be present in captures-only
                Assert.DoesNotContain(quietCandidates[0], captures.ToArray());
            }
        }

        [Fact]
        public void EnPassantMove_IsEncoded_WithFlag_WhenConstructed()
        {
            var m = Move.CreateEnPassant(12, 3);
            // verify flag encoding:
            Assert.Equal(MoveFlag.EnPassantCapture, m.Flag);
        }

        [Fact]
        public void PromotionFlags_MapToPromotionPieceType()
        {
            var q = Move.CreatePromotion(48, 56, MoveFlag.PromoteToQueen);
            var n = Move.CreatePromotion(48, 56, MoveFlag.PromoteToKnight);
            var r = Move.CreatePromotion(48, 56, MoveFlag.PromoteToRook);
            var b = Move.CreatePromotion(48, 56, MoveFlag.PromoteToBishop);

            Assert.Equal(PieceType.Queen, q.PromotionPieceType);
            Assert.Equal(PieceType.Knight, n.PromotionPieceType);
            Assert.Equal(PieceType.Rook, r.PromotionPieceType);
            Assert.Equal(PieceType.Bishop, b.PromotionPieceType);
        }

        [Fact]
        public void Generator_DoesNotExceed_MaxMoves_Buffer_OnStandardPosition()
        {
            var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            var pos = Position.FromFen(fen);
            var gen = new MoveGenerator();
            var moves = gen.GenerateMoves(pos);

            Assert.InRange(moves.Length, 0, MoveGenerator.MaxMoves);
        }
    }
}
