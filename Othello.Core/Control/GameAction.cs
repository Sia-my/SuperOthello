using Othello.Core.Cards;

namespace Othello.Core.Control {
    /*
     * GameAction.cs - 한 턴의 행동 데이터
     *
     * [사용 안내]
     *
     * 일반 착수:
     *   GameAction.Place(row, col)
     *
     * 패스:
     *   GameAction.Pass()
     *
     * 카드 사용 + 착수:
     *   GameAction.WithCard(card, target, move)
     *
     * 카드 사용 + 패스:
     *   GameAction.WithCard(card, target, null)
     *
     * 패스 여부:
     *   action.IsPass
     *
     * 착수 위치:
     *   action.Move
     *
     * 카드 대상:
     *   action.CardTarget
     *
     * Position 대신 좌표 튜플을 사용한다.
     * 실제 행동 처리는 GameController가 담당한다.
     */

    public sealed record GameAction(
        ISkillCard? Card,
        (int Row, int Col)? CardTarget,
        (int Row, int Col)? Move) {
        public bool IsPass => Move is null;

        public static GameAction Place(int row, int col)
            => new(null, null, (row, col));

        public static GameAction Place((int Row, int Col) move)
            => new(null, null, move);

        public static GameAction Pass()
            => new(null, null, null);

        public static GameAction WithCard(
            ISkillCard card,
            (int Row, int Col)? target,
            (int Row, int Col)? move)
            => new(card, target, move);
    }
}
