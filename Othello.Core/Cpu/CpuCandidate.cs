
using Othello.Core.Cards;
using Othello.Core.Control;

namespace Othello.Core.Cpu {
    // CPU가 검토하는 후보 한 개 = (A형 카드 + 대상) + 착수 칸
    // - 카드 없이 착수만 하는 후보도 같은 형식 (Card = null)

    public sealed record CpuCandidate(
        ISkillCard? Card,
        (int Row, int Col)? CardTarget,
        (int Row, int Col) Move) {
        public bool UsesCard => Card != null;

        public static CpuCandidate Plain((int Row, int Col) move)
            => new(null, null, move);

        // GameController에 넘길 행동으로 변환
        public GameAction ToAction()
            => Card == null
                ? GameAction.Place(Move)
                : GameAction.WithCard(Card, CardTarget, Move);
    }
}
