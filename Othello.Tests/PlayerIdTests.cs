using Othello.Core.Model;

namespace Othello.Tests {
    // 플레이어 ID 규칙·대전 설정 조합 테스트 (설계서 2.4, 운영 세부 기준)

    [TestClass]
    public class PlayerIdTests {
        [TestMethod]
        public void UI01_ID_규칙_정상() {
            foreach (string id in new[] { "홍길동", "abc123", "가나다라마바사아자차카타", "A" })
                Assert.IsTrue(PlayerIdRule.IsValid(id), id);
        }

        [TestMethod]
        public void UI02_ID_규칙_위반() {
            foreach (string? id in new[] { "", "abcdefghijklm", "홍 길동", "#CPU", "a_b", " abc", null })
                Assert.IsFalse(PlayerIdRule.IsValid(id), id ?? "null");
        }

        [TestMethod]
        public void UI03_대전설정_조합_검사() {
            var cpu = PlayerSlot.Cpu(Difficulty.Normal);

            Assert.IsNull(PlayerIdRule.GetSetupError(PlayerSlot.Human("홍정호"), cpu));
            Assert.IsNull(PlayerIdRule.GetSetupError(cpu, PlayerSlot.Human("홍정호")));
            Assert.IsNull(PlayerIdRule.GetSetupError(PlayerSlot.Human("a"), PlayerSlot.Human("b")));

            Assert.IsNotNull(PlayerIdRule.GetSetupError(PlayerSlot.Human("a"), PlayerSlot.Human("a")), "같은 ID");
            Assert.IsNotNull(PlayerIdRule.GetSetupError(cpu, PlayerSlot.Cpu(Difficulty.Hard)), "CPU vs CPU");
            Assert.IsNotNull(PlayerIdRule.GetSetupError(PlayerSlot.Human(""), cpu), "빈 ID");
            Assert.IsNotNull(PlayerIdRule.GetSetupError(cpu, PlayerSlot.Human("a#b")), "# 포함");
        }
    }
}
