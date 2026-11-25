using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Othello.Core.Cards;

namespace Othello.Tests {
    // 카드 도감 표시 값 테스트 (설계서 2.2 후보 24종 표)

    [TestClass]
    public class CardTests {
        [TestMethod]
        public void CARD01_ScoreEffect_설계서값() {
            // 설계서 표의 "점수 영향" 열 (Id = 후보 번호)
            var expected = new Dictionary<int, string> {
                [4] = "사용자 -2",      // 전향
                [6] = "획득 점수 ×2",   // 점수 2배
                [7] = "없음",           // 고정
                [9] = "없음",           // 봉인
                [11] = "없음"           // 침묵
            };

            var cards = new SkillCardBase[] {
                new ConvertCard(), new DoubleScoreCard(), new FixCard(), new SealCard(), new SilenceCard()
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
