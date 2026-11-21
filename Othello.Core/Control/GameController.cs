using System;
using System.Collections.Generic;
using System.Linq;
using Othello.Core.Cards;
using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Control
{
    /*
     * GameController.cs - 게임 흐름 조정 (UI에서 사용할 진입점)
     *
     * [게임 만들기]
     *   new GameController()
     *   new GameController(blackSlot, whiteSlot, cardPool, seed)
     *
     * [UI에서 사용]
     *   controller.Data                         현재 게임 데이터 조회
     *   controller.Start()                      게임 시작 알림
     *   controller.ValidMoves()                 현재 차례의 착수 가능 좌표
     *   controller.CanUseCard(card)             카드 사용 가능 여부
     *   controller.TryExecuteTurn(action, out error)
     *                                         카드 + 착수/패스를 한 턴으로 실행
     *   controller.TryPlace(row, col, out error) 카드 없는 일반 착수
     *   controller.TryPass(out error)           카드 없는 패스
     *   controller.SlotOf(stone)                플레이어 설정 조회
     *   controller.CurrentSlot                  현재 플레이어 설정
     *   controller.LastMove                     마지막 착수 좌표
     *   controller.ChooseDiscard                손패 4장일 때 버릴 카드 선택 콜백
     *
     * [UI 이벤트]
     *   StateChanged   게임 상태가 갱신되면 호출 (화면 다시 그리기)
     *   LogAdded       로그 문장 전달
     *   GameOver       승자 전달 (Empty면 무승부)
     *
     * [내부 책임 분리]
     *   BoardLogic       오셀로 착수/뒤집기
     *   CellStateLogic   봉인 등 칸별 턴 종료 효과
     *   GameStateLogic   침묵/방패 등 플레이어별 턴 종료 효과
     *   ScoreLogic       착수 점수/방패 감점/종료 점수
     *   GameLogic        턴 교체/패스/종료 판단
     *
     * 실제 데이터는 Data.cs + CardData.cs의 partial Data에 저장한다.
     * 초능력 카드 자체의 동작은 ISkillCard.Apply에서 실행한다.
     */
    public class GameController
    {
        // 한 번에 이 개수 이상 뒤집으면 카드 1장 획득 (기존 규칙)
        public const int DrawThreshold = 3;

        private readonly PlayerSlot[] slots;

        public Data Data { get; }
        public (int Row, int Col)? LastMove { get; private set; }

        public event Action? StateChanged;
        public event Action<string>? LogAdded;
        public event Action<IsOnStone>? GameOver;

        // 사람이 손패 4장 중 버릴 카드를 선택할 때 UI에서 연결한다.
        // 없으면 방금 뽑은 카드를 버린다.
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

            // 기존 NewGame 규칙: 공용 덱을 섞고 양측에 카드 한 장씩 지급한다.
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

        // 플레이어가 카드 사용을 시도할 수 있는가?
        public bool CanUseCard(ISkillCard card)
            => !Data.IsOver
               && !GameStateLogic.IsSilenced(Data, Data.Current)
               && Data.HandOf(Data.Current).Cards.Contains(card)
               && card.CanUse(Data, Data.Current);

        // 카드 사용 + 착수(또는 패스)를 한 번의 턴으로 실행한다.
        // 먼저 복제된 데이터로 검증하므로 실패한 행동은 실제 게임판에 적용하지 않는다.
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

        // 카드 없는 착수/패스용 편의 메서드. 처리 경로는 TryExecuteTurn으로 통일.
        public bool TryPlace(int row, int col, out string error)
            => TryExecuteTurn(GameAction.Place(row, col), out error);

        public bool TryPass(out string error)
            => TryExecuteTurn(GameAction.Pass(), out error);

        private void Apply(Data data, GameAction action, bool log)
        {
            IsOnStone me = data.Current;
            Hand hand = data.HandOf(me);

            // 1. 카드 사용 (선택 사항). 카드 효과가 착수보다 먼저 적용된다.
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
                data.Deck.Return(card); // 사용한 카드는 덱으로 돌려보내 다시 섞는다.

                if (log)
                    Log($"{Tag(me)} [{card.Name}] {message}");
            }

            // 2. 돌을 놓거나 패스한다.
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

                    // 검증용 복제본에서는 카드를 뽑지 않는다.
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

        // 기존 카드 획득 규칙: 3개 이상 뒤집으면 한 장 획득.
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

            // CPU는 손패가 가득 차면 새 카드를 덱으로 반환한다.
            if (SlotOf(player).IsCpu)
            {
                data.Deck.Return(drawn);
                Log($"{Tag(player)} 손패가 가득 차 획득 생략");
                return;
            }

            // 사람은 4장 중 한 장을 선택하여 버린다.
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
            // 순서 중요: 현재 턴의 플레이어가 바뀌기 전에 상태를 처리한다.
            IsOnStone endedPlayer = Data.Current;
            CellStateLogic.ProcessTurnEnd(Data);
            GameStateLogic.ProcessTurnEnd(Data, endedPlayer);

            // 배율 초기화, 차례 교체, 턴 번호 증가.
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
