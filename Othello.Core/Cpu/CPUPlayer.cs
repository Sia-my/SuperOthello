using Othello.Core.Cards;
using Othello.Core.Control;
using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Cpu {
    // CPU의 한 턴 행동 선택 (설계서 3.4.4 ~ 3.4.6)
    // 1. 위치 가중치 상위 3칸을 후보로 선택
    // 2. A형 카드를 각 후보에 적용한 경우 추가 (최대 3 × 4 = 12개)
    // 3. 후보마다 상대 응수 1수까지 읽고 가장 나쁜 결과로 평가 (깊이 2, 알파-베타)
    // 4. 카드를 안 쓴 경우 B·C형 카드를 조건문으로 판단
    // - 전달받은 Data는 바꾸지 않음 (내부에서 복제본만 사용)
    // - 계산 중 예외가 나면 가중치 최고 칸에 일반 착수 (설계서 2.5)

    public class CPUPlayer {
        public const int CandidateMoves = 3;

        private readonly Random rng;

        public Difficulty Difficulty { get; }

        // 알파-베타 가지치기 사용 여부 (끄면 결과는 같고 느려짐, 설계서 3.5 TC-25 검증용)
        public bool UsePruning { get; init; } = true;

        public CPUPlayer(Difficulty difficulty, int? seed = null) {
            Difficulty = difficulty;
            rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public GameAction ChooseAction(Data data) {
            try {
                return Choose(data.Clone());
            }
            catch (Exception) {
                return FallbackAction(data);
            }
        }

        // 대체 행동: 카드 없이 가중치 최고 칸 (둘 곳이 없으면 패스)
        public GameAction FallbackAction(Data data) {
            var moves = TopMoves(data, data.Current, 1);
            return moves.Count == 0 ? GameAction.Pass() : GameAction.Place(moves[0]);
        }

        // ───────────── 행동 선택 ─────────────

        private GameAction Choose(Data data) {
            IsOnStone me = data.Current;
            var moves = TopMoves(data, me, CandidateMoves);

            // 둘 곳이 없으면 패스 (자유 착수는 최종 12종에 없음)
            if (moves.Count == 0)
                return GameAction.Pass();

            // 카드 없는 후보부터 평가 → 동점이면 카드를 아끼는 쪽이 남음
            var (best, bestValue) = Search(data, me, moves.Select(CpuCandidate.Plain), null, EvalResult.MinValue);
            if (best == null)
                return FallbackAction(data);

            if (GameStateLogic.IsSilenced(data, me))
                return best.ToAction();

            switch (Difficulty) {
                case Difficulty.Easy:
                    return EasyCardAction(data, me, best);

                case Difficulty.Normal:
                    // 손패 앞에서부터 확인 → 카드 없는 최선보다 좋아지는 첫 카드 사용
                    foreach (ISkillCard card in UsableACards(data, me)) {
                        var (withCard, value) = Search(data, me, CardCandidates(data, me, card, moves), null, bestValue);
                        if (withCard != null) {
                            best = withCard;
                            bestValue = value;
                            break;
                        }
                    }
                    break;

                case Difficulty.Hard:
                    // 모든 A형 카드 후보를 한꺼번에 비교
                    var all = UsableACards(data, me).SelectMany(card => CardCandidates(data, me, card, moves));
                    var (bestCard, cardValue) = Search(data, me, all, best, bestValue);
                    best = bestCard ?? best;
                    bestValue = cardValue;
                    break;
            }

            // A형을 안 썼을 때만 B·C형 판단 (한 턴에 카드는 1장)
            if (!best.UsesCard) {
                ISkillCard? condition = CpuCardLogic.ChooseConditionCard(data, me, best.Move);
                if (condition != null)
                    return GameAction.WithCard(condition, null, best.Move);
            }

            return best.ToAction();
        }

        // 쉬움: 손익 계산 없이 1/3 확률로 아무 카드나 아무 대상에 사용
        private GameAction EasyCardAction(Data data, IsOnStone me, CpuCandidate best) {
            var usable = data.HandOf(me).Cards.Where(c => c.CanUse(data, me)).ToList();
            if (usable.Count == 0 || rng.Next(3) != 0)
                return best.ToAction();

            ISkillCard card = usable[rng.Next(usable.Count)];
            (int Row, int Col)? target = null;

            if (card.NeedsTarget) {
                var targets = card.GetTargets(data, me);
                target = targets[rng.Next(targets.Count)];
            }

            // 카드를 쓴 뒤에도 둘 수 있는 칸이어야 함
            var after = data.Clone();
            card.Apply(after, me, target);

            var movesAfter = TopMoves(after, me, 1);
            if (movesAfter.Count == 0)
                return best.ToAction();   // 쓰면 둘 곳이 사라지는 카드는 포기

            var move = BoardLogic.GetFlips(after, best.Move.Row, best.Move.Col, me).Count > 0
                ? best.Move
                : movesAfter[0];

            return GameAction.WithCard(card, target, move);
        }

        // ───────────── 탐색 ─────────────

        // 깊이 2 알파-베타: 후보 평가 = 상대 응수 결과 중 가장 나쁜 값
        // start보다 확실히 좋은 후보가 있을 때만 best가 바뀜
        private (CpuCandidate? Best, EvalResult Value) Search(
            Data data, IsOnStone me, IEnumerable<CpuCandidate> candidates,
            CpuCandidate? best, EvalResult start) {
            IsOnStone opp = GameLogic.Opponent(me);
            EvalResult bestValue = start;

            foreach (CpuCandidate cand in candidates) {
                Data after = data.Clone();
                if (!CpuSimLogic.TryApply(after, cand, me))
                    continue;

                EvalResult worst = EvalResult.MaxValue;
                var replies = BoardLogic.GetValidMoves(after, opp);

                if (replies.Count == 0) {
                    worst = Evaluator.Evaluate(after, me, Difficulty);   // 상대가 패스하는 경우
                }
                else {
                    foreach (var reply in replies) {
                        Data next = after.Clone();
                        CpuSimLogic.PlaceAndScore(next, reply, opp);

                        EvalResult value = Evaluator.Evaluate(next, me, Difficulty);
                        if (value.CompareTo(worst) < 0)
                            worst = value;

                        // 가지치기: 이미 찾은 최선보다 나빠졌으면 나머지 응수는 볼 필요 없음
                        if (UsePruning && worst.CompareTo(bestValue) <= 0)
                            break;
                    }
                }

                if (worst.CompareTo(bestValue) > 0) {
                    best = cand;
                    bestValue = worst;
                }
            }

            return (best, bestValue);
        }

        // ───────────── 후보 만들기 ─────────────

        // 착수 가능 칸을 가중치 → 뒤집는 개수 순으로 정렬해 상위 count개
        private static List<(int Row, int Col)> TopMoves(Data data, IsOnStone me, int count)
            => BoardLogic.GetValidMoves(data, me)
                .OrderByDescending(m => WeightTables.WeightAt(data.Board, m.Row, m.Col))
                .ThenByDescending(m => BoardLogic.GetFlips(data, m.Row, m.Col, me).Count)
                .Take(count)
                .ToList();

        private static IEnumerable<ISkillCard> UsableACards(Data data, IsOnStone me)
            => data.HandOf(me).Cards
                .Where(c => c.Type == CardType.A && c.CanUse(data, me))
                .ToList();   // 탐색 중 손패가 바뀌어도 안전하게 미리 복사

        private static IEnumerable<CpuCandidate> CardCandidates(
            Data data, IsOnStone me, ISkillCard card, IEnumerable<(int Row, int Col)> moves) {
            foreach (var move in moves) {
                if (CpuCardLogic.TryChooseTarget(data, me, card, move, out var target))
                    yield return new CpuCandidate(card, target, move);
            }
        }
    }
}
