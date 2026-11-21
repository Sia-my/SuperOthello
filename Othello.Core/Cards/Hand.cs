using System.Collections.Generic;

namespace Othello.Core.Cards {
    /*
     * Hand.cs - 플레이어 한 명의 손패 관리
     *
     * [사용 안내]
     * 빈 손패 생성         : Hand hand = new Hand();
     * 손패 카드 목록 조회  : hand.Cards
     * 현재 카드 개수 조회  : hand.Count
     * 손패가 꽉 찼는지    : hand.IsFull
     * 카드 추가 시도      : hand.TryAdd(card)   // 성공 true, 최대 3장
     * 카드 강제 추가      : hand.ForceAdd(card) // 4장 중 1장 버릴 때 사용
     * 카드 제거           : hand.Remove(card)  // 성공 true
     * 손패 복제           : hand.Clone()
     *
     * 이 클래스는 카드 목록만 관리한다.
     * 카드 사용 가능 여부, 효과 처리, 카드 획득 및 버리기 시점은
     * 다른 로직과 GameController가 담당한다.
     *
     * Clone()은 카드 목록을 새로 만들지만 ISkillCard 인스턴스는 공유한다.
     */

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
