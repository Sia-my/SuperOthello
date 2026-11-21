using Othello.Core.Cards;
using Othello.Core.Model;
using Othello.Core.Rules;

namespace Othello.Core.Control;

/// <summary>
/// 턴 진행, 입력 검증, 패 획득 판정, 종료 처리 (설계서 3.4.1).
/// UI는 이 클래스만 호출하고, 결과는 이벤트로 받는다. 화면 코드에 의존하지 않는다.
/// </summary>
public class GameController
{
    /// <summary>한 번의 착수로 이 개수 이상 뒤집으면 패 1장 획득 (초기 기준, 테스트 후 조정).</summary>
    public const int DrawThreshold = 3;

    private readonly PlayerSlot[] slots;

    public GameState State { get; private set; }
    public Position? LastMove { get; private set; }

    public event Action? StateChanged;
    public event Action<string>? LogAdded;
    public event Action<Stone>? GameOver;   // 무승부면 Stone.Empty

    /// <summary>
    /// 사람의 손패가 넘칠 때 버릴 패를 고르는 콜백 (UI가 대화상자로 구현).
    /// 설정하지 않으면 새로 받은 패를 버린다.
    /// </summary>
    public Func<IReadOnlyList<ISkillCard>, ISkillCard>? ChooseDiscard { get; set; }

    public GameController(PlayerSlot black, PlayerSlot white, IEnumerable<ISkillCard>? pool = null, int? seed = null)
    {
        if (black.IsCpu && white.IsCpu)
            throw new ArgumentException("CPU vs CPU는 지원하지 않습니다.");   // 운영 세부 기준

        slots = new[] { black, white };
        State = GameState.NewGame(pool ?? CardPool.Current(), seed);
    }

    public PlayerSlot SlotOf(Stone s) => slots[s.Index()];
    public PlayerSlot CurrentSlot => SlotOf(State.Current);

    public IReadOnlyList<Position> ValidMoves() => State.Board.GetValidMoves(State.Current);

    /// <summary>게임 화면이 열린 뒤 한 번 호출. 첫 턴부터 둘 곳이 없는 경우까지 처리한다.</summary>
    public void Start()
    {
        Log($"게임 시작: 흑 {slots[0].Id} vs 백 {slots[1].Id}");
        SkipTurnsWithoutMoves();
        StateChanged?.Invoke();
    }

    /// <summary>패를 쓸 수 있는가 (침묵 상태면 불가).</summary>
    public bool CanUseCard(ISkillCard card)
        => !State.IsOver
           && !State.StatusOf(State.Current).Silenced
           && State.HandOf(State.Current).Cards.Contains(card)
           && card.CanUse(State, State.Current);

    /// <summary>
    /// 한 턴 실행. 잘못된 행동이면 상태를 전혀 바꾸지 않고 false와 이유를 돌려준다.
    /// (복사본에서 먼저 실행해 보고 성공할 때만 원본에 적용)
    /// </summary>
    public bool TryExecuteTurn(GameAction action, out string error)
    {
        error = "";
        if (State.IsOver) { error = "게임이 이미 끝났습니다."; return false; }

        try
        {
            Apply(State.Clone(), action, log: false);
        }
        catch (InvalidOperationException ex)
        {
            error = ex.Message;
            return false;
        }

        Apply(State, action, log: true);
        EndTurn();
        return true;
    }

    // ───────────────────────── 내부 처리 ─────────────────────────

    private void Apply(GameState s, GameAction a, bool log)
    {
        Stone me = s.Current;
        Hand hand = s.HandOf(me);

        // ① 패 사용 (선택)
        if (a.Card is ISkillCard card)
        {
            if (s.StatusOf(me).Silenced) throw new InvalidOperationException("침묵 상태라 패를 쓸 수 없습니다.");
            if (!hand.Cards.Contains(card)) throw new InvalidOperationException("손패에 없는 패입니다.");
            if (!card.CanUse(s, me)) throw new InvalidOperationException($"[{card.Name}]을(를) 지금 쓸 수 없습니다.");

            string msg = card.Apply(s, me, a.CardTarget);
            hand.Remove(card);
            s.Deck.Return(card);   // F2-7 사용한 패는 덱으로
            if (log) Log($"{Tag(me)} [{card.Name}] {msg}");
        }

        // ② 착수 또는 패스
        if (a.Move is Position m)
        {
            List<Position> flips = s.Board.Place(m.Row, m.Col, me);   // 둘 수 없으면 예외
            var (gain, penalty) = ScoreRule.OnPlace(s, me, flips.Count);
            s.ConsecutivePasses = 0;
            if (log)
            {
                LastMove = m;
                Log($"{Tag(me)} {m} 착수, {flips.Count}개 뒤집음 (+{gain} / 상대 -{penalty})");
            }

            // ③ 패 획득 판정 (복사본에서는 드로우하지 않는다)
            if (log && flips.Count >= DrawThreshold) DrawCard(s, me);
        }
        else
        {
            if (s.Board.HasValidMove(me)) throw new InvalidOperationException("둘 수 있는 곳이 있으면 패스할 수 없습니다.");
            s.ConsecutivePasses++;
            if (log) Log($"{Tag(me)} 패스");
        }
    }

    private void DrawCard(GameState s, Stone me)
    {
        Hand hand = s.HandOf(me);
        ISkillCard? drawn = s.Deck.Draw();
        if (drawn == null) { Log($"{Tag(me)} 덱이 비어 패를 받지 못함"); return; }

        if (!hand.IsFull)
        {
            hand.TryAdd(drawn);
            Log($"{Tag(me)} [{drawn.Name}] 획득");
            return;
        }

        // F2-3 손패 초과: CPU는 획득 생략, 사람은 4장 중 1장 버리기
        if (SlotOf(me).IsCpu)
        {
            s.Deck.Return(drawn);
            Log($"{Tag(me)} 손패가 가득 차 획득 생략");
            return;
        }

        hand.ForceAdd(drawn);
        ISkillCard discard = ChooseDiscard?.Invoke(hand.Cards) ?? drawn;
        hand.Remove(discard);
        s.Deck.Return(discard);
        Log($"{Tag(me)} [{drawn.Name}] 획득, [{discard.Name}] 버림");
    }

    private void EndTurn()
    {
        Stone me = State.Current;

        // ④ 상태 갱신: 봉인 턴 감소, 이번 턴 한정 효과 해제
        StatusEffects.ProcessTurnEnd(State.Board.Cells);
        State.TurnScoreMultiplier = 1;
        State.StatusOf(me).Silenced = false;              // 내 침묵은 내 턴이 끝나면 풀림
        State.StatusOf(me.Opponent()).Shielded = false;   // 상대 방패는 내 턴 동안만 유효

        State.Current = me.Opponent();
        State.TurnNumber++;

        // ⑤ 종료 판정
        if (State.Board.IsFull || State.ConsecutivePasses >= 2)
        {
            Finish();
            return;
        }

        SkipTurnsWithoutMoves();
        StateChanged?.Invoke();
    }

    /// <summary>F1-4 둘 곳이 없으면 자동 패스. 양측 모두 없으면 종료.</summary>
    private void SkipTurnsWithoutMoves()
    {
        // TODO: 자유 착수 패(1번)를 넣으면, 보유 시 자동 패스 대신 사용 여부를 물어본다 (2.1.3)
        if (State.IsOver || State.Board.HasValidMove(State.Current)) return;

        Log($"{Tag(State.Current)} 둘 곳이 없어 자동 패스");
        State.ConsecutivePasses++;
        State.Current = State.Current.Opponent();

        if (!State.Board.HasValidMove(State.Current))
        {
            Log("양측 모두 둘 곳이 없습니다.");
            Finish();
        }
    }

    private void Finish()
    {
        State.IsOver = true;
        ScoreRule.ApplyFinalBonus(State);
        Stone winner = ScoreRule.Winner(State);
        Log($"게임 종료 — 흑 {State.Scores[0]} : 백 {State.Scores[1]} → " +
            (winner == Stone.Empty ? "무승부" : $"{SlotOf(winner).Id} 승리"));

        StateChanged?.Invoke();
        GameOver?.Invoke(winner);
        // TODO(4주차): RecordRepository.Save(...) 로 전적 저장
    }

    private string Tag(Stone s) => $"[{State.TurnNumber}턴] {s.ToKorean()}";
    private void Log(string msg) => LogAdded?.Invoke(msg);
}
