using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Othello.Core.Cards;
using Othello.Core.Control;
using Othello.Core.Cpu;
using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Tests {
    // 카드 효과·도감 값 테스트 (설계서 2.2 후보 24종 표, 3.5 TC-07)
    // - 판을 직접 만들 때는 빈 Data에 BoardLogic.PutStone으로 돌 배치

    [TestClass]
    public class CardTests {
        private static Data NewGameData() {
            var data = new Data();
            GameLogic.Reset(data);
            return data;
        }

        // 흑이 (0,0)에 두면 정확히 n개(1~7)를 뒤집는 판
        // - 가로: (0,1)~(0,k) 백 + 끝에 흑, 나머지는 세로로
        private static Data FlipBoard(int n) {
            var data = new Data();
            int across = System.Math.Min(n, 6);
            for (int c = 1; c <= across; c++) BoardLogic.PutStone(data, 0, c, IsOnStone.White);
            BoardLogic.PutStone(data, 0, across + 1, IsOnStone.Black);

            int down = n - across;
            if (down > 0) {
                for (int r = 1; r <= down; r++) BoardLogic.PutStone(data, r, 0, IsOnStone.White);
                BoardLogic.PutStone(data, down + 1, 0, IsOnStone.Black);
            }
            return data;
        }

        private static (int gain, int penalty) PlaceBlackAtCorner(Data data, bool whiteShielded = false) {
            var flips = BoardLogic.Place(data, 0, 0, IsOnStone.Black);
            return ScoreLogic.OnPlace(data, IsOnStone.Black, flips.Count, whiteShielded);
        }

        // ───────────── B형: 방패·럭키 세븐·마이너스의 손 ─────────────

        [TestMethod]
        public void TC07e_방패_상대턴_감점절반_다음내턴_해제() {
            var ctrl = new GameController(
                PlayerSlot.Human("a"), PlayerSlot.Human("b"),
                new ISkillCard[] { new ShieldCard(), new ShieldCard(), new ShieldCard() }, seed: 1);
            ISkillCard shield = ctrl.Data.HandOf(IsOnStone.Black).Cards[0];

            // 흑: 방패 + (2,3) 착수 → 1개 뒤집기 (흑 +1, 백 -1)
            Assert.IsTrue(ctrl.TryExecuteTurn(GameAction.WithCard(shield, null, (2, 3)), out string err), err);
            Assert.IsTrue(GameStateLogic.IsShielded(ctrl.Data, IsOnStone.Black), "상대 턴 동안 유지");
            Assert.AreEqual(1, ctrl.Data.Scores[0]);

            // 백: (2,2) 착수 → 흑 돌 1개 뒤집기. 감점 1 → 절반(내림) 0
            Assert.IsTrue(ctrl.TryPlace(2, 2, out err), err);
            Assert.AreEqual(1, ctrl.Data.Scores[0], "방패로 감점 0");
            Assert.IsFalse(GameStateLogic.IsShielded(ctrl.Data, IsOnStone.Black), "상대 턴이 끝나면 해제");
        }

        [TestMethod]
        public void TC07f_럭키세븐_정확히7개면_플러스7_아니면_없음() {
            Data seven = FlipBoard(7);
            new LuckySevenCard().Apply(seven, IsOnStone.Black, null);
            Assert.AreEqual((7 + 7, 7), PlaceBlackAtCorner(seven));

            Data six = FlipBoard(6);
            new LuckySevenCard().Apply(six, IsOnStone.Black, null);
            Assert.AreEqual((6, 6), PlaceBlackAtCorner(six));
        }

        [TestMethod]
        public void TC07g_마이너스의손_상대감점2배() {
            Data data = FlipBoard(3);
            new MinusHandCard().Apply(data, IsOnStone.Black, null);

            Assert.AreEqual((3, 6), PlaceBlackAtCorner(data));
            Assert.AreEqual(3, data.Scores[0]);
            Assert.AreEqual(-6, data.Scores[1]);
        }

        [TestMethod]
        public void TC07h_마이너스의손과_방패_동시면_2n의_절반() {
            Data data = FlipBoard(3);
            new MinusHandCard().Apply(data, IsOnStone.Black, null);

            Assert.AreEqual((3, 3), PlaceBlackAtCorner(data, whiteShielded: true));   // 2×3 = 6 → 3

            Data odd = FlipBoard(3);
            Assert.AreEqual((3, 1), PlaceBlackAtCorner(odd, whiteShielded: true));     // 방패만: 3 → 1 (내림)
        }

        [TestMethod]
        public void TC07l_턴한정효과는_턴종료후_초기화() {
            // 실제 턴 종료
            Data data = NewGameData();
            new DoubleScoreCard().Apply(data, IsOnStone.Black, null);
            new LuckySevenCard().Apply(data, IsOnStone.Black, null);
            new MinusHandCard().Apply(data, IsOnStone.Black, null);

            GameLogic.EndTurn(data);
            Assert.AreEqual(1, data.TurnScoreMultiplier);
            Assert.AreEqual(1, data.TurnPenaltyMultiplier);
            Assert.IsFalse(data.TurnLuckySeven);

            // 새 게임
            new MinusHandCard().Apply(data, IsOnStone.Black, null);
            GameLogic.Reset(data);
            Assert.AreEqual(1, data.TurnPenaltyMultiplier);

            // CPU 시뮬레이션 착수 후
            foreach (ISkillCard card in new ISkillCard[] { new DoubleScoreCard(), new LuckySevenCard(), new MinusHandCard() }) {
                Data sim = NewGameData();
                sim.HandOf(IsOnStone.Black).TryAdd(card);
                Assert.IsTrue(CpuSimLogic.TryApply(sim, new CpuCandidate(card, null, (2, 3)), IsOnStone.Black), card.Name);
                Assert.AreEqual(1, sim.TurnScoreMultiplier, card.Name);
                Assert.AreEqual(1, sim.TurnPenaltyMultiplier, card.Name);
                Assert.IsFalse(sim.TurnLuckySeven, card.Name);
            }
        }

        [TestMethod]
        public void TC11b_새필드도_Clone으로_독립() {
            Data original = NewGameData();
            Data copy = original.Clone();

            copy.TurnPenaltyMultiplier = 2;
            copy.TurnLuckySeven = true;
            GameStateLogic.SetShielded(copy, IsOnStone.Black, true);

            Assert.AreEqual(1, original.TurnPenaltyMultiplier);
            Assert.IsFalse(original.TurnLuckySeven);
            Assert.IsFalse(GameStateLogic.IsShielded(original, IsOnStone.Black));
        }

        // ───────────── C형: 약탈·보급·역전의 발판 ─────────────

        // 흑 손패를 원하는 카드로 채운 컨트롤러 (덱은 침묵 8장)
        private static GameController ControllerWithBlackHand(params ISkillCard[] hand) {
            var pool = Enumerable.Range(0, 8).Select(_ => (ISkillCard)new SilenceCard()).ToArray();
            var ctrl = new GameController(PlayerSlot.Human("a"), PlayerSlot.Human("b"), pool, seed: 1);

            Hand black = ctrl.Data.HandOf(IsOnStone.Black);
            foreach (var c in black.Cards.ToList()) {
                black.Remove(c);
                ctrl.Data.Deck.Return(c);
            }
            foreach (var c in hand) black.ForceAdd(c);
            return ctrl;
        }

        [TestMethod]
        public void TC07i_약탈_상대손패1장_내손패로() {
            var plunder = new PlunderCard();
            var ctrl = ControllerWithBlackHand(plunder);
            Hand white = ctrl.Data.HandOf(IsOnStone.White);
            ISkillCard whiteCard = white.Cards[0];
            int deckBefore = ctrl.Data.Deck.Count;

            Assert.IsTrue(ctrl.TryExecuteTurn(GameAction.WithCard(plunder, null, (2, 3)), out string err), err);

            CollectionAssert.AreEqual(new[] { whiteCard }, ctrl.Data.HandOf(IsOnStone.Black).Cards.ToList());
            Assert.AreEqual(0, white.Count);
            Assert.AreEqual(deckBefore + 1, ctrl.Data.Deck.Count, "사용한 약탈은 덱으로");

            // 상대(백) 손패가 없으면 흑은 약탈 사용 불가
            Assert.IsFalse(new PlunderCard().CanUse(ctrl.Data, IsOnStone.Black));
        }

        [TestMethod]
        public void TC07j_보급_손패3장_한도초과분은_덱으로() {
            var supply = new SupplyCard();
            var keep1 = new SealCard();
            var keep2 = new FixCard();
            var ctrl = ControllerWithBlackHand(supply, keep1, keep2);
            int deckBefore = ctrl.Data.Deck.Count;

            // 보급을 뺀 2장 + 받을 수 있는 1장 = 3장. 2장 중 1장은 덱으로
            Assert.IsTrue(ctrl.TryExecuteTurn(GameAction.WithCard(supply, null, (2, 3)), out string err), err);

            Hand black = ctrl.Data.HandOf(IsOnStone.Black);
            Assert.AreEqual(Hand.MaxSize, black.Count);
            Assert.IsFalse(black.Cards.Contains(supply));
            CollectionAssert.IsSubsetOf(new ISkillCard[] { keep1, keep2 }, black.Cards.ToList());
            Assert.AreEqual(deckBefore, ctrl.Data.Deck.Count, "2장 뽑고 1장 + 보급이 돌아옴");

            // 손패 1장(보급뿐)이면 2장 모두 받음
            var supply2 = new SupplyCard();
            var ctrl2 = ControllerWithBlackHand(supply2);
            Assert.IsTrue(ctrl2.TryExecuteTurn(GameAction.WithCard(supply2, null, (2, 3)), out err), err);
            Assert.AreEqual(2, ctrl2.Data.HandOf(IsOnStone.Black).Count);

            // 덱이 1장뿐이면 1장만 (예외 없음)
            Data small = NewGameData();
            small.Deck = new CardDeck(new ISkillCard[] { new SilenceCard() }, seed: 1);
            var supply3 = new SupplyCard();
            small.HandOf(IsOnStone.Black).TryAdd(supply3);
            Assert.AreEqual("1장 획득", supply3.Apply(small, IsOnStone.Black, null));
            Assert.AreEqual(0, small.Deck.Count);
        }

        [TestMethod]
        public void TC07k_역전의발판_이기고있으면_사용불가() {
            Data data = NewGameData();
            data.Deck = new CardDeck(new ISkillCard[] { new SilenceCard(), new SilenceCard(), new SilenceCard() }, seed: 1);
            var card = new ComebackCard();
            data.HandOf(IsOnStone.Black).TryAdd(card);

            data.Scores[0] = 5; data.Scores[1] = 3;
            Assert.IsFalse(card.CanUse(data, IsOnStone.Black), "이기고 있음");

            data.Scores[0] = 3; data.Scores[1] = 3;
            Assert.IsFalse(card.CanUse(data, IsOnStone.Black), "동점");

            data.Scores[0] = 2; data.Scores[1] = 3;
            Assert.IsTrue(card.CanUse(data, IsOnStone.Black), "지고 있음");

            // 역전의 발판을 뺀 0장 + 3장 = 3장
            Assert.AreEqual("3장 획득", card.Apply(data, IsOnStone.Black, null));
            data.HandOf(IsOnStone.Black).Remove(card);
            Assert.AreEqual(3, data.HandOf(IsOnStone.Black).Count);

            // 덱이 비면 사용 불가
            Assert.IsFalse(new ComebackCard().CanUse(data, IsOnStone.Black));
        }

        // ───────────── 도감 값 ─────────────

        [TestMethod]
        public void CARD01_ScoreEffect_설계서값() {
            // 설계서 표의 "점수 영향" 열 (Id = 후보 번호)
            var expected = new Dictionary<int, string> {
                [4] = "사용자 -2",            // 전향
                [6] = "획득 점수 ×2",         // 점수 2배
                [7] = "없음",                 // 고정
                [8] = "감점 ÷ 2",             // 방패
                [9] = "없음",                 // 봉인
                [10] = "없음",                // 약탈
                [11] = "없음",                // 침묵
                [12] = "없음",                // 보급
                [17] = "사용자 +7",           // 럭키 세븐
                [19] = "사용자 +n, 상대 -2n", // 마이너스의 손
                [23] = "없음"                 // 역전의 발판
            };

            var cards = new SkillCardBase[] {
                new ConvertCard(), new DoubleScoreCard(), new FixCard(), new ShieldCard(),
                new SealCard(), new PlunderCard(), new SilenceCard(), new SupplyCard(),
                new LuckySevenCard(), new MinusHandCard(), new ComebackCard()
            };

            Assert.AreEqual(expected.Count, cards.Length);
            foreach (var card in cards)
                Assert.AreEqual(expected[card.Id], card.ScoreEffect, card.Name);

            // 현재 덱의 카드가 모두 확인 대상에 포함되는지
            CollectionAssert.IsSubsetOf(
                CardPool.Current().Select(c => c.Id).Distinct().ToList(),
                expected.Keys.ToList());
        }
    }
}
