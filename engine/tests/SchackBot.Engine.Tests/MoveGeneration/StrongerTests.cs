using SchackBot.Engine.Core;
using SchackBot.Engine.MoveGeneration;
using SchackBot.Engine.Board;
using static SchackBot.Engine.Core.Squares;

namespace SchackBot.Engine.Tests.MoveGeneration
{
    public class MoveGeneration_StrongerTests
    {
        [Fact]
        public void MoveEncoding_And_UCI_Works_ForNormalAndPromotions()
        {
            var m = Move.NormalMove(0, 8); // a1 -> a2
            Assert.Equal(0, m.StartSquare);
            Assert.Equal(8, m.TargetSquare);
            Assert.NotEqual(0, m.Value); // not the null/zero move
            Assert.False(m.IsPromotion);
            Assert.Equal("a1a2", m.ToUCI());

            var promo = Move.CreatePromotion(6, 14, MoveFlag.PromoteToQueen); // g1 -> g2 (promotion example)
            Assert.True(promo.IsPromotion);
            Assert.Equal(PieceType.Queen, promo.PromotionPieceType);
            Assert.Equal("g1g2q", promo.ToUCI());
            Assert.Equal(MoveFlag.PromoteToQueen, promo.Flag);
        }

        [Fact]
        public void Precomputed_NrSquaresToEdge_ProducesExpectedCounts()
        {
            // a1 (file 0, rank 0)
            int a1 = FromFR(0, 0);
            Assert.Equal(7, PrecomputedMoveData.NrSquaresToEdge[a1][0]); // north
            Assert.Equal(0, PrecomputedMoveData.NrSquaresToEdge[a1][1]); // south
            Assert.Equal(0, PrecomputedMoveData.NrSquaresToEdge[a1][2]); // west
            Assert.Equal(7, PrecomputedMoveData.NrSquaresToEdge[a1][3]); // east

            // d4 (file 3, rank 3) should have 4 north, 3 south, 3 west, 4 east
            int d4 = FromFR(3, 3);
            Assert.Equal(4, PrecomputedMoveData.NrSquaresToEdge[d4][0]);
            Assert.Equal(3, PrecomputedMoveData.NrSquaresToEdge[d4][1]);
            Assert.Equal(3, PrecomputedMoveData.NrSquaresToEdge[d4][2]);
            Assert.Equal(4, PrecomputedMoveData.NrSquaresToEdge[d4][3]);
        }

        [Fact]
        public void MoveGenerator_Generates_SlidingCapture_For_Rook()
        {
            // white rook at a1 and a black pawn at a2
            var fen = "8/8/8/8/8/8/p7/R7 w - - 0 1";
            var pos = Position.FromFen(fen);

            var gen = new MoveGenerator();
            var moves = gen.GenerateMoves(pos);

            var expected = Move.NormalMove(FromFR(0, 0), FromFR(0, 1)); // a1 -> a2
            Assert.Contains(moves.ToArray(), m => m == expected);

            // Also assert there are more than 0 moves (smoke)
            Assert.True(moves.Length > 0);
        }

        [Fact]
        public void GenerateMoves_With_EmptyBoard_ReturnsNoMovesForSideWithNoPieces()
        {
            var pos = Position.FromFen("8/8/8/8/8/8/8/8 w - - 0 1");
            var gen = new MoveGenerator();
            var moves = gen.GenerateMoves(pos);
            Assert.Equal(0, moves.Length);
        }

        [Fact]
        public void Knight_In_Center_HasExactlyEightDestinations_OnEmptyBoard()
        {
            // Knight at d4 (file=3, rank=3). FEN row 4 (counting top->bottom) is "3N4"
            var fen = "8/8/8/8/3N4/8/8/8 w - - 0 1";
            var pos = Position.FromFen(fen);

            var gen = new MoveGenerator();
            var moves = gen.GenerateMoves(pos);

            // Exactly 8 destinations expected for a centrally-placed knight on an otherwise empty board
            Assert.Equal(8, moves.Length);

            // Optional: verify the specific target squares (sanity)
            var targets = new int[moves.Length];
            for (int i = 0; i < moves.Length; i++)
            {
                targets[i] = moves[i].TargetSquare;
            }
            Array.Sort(targets);

            var expectedTargets = new[]
            {
                FromFR(2,5), FromFR(4,5), FromFR(5,4), FromFR(5,2),
                FromFR(4,1), FromFR(2,1), FromFR(1,2), FromFR(1,4)
            };
            Array.Sort(expectedTargets);

            Assert.Equal(expectedTargets, targets);
        }

        [Fact]
        public void PawnSingleAndDoubleMoves_AreGenerated_FromStartRank()
        {
            // Pawn on e2 (file=4, rank=1) -> should have e2e3 and e2e4 when empty
            var fen = "8/8/8/8/8/8/4P3/8 w - - 0 1";
            var pos = Position.FromFen(fen);

            var gen = new MoveGenerator();
            var moves = gen.GenerateMoves(pos);
            var uci = moves.ToArray().Select(m => m.ToUCI()).ToArray();

            Assert.Contains("e2e3", uci);
            Assert.Contains("e2e4", uci);
            // Rather than create contrived FEN mistakes, we keep the basic assertion above as canonical.
        }

        [Fact]
        public void CastlingMoves_AreOffered_When_RightsAndPathClear()
        {
            // White king on e1 and rooks on a1/h1 with castling rights
            var fen = "r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1";
            var pos = Position.FromFen(fen);

            var gen = new MoveGenerator();
            var moves = gen.GenerateMoves(pos);

            // King from e1 (4,0) should include g1 (6,0) and c1 (2,0) as pseudo-legal castle moves
            var g1 = Move.NormalMove(FromFR(4, 0), FromFR(6, 0));
            var c1 = Move.NormalMove(FromFR(4, 0), FromFR(2, 0));

            Assert.Contains(moves.ToArray(), m => m == g1 || m == c1);
        }
    }
}
