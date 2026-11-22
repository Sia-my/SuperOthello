using System.Collections.Generic;

namespace Othello.Core.Cards {
    // 플레이어 손패 (최대 3장)
    // - 한도 초과는 ForceAdd()로 잠깐만 허용 → 사람이 1장 버릴 때 사용

    /// <summary>플레이어 한 명의 손패. 기본 최대 3장 (F2-3).</summary>
    public class Hand {
        public const int MaxSize = 3;
        private readonly List<ISkillCard> cards = new();

        public IReadOnlyList<ISkillCard> Cards => cards;
        public int Count => cards.Count;
        public bool IsFull => cards.Count >= MaxSize;

        // 손패가 가득 찬 경우 false를 반환하고 추가하지 않는다.
        public bool TryAdd(ISkillCard card) {
            if (IsFull) return false;
            cards.Add(card);
            return true;
        }

        // 4장 중 1장을 선택해 버리는 상황에서 일시적으로 한도를 넘겨 추가한다.
        public void ForceAdd(ISkillCard card) => cards.Add(card);

        // 카드가 존재하면 제거 후 true, 없으면 false 반환
        public bool Remove(ISkillCard card) => cards.Remove(card);

        // 목록은 독립적으로 복제하되 카드 객체 자체는 공유한다.
        public Hand Clone() {
            var copy = new Hand();
            copy.cards.AddRange(cards);
            return copy;
        }
    }
}
