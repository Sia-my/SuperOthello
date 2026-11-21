using Othello.Core.Model;

namespace Othello.Core.Cards;

/// <summary>A: 판 변경형, B: 점수형, C: 운영형 (설계서 패 유형 표).</summary>
public enum CardType { A, B, C }

/// <summary>
/// 모든 초능력 패의 공통 인터페이스.
/// 패는 상태를 갖지 않는다(같은 인스턴스를 복사본 GameState끼리 공유해도 안전).
/// 새 패 = 이 인터페이스를 구현한 클래스 1개 추가 (비기능 요구사항: 유지보수성).
/// </summary>
public interface ISkillCard
{
    int Id { get; }               // Psionics 테이블의 PsionicId와 맞춘다
    string Name { get; }
    string Description { get; }   // 툴팁·도감용 한 줄 설명
    CardType Type { get; }
    bool NeedsTarget { get; }

    /// <summary>지금 user가 이 패를 쓸 수 있는가 (대상이 하나라도 있는가 포함).</summary>
    bool CanUse(GameState s, Stone user);

    /// <summary>대상으로 고를 수 있는 칸. NeedsTarget이 false면 빈 목록.</summary>
    IReadOnlyList<Position> GetTargets(GameState s, Stone user);

    /// <summary>효과 적용. 행동 로그에 남길 문장을 돌려준다.</summary>
    string Apply(GameState s, Stone user, Position? target);
}

/// <summary>공통 부분을 모아 둔 기반 클래스.</summary>
public abstract class SkillCardBase : ISkillCard
{
    public abstract int Id { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract CardType Type { get; }
    public virtual bool NeedsTarget => true;

    public virtual bool CanUse(GameState s, Stone user)
        => !NeedsTarget || GetTargets(s, user).Count > 0;

    public virtual IReadOnlyList<Position> GetTargets(GameState s, Stone user)
        => Array.Empty<Position>();

    public abstract string Apply(GameState s, Stone user, Position? target);

    protected Position RequireTarget(GameState s, Stone user, Position? target)
    {
        if (target is not Position t || !GetTargets(s, user).Contains(t))
            throw new InvalidOperationException($"[{Name}] 대상이 올바르지 않습니다.");
        return t;
    }

    public override string ToString() => Name;
}
