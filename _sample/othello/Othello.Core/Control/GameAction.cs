using Othello.Core.Cards;
using Othello.Core.Model;

namespace Othello.Core.Control;

/// <summary>한 턴의 결정: (패 사용 선택) + (착수 또는 패스). 사람과 CPU가 같은 형식을 쓴다.</summary>
public sealed record GameAction(ISkillCard? Card, Position? CardTarget, Position? Move)
{
    public bool IsPass => Move is null;

    public static GameAction Place(Position move) => new(null, null, move);
    public static GameAction Pass() => new(null, null, null);
    public static GameAction WithCard(ISkillCard card, Position? target, Position? move) => new(card, target, move);
}

/// <summary>대전 슬롯 (설계서 3.3 PlayerSlot). IsCpu는 대전 설정 때 고정된다.</summary>
public enum Difficulty { Easy, Normal, Hard }

public sealed record PlayerSlot(string Id, bool IsCpu, Difficulty Difficulty = Difficulty.Normal)
{
    public static PlayerSlot Human(string id) => new(id, false);

    public static PlayerSlot Cpu(Difficulty d) => new(d switch
    {
        Difficulty.Easy => "#CPU-쉬움",
        Difficulty.Normal => "#CPU-보통",
        _ => "#CPU-어려움"
    }, true, d);
}
