using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Cards {
    // B형(점수) 카드: 방패·오목·마이너스의 손 (번호 = 설계서 2.2 후보 표)
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

    /// <summary>18. 오목 (B): 이번 착수에서 한 방향으로 5개 이상 뒤집으면 +5점.</summary>
    public sealed class OmokCard : SkillCardBase {
        public override int Id => 18;
        public override string Name => "오목";
        public override string Description => "이번 착수에서 한 방향으로 돌 5개 이상을 뒤집으면 5점을 추가로 얻는다";
        public override CardType Type => CardType.B;
        public override string ScoreEffect => "조건 충족 시 사용자 +5";
        public override bool NeedsTarget => false;

        public override string Apply(Data data, IsOnStone user, (int Row, int Col)? target) {
            data.TurnOmok = true;
            return "한 방향으로 5개 이상 뒤집으면 +5점";
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
