
using System;
using Othello.Core.Model;

namespace Othello.Core.Logic {
    // 점수 계산: 착수 점수, 카드 점수, 종료 보너스, 승패
    // - 방패 여부는 호출하는 쪽에서 전달
    // - Scores[0] = 흑, Scores[1] = 백 (음수 허용)

    public static class ScoreLogic {
        // 돌 색상을 점수 배열 인덱스로 변환
        public static int PlayerIndex(IsOnStone stone)
            => stone switch {
                IsOnStone.Black => 0,
                IsOnStone.White => 1,
                _ => throw new ArgumentException(
                    "Empty에는 플레이어 인덱스가 없습니다.",
                    nameof(stone))
            };

        // 착수 점수: 착수자 +n×배율, 상대 −n (방패면 절반)
        public static (int gain, int penalty) OnPlace(
            Data data,
            IsOnStone mover,
            int flipped,
            bool opponentShielded = false) {
            IsOnStone opponent = GameLogic.Opponent(mover);

            int gain = flipped * data.TurnScoreMultiplier;

            int penalty = opponentShielded
                ? flipped / 2
                : flipped;

            data.Scores[PlayerIndex(mover)] += gain;
            data.Scores[PlayerIndex(opponent)] -= penalty;

            return (gain, penalty);
        }

        // 카드 효과 점수 (방패면 감점 절반)
        public static void OnCard(
            Data data,
            IsOnStone who,
            int delta,
            bool shielded = false) {
            if (delta < 0 && shielded)
                delta /= 2;

            data.Scores[PlayerIndex(who)] += delta;
        }

        // 종료 보너스: 남은 자기 돌 수만큼 가산
        public static void ApplyFinalBonus(Data data) {
            data.Scores[0] += FinalBonusOf(data, IsOnStone.Black);
            data.Scores[1] += FinalBonusOf(data, IsOnStone.White);
        }

        // 종료 보너스 값 (판에 남은 자기 돌 수, 종료 후엔 판이 바뀌지 않음)
        public static int FinalBonusOf(Data data, IsOnStone who)
            => BoardLogic.Count(data, who);

        // 결과 화면용 분리 점수: 뒤집기·카드 점수 + 보너스 = 최종 점수 (F3-3·F3-4)
        public static (int Play, int Bonus, int Final) BreakdownOf(Data data, IsOnStone who) {
            int final = data.Scores[PlayerIndex(who)];
            int bonus = data.IsOver ? FinalBonusOf(data, who) : 0;
            return (final - bonus, bonus, final);
        }

        // 승패 판정 (무승부는 Empty)
        public static IsOnStone Winner(Data data) {
            if (data.Scores[0] > data.Scores[1])
                return IsOnStone.Black;

            if (data.Scores[1] > data.Scores[0])
                return IsOnStone.White;

            return IsOnStone.Empty;
        }
    }
}
