using Othello.Core.Model;

namespace Othello.Core.Logic {
    // 턴·패스·종료 규칙
    // - 돌 배치는 BoardLogic, 점수는 ScoreLogic 담당
    // - 실행 순서는 GameController가 결정

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
            ScoreLogic.ResetTurnModifiers(data);
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
            ScoreLogic.ResetTurnModifiers(data);
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
            // 이미 끝났으면 보너스를 다시 더하지 않음
            if (data.IsOver)
                return ScoreLogic.Winner(data);

            data.IsOver = true;
            ScoreLogic.ApplyFinalBonus(data);

            return ScoreLogic.Winner(data);
        }
    }
}
