using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Cards {
    // B형(점수) 카드: 방패·럭키 세븐·마이너스의 손 (번호 = 설계서 2.2 후보 표)
    // - 카드는 플래그만 켜고 점수 계산은 ScoreLogic.OnPlace가 담당

    /// <summary>8. 방패 (B): 상대의 다음 턴 동안 받는 감점 절반(내림).</summary>
    public sealed class ShieldCard : SkillCardBase {
        public override int Id => 8;
        public override string Name => "방패";
        public override string Description => "상대의 다음 턴 동안 받는 감점을 절반(내림)으로 줄인다";
        public override CardType Type => CardType.B;
        public override string ScoreEffect => "감점 ÷ 2";
        public override bool NeedsTarget => false;

        // 해제는 상대 턴 종료 처리(GameStateLogic.ProcessTurnEnd)가 담당
        public override string Apply(Data data, IsOnStone user, (int Row, int Col)? target) {
            GameStateLogic.SetShielded(data, user, true);
            return "방패: 상대 턴 동안 감점 절반";
        }
    }

    /// <summary>17. 럭키 세븐 (B): 이번 착수로 정확히 7개를 뒤집으면 +7점.</summary>
    public sealed class LuckySevenCard : SkillCardBase {
        public override int Id => 17;
        public override string Name => "럭키 세븐";
        public override string Description => "이번 착수로 정확히 7개를 뒤집으면 +7점을 더 얻는다";
        public override CardType Type => CardType.B;
        public override string ScoreEffect => "사용자 +7";
        public override bool NeedsTarget => false;

        public override string Apply(Data data, IsOnStone user, (int Row, int Col)? target) {
            data.TurnLuckySeven = true;
            return "이번 착수가 정확히 7개면 +7점";
        }
    }

    /// <summary>19. 마이너스의 손 (B): 이번 착수로 뒤집은 수만큼 상대를 추가 감점 (상대 -2n).</summary>
    public sealed class MinusHandCard : SkillCardBase {
        public const int PenaltyMultiplier = 2;

        public override int Id => 19;
        public override string Name => "마이너스의 손";
        public override string Description => "이번 착수로 뒤집은 수만큼 상대 점수를 추가로 깎는다";
        public override CardType Type => CardType.B;
        public override string ScoreEffect => "사용자 +n, 상대 -2n";
        public override bool NeedsTarget => false;

        public override string Apply(Data data, IsOnStone user, (int Row, int Col)? target) {
            data.TurnPenaltyMultiplier = PenaltyMultiplier;
            return "이번 착수 상대 감점 2배";
        }
    }
}
