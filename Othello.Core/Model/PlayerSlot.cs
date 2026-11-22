
namespace Othello.Core.Model {
    // 대전 참가자 설정 (사람 / CPU + 난이도)
    // - CPU ID는 #CPU-쉬움 / #CPU-보통 / #CPU-어려움

    public enum Difficulty {
        Easy,
        Normal,
        Hard
    }

    // IsCpu는 대전 설정 때 정해지고 게임 중엔 바뀌지 않음
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
