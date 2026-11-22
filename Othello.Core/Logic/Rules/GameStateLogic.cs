using Othello.Core.Model;

namespace Othello.Core.Logic {
    // 게임 전체 상태 플래그 관리 (data.Board.GameFlags)
    // - 플레이어별 상태(침묵·방패)는 CardGameStateLogic.cs에서 처리
    // - 플래그 없음 확인은 GameFlags == GameState.None 으로 (Has(None)은 항상 true)

    public static partial class GameStateLogic {
        // 플래그 보유 여부
        public static bool Has(Data data, GameState flag)
            => (data.Board.GameFlags & flag) == flag;

        // 플래그 추가
        public static void Add(Data data, GameState flag) {
            data.Board.GameFlags |= flag;
            AfterCardStateAdded(data, flag);
        }

        // 플래그 해제
        public static void Remove(Data data, GameState flag) {
            data.Board.GameFlags &= ~flag;
            AfterCardStateRemoved(data, flag);
        }

        // 전체 초기화
        public static void Clear(Data data) {
            data.Board.GameFlags = GameState.None;
            AfterCardStatesCleared(data);
        }

        // 턴이 끝난 플레이어의 일시 상태 정리 (차례 교체는 GameLogic 담당)
        public static void ProcessTurnEnd(Data data, IsOnStone endedPlayer) {
            ProcessCardTurnEnd(data, endedPlayer);
        }

        // 초능력 확장 지점 → 구현은 CardGameStateLogic.cs
        static partial void AfterCardStateAdded(Data data, GameState added);
        static partial void AfterCardStateRemoved(Data data, GameState removed);
        static partial void AfterCardStatesCleared(Data data);
        static partial void ProcessCardTurnEnd(Data data, IsOnStone endedPlayer);
    }
}
