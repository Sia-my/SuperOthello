using Othello.Core.Cards;
using Othello.Core.Control;
using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Tests {
    // 핵심 규칙 테스트
    // - 메서드 이름 앞 TC 번호 = 설계서 3.5 테스트 번호

    [TestClass]
    public class CoreTests {
        // 중앙에 돌 4개가 배치된 게임 데이터
        private static Data NewGameData() {
            var data = new Data();
            GameLogic.Reset(data);
            return data;
        }

        [TestMethod]
        public void TC01_판초기화_중앙4칸() {
            Data data = NewGameData();

            Assert.AreEqual(
                IsOnStone.White,
                data.Board.Cells[3, 3].Stone);

            Assert.AreEqual(
                IsOnStone.Black,
                data.Board.Cells[3, 4].Stone);

            Assert.AreEqual(
                IsOnStone.Black,
                data.Board.Cells[4, 3].Stone);

            Assert.AreEqual(
                IsOnStone.White,
                data.Board.Cells[4, 4].Stone);

            Assert.AreEqual(60, BoardLogic.EmptyCount(data));
        }

        [TestMethod]
        public void TC02_초기상태_흑착수가능_4곳() {
            Data data = NewGameData();

            var moves = BoardLogic.GetValidMoves(
                data, IsOnStone.Black);

            CollectionAssert.AreEquivalent(
                new[]
                {
                    (2, 3),
                    (3, 2),
                    (4, 5),
                    (5, 4)
                },
                moves.ToArray());
        }

        [TestMethod]
        public void TC03_고정돌은_뒤집히지_않음() {
            // 행 0: 흑 백(고정) 백 [빈칸]
            // 흑이 (0,3)에 두면 (0,2)만 뒤집힌다.

            var data = new Data();

            BoardLogic.PutStone(
                data, 0, 0, IsOnStone.Black);

            BoardLogic.PutStone(
                data, 0, 1, IsOnStone.White);

            BoardLogic.PutStone(
                data, 0, 2, IsOnStone.White);

            CellStateLogic.Add(
                data, 0, 1, CellState.Fixed);

            var flips = BoardLogic.Place(
                data, 0, 3, IsOnStone.Black);

            Assert.AreEqual(1, flips.Count);

            Assert.AreEqual(
                IsOnStone.White,
                data.Board.Cells[0, 1].Stone);

            Assert.AreEqual(
                IsOnStone.Black,
                data.Board.Cells[0, 2].Stone);
        }

        [TestMethod]
        public void TC03b_선물은_내돌만_대상이고_고정돌은_제외() {
            Data data = NewGameData();
            var gift = new GiftCard();
            var targets = gift.GetTargets(data, IsOnStone.Black);
            CollectionAssert.AreEquivalent(new[] { (3, 4), (4, 3) }, targets.ToArray());

            CellStateLogic.Add(data, 3, 4, CellState.Fixed);
            Assert.IsFalse(gift.GetTargets(data, IsOnStone.Black).Contains((3, 4)));
            Assert.IsTrue(gift.GetTargets(data, IsOnStone.Black).Contains((4, 3)));
        }

        [TestMethod]
        public void TC05_점수계산_3개뒤집기() {
            var data = new Data();

            ScoreLogic.OnPlace(
                data, IsOnStone.Black, 3);

            Assert.AreEqual(3, data.Scores[0]);
            Assert.AreEqual(-3, data.Scores[1]);
        }

        [TestMethod]
        public void TC05b_점수2배는_득점에만_적용() {
            var data = new Data {
                TurnScoreMultiplier = 2
            };

            ScoreLogic.OnPlace(
                data, IsOnStone.Black, 3);

            Assert.AreEqual(6, data.Scores[0]);
            Assert.AreEqual(-3, data.Scores[1]);
        }

        [TestMethod]
        public void TC06_종료보너스_남은돌수만큼() {
            Data data = NewGameData();

            ScoreLogic.ApplyFinalBonus(data);

            Assert.AreEqual(2, data.Scores[0]);
            Assert.AreEqual(2, data.Scores[1]);
        }

        [TestMethod]
        public void TC06b_종료보너스와_최종점수_분리계산() {
            Data data = NewGameData();
            data.Scores[0] = 7;    // 종료 전 뒤집기·카드 점수
            data.Scores[1] = -3;
            BoardLogic.PutStone(data, 0, 0, IsOnStone.Black);   // 흑 3개, 백 2개

            GameLogic.Finish(data);

            var black = ScoreLogic.BreakdownOf(data, IsOnStone.Black);
            var white = ScoreLogic.BreakdownOf(data, IsOnStone.White);

            Assert.AreEqual((7, 3, 10), black);
            Assert.AreEqual((-3, 2, -1), white);
            Assert.AreEqual(black.Play + black.Bonus, black.Final);
            Assert.AreEqual(white.Play + white.Bonus, white.Final);
            Assert.AreEqual(data.Scores[0], black.Final);
            Assert.AreEqual(data.Scores[1], white.Final);
        }

        [TestMethod]
        public void TC07_전향_상대돌을_내돌로_사용자_마이너스2() {
            Data data = NewGameData();

            var card = new ConvertCard();

            card.Apply(
                data, IsOnStone.Black, (3, 3));

            Assert.AreEqual(
                IsOnStone.Black,
                data.Board.Cells[3, 3].Stone);

            Assert.AreEqual(-2, data.Scores[0]);
        }

        [TestMethod]
        public void TC07b_선물_내돌을_상대돌로_바꾸고_1점획득() {
            Data data = NewGameData();
            var gift = new GiftCard();
            gift.Apply(data, IsOnStone.Black, (3, 4));

            Assert.AreEqual(IsOnStone.White, data.Board.Cells[3, 4].Stone);
            Assert.AreEqual(1, data.Scores[0]);
            Assert.AreEqual(0, data.Scores[1]);
        }

        [TestMethod]
        public void TC09_손패한도_3장() {
            var hand = new Hand();

            Assert.IsTrue(
                hand.TryAdd(new FixCard()));

            Assert.IsTrue(
                hand.TryAdd(new FixCard()));

            Assert.IsTrue(
                hand.TryAdd(new FixCard()));

            Assert.IsFalse(
                hand.TryAdd(new FixCard()));
        }

        [TestMethod]
        public void TC10_사용한패는_덱으로_돌아감() {
            var controller = new GameController(
                PlayerSlot.Human("a"),
                PlayerSlot.Human("b"),
                new ISkillCard[]
                {
                    new SilenceCard(),
                    new SilenceCard(),
                    new SilenceCard()
                },
                seed: 1);

            // 3장 중 2장을 지급했으므로 덱에는 1장
            int before = controller.Data.Deck.Count;

            ISkillCard card = controller.Data
                .HandOf(IsOnStone.Black).Cards[0];

            bool ok = controller.TryExecuteTurn(
                GameAction.WithCard(
                    card, null, (2, 3)),
                out string error);

            Assert.IsTrue(ok, error);

            Assert.AreEqual(
                before + 1,
                controller.Data.Deck.Count);
        }

        [TestMethod]
        public void TC11_Clone은_원본과_독립() {
            var controller = new GameController(
                PlayerSlot.Human("a"),
                PlayerSlot.Human("b"),
                CardPool.Current(),
                seed: 1);

            Data original = controller.Data;
            Data copy = original.Clone();

            BoardLogic.Place(
                copy, 2, 3, IsOnStone.Black);

            copy.Scores[0] = 99;

            copy.HandOf(IsOnStone.Black).Remove(
                copy.HandOf(IsOnStone.Black).Cards[0]);

            Assert.AreEqual(
                IsOnStone.Empty,
                original.Board.Cells[2, 3].Stone);

            Assert.AreEqual(0, original.Scores[0]);

            Assert.AreEqual(
                1,
                original.HandOf(IsOnStone.Black).Count);
        }

        [TestMethod]
        public void 잘못된착수는_상태를_바꾸지_않음() {
            var controller = new GameController(
                PlayerSlot.Human("a"),
                PlayerSlot.Human("b"),
                seed: 1);

            bool ok = controller.TryExecuteTurn(
                GameAction.Place(0, 0),
                out _);

            Assert.IsFalse(ok);

            Assert.AreEqual(
                IsOnStone.Black,
                controller.Data.Current);

            Assert.AreEqual(
                60,
                BoardLogic.EmptyCount(controller.Data));
        }

        // ───────────── 설계서 3.5 대조로 추가 ─────────────

        // 판을 비우고 원하는 돌만 놓은 컨트롤러 (덱은 침묵 8장, 흑 차례)
        private static GameController ControllerWithBoard(
            PlayerSlot? black, params (int Row, int Col, IsOnStone Stone)[] stones) {
            var pool = Enumerable.Range(0, 8).Select(_ => (ISkillCard)new SilenceCard()).ToArray();
            var ctrl = new GameController(black ?? PlayerSlot.Human("a"), PlayerSlot.Human("b"), pool, seed: 1);
            ctrl.Data.Board.Cells = new Cell[Board.Size, Board.Size];
            foreach (var (r, c, s) in stones) BoardLogic.PutStone(ctrl.Data, r, c, s);
            return ctrl;
        }

        [TestMethod]
        public void TC03c_다방향_뒤집기() {
            // (3,3)에 두면 가로·세로·대각선 세 방향이 함께 뒤집힘
            var data = new Data();
            BoardLogic.PutStone(data, 3, 4, IsOnStone.White);
            BoardLogic.PutStone(data, 3, 5, IsOnStone.Black);
            BoardLogic.PutStone(data, 4, 3, IsOnStone.White);
            BoardLogic.PutStone(data, 5, 3, IsOnStone.White);
            BoardLogic.PutStone(data, 6, 3, IsOnStone.Black);
            BoardLogic.PutStone(data, 4, 4, IsOnStone.White);
            BoardLogic.PutStone(data, 5, 5, IsOnStone.Black);
            BoardLogic.PutStone(data, 2, 3, IsOnStone.White);   // 끝에 흑이 없는 방향은 그대로

            var flips = BoardLogic.Place(data, 3, 3, IsOnStone.Black);

            Assert.AreEqual(4, flips.Count);
            foreach (var (r, c) in new[] { (3, 4), (4, 3), (5, 3), (4, 4) })
                Assert.AreEqual(IsOnStone.Black, data.Board.Cells[r, c].Stone, $"({r},{c})");
            Assert.AreEqual(IsOnStone.White, data.Board.Cells[2, 3].Stone);
        }

        [TestMethod]
        public void TC04_패스와_종료() {
            // 1인 무착수: 흑은 둘 곳이 없고 백은 (0,2)에 둘 수 있음 → 흑 자동 패스
            var data = new Data();
            BoardLogic.PutStone(data, 0, 0, IsOnStone.White);
            BoardLogic.PutStone(data, 0, 1, IsOnStone.Black);

            Assert.IsTrue(GameLogic.TryAutoPass(data, out IsOnStone passed));
            Assert.AreEqual(IsOnStone.Black, passed);
            Assert.AreEqual(IsOnStone.White, data.Current);
            Assert.IsFalse(GameLogic.BothPlayersCannotMove(data));

            // 양측 무착수: 흑이 (0,2)에 두면 판에 흑만 남아 아무도 둘 수 없음 → 종료
            var ctrl = ControllerWithBoard(null,
                (0, 0, IsOnStone.Black), (0, 1, IsOnStone.White));
            bool over = false;
            ctrl.GameOver += _ => over = true;

            Assert.IsTrue(ctrl.TryPlace(0, 2, out string err), err);
            Assert.IsTrue(ctrl.Data.IsOver);
            Assert.IsTrue(over);
        }

        [TestMethod]
        public void TC07m_침묵_상대는_다음턴_패사용불가_후_해제() {
            var ctrl = ControllerWithBoard(null);
            GameLogic.Reset(ctrl.Data);
            ISkillCard silence = ctrl.Data.HandOf(IsOnStone.Black).Cards[0];
            ISkillCard whiteCard = ctrl.Data.HandOf(IsOnStone.White).Cards[0];

            Assert.IsTrue(ctrl.TryExecuteTurn(GameAction.WithCard(silence, null, (2, 3)), out string err), err);

            Assert.IsTrue(GameStateLogic.IsSilenced(ctrl.Data, IsOnStone.White));
            Assert.IsFalse(ctrl.CanUseCard(whiteCard));
            Assert.IsFalse(ctrl.TryExecuteTurn(GameAction.WithCard(whiteCard, null, (2, 2)), out _));

            Assert.IsTrue(ctrl.TryPlace(2, 2, out err), err);
            Assert.IsFalse(GameStateLogic.IsSilenced(ctrl.Data, IsOnStone.White), "자기 턴이 끝나면 해제");
        }

        [TestMethod]
        public void TC07n_고정_내돌만_대상이고_적용후_뒤집기_제거불가() {
            Data data = NewGameData();
            var fix = new FixCard();

            var targets = fix.GetTargets(data, IsOnStone.Black);
            CollectionAssert.AreEquivalent(new[] { (3, 4), (4, 3) }, targets.ToList());   // 흑 돌만

            fix.Apply(data, IsOnStone.Black, (3, 4));
            Assert.IsTrue(CellStateLogic.Has(data, 3, 4, CellState.Fixed));
            Assert.IsFalse(CellStateLogic.CanFlip(data.Board.Cells[3, 4]));
            Assert.IsFalse(CellStateLogic.CanRemove(data.Board.Cells[3, 4]));
            Assert.IsFalse(fix.GetTargets(data, IsOnStone.Black).Contains((3, 4)), "이미 고정된 돌은 대상 아님");
        }

        [TestMethod]
        public void TC08_3개이상_뒤집으면_1장_획득_미만이면_없음() {
            // 흑 [백 ×3] 빈칸 → (0,4)에 두면 3개
            var three = ControllerWithBoard(null,
                (0, 0, IsOnStone.Black), (0, 1, IsOnStone.White), (0, 2, IsOnStone.White), (0, 3, IsOnStone.White));
            int before = three.Data.HandOf(IsOnStone.Black).Count;
            Assert.IsTrue(three.TryPlace(0, 4, out string err), err);
            Assert.AreEqual(before + 1, three.Data.HandOf(IsOnStone.Black).Count);

            // 2개면 획득 없음
            var two = ControllerWithBoard(null,
                (0, 0, IsOnStone.Black), (0, 1, IsOnStone.White), (0, 2, IsOnStone.White), (5, 5, IsOnStone.White), (5, 6, IsOnStone.Black));
            before = two.Data.HandOf(IsOnStone.Black).Count;
            Assert.IsTrue(two.TryPlace(0, 3, out err), err);
            Assert.AreEqual(before, two.Data.HandOf(IsOnStone.Black).Count);
        }

        [TestMethod]
        public void TC09b_손패3장_사람은_버릴패선택_CPU는_획득생략() {
            var stones = new[] {
                (0, 0, IsOnStone.Black), (0, 1, IsOnStone.White), (0, 2, IsOnStone.White), (0, 3, IsOnStone.White),
                (5, 5, IsOnStone.White), (5, 6, IsOnStone.Black)
            };

            // 사람: 4장 중 1장을 골라 버림
            var human = ControllerWithBoard(null, stones);
            Hand hand = human.Data.HandOf(IsOnStone.Black);
            hand.TryAdd(new FixCard());
            hand.TryAdd(new GiftCard());
            ISkillCard toDiscard = hand.Cards[1];
            int offered = 0;
            human.ChooseDiscard = cards => { offered = cards.Count; return toDiscard; };
            int deck = human.Data.Deck.Count;

            Assert.IsTrue(human.TryPlace(0, 4, out string err), err);
            Assert.AreEqual(4, offered, "새 패 포함 4장 중 선택");
            Assert.AreEqual(Hand.MaxSize, hand.Count);
            Assert.IsFalse(hand.Cards.Contains(toDiscard));
            Assert.AreEqual(deck, human.Data.Deck.Count, "1장 뽑고 1장 버림");

            // CPU: 손패가 가득 차면 새 패를 받지 않음
            var cpu = ControllerWithBoard(PlayerSlot.Cpu(Difficulty.Normal), stones);
            Hand cpuHand = cpu.Data.HandOf(IsOnStone.Black);
            cpuHand.TryAdd(new FixCard());
            cpuHand.TryAdd(new GiftCard());
            var kept = cpuHand.Cards.ToList();
            deck = cpu.Data.Deck.Count;

            Assert.IsTrue(cpu.TryPlace(0, 4, out err), err);
            CollectionAssert.AreEqual(kept, cpuHand.Cards.ToList());
            Assert.AreEqual(deck, cpu.Data.Deck.Count);
        }

        [TestMethod]
        public void TC12_최종점수는_행동로그_합계와_일치() {
            // 설계서 2.3 신뢰성: 착수 로그(+획득 / 상대 -감점) + 카드 로그(-n점) + 종료 보너스 = 최종 점수
            var place = new System.Text.RegularExpressions.Regex(@"^\[\d+턴\] (흑|백) \(\d,\d\) 착수, \d+개 뒤집음 \(\+(\d+) / 상대 -(\d+)\)$");
            var card = new System.Text.RegularExpressions.Regex(@"^\[\d+턴\] (흑|백) \[.+\] .*\(-(\d+)점\)$");
            var gift = new System.Text.RegularExpressions.Regex(@"^\[\d+턴\] (흑|백) \[선물\] .*\(\+(\d+)점\)$");
            var bonus = new System.Text.RegularExpressions.Regex(@"^종료 보너스 - 흑 \+(\d+) / 백 \+(\d+)$");

            for (int seed = 0; seed < 30; seed++) {
                var ctrl = new GameController(PlayerSlot.Cpu(Difficulty.Hard), PlayerSlot.Human("b"), seed: seed);
                var cpus = new[] { new Othello.Core.Cpu.CPUPlayer(Difficulty.Hard, seed), new Othello.Core.Cpu.CPUPlayer(Difficulty.Easy, seed + 1) };
                var sum = new int[2];
                bool sawBonus = false;

                ctrl.LogAdded += msg => {
                    var m = place.Match(msg);
                    if (m.Success) {
                        int me = m.Groups[1].Value == "흑" ? 0 : 1;
                        sum[me] += int.Parse(m.Groups[2].Value);
                        sum[1 - me] -= int.Parse(m.Groups[3].Value);
                        return;
                    }
                    m = card.Match(msg);
                    if (m.Success) {
                        sum[m.Groups[1].Value == "흑" ? 0 : 1] -= int.Parse(m.Groups[2].Value);
                        return;
                    }
                    m = gift.Match(msg);
                    if (m.Success) {
                        sum[m.Groups[1].Value == "흑" ? 0 : 1] += int.Parse(m.Groups[2].Value);
                        return;
                    }

                    m = bonus.Match(msg);
                    if (m.Success) {
                        sum[0] += int.Parse(m.Groups[1].Value);
                        sum[1] += int.Parse(m.Groups[2].Value);
                        sawBonus = true;
                    }
                };

                ctrl.Start();
                for (int t = 0; t < 200 && !ctrl.Data.IsOver; t++) {
                    var who = cpus[ctrl.Data.Current == IsOnStone.Black ? 0 : 1];
                    Assert.IsTrue(ctrl.TryExecuteTurn(who.ChooseAction(ctrl.Data.Clone()), out string err), $"seed {seed}: {err}");
                }

                Assert.IsTrue(ctrl.Data.IsOver, $"seed {seed}");
                Assert.IsTrue(sawBonus, $"seed {seed}: 보너스 로그");
                Assert.AreEqual(ctrl.Data.Scores[0], sum[0], $"seed {seed} 흑");
                Assert.AreEqual(ctrl.Data.Scores[1], sum[1], $"seed {seed} 백");
            }
        }
    }
}
