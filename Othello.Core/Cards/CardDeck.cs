using System;
using System.Collections.Generic;
using System.Linq;

namespace Othello.Core.Cards {
    /*
     * CardDeck.cs - 공용 초능력 카드 덱
     *
     * [사용 안내]
     *
     * 빈 덱 생성:
     *   new CardDeck()
     *
     * 카드 풀로 덱 생성:
     *   new CardDeck(cardPool)
     *
     * 고정 시드로 생성:
     *   new CardDeck(cardPool, seed)
     *
     * 남은 카드 수 확인:
     *   deck.Count
     *
     * 화면 표시용 목록:
     *   deck.Peek()
     *
     * 카드 한 장 뽑기:
     *   deck.Draw()
     *
     * 사용한 카드 반환:
     *   deck.Return(card)
     *
     * 덱 섞기:
     *   deck.Shuffle()
     *
     * 덱 복제:
     *   deck.Clone()
     *
     * 카드 효과는 처리하지 않는다.
     * 드로우와 반환 시점은 GameController가 관리한다.
     */

    public class CardDeck {
        private readonly List<ISkillCard> cards = new();
        private readonly Random rng;

        public CardDeck()
            : this(Array.Empty<ISkillCard>()) { }

        public CardDeck(
            IEnumerable<ISkillCard> pool,
            int? seed = null) {
            rng = seed.HasValue
                ? new Random(seed.Value)
                : new Random();

            cards.AddRange(pool);
            Shuffle();
        }

        private CardDeck(
            List<ISkillCard> copy,
            Random rng) {
            cards = copy;
            this.rng = rng;
        }

        // 남은 카드 개수
        public int Count => cards.Count;

        // 카드 목록 확인 (Id 순 정렬)
        public IEnumerable<ISkillCard> Peek()
            => cards.OrderBy(c => c.Id);

        // 카드 한 장 뽑기
        public ISkillCard? Draw() {
            if (cards.Count == 0)
                return null;

            var top = cards[^1];

            cards.RemoveAt(cards.Count - 1);

            return top;
        }

        // 카드를 덱에 반환하고 섞기
        public void Return(ISkillCard card) {
            cards.Add(card);
            Shuffle();
        }

        // Fisher-Yates 셔플
        public void Shuffle() {
            for (int i = cards.Count - 1; i > 0; i--) {
                int j = rng.Next(i + 1);

                (cards[i], cards[j]) =
                    (cards[j], cards[i]);
            }
        }

        // 덱 복제
        public CardDeck Clone()
            => new(
                new List<ISkillCard>(cards),
                new Random(rng.Next()));
    }
}
