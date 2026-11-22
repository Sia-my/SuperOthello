
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Othello.Core.Cards;
using Othello.Core.Control;
using Othello.Core.Cpu;
using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Tests {
    // CPU 알고리즘 테스트 (설계서 3.4)
    // - 판을 직접 만들 때는 빈 Data에 BoardLogic.PutStone으로 돌 배치

    [TestClass]
    public class CpuTests {
        private static Data NewGameData() {
            var data = new Data();
            GameLogic.Reset(data);
            return data;
        }

        // 빈 판에서 시작 (초기 돌 4개 없음)
        private static Data EmptyData() => new Data();

        // ───────────── EvalResult ─────────────

        [TestMethod]
        public void CPU01_평가_확정돌이_이동가능성보다_우선() {
            var a = EvalResult.For(Difficulty.Hard);
            a.Stability = 1; a.Mobility = -5;

            var b = EvalResult.For(Difficulty.Hard);
            b.Stability = 0; b.Mobility = 10;

            Assert.IsTrue(a.CompareTo(b) > 0);
        }

        [TestMethod]
        public void CPU02_쉬움은_확정돌과_이동가능성을_보지않음() {
            var a = EvalResult.For(Difficulty.Easy);
            a.Stability = 5; a.Mobility = 5; a.Position = 0;

            var b = EvalResult.For(Difficulty.Easy);
            b.Stability = -5; b.Mobility = -5; b.Position = 1;

            Assert.IsTrue(a.CompareTo(b) < 0);   // 위치 가중치만으로 비교
        }

        // ───────────── 위치 가중치 ─────────────

        [TestMethod]
        public void CPU03_초반은_기본표() {
            Data data = NewGameData();
            Assert.AreEqual(WeightTables.Phase.Opening, WeightTables.PhaseOf(data.Board));
            Assert.AreEqual(-20, WeightTables.WeightAt(data.Board, 0, 1));
            Assert.AreEqual(-50, WeightTables.WeightAt(data.Board, 1, 1));
        }

        [TestMethod]
        public void CPU04_중반_차지된모서리_주변은_0() {
            Data data = EmptyData();
            // 돌 30개를 채워 빈칸 34개 → 중반
            for (int i = 0; i < 30; i++)
                BoardLogic.PutStone(data, 2 + i / 8, i % 8, IsOnStone.Black);
            BoardLogic.PutStone(data, 0, 0, IsOnStone.White);

            Assert.AreEqual(WeightTables.Phase.Middle, WeightTables.PhaseOf(data.Board));
            Assert.AreEqual(0, WeightTables.WeightAt(data.Board, 0, 1));    // C칸
            Assert.AreEqual(0, WeightTables.WeightAt(data.Board, 1, 1));    // X칸
            Assert.AreEqual(-20, WeightTables.WeightAt(data.Board, 0, 6));  // 빈 모서리 옆은 그대로
        }

        [TestMethod]
        public void CPU05_종반은_평탄표() {
            Data data = EmptyData();
            for (int i = 0; i < 50; i++)
                BoardLogic.PutStone(data, 1 + i / 8, i % 8, IsOnStone.Black);

            Assert.AreEqual(WeightTables.Phase.End, WeightTables.PhaseOf(data.Board));
            Assert.AreEqual(30, WeightTables.WeightAt(data.Board, 0, 0));
            Assert.AreEqual(0, WeightTables.WeightAt(data.Board, 0, 1));
            Assert.AreEqual(5, WeightTables.WeightAt(data.Board, 3, 3));
        }

        // ───────────── 확정돌 ─────────────

        [TestMethod]
        public void CPU06_확정돌_모서리에서_변따라_이어진돌() {
            Data data = EmptyData();
            BoardLogic.PutStone(data, 0, 0, IsOnStone.Black);
            BoardLogic.PutStone(data, 0, 1, IsOnStone.Black);
            BoardLogic.PutStone(data, 0, 2, IsOnStone.Black);
            BoardLogic.PutStone(data, 1, 0, IsOnStone.Black);
            BoardLogic.PutStone(data, 0, 4, IsOnStone.Black);   // 빈칸으로 끊김 → 확정 아님

            Assert.AreEqual(4, StabilityLogic.Count(data, IsOnStone.Black));
            Assert.IsFalse(StabilityLogic.IsStable(data, 0, 4));
        }

        [TestMethod]
        public void CPU07_고정돌은_확정돌() {
            Data data = NewGameData();
            CellStateLogic.Add(data, 3, 4, CellState.Fixed);

            Assert.AreEqual(1, StabilityLogic.Count(data, IsOnStone.Black));
        }

        // ───────────── CPU 행동 ─────────────

        [TestMethod]
        public void CPU08_어려움은_모서리를_차지() {
            Data data = NewGameData();
            // 흑이 (0,0)에 두면 (0,1) 백을 뒤집으며 모서리 확보
            BoardLogic.PutStone(data, 0, 1, IsOnStone.White);
            BoardLogic.PutStone(data, 0, 2, IsOnStone.Black);

            GameAction action = new CPUPlayer(Difficulty.Hard, seed: 1).ChooseAction(data);

            Assert.AreEqual((0, 0), action.Move);
        }

        [TestMethod]
        public void CPU09_CPU는_원본데이터를_바꾸지않음() {
            Data data = NewGameData();
            data.HandOf(IsOnStone.Black).TryAdd(new ConvertCard());
            int emptyBefore = BoardLogic.EmptyCount(data);
            int handBefore = data.HandOf(IsOnStone.Black).Count;

            new CPUPlayer(Difficulty.Hard, seed: 1).ChooseAction(data);

            Assert.AreEqual(emptyBefore, BoardLogic.EmptyCount(data));
            Assert.AreEqual(handBefore, data.HandOf(IsOnStone.Black).Count);
            Assert.AreEqual(0, data.Scores[0]);
        }

        [TestMethod]
        public void CPU10_둘곳이_없으면_패스() {
            Data data = EmptyData();
            BoardLogic.PutStone(data, 0, 0, IsOnStone.Black);

            GameAction action = new CPUPlayer(Difficulty.Normal, seed: 1).ChooseAction(data);

            Assert.IsTrue(action.IsPass);
        }

        [TestMethod]
        public void CPU11_5개이상_뒤집으면_점수2배_사용() {
            Data data = EmptyData();
            // 흑 [백 ×5] 빈칸 → (0,6)에 두면 5개 뒤집기
            BoardLogic.PutStone(data, 0, 0, IsOnStone.Black);
            for (int c = 1; c <= 5; c++)
                BoardLogic.PutStone(data, 0, c, IsOnStone.White);

            var card = new DoubleScoreCard();
            data.HandOf(IsOnStone.Black).TryAdd(card);

            GameAction action = new CPUPlayer(Difficulty.Normal, seed: 1).ChooseAction(data);

            Assert.AreSame(card, action.Card);
            Assert.AreEqual((0, 6), action.Move);
        }

        [TestMethod]
        public void CPU12_침묵상태면_카드를_쓰지않음() {
            Data data = EmptyData();
            BoardLogic.PutStone(data, 0, 0, IsOnStone.Black);
            for (int c = 1; c <= 5; c++)
                BoardLogic.PutStone(data, 0, c, IsOnStone.White);

            data.HandOf(IsOnStone.Black).TryAdd(new DoubleScoreCard());
            GameStateLogic.SetSilenced(data, IsOnStone.Black, true);

            GameAction action = new CPUPlayer(Difficulty.Hard, seed: 1).ChooseAction(data);

            Assert.IsNull(action.Card);
        }

        [TestMethod]
        public void CPU13_CPU행동은_컨트롤러에서_그대로_실행가능() {
            // 실제 대전처럼 진행했을 때 CPU 행동이 거부되지 않는지 확인
            var ctrl = new GameController(
                PlayerSlot.Cpu(Difficulty.Hard), PlayerSlot.Human("b"), seed: 3);
            var cpu = new CPUPlayer(Difficulty.Hard, seed: 3);
            var other = new CPUPlayer(Difficulty.Normal, seed: 4);

            for (int turn = 0; turn < 60 && !ctrl.Data.IsOver; turn++) {
                CPUPlayer who = ctrl.Data.Current == IsOnStone.Black ? cpu : other;
                GameAction action = who.ChooseAction(ctrl.Data.Clone());

                bool ok = ctrl.TryExecuteTurn(action, out string err);
                Assert.IsTrue(ok, $"{turn}턴: {err}");
            }
        }
    }
}
