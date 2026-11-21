using Othello.Core.Control;
using Othello.Core.Model;

namespace Othello.Core.Cpu;

/// <summary>
/// ⚠ 임시 CPU: UI 연결 확인용. 패는 쓰지 않고 위치 가중치가 가장 높은 칸에 둔다.
/// TODO(3주차): EvalResult 사전식 비교, 후보 3개 × A형 패, 깊이 2 알파-베타 (설계서 3.4.4)
/// </summary>
public class CPUPlayer
{
    public Difficulty Difficulty { get; }

    public CPUPlayer(Difficulty difficulty) => Difficulty = difficulty;

    /// <summary>s는 복사본을 넘긴다. 원본 상태를 바꾸지 않는다.</summary>
    public GameAction ChooseAction(GameState s)
    {
        var moves = s.Board.GetValidMoves(s.Current);
        if (moves.Count == 0) return GameAction.Pass();

        Position best = moves
            .OrderByDescending(p => WeightTables.WeightAt(s.Board, p.Row, p.Col))
            .ThenByDescending(p => s.Board.GetFlips(p.Row, p.Col, s.Current).Count)
            .First();

        return GameAction.Place(best);
    }
}
