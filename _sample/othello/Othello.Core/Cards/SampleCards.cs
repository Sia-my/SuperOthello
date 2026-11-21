using Othello.Core.Model;
using Othello.Core.Rules;

namespace Othello.Core.Cards;

// 지금까지 구현한 패 5종. 나머지는 최종 12종 확정 후 같은 방식으로 추가한다.
// 번호(Id)는 설계서 '초능력 패 후보 24종' 표의 번호를 그대로 쓴다.

/// <summary>4. 전향 (A): 상대 돌 1개를 내 돌로 바꿈. 모서리 불가. 사용자 −2.</summary>
public sealed class ConvertCard : SkillCardBase
{
    public override int Id => 4;
    public override string Name => "전향";
    public override string Description => "상대 돌 1개를 내 돌로 바꾼다 (모서리 불가, 사용자 -2점)";
    public override CardType Type => CardType.A;

    private static bool IsCorner(int r, int c)
        => (r == 0 || r == Board.Size - 1) && (c == 0 || c == Board.Size - 1);

    public override IReadOnlyList<Position> GetTargets(GameState s, Stone user)
    {
        var list = new List<Position>();
        Stone opp = user.Opponent();
        for (int r = 0; r < Board.Size; r++)
            for (int c = 0; c < Board.Size; c++)
                if (s.Board[r, c].Color == opp && !IsCorner(r, c) && StatusEffects.CanFlip(s.Board[r, c]))
                    list.Add(new Position(r, c));
        return list;
    }

    public override string Apply(GameState s, Stone user, Position? target)
    {
        var t = RequireTarget(s, user, target);
        s.Board[t].Color = user;
        ScoreRule.OnCard(s, user, -2);
        return $"{t}의 돌을 전향 (-2점)";
    }
}

/// <summary>6. 점수 2배 (B): 이번 착수로 얻는 점수 2배.</summary>
public sealed class DoubleScoreCard : SkillCardBase
{
    public override int Id => 6;
    public override string Name => "점수 2배";
    public override string Description => "이번 착수로 얻는 점수를 2배로 계산한다";
    public override CardType Type => CardType.B;
    public override bool NeedsTarget => false;

    public override string Apply(GameState s, Stone user, Position? target)
    {
        s.TurnScoreMultiplier = 2;
        return "이번 착수 점수 2배";
    }
}

/// <summary>7. 고정 (A): 내 돌 1개가 게임 끝까지 뒤집히거나 제거되지 않음.</summary>
public sealed class FixCard : SkillCardBase
{
    public override int Id => 7;
    public override string Name => "고정";
    public override string Description => "내 돌 1개를 게임 끝까지 뒤집히거나 제거되지 않게 한다";
    public override CardType Type => CardType.A;

    public override IReadOnlyList<Position> GetTargets(GameState s, Stone user)
    {
        var list = new List<Position>();
        for (int r = 0; r < Board.Size; r++)
            for (int c = 0; c < Board.Size; c++)
                if (s.Board[r, c].Color == user && !s.Board[r, c].Has(CellState.Fixed))
                    list.Add(new Position(r, c));
        return list;
    }

    public override string Apply(GameState s, Stone user, Position? target)
    {
        var t = RequireTarget(s, user, target);
        s.Board[t].State |= CellState.Fixed;
        return $"{t}의 돌을 고정";
    }
}

/// <summary>9. 봉인 (A): 빈칸 1개를 3턴 동안 아무도 둘 수 없게 막음.</summary>
public sealed class SealCard : SkillCardBase
{
    // '3턴'은 양측 턴을 모두 센다: 내 턴 끝 2 → 상대 턴 끝 1 → 내 턴 끝 0(해제).
    // 플레이 테스트 후 조정할 수 있게 상수로 뺐다.
    public const byte SealDuration = 3;

    public override int Id => 9;
    public override string Name => "봉인";
    public override string Description => "빈칸 1개를 3턴 동안 아무도 둘 수 없게 막는다";
    public override CardType Type => CardType.A;

    public override IReadOnlyList<Position> GetTargets(GameState s, Stone user)
    {
        var list = new List<Position>();
        for (int r = 0; r < Board.Size; r++)
            for (int c = 0; c < Board.Size; c++)
                if (StatusEffects.CanPlace(s.Board[r, c]))
                    list.Add(new Position(r, c));
        return list;
    }

    public override string Apply(GameState s, Stone user, Position? target)
    {
        var t = RequireTarget(s, user, target);
        ref Cell cell = ref s.Board[t];
        cell.State |= CellState.Sealed;
        cell.SealTurns = SealDuration;
        return $"{t} 칸을 {SealDuration}턴 봉인";
    }
}

/// <summary>11. 침묵 (C): 상대가 다음 턴에 패를 쓸 수 없게 함.</summary>
public sealed class SilenceCard : SkillCardBase
{
    public override int Id => 11;
    public override string Name => "침묵";
    public override string Description => "상대가 다음 턴에 패를 쓸 수 없게 만든다";
    public override CardType Type => CardType.C;
    public override bool NeedsTarget => false;

    public override string Apply(GameState s, Stone user, Position? target)
    {
        s.StatusOf(user.Opponent()).Silenced = true;
        return "상대를 침묵시킴";
    }
}

public static class CardPool
{
    /// <summary>현재 덱 구성. TODO: 최종 12종 확정 후 12장으로 채운다.</summary>
    public static IEnumerable<ISkillCard> Current() => new ISkillCard[]
    {
        new ConvertCard(), new DoubleScoreCard(), new FixCard(), new SealCard(), new SilenceCard(),
        // 임시로 같은 패를 한 장씩 더 넣어 덱을 10장으로 맞춰 둠
        new ConvertCard(), new DoubleScoreCard(), new FixCard(), new SealCard(), new SilenceCard()
    };
}
