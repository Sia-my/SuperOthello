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

        [TestMethod]
        public void CPU14_어려움_한수_0_5초_이내() {
            // 20턴 진행한 중반 판에서 어려움 CPU 한 수가 500ms 미만 (설계서 2.3)
            var ctrl = new GameController(
                PlayerSlot.Cpu(Difficulty.Hard), PlayerSlot.Human("b"), seed: 3);
            var cpu = new CPUPlayer(Difficulty.Hard, seed: 3);
            var other = new CPUPlayer(Difficulty.Normal, seed: 4);

            for (int turn = 0; turn < 20 && !ctrl.Data.IsOver; turn++) {
                CPUPlayer who = ctrl.Data.Current == IsOnStone.Black ? cpu : other;
                Assert.IsTrue(ctrl.TryExecuteTurn(who.ChooseAction(ctrl.Data.Clone()), out string err),
                    $"{turn}턴: {err}");
            }
            Assert.IsFalse(ctrl.Data.IsOver, "20턴 안에 게임이 끝나 중반 판이 아님");

            Data snapshot = ctrl.Data.Clone();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            cpu.ChooseAction(snapshot);
            sw.Stop();

            Assert.IsTrue(sw.ElapsedMilliseconds < 500, $"응답 시간 {sw.ElapsedMilliseconds}ms");
        }

        // ───────────── 추가 카드 판단 (설계서 3.4.4·3.4.5) ─────────────

        // 흑 (2,2) · 백 (2,3) → 흑은 (2,4)에 둠 (1개 뒤집기)
        private static Data SimpleMoveData() {
            Data data = EmptyData();
            BoardLogic.PutStone(data, 2, 2, IsOnStone.Black);
            BoardLogic.PutStone(data, 2, 3, IsOnStone.White);
            return data;
        }

        [TestMethod]
        public void CPU15_폭탄_상대돌많은_중심선택() {
            Data data = SimpleMoveData();
            // 오른쪽 아래 모서리 근처 백 돌 (가중치 100 + 10 + 10)
            BoardLogic.PutStone(data, 7, 7, IsOnStone.White);
            BoardLogic.PutStone(data, 7, 5, IsOnStone.White);
            BoardLogic.PutStone(data, 5, 7, IsOnStone.White);

            var bomb = new BombCard();
            Assert.IsTrue(CpuCardLogic.TryChooseTarget(data, IsOnStone.Black, bomb, (2, 4), out var target));
            Assert.AreEqual((6, 6), target, "세 돌을 모두 덮는 중심");

            // 상대 돌을 지우면 착수가 막히고, 나머지는 내 돌만 지우는 판 → 쓰지 않음
            Data none = SimpleMoveData();
            Assert.IsFalse(CpuCardLogic.TryChooseTarget(none, IsOnStone.Black, bomb, (2, 4), out _));

            // 고정 돌은 이득 계산에서 제외
            CellStateLogic.Add(data, 7, 7, CellState.Fixed);
            Assert.AreEqual(20, CpuCardLogic.BombGain(data, IsOnStone.Black, (6, 6)));
        }

        [TestMethod]
        public void CPU16_럭키세븐_7개일때만_사용() {
            // 흑이 (0,0)에 두면 가로 6개 + 세로 1개 = 7개
            Data seven = EmptyData();
            for (int c = 1; c <= 6; c++) BoardLogic.PutStone(seven, 0, c, IsOnStone.White);
            BoardLogic.PutStone(seven, 0, 7, IsOnStone.Black);
            BoardLogic.PutStone(seven, 1, 0, IsOnStone.White);
            BoardLogic.PutStone(seven, 2, 0, IsOnStone.Black);
            var card = new LuckySevenCard();
            seven.HandOf(IsOnStone.Black).TryAdd(card);
            Assert.AreSame(card, CpuCardLogic.ChooseConditionCard(seven, IsOnStone.Black, (0, 0)));

            // 6개면 쓰지 않음
            Data six = EmptyData();
            for (int c = 1; c <= 6; c++) BoardLogic.PutStone(six, 0, c, IsOnStone.White);
            BoardLogic.PutStone(six, 0, 7, IsOnStone.Black);
            six.HandOf(IsOnStone.Black).TryAdd(new LuckySevenCard());
            Assert.IsNull(CpuCardLogic.ChooseConditionCard(six, IsOnStone.Black, (0, 0)));
        }

        [TestMethod]
        public void CPU17_방패_상대응수가_4개이상일때_사용() {
            // 백 (7,0) + 흑 (7,1)~(7,n) → 백이 (7,n+1)에 두면 흑 n개 뒤집기
            Data Build(int n) {
                Data data = SimpleMoveData();
                BoardLogic.PutStone(data, 7, 0, IsOnStone.White);
                for (int c = 1; c <= n; c++) BoardLogic.PutStone(data, 7, c, IsOnStone.Black);
                data.HandOf(IsOnStone.Black).TryAdd(new ShieldCard());
                return data;
            }

            Data four = Build(4);
            Assert.AreEqual(4, CpuCardLogic.MaxOpponentFlipsAfter(four, IsOnStone.Black, (2, 4)));
            Assert.IsInstanceOfType(CpuCardLogic.ChooseConditionCard(four, IsOnStone.Black, (2, 4)), typeof(ShieldCard));

            Data three = Build(3);
            Assert.IsNull(CpuCardLogic.ChooseConditionCard(three, IsOnStone.Black, (2, 4)));
        }

        // ───────────── 설계서 3.5 대조로 추가 ─────────────

        // 시드별로 실제 대국을 진행하며 각 차례의 상태를 넘겨줌 (흑: 어려움, 백: 쉬움 → 무작위 카드 사용으로 상태 다양화)
        private static IEnumerable<GameController> PlayedStates(int seeds) {
            for (int seed = 0; seed < seeds; seed++) {
                var ctrl = new GameController(PlayerSlot.Cpu(Difficulty.Hard), PlayerSlot.Human("b"), seed: seed);
                var black = new CPUPlayer(Difficulty.Hard, seed);
                var white = new CPUPlayer(Difficulty.Easy, seed + 500);
                ctrl.Start();

                for (int t = 0; t < 200 && !ctrl.Data.IsOver; t++) {
                    yield return ctrl;   // 이 시점 상태로 검사
                    if (ctrl.Data.IsOver) break;
                    var who = ctrl.Data.Current == IsOnStone.Black ? black : white;
                    if (!ctrl.TryExecuteTurn(who.ChooseAction(ctrl.Data.Clone()), out _))
                        ctrl.TryExecuteTurn(who.FallbackAction(ctrl.Data), out _);
                }
            }
        }

        [TestMethod]
        public void TC21_행동합법성_상태_1000개() {
            // 각 상태에서 세 난이도 CPU의 행동이 모두 규칙상 합법 (컨트롤러 검증 통과)
            var cpus = new[] {
                new CPUPlayer(Difficulty.Easy, 1), new CPUPlayer(Difficulty.Normal, 2), new CPUPlayer(Difficulty.Hard, 3)
            };
            int checkedStates = 0;

            foreach (var ctrl in PlayedStates(40)) {
                foreach (var cpu in cpus) {
                    // 같은 상태를 복제한 컨트롤러가 없으므로, 실행 대신 같은 검증(복제본 적용)만 수행
                    var action = cpu.ChooseAction(ctrl.Data.Clone());
                    Assert.IsTrue(IsLegal(ctrl.Data, action, out string err), $"{cpu.Difficulty}: {err}");
                }
                if (++checkedStates >= 1000) break;
            }

            Assert.IsTrue(checkedStates >= 1000, $"검사한 상태 {checkedStates}개");
        }

        // GameController.TryExecuteTurn의 사전 검증과 같은 규칙으로 복제본에 적용해 봄
        private static bool IsLegal(Data data, GameAction action, out string error) {
            error = "";
            Data copy = data.Clone();
            IsOnStone me = copy.Current;
            try {
                if (action.Card is ISkillCard card) {
                    if (GameStateLogic.IsSilenced(copy, me)) { error = "침묵 중 카드"; return false; }
                    if (!copy.HandOf(me).Cards.Contains(card)) { error = "손패에 없는 카드"; return false; }
                    if (!card.CanUse(copy, me)) { error = $"{card.Name} 사용 불가"; return false; }
                    card.Apply(copy, me, action.CardTarget);
                }
                if (action.Move is (int Row, int Col) m)
                    BoardLogic.Place(copy, m.Row, m.Col, me);
                else
                    GameLogic.RegisterPass(copy);
                return true;
            }
            catch (InvalidOperationException ex) {
                error = ex.Message;
                return false;
            }
        }

        [TestMethod]
        public void TC24_가중치표_빈칸_40_39_15_14() {
            Data WithEmpty(int empty) {
                Data data = EmptyData();
                for (int i = 0; i < 64 - empty; i++)
                    BoardLogic.PutStone(data, 7 - i / 8, 7 - i % 8, IsOnStone.Black);   // 아래쪽부터 채움 (0행 모서리는 비움)
                BoardLogic.PutStone(data, 7, 7, IsOnStone.White);   // 오른쪽 아래 모서리는 차지됨
                return data;
            }

            Data e40 = WithEmpty(40), e39 = WithEmpty(39), e15 = WithEmpty(15), e14 = WithEmpty(14);
            Assert.AreEqual(WeightTables.Phase.Opening, WeightTables.PhaseOf(e40.Board));
            Assert.AreEqual(WeightTables.Phase.Middle, WeightTables.PhaseOf(e39.Board));
            Assert.AreEqual(WeightTables.Phase.Middle, WeightTables.PhaseOf(e15.Board));
            Assert.AreEqual(WeightTables.Phase.End, WeightTables.PhaseOf(e14.Board));

            // 초반: 차지된 모서리 옆이어도 기본표 감점 유지 / 중반: 0 / 종반: 평탄표
            Assert.AreEqual(-50, WeightTables.WeightAt(e40.Board, 6, 6));
            Assert.AreEqual(0, WeightTables.WeightAt(e39.Board, 6, 6));
            Assert.AreEqual(-20, WeightTables.WeightAt(e39.Board, 0, 1), "빈 모서리 옆은 그대로");
            Assert.AreEqual(5, WeightTables.WeightAt(e14.Board, 3, 3));
            Assert.AreEqual(30, WeightTables.WeightAt(e14.Board, 0, 0));
        }

        [TestMethod]
        public void TC25_가지치기_켬끔_동일행동() {
            int compared = 0;
            foreach (var ctrl in PlayedStates(15)) {
                foreach (var level in new[] { Difficulty.Normal, Difficulty.Hard }) {
                    var on = new CPUPlayer(level, 7).ChooseAction(ctrl.Data.Clone());
                    var off = new CPUPlayer(level, 7) { UsePruning = false }.ChooseAction(ctrl.Data.Clone());
                    Assert.AreEqual(on, off, $"{level}, 턴 {ctrl.Data.TurnNumber}");
                    compared++;
                }
            }
            Assert.IsTrue(compared > 300, $"비교 {compared}회");
        }

        [TestMethod]
        public void TC26_어려움_30수_각_0_5초_이내() {
            var hard = new CPUPlayer(Difficulty.Hard, 1);
            long max = 0;
            int measured = 0;

            foreach (var ctrl in PlayedStates(5)) {
                if (ctrl.Data.IsOver || ctrl.Data.Current != IsOnStone.Black) continue;
                Data snapshot = ctrl.Data.Clone();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                hard.ChooseAction(snapshot);
                sw.Stop();
                max = Math.Max(max, sw.ElapsedMilliseconds);
                if (++measured >= 30) break;
            }

            Assert.AreEqual(30, measured);
            Assert.IsTrue(max < 500, $"최대 {max}ms");
        }
    }
}
