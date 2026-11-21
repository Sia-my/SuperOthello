using Othello.Core.Model;

namespace Othello.Core.Logic {
    /*
     * GameStateLogic.cs - 게임 전체 상태의 공통 관리
     *
     * [사용 안내]
     * 게임 전체 상태 확인 : GameStateLogic.Has(data, flag)
     * 게임 전체 상태 추가 : GameStateLogic.Add(data, flag)
     * 게임 전체 상태 제거 : GameStateLogic.Remove(data, flag)
     * 게임 전체 상태 초기화 : GameStateLogic.Clear(data)
     * 턴 종료 상태 처리   : GameStateLogic.ProcessTurnEnd(data, endedPlayer)
     *
     * [파일 분리]
     * GameStateLogic.cs     : 게임 전체 상태 플래그 관리
     * CardGameStateLogic.cs : 카드 효과, 플레이어별 상태 및 턴 종료 처리
     *
     * 두 파일은 같은 static partial class로 컴파일된다.
     * 게임 전체 상태값은 data.Board.GameFlags에 저장한다.
     * 플레이어별 상태값은 CardData.cs의 data.Status에 저장한다.
     *
     * 주의:
     * - 현재 GameState에는 None만 정의되어 있다.
     * - Has(data, GameState.None)은 항상 true다.
     *   상태가 없는지 검사하려면 data.Board.GameFlags == GameState.None을 사용한다.
     * - ProcessTurnEnd는 종료되는 플레이어를 인수로 받는다.
     *   GameController에서 턴을 교체하기 전에 호출해야 한다.
     * - 턴 교체, 패스, 게임 종료 자체는 GameLogic이 담당한다.
     */
    public static partial class GameStateLogic {
        // 지정한 상태 플래그가 모두 설정되어 있는가?
        public static bool Has(Data data, GameState flag)
            => (data.Board.GameFlags & flag) == flag;

        // 기존 플래그를 유지하면서 상태를 추가한다.
        public static void Add(Data data, GameState flag) {
            data.Board.GameFlags |= flag;
            AfterCardStateAdded(data, flag);
        }

        // 지정한 상태만 해제한다.
        public static void Remove(Data data, GameState flag) {
            data.Board.GameFlags &= ~flag;
            AfterCardStateRemoved(data, flag);
        }

        // 게임 전체 상태를 초기화한다.
        public static void Clear(Data data) {
            data.Board.GameFlags = GameState.None;
            AfterCardStatesCleared(data);
        }

        // 턴이 끝난 플레이어를 기준으로 일시적인 상태를 정리한다.
        // 점수 배율 초기화 및 플레이어 교체는 여기서 처리하지 않는다.
        public static void ProcessTurnEnd(Data data, IsOnStone endedPlayer) {
            ProcessCardTurnEnd(data, endedPlayer);
        }

        // CardGameStateLogic.cs에서 구현할 확장 지점.
        // 해당 구현이 없으면 컴파일러가 호출을 제거한다.
        static partial void AfterCardStateAdded(Data data, GameState added);
        static partial void AfterCardStateRemoved(Data data, GameState removed);
        static partial void AfterCardStatesCleared(Data data);
        static partial void ProcessCardTurnEnd(Data data, IsOnStone endedPlayer);
    }
}
