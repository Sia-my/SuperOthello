
using System.Linq;
using Othello.Core.Control;
using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Cpu
{
    /*
     * CPUPlayer.cs - CPU의 한 턴 행동 선택
     *
     * [사용 안내]
     *
     * CPU 생성:
     *   new CPUPlayer(Difficulty.Normal)
     *
     * 행동 선택:
     *   cpu.ChooseAction(data)
     *
     * 선택한 착수 위치:
     *   action.Move
     *
     * 패스 여부:
     *   action.IsPass
     *
     * ChooseAction은 GameAction만 반환하며,
     * 전달받은 Data를 변경하지 않는다.
     *
     * 실제 행동 실행은 GameController가 담당한다.
     *
     * [현재 알고리즘]
     *
     * 1. 착수 가능한 모든 위치를 탐색한다.
     * 2. 착수할 곳이 없으면 패스한다.
     * 3. 위치 가중치가 가장 높은 칸을 선택한다.
     * 4. 가중치가 같다면 뒤집는 돌이 많은 칸을 선택한다.
     *
     * 현재 Difficulty는 설정값만 보관한다.
     * 난이도별 알고리즘과 카드 사용은 아직 구현되지 않았다.
     */

    public class CPUPlayer
    {
        public Difficulty Difficulty { get; }

        public CPUPlayer(Difficulty difficulty)
            => Difficulty = difficulty;

        // 현재 게임 데이터를 읽고 최적의 행동 선택
        public GameAction ChooseAction(Data data)
        {
            var moves = BoardLogic.GetValidMoves(
                data, data.Current);

            // 착수 가능한 곳이 없으면 패스
            if (moves.Count == 0)
                return GameAction.Pass();

            // 위치 가중치 우선, 동점이면 뒤집는 돌 개수 비교
            var best = moves
                .OrderByDescending(p =>
                    WeightTables.WeightAt(
                        data.Board, p.Row, p.Col))
                .ThenByDescending(p =>
                    BoardLogic.GetFlips(
                        data, p.Row, p.Col,
                        data.Current).Count)
                .First();

            return GameAction.Place(best);
        }
    }
}
