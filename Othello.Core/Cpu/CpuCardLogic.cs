
using System.Collections.Generic;
using System.Linq;
using Othello.Core.Cards;
using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Cpu {
    // CPU의 카드 판단 규칙
    // - A형(판 변경): 카드마다 대표 대상 1곳만 고름 → 후보 수가 대상 수만큼 늘지 않음 (설계서 3.4.4 표)
    // - B·C형(점수·운영): 평가함수로 차이가 안 나므로 조건문으로 결정 (설계서 3.4.5 표)
    // - 새 카드 추가 시 이 파일의 switch에 한 줄씩 추가

    public static class CpuCardLogic {
        // B·C형 사용 조건 수치 (초기 기준, 테스트 후 조정)
        public const int DoubleScoreMinFlips = 5;
        public const int SilenceMinOpponentHand = 2;

        // A형 카드의 대표 대상 선택. 쓸 이유가 없거나 대상이 없으면 false
        public static bool TryChooseTarget(
            Data data, IsOnStone me, ISkillCard card,
            (int Row, int Col) move, out (int Row, int Col)? target) {
            target = null;

            if (!card.NeedsTarget)
                return true;

            IReadOnlyList<(int Row, int Col)> targets = card.GetTargets(data, me);
            IEnumerable<(int Row, int Col)> pool = card switch {
                // 전향: 상대 돌 중 가중치 최고 (모서리는 GetTargets에서 이미 제외)
                ConvertCard => targets,

                // 고정: 이미 확정돌인 돌은 고정할 필요 없음
                FixCard => targets.Where(t => !StabilityLogic.IsStable(data, t.Row, t.Col)),

                // 봉인: 상대가 둘 수 있는 칸 중 가중치 최고 (내가 둘 칸은 제외)
                SealCard => targets.Where(t => t != move
                    && BoardLogic.GetFlips(data, t.Row, t.Col, GameLogic.Opponent(me)).Count > 0),

                // 그 외: 착수 칸을 뺀 대상 중 가중치 최고
                _ => targets.Where(t => t != move)
            };

            var best = pool
                .OrderByDescending(t => WeightTables.WeightAt(data.Board, t.Row, t.Col))
                .Select(t => ((int Row, int Col)?)t)
                .FirstOrDefault();

            if (best == null)
                return false;

            target = best;
            return true;
        }

        // B·C형 카드 중 이번 턴에 쓸 카드 (없으면 null)
        public static ISkillCard? ChooseConditionCard(
            Data data, IsOnStone me, (int Row, int Col) move) {
            IsOnStone opp = GameLogic.Opponent(me);
            int flips = BoardLogic.GetFlips(data, move.Row, move.Col, me).Count;

            foreach (ISkillCard card in data.HandOf(me).Cards) {
                if (card.Type == CardType.A || !card.CanUse(data, me))
                    continue;

                bool use = card switch {
                    DoubleScoreCard => flips >= DoubleScoreMinFlips,               // 크게 뒤집을 때만
                    SilenceCard => data.HandOf(opp).Count >= SilenceMinOpponentHand, // 상대 손패가 많을 때
                    _ => false   // TODO: 방패·보급·약탈 등은 12종 확정 후 추가
                };

                if (use)
                    return card;
            }

            return null;
        }
    }
}
