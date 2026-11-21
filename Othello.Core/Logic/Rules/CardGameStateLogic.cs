
using Othello.Core.Model;

namespace Othello.Core.Logic {
    /*
     * CardGameStateLogic.cs - 플레이어별 카드 상태와 턴 종료 효과
     *
     * [사용 안내]
     * 침묵 여부 확인      : GameStateLogic.IsSilenced(data, player)
     * 방패 여부 확인      : GameStateLogic.IsShielded(data, player)
     * 침묵 적용/해제     : GameStateLogic.SetSilenced(data, player, true/false)
     * 방패 적용/해제     : GameStateLogic.SetShielded(data, player, true/false)
     * 턴 종료 상태 갱신  : GameStateLogic.ProcessTurnEnd(data, endedPlayer)
     * 전체 상태 초기화  : GameStateLogic.Clear(data)
     *
     * [기존 GameController의 턴 종료 규칙]
     * - 턴을 마친 플레이어의 침묵 해제
     * - 그 상대의 방패 해제 (상대 방패는 이번 턴 동안만 유효)
     * - 현재 플레이어 교체/턴 번호 증가/점수 배율 초기화는 GameLogic 담당
     * - 봉인 남은 턴 감소는 CellStateLogic.ProcessTurnEnd(data) 담당
     *
     * GameStateLogic.cs와 이 파일은 하나의 static partial class로 합쳐진다.
     * 현재 GameState 플래그에는 None만 있으므로 개별 플래그 추가/제거 후
     * 실행할 특별한 카드 로직은 아직 없다.
     */

    public static partial class GameStateLogic {
        // 현재 플레이어가 카드 사용 금지 상태인가?
        public static bool IsSilenced(Data data, IsOnStone player)
            => data.StatusOf(player).Silenced;

        // 현재 플레이어가 감점 감소 상태인가?
        public static bool IsShielded(Data data, IsOnStone player)
            => data.StatusOf(player).Shielded;

        // 침묵 상태 적용 또는 해제
        public static void SetSilenced(
            Data data, IsOnStone player, bool enabled)
            => data.StatusOf(player).Silenced = enabled;

        // 방패 상태 적용 또는 해제
        public static void SetShielded(
            Data data, IsOnStone player, bool enabled)
            => data.StatusOf(player).Shielded = enabled;

        // 게임 전체 상태 초기화 시 플레이어별 임시 상태도 초기화
        static partial void AfterCardStatesCleared(Data data) {
            for (int i = 0; i < data.Status.Length; i++)
                data.Status[i] = default;
        }

        // 기존 GameController.EndTurn()의 침묵/방패 해제 규칙
        // 차례를 교체하기 전에 호출해야 한다.
        static partial void ProcessCardTurnEnd(
            Data data, IsOnStone endedPlayer) {
            // 턴을 마친 플레이어의 침묵 해제
            data.StatusOf(endedPlayer).Silenced = false;

            // 상대 플레이어의 방패 해제
            IsOnStone opponent =
                GameLogic.Opponent(endedPlayer);

            data.StatusOf(opponent).Shielded = false;
        }
    }
}
