using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Cards {
    // C형(운영) 카드: 약탈·보급·흑기사 (번호 = 설계서 2.2 후보 표)
    // - 손패 받기는 CardHandLogic.DrawForCard 하나로 처리 (사용 중인 카드 1장은 한도에서 제외)

    /// <summary>10. 약탈 (C): 상대 손패 1장을 무작위로 가져온다.</summary>
    public sealed class PlunderCard : SkillCardBase {
        public override int Id => 10;
        public override string Name => "약탈";
        public override string Description => "상대 손패 1장을 무작위로 가져온다";
        public override CardType Type => CardType.C;
        public override string ScoreEffect => "없음";
        public override bool NeedsTarget => false;

        public override bool CanUse(Data data, IsOnStone user)
            => data.HandOf(GameLogic.Opponent(user)).Count > 0;

        // 카드 객체는 상태를 갖지 않으므로 Random.Shared 사용 (검증용 복제본과 결과가 달라도 됨)
        public override string Apply(Data data, IsOnStone user, (int Row, int Col)? target) {
            Hand opp = data.HandOf(GameLogic.Opponent(user));
            if (opp.Count == 0)
                throw new InvalidOperationException("[약탈] 상대 손패가 없습니다.");

            ISkillCard taken = opp.Cards[Random.Shared.Next(opp.Count)];
            opp.Remove(taken);
            data.HandOf(user).ForceAdd(taken);   // 약탈 카드가 빠지면 3장 이하

            return $"[{taken.Name}] 약탈";
        }
    }

    /// <summary>12. 보급 (C): 덱에서 2장 드로우 (한도 초과분은 덱으로).</summary>
    public sealed class SupplyCard : SkillCardBase {
        public const int DrawCount = 2;

        public override int Id => 12;
        public override string Name => "보급";
        public override string Description => "덱에서 2장을 뽑는다 (손패 한도를 넘는 패는 버림)";
        public override CardType Type => CardType.C;
        public override string ScoreEffect => "없음";
        public override bool NeedsTarget => false;

        public override bool CanUse(Data data, IsOnStone user)
            => data.Deck.Count > 0;

        public override string Apply(Data data, IsOnStone user, (int Row, int Col)? target) {
            int got = CardHandLogic.DrawForCard(data, user, DrawCount, this);
            return $"{got}장 획득";
        }
    }

    /// <summary>20. 흑기사 (C): 상대의 다음 차례를 무작위 합법 착수로 강제하고 카드 사용 금지.</summary>
    public sealed class BlackKnightCard : SkillCardBase {
        public override int Id => 20;
        public override string Name => "흑기사";
        public override string Description => "상대의 다음 차례에 카드 사용을 금지하고, 합법적인 칸 중 하나에 무작위로 자동 착수하게 만든다";
        public override CardType Type => CardType.C;
        public override string ScoreEffect => "없음";
        public override bool NeedsTarget => false;

        public override string Apply(Data data, IsOnStone user, (int Row, int Col)? target) {
            GameStateLogic.SetForcedRandomMove(data, GameLogic.Opponent(user), true);
            return "상대 다음 차례 무작위 착수 강제 (카드 사용 불가)";
        }
    }

}
