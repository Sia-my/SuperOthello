namespace Othello.Core.Cards {
    // 공용 카드 덱
    // - 쓰거나 버린 카드는 Return()으로 되돌린 뒤 다시 섞음
    // - Peek()은 화면 표시용 (실제 순서는 비공개)

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
