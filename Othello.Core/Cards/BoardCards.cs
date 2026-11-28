using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Cards {
    // A형(판 변경) 카드: 폭탄 (번호 = 설계서 2.2 후보 표)
    // - 돌 제거는 BoardLogic.RemoveStone (고정 돌은 남음)

    /// <summary>5. 폭탄 (A): 지정 칸 중심 3×3 범위의 돌을 모두 제거. 제거된 내 돌 1개당 사용자 -1.</summary>
    public sealed class BombCard : SkillCardBase {
        public const int Radius = 1;   // 중심 ±1 = 3×3

        public override int Id => 5;
        public override string Name => "폭탄";
        public override string Description => "지정 칸 중심 3×3 범위의 돌을 모두 제거한다 (고정 돌 제외)";
        public override CardType Type => CardType.A;
        public override string ScoreEffect => "제거된 내 돌 1개당 사용자 -1";

        // 중심 칸의 3×3 범위 (판 밖은 제외)
        public static IEnumerable<(int Row, int Col)> Area(int row, int col) {
            for (int r = row - Radius; r <= row + Radius; r++)
                for (int c = col - Radius; c <= col + Radius; c++)
                    if (BoardLogic.InBounds(r, c))
                        yield return (r, c);
        }

        // 범위 안에 제거 가능한 돌이 1개 이상 있는 모든 칸 (빈칸 중심도 가능)
        public override IReadOnlyList<(int Row, int Col)> GetTargets(Data data, IsOnStone user) {
            var targets = new List<(int Row, int Col)>();

            for (int row = 0; row < Board.Size; row++) {
                for (int col = 0; col < Board.Size; col++) {
                    foreach (var (r, c) in Area(row, col)) {
                        if (CellStateLogic.CanRemove(data.Board.Cells[r, c])) {
                            targets.Add((row, col));
                            break;
                        }
                    }
                }
            }

            return targets;
        }

        public override string Apply(Data data, IsOnStone user, (int Row, int Col)? target) {
            var (row, col) = RequireTarget(data, user, target);
            int removed = 0;
            int mine = 0;

            foreach (var (r, c) in Area(row, col)) {
                IsOnStone stone = data.Board.Cells[r, c].Stone;
                if (!BoardLogic.RemoveStone(data, r, c))
                    continue;

                removed++;
                if (stone == user)
                    mine++;
            }

            if (mine > 0)
                ScoreLogic.OnCard(data, user, -mine);

            return $"({row},{col}) 중심 폭탄: {removed}개 제거" + (mine > 0 ? $" (-{mine}점)" : "");   // 내 돌을 안 지웠으면 점수 생략
        }
    }
}
