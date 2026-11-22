using Othello.Core.Cards;

namespace Othello.Core.Control {
    // 한 턴의 행동 = 카드 사용(선택) + 착수 또는 패스
    // - 사람과 CPU가 같은 형식을 사용

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
