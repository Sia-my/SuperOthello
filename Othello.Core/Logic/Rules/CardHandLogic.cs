using Othello.Core.Cards;
using Othello.Core.Model;

namespace Othello.Core.Logic {
    // 카드 효과로 손패를 받는 공통 처리 (보급 등)
    // - 카드 효과 적용 중에는 사용한 카드가 아직 손패에 있음 → 그 1장은 빼고 한도 계산

    public static class CardHandLogic {
        // 카드가 빠진 뒤 기준으로 더 받을 수 있는 장수
        public static int Room(Data data, IsOnStone user, ISkillCard? usingCard) {
            Hand hand = data.HandOf(user);
            int held = hand.Count;
            if (usingCard != null && hand.Cards.Contains(usingCard))
                held--;

            return Math.Max(0, Hand.MaxSize - held);
        }

        // 덱에서 count장 드로우 → 한도 안은 손패로, 초과분은 덱으로 (덱이 부족하면 남은 만큼만)
        // 받은 장수 반환
        public static int DrawForCard(Data data, IsOnStone user, int count, ISkillCard? usingCard) {
            Hand hand = data.HandOf(user);
            int room = Room(data, user, usingCard);
            int received = 0;

            for (int i = 0; i < count; i++) {
                ISkillCard? drawn = data.Deck.Draw();
                if (drawn == null)
                    break;

                if (received < room) {
                    hand.ForceAdd(drawn);   // 사용한 카드가 빠지면 3장 이하
                    received++;
                } else {
                    data.Deck.Return(drawn);
                }
            }

            return received;
        }
    }
}
