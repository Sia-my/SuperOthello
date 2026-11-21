
using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Othello.Core.Cards;
using Othello.Core.Control;
using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Tests {
    /*
     * CoreTests.cs - 현재 게임 구조의 핵심 기능 테스트
     *
     * [사용 안내]
     * MSTest 테스트 프로젝트에서 실행한다.
     *
     * Data + GameLogic.Reset : 초기 게임 상태
     * BoardLogic             : 착수 및 돌 뒤집기
     * CellStateLogic         : 고정, 봉인 상태
     * ScoreLogic             : 점수 계산
     * GameController         : 카드 사용 및 턴 처리
     *
     * 기존 테스트의 TC 번호와 검증 목적을 유지한다.
     */

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
        public void TC03b_봉인칸에는_둘_수_없음() {
            Data data = NewGameData();

            CellStateLogic.Add(
                data, 2, 3, CellState.Sealed);

            data.Board.Cells[2, 3].SealTurns = 3;

            var moves = BoardLogic.GetValidMoves(
                data, IsOnStone.Black);

            Assert.IsFalse(moves.Contains((2, 3)));
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
        public void TC07b_봉인_3턴뒤_해제() {
            Data data = NewGameData();

            new SealCard().Apply(
                data, IsOnStone.Black, (0, 0));

            for (int i = 0; i < SealCard.SealDuration; i++) {
                Assert.IsTrue(
                    CellStateLogic.Has(
                        data, 0, 0, CellState.Sealed));

                CellStateLogic.ProcessTurnEnd(data);
            }

            Assert.IsFalse(
                CellStateLogic.Has(
                    data, 0, 0, CellState.Sealed));

            Assert.AreEqual(
                (byte)0,
                data.Board.Cells[0, 0].SealTurns);
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
    }
}
