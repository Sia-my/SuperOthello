
using System;
using Othello.Core.Model;

namespace Othello.Core.Cpu {
    // CPU 평가 결과 (설계서 3.4.2)
    // - 점수 하나로 합치지 않고 4개 항목을 중요도 순서대로 비교 (사전식)
    // - 고정값(확정돌) > 이동 가능성 > 위치 가중치 > 점수 차
    // - 난이도에 따라 앞의 두 항목은 비교에서 빠질 수 있음

    public struct EvalResult : IComparable<EvalResult> {
        public int Stability;   // 내 확정돌 − 상대 확정돌
        public int Mobility;    // 내 착수 가능 칸 − 상대 착수 가능 칸
        public int Position;    // 내 돌 가중치 합 − 상대 돌 가중치 합
        public int Score;       // 내 점수 − 상대 점수

        public bool UseStability;   // 어려움만 사용
        public bool UseMobility;    // 보통·어려움 사용

        // 탐색 시작값: 어떤 실제 평가보다도 낮음 / 높음
        public static readonly EvalResult MinValue = new() {
            Stability = int.MinValue, Mobility = int.MinValue,
            Position = int.MinValue, Score = int.MinValue,
            UseStability = true, UseMobility = true
        };

        public static readonly EvalResult MaxValue = new() {
            Stability = int.MaxValue, Mobility = int.MaxValue,
            Position = int.MaxValue, Score = int.MaxValue,
            UseStability = true, UseMobility = true
        };

        // 난이도별 비교 항목 (설계서 3.4.6)
        public static EvalResult For(Difficulty difficulty) => new() {
            UseStability = difficulty == Difficulty.Hard,
            UseMobility = difficulty != Difficulty.Easy
        };

        // 앞 항목이 다르면 뒤 항목은 보지 않음
        public int CompareTo(EvalResult o) {
            if (UseStability && Stability != o.Stability)
                return Stability.CompareTo(o.Stability);
            if (UseMobility && Mobility != o.Mobility)
                return Mobility.CompareTo(o.Mobility);
            if (Position != o.Position)
                return Position.CompareTo(o.Position);
            return Score.CompareTo(o.Score);
        }

        public override string ToString()
            => $"확정돌 {Stability} / 이동 {Mobility} / 위치 {Position} / 점수 {Score}";
    }
}
