using System;
using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Cards {
    // C형(운영) 카드: 약탈·보급·역전의 발판 (번호 = 설계서 2.2 후보 표)
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

    /// <summary>23. 역전의 발판 (C): 지고 있을 때만, 덱에서 3장 드로우 (한도 초과분은 덱으로).</summary>
    public sealed class ComebackCard : SkillCardBase {
        public const int DrawCount = 3;

        public override int Id => 23;
        public override string Name => "역전의 발판";
        public override string Description => "점수가 지고 있을 때만 쓸 수 있다. 덱에서 3장을 뽑는다 (손패 한도를 넘는 패는 버림)";
        public override CardType Type => CardType.C;
        public override string ScoreEffect => "없음";
        public override bool NeedsTarget => false;

        public override bool CanUse(Data data, IsOnStone user)
            => IsBehind(data, user) && data.Deck.Count > 0;

        public static bool IsBehind(Data data, IsOnStone user)
            => data.Scores[ScoreLogic.PlayerIndex(user)]
               < data.Scores[ScoreLogic.PlayerIndex(GameLogic.Opponent(user))];

        public override string Apply(Data data, IsOnStone user, (int Row, int Col)? target) {
            int got = CardHandLogic.DrawForCard(data, user, DrawCount, this);
            return $"{got}장 획득";
        }
    }
}
