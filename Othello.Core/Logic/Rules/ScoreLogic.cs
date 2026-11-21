
using System;
using Othello.Core.Model;

namespace Othello.Core.Logic {
    /*
     * ScoreLogic - 기존 ScoreRule.cs의 점수 알고리즘
     *
     * 플레이어 인덱스 : ScoreLogic.PlayerIndex(stone)
     * 착수 점수 계산   : ScoreLogic.OnPlace(data, mover, flipped)
     * 카드 점수 처리   : ScoreLogic.OnCard(data, who, delta)
     * 종료 보너스 적용 : ScoreLogic.ApplyFinalBonus(data)
     * 최종 승자 판정   : ScoreLogic.Winner(data)
     *
     * 점수 배열:
     *   data.Scores[0] = 흑돌 점수
     *   data.Scores[1] = 백돌 점수
     *
     * 반환값:
     *   OnPlace() -> (착수자 득점, 상대 감점)
     *   Winner()  -> Black / White / Empty(무승부)
     *
     * 방패 계산은 bool 인수로 보존했다.
     * 실제 방패 상태와의 연결은 추후 구현한다.
     */

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

        // 돌을 뒤집었을 때 점수 계산
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

        // 카드에 의한 점수 변화
        public static void OnCard(
            Data data,
            IsOnStone who,
            int delta,
            bool shielded = false) {
            if (delta < 0 && shielded)
                delta /= 2;

            data.Scores[PlayerIndex(who)] += delta;
        }

        // 게임 종료 보너스
        public static void ApplyFinalBonus(Data data) {
            data.Scores[0] +=
                BoardLogic.Count(data, IsOnStone.Black);

            data.Scores[1] +=
                BoardLogic.Count(data, IsOnStone.White);
        }

        // 승패 판정
        public static IsOnStone Winner(Data data) {
            if (data.Scores[0] > data.Scores[1])
                return IsOnStone.Black;

            if (data.Scores[1] > data.Scores[0])
                return IsOnStone.White;

            return IsOnStone.Empty;
        }
    }
}
