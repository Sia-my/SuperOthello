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
        public const int ShieldMinOpponentFlips = 4;   // 상대 응수가 내 돌을 이만큼 뒤집을 수 있으면 방패
        public const int LuckySevenFlips = ScoreLogic.LuckySevenFlips;
        public const int MinusHandMinFlips = 4;
        public const int PlunderMaxMyHand = 2;         // 약탈 포함 내 손패 장수
        public const int SupplyMaxOtherCards = 1;      // 보급을 뺀 내 손패 장수
        public const int SupplyMinDeck = 2;
        public const int ComebackMaxHandEnd = 2;       // 종반: 역전의 발판 포함 손패 장수

        // A형 카드의 대표 대상 선택. 쓸 이유가 없거나 대상이 없으면 false
        public static bool TryChooseTarget(
            Data data, IsOnStone me, ISkillCard card,
            (int Row, int Col) move, out (int Row, int Col)? target) {
            target = null;

            if (!card.NeedsTarget)
                return true;

            IReadOnlyList<(int Row, int Col)> targets = card.GetTargets(data, me);

            // 폭탄: (범위 안 상대 돌 가중치 합) − (내 돌 가중치 합)이 최대인 중심. 0 이하면 쓰지 않음 (설계서 3.4.4)
            if (card is BombCard) {
                target = BestBombCenter(data, me, targets, move);
                return target != null;
            }

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

        // 폭탄 이득: 제거 가능한(고정 아닌) 돌만 합산
        public static int BombGain(Data data, IsOnStone me, (int Row, int Col) center) {
            IsOnStone opp = GameLogic.Opponent(me);
            int gain = 0;

            foreach (var (r, c) in BombCard.Area(center.Row, center.Col)) {
                Cell cell = data.Board.Cells[r, c];
                if (!CellStateLogic.CanRemove(cell))
                    continue;

                int w = WeightTables.WeightAt(data.Board, r, c);
                if (cell.Stone == opp) gain += w;
                else if (cell.Stone == me) gain -= w;
            }

            return gain;
        }

        // 폭탄 후에도 이번 착수가 가능한 중심만 (착수가 막히면 후보 자체가 사라지므로)
        private static (int Row, int Col)? BestBombCenter(
            Data data, IsOnStone me, IReadOnlyList<(int Row, int Col)> targets, (int Row, int Col) move) {
            (int Row, int Col)? best = null;
            int bestGain = 0;

            foreach (var t in targets) {
                int gain = BombGain(data, me, t);
                if (gain <= bestGain)
                    continue;

                Data after = data.Clone();
                foreach (var (r, c) in BombCard.Area(t.Row, t.Col))
                    BoardLogic.RemoveStone(after, r, c);
                if (BoardLogic.GetFlips(after, move.Row, move.Col, me).Count == 0)
                    continue;

                best = t;
                bestGain = gain;
            }

            return best;
        }

        // 내 착수 후 상대 응수 중 내 돌을 가장 많이 뒤집는 개수
        public static int MaxOpponentFlipsAfter(Data data, IsOnStone me, (int Row, int Col) move) {
            IsOnStone opp = GameLogic.Opponent(me);
            Data after = data.Clone();
            if (BoardLogic.GetFlips(after, move.Row, move.Col, me).Count > 0)
                BoardLogic.Place(after, move.Row, move.Col, me);

            int max = 0;
            foreach (var (r, c) in BoardLogic.GetValidMoves(after, opp))
                max = System.Math.Max(max, BoardLogic.GetFlips(after, r, c, opp).Count);
            return max;
        }

        // 역전의 발판: 지고 있고, 초반·중반엔 손패가 이 카드뿐 / 종반엔 손패 2장 이하
        private static bool ShouldUseComeback(Data data, IsOnStone me) {
            if (!ComebackCard.IsBehind(data, me))
                return false;

            int hand = data.HandOf(me).Count;
            return WeightTables.PhaseOf(data.Board) == WeightTables.Phase.End
                ? hand <= ComebackMaxHandEnd
                : hand == 1;
        }

        // B·C형 카드 중 이번 턴에 쓸 카드 (없으면 null)
        public static ISkillCard? ChooseConditionCard(
            Data data, IsOnStone me, (int Row, int Col) move) {
            IsOnStone opp = GameLogic.Opponent(me);
            int flips = BoardLogic.GetFlips(data, move.Row, move.Col, me).Count;

            foreach (ISkillCard card in data.HandOf(me).Cards) {
                if (card.Type == CardType.A || !card.CanUse(data, me))
                    continue;

                int myHand = data.HandOf(me).Count;   // 이 카드 포함

                bool use = card switch {
                    DoubleScoreCard => flips >= DoubleScoreMinFlips,                 // 크게 뒤집을 때만
                    LuckySevenCard => flips == LuckySevenFlips,                      // 정확히 7개일 때만
                    MinusHandCard => flips >= MinusHandMinFlips,                     // 많이 뒤집을 때
                    ShieldCard => MaxOpponentFlipsAfter(data, me, move) >= ShieldMinOpponentFlips, // 크게 당할 위험
                    SilenceCard => data.HandOf(opp).Count >= SilenceMinOpponentHand, // 상대 손패가 많을 때
                    PlunderCard => data.HandOf(opp).Count > 0 && myHand <= PlunderMaxMyHand,
                    SupplyCard => myHand - 1 <= SupplyMaxOtherCards && data.Deck.Count >= SupplyMinDeck,
                    ComebackCard => ShouldUseComeback(data, me),
                    _ => false
                };

                if (use)
                    return card;
            }

            return null;
        }
    }
}
