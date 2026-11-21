using Othello.Core.Model;

namespace Othello.Core.Rules;

/// <summary>점수 계산을 한곳에 모은 클래스 (설계서 F3).</summary>
public static class ScoreRule
{
    /// <summary>
    /// F3-1 뒤집기 점수: n개 뒤집으면 착수자 +n, 상대 −n.
    /// 점수 2배 패는 착수자 득점에만, 방패는 상대 감점에만 적용된다.
    /// 돌려주는 값: (착수자 득점, 상대 감점)
    /// </summary>
    public static (int gain, int penalty) OnPlace(GameState s, Stone mover, int flipped)
    {
        Stone opp = mover.Opponent();
        int gain = flipped * s.TurnScoreMultiplier;
        int penalty = s.StatusOf(opp).Shielded ? flipped / 2 : flipped;

        s.Scores[mover.Index()] += gain;
        s.Scores[opp.Index()] -= penalty;
        return (gain, penalty);
    }

    /// <summary>F3-2 패 효과 점수 (전향 −2 등). 음수 허용.</summary>
    public static void OnCard(GameState s, Stone who, int delta)
    {
        if (delta < 0 && s.StatusOf(who).Shielded) delta /= 2;   // C# 정수 나눗셈은 0 방향으로 버림
        s.Scores[who.Index()] += delta;
    }

    /// <summary>F3-3 종료 보너스: 판에 남은 자기 돌 수만큼 가산.</summary>
    public static void ApplyFinalBonus(GameState s)
    {
        s.Scores[0] += s.Board.Count(Stone.Black);
        s.Scores[1] += s.Board.Count(Stone.White);
    }

    /// <summary>F3-4 승패 판정. 무승부면 Stone.Empty.</summary>
    public static Stone Winner(GameState s)
    {
        if (s.Scores[0] > s.Scores[1]) return Stone.Black;
        if (s.Scores[1] > s.Scores[0]) return Stone.White;
        return Stone.Empty;
    }
}
