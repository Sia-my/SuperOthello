
using Othello.Core.Model;

namespace Othello.Core.Logic {
    // GameStateLogic의 초능력 부분 (침묵·방패)
    // - 턴 종료 시 침묵·방패 해제

    public static partial class GameStateLogic {
        // 침묵 상태인가 (카드 사용 불가)
        public static bool IsSilenced(Data data, IsOnStone player)
            => data.StatusOf(player).Silenced;

        // 방패 상태인가 (감점 절반)
        public static bool IsShielded(Data data, IsOnStone player)
            => data.StatusOf(player).Shielded;

        // 침묵 켜기/끄기
        public static void SetSilenced(
            Data data, IsOnStone player, bool enabled)
            => data.StatusOf(player).Silenced = enabled;

        // 방패 켜기/끄기
        public static void SetShielded(
            Data data, IsOnStone player, bool enabled)
            => data.StatusOf(player).Shielded = enabled;

        // 전체 초기화 시 플레이어 상태도 초기화
        static partial void AfterCardStatesCleared(Data data) {
            for (int i = 0; i < data.Status.Length; i++)
                data.Status[i] = default;
        }

        // 턴 종료 처리 (차례를 바꾸기 전에 호출)
        static partial void ProcessCardTurnEnd(
            Data data, IsOnStone endedPlayer) {
            // 내 침묵은 내 턴이 끝나면 풀림
            data.StatusOf(endedPlayer).Silenced = false;

            // 상대 방패는 내 턴 동안만 유효
            IsOnStone opponent =
                GameLogic.Opponent(endedPlayer);

            data.StatusOf(opponent).Shielded = false;
        }
    }
}
