
using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Cpu {
    // 평가함수: 지금 판이 me에게 얼마나 유리한지 EvalResult로 계산
    // - 난이도에서 쓰지 않는 항목은 계산을 건너뜀 (속도용)
    // - 항상 me(CPU) 기준으로 계산

    public static class Evaluator {
        public static EvalResult Evaluate(Data data, IsOnStone me, Difficulty difficulty) {
            IsOnStone opp = GameLogic.Opponent(me);
            EvalResult result = EvalResult.For(difficulty);

            if (result.UseStability)
                result.Stability = StabilityLogic.Count(data, me)
                                 - StabilityLogic.Count(data, opp);

            if (result.UseMobility)
                result.Mobility = BoardLogic.GetValidMoves(data, me).Count
                                - BoardLogic.GetValidMoves(data, opp).Count;

            result.Position = PositionScore(data, me);
            result.Score = data.Scores[ScoreLogic.PlayerIndex(me)]
                         - data.Scores[ScoreLogic.PlayerIndex(opp)];

            return result;
        }

        // 내 돌 가중치 합 − 상대 돌 가중치 합
        public static int PositionScore(Data data, IsOnStone me) {
            var phase = WeightTables.PhaseOf(data.Board);
            Cell[,] cells = data.Board.Cells;
            int sum = 0;

            for (int row = 0; row < Board.Size; row++) {
                for (int col = 0; col < Board.Size; col++) {
                    IsOnStone stone = cells[row, col].Stone;
                    if (stone == IsOnStone.Empty) continue;

                    int w = WeightTables.WeightAt(data.Board, row, col, phase);
                    sum += stone == me ? w : -w;
                }
            }

            return sum;
        }
    }
}
