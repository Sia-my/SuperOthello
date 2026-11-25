using Othello.Core.Cards;
using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Control
{
    // 턴 진행 담당: 카드 사용 → 착수 → 점수 → 카드 획득 → 턴 종료
    // - 규칙 계산은 각 Logic 클래스에 맡기고 순서만 관리
    // - 결과는 이벤트(StateChanged, LogAdded, GameOver)로 UI에 전달

    public class GameController
    {
        // 한 번에 이 개수 이상 뒤집으면 카드 1장 획득 (초기 기준, 테스트 후 조정)
        public const int DrawThreshold = 3;

        private readonly PlayerSlot[] slots;

        public Data Data { get; }
        public (int Row, int Col)? LastMove { get; private set; }

        public event Action? StateChanged;
        public event Action<string>? LogAdded;
        public event Action<IsOnStone>? GameOver;

        // 손패 4장 중 버릴 카드 선택 (UI가 연결, 없으면 방금 뽑은 카드를 버림)
        public Func<IReadOnlyList<ISkillCard>, ISkillCard>? ChooseDiscard { get; set; }

        public GameController()
            : this(PlayerSlot.Human("흑"), PlayerSlot.Human("백"))
        {
        }

        public GameController(
            PlayerSlot black,
            PlayerSlot white,
            IEnumerable<ISkillCard>? pool = null,
            int? seed = null)
        {
            if (black.IsCpu && white.IsCpu)
                throw new ArgumentException("CPU vs CPU는 지원하지 않습니다.");

            slots = new[] { black, white };
            Data = new Data();
            GameLogic.Reset(Data);

            // 새 게임: 덱 섞기 + 양측에 카드 1장씩 지급
            Data.Deck = new CardDeck(pool ?? CardPool.Current(), seed);
            foreach (Hand hand in Data.Hands)
            {
                ISkillCard? firstCard = Data.Deck.Draw();
                if (firstCard != null)
                    hand.TryAdd(firstCard);
            }
        }

        public PlayerSlot SlotOf(IsOnStone stone)
            => slots[ScoreLogic.PlayerIndex(stone)];

        public PlayerSlot CurrentSlot => SlotOf(Data.Current);

        public IReadOnlyList<(int Row, int Col)> ValidMoves()
            => BoardLogic.GetValidMoves(Data, Data.Current);

        public void Start()
        {
            Log($"게임 시작: 흑 {slots[0].Id} vs 백 {slots[1].Id}");
            SkipTurnsWithoutMoves();
            StateChanged?.Invoke();
        }

        // 지금 이 카드를 쓸 수 있는가 (침묵이면 불가)
        public bool CanUseCard(ISkillCard card)
            => !Data.IsOver
               && !GameStateLogic.IsSilenced(Data, Data.Current)
               && Data.HandOf(Data.Current).Cards.Contains(card)
               && card.CanUse(Data, Data.Current);

        // 한 턴 실행: 복제본에서 먼저 검증 → 성공할 때만 실제 데이터에 적용
        public bool TryExecuteTurn(GameAction action, out string error)
        {
            error = "";

            if (Data.IsOver)
            {
                error = "게임이 이미 끝났습니다.";
                return false;
            }

            try
            {
                Apply(Data.Clone(), action, log: false);
            }
            catch (InvalidOperationException ex)
            {
                error = ex.Message;
                return false;
            }

            Apply(Data, action, log: true);
            EndTurn();
            return true;
        }

        // 카드 없는 착수·패스용 단축 메서드
        public bool TryPlace(int row, int col, out string error)
            => TryExecuteTurn(GameAction.Place(row, col), out error);

        public bool TryPass(out string error)
            => TryExecuteTurn(GameAction.Pass(), out error);

        private void Apply(Data data, GameAction action, bool log)
        {
            IsOnStone me = data.Current;
            Hand hand = data.HandOf(me);

            // 1. 카드 사용 (선택) → 착수보다 먼저 적용
            if (action.Card is ISkillCard card)
            {
                if (GameStateLogic.IsSilenced(data, me))
                    throw new InvalidOperationException("침묵 상태라 패를 쓸 수 없습니다.");

                if (!hand.Cards.Contains(card))
                    throw new InvalidOperationException("손패에 없는 패입니다.");

                if (!card.CanUse(data, me))
                    throw new InvalidOperationException($"[{card.Name}]을(를) 지금 쓸 수 없습니다.");

                string message = card.Apply(data, me, action.CardTarget);
                hand.Remove(card);
                data.Deck.Return(card); // 사용한 카드는 덱으로

                if (log)
                    Log($"{Tag(me)} [{card.Name}] {message}");
            }

            // 2. 착수 또는 패스
            if (action.Move.HasValue)
            {
                (int Row, int Col) move = action.Move.Value;
                List<(int Row, int Col)> flips =
                    BoardLogic.Place(data, move.Row, move.Col, me);

                bool opponentShielded = GameStateLogic.IsShielded(
                    data, GameLogic.Opponent(me));

                var (gain, penalty) = ScoreLogic.OnPlace(
                    data, me, flips.Count, opponentShielded);

                GameLogic.RegisterMove(data);

                if (log)
                {
                    LastMove = move;
                    Log($"{Tag(me)} ({move.Row},{move.Col}) 착수, " +
                        $"{flips.Count}개 뒤집음 (+{gain} / 상대 -{penalty})");

                    // 3. 카드 획득 (검증용 복제본에서는 뽑지 않음)
                    if (flips.Count >= DrawThreshold)
                        DrawCard(data, me);
                }
            }
            else
            {
                GameLogic.RegisterPass(data);
                if (log)
                    Log($"{Tag(me)} 패스");
            }
        }

        // 카드 획득: 손패가 가득 차면 사람은 1장 버리기, CPU는 획득 생략
        private void DrawCard(Data data, IsOnStone player)
        {
            Hand hand = data.HandOf(player);
            ISkillCard? drawn = data.Deck.Draw();

            if (drawn == null)
            {
                Log($"{Tag(player)} 덱이 비어 패를 받지 못함");
                return;
            }

            if (!hand.IsFull)
            {
                hand.TryAdd(drawn);
                Log($"{Tag(player)} [{drawn.Name}] 획득");
                return;
            }

            // CPU: 새 카드를 덱으로 되돌림
            if (SlotOf(player).IsCpu)
            {
                data.Deck.Return(drawn);
                Log($"{Tag(player)} 손패가 가득 차 획득 생략");
                return;
            }

            // 사람: 4장 중 1장 선택해서 버림
            hand.ForceAdd(drawn);
            ISkillCard discard = ChooseDiscard?.Invoke(hand.Cards) ?? drawn;
            if (!hand.Cards.Contains(discard))
                discard = drawn;

            hand.Remove(discard);
            data.Deck.Return(discard);
            Log($"{Tag(player)} [{drawn.Name}] 획득, [{discard.Name}] 버림");
        }

        private void EndTurn()
        {
            // 4. 턴 종료 (순서 주의: 차례를 바꾸기 전에 상태부터 정리)
            IsOnStone endedPlayer = Data.Current;
            CellStateLogic.ProcessTurnEnd(Data);
            GameStateLogic.ProcessTurnEnd(Data, endedPlayer);

            // 배율 초기화 + 차례 교체 + 턴 번호 증가
            GameLogic.EndTurn(Data);

            if (GameLogic.ShouldFinish(Data))
            {
                Finish();
                return;
            }

            SkipTurnsWithoutMoves();
            if (!Data.IsOver)
                StateChanged?.Invoke();
        }

        private void SkipTurnsWithoutMoves()
        {
            if (!GameLogic.TryAutoPass(Data, out IsOnStone passed))
                return;

            Log($"{Tag(passed)} 둘 곳이 없어 자동 패스");

            if (GameLogic.BothPlayersCannotMove(Data))
            {
                Log("양측 모두 둘 곳이 없습니다.");
                Finish();
            }
        }

        private void Finish()
        {
            if (Data.IsOver)
                return;

            IsOnStone winner = GameLogic.Finish(Data);
            string result = winner == IsOnStone.Empty
                ? "무승부"
                : $"{SlotOf(winner).Id} 승리";

            // 행동 로그만으로 최종 점수를 맞춰 볼 수 있게 보너스도 기록 (설계서 2.3 신뢰성)
            Log($"종료 보너스 - 흑 +{ScoreLogic.FinalBonusOf(Data, IsOnStone.Black)} / " +
                $"백 +{ScoreLogic.FinalBonusOf(Data, IsOnStone.White)}");
            Log($"게임 종료 - 흑 {Data.Scores[0]} : 백 {Data.Scores[1]} -> {result}");
            StateChanged?.Invoke();
            GameOver?.Invoke(winner);
        }

        private string Tag(IsOnStone stone)
            => $"[{Data.TurnNumber}턴] {(stone == IsOnStone.Black ? "흑" : "백")}";

        private void Log(string message)
            => LogAdded?.Invoke(message);
    }
}
