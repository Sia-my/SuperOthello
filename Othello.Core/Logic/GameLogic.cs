
using System;
using Othello.Core.Model;
using Othello.Core.Logic;

namespace Othello.Core.Logic {
    /*
     * GameLogic.cs - 게임 진행 규칙
     *
     * 상대 돌 색상 얻기       : GameLogic.Opponent(stone)
     * 새 게임 데이터 초기화  : GameLogic.Reset(data)
     * 현재 플레이어 패스 가능: GameLogic.CanPass(data)
     * 정상 착수 기록        : GameLogic.RegisterMove(data)
     * 수동 패스 기록        : GameLogic.RegisterPass(data)
     * 턴 종료 및 차례 교체   : GameLogic.EndTurn(data)
     * 자동 패스 처리        : GameLogic.TryAutoPass(data, out passedPlayer)
     * 양측 모두 착수 불가    : GameLogic.BothPlayersCannotMove(data)
     * 기본 게임 종료 조건   : GameLogic.ShouldFinish(data)
     * 종료 확정 및 보너스   : GameLogic.Finish(data)
     *
     * 실제 돌 배치: BoardLogic.Place()
     * 실제 점수 계산: ScoreLogic.OnPlace()
     * 실행 순서와 UI 알림: GameController
     *
     * 초능력 카드, 손패, 덱 및 봉인 지속시간은 아직 연결되지 않았다.
     */

    public static class GameLogic {
        // 상대 플레이어
        public static IsOnStone Opponent(IsOnStone stone)
            => stone switch {
                IsOnStone.Black => IsOnStone.White,
                IsOnStone.White => IsOnStone.Black,
                _ => IsOnStone.Empty
            };

        // 새 게임 초기화
        public static void Reset(Data data) {
            BoardLogic.Reset(data);
            GameStateLogic.Clear(data);

            data.Scores = new int[2];
            data.Current = IsOnStone.Black;
            data.TurnNumber = 1;
            data.ConsecutivePasses = 0;
            data.IsOver = false;
            data.TurnScoreMultiplier = 1;
        }

        // 현재 플레이어가 패스할 수 있는가?
        public static bool CanPass(Data data)
            => !BoardLogic.HasValidMove(data, data.Current);

        // 정상 착수 기록
        public static void RegisterMove(Data data)
            => data.ConsecutivePasses = 0;

        // 수동 패스
        public static void RegisterPass(Data data) {
            if (!CanPass(data))
                throw new InvalidOperationException(
                    "둘 수 있는 곳이 있으면 패스할 수 없습니다.");

            data.ConsecutivePasses++;
        }

        // 턴 종료
        public static void EndTurn(Data data) {
            data.TurnScoreMultiplier = 1;
            data.Current = Opponent(data.Current);
            data.TurnNumber++;
        }

        // 자동 패스
        public static bool TryAutoPass(
            Data data, out IsOnStone passedPlayer) {
            passedPlayer = IsOnStone.Empty;

            if (data.IsOver ||
                BoardLogic.HasValidMove(data, data.Current))
                return false;

            passedPlayer = data.Current;
            data.ConsecutivePasses++;
            data.Current = Opponent(data.Current);

            return true;
        }

        // 양측 모두 착수할 수 없는가?
        public static bool BothPlayersCannotMove(Data data)
            => !BoardLogic.HasValidMove(data, IsOnStone.Black)
               && !BoardLogic.HasValidMove(data, IsOnStone.White);

        // 기본 게임 종료 조건
        public static bool ShouldFinish(Data data)
            => BoardLogic.IsFull(data)
               || data.ConsecutivePasses >= 2;

        // 게임 종료 확정
        public static IsOnStone Finish(Data data) {
            // 종료 보너스 중복 적용 방지
            if (data.IsOver)
                return ScoreLogic.Winner(data);

            data.IsOver = true;
            ScoreLogic.ApplyFinalBonus(data);

            return ScoreLogic.Winner(data);
        }
    }
}
