
namespace Othello.Core.Model {
    /*
     * PlayerSlot.cs - 대전 참가자 설정 데이터
     *
     * [사용 안내]
     * 사람 플레이어 생성 : PlayerSlot.Human("player1")
     * CPU 플레이어 생성  : PlayerSlot.Cpu(Difficulty.Normal)
     * CPU 여부 확인     : slot.IsCpu
     * 플레이어 ID 확인  : slot.Id
     * CPU 난이도 확인   : slot.Difficulty
     *
     * Difficulty.Easy / Normal / Hard 중 하나를 선택한다.
     * 여기서는 설정 데이터만 보관하고,
     * CPU의 수 선택은 CPUPlayer가 처리한다.
     *
     * 원본 GameAction.cs에 있던 정의를 분리했다.
     * PlayerSlot 사용 파일은
     * using Othello.Core.Model;이 필요하다.
     */

    public enum Difficulty {
        Easy,
        Normal,
        Hard
    }

    // 대전 슬롯: IsCpu는 대전 설정 시 고정된다.
    public sealed record PlayerSlot(
        string Id,
        bool IsCpu,
        Difficulty Difficulty = Difficulty.Normal) {
        public static PlayerSlot Human(string id)
            => new(id, false);

        public static PlayerSlot Cpu(Difficulty d)
            => new(d switch {
                Difficulty.Easy => "#CPU-쉬움",
                Difficulty.Normal => "#CPU-보통",
                _ => "#CPU-어려움"
            }, true, d);
    }
}
