namespace Othello.Core.Cards;

/// <summary>공용 덱 (F2-1, F2-7). 쓰거나 버린 패는 Return으로 되돌리고 다시 섞는다.</summary>
public class CardDeck
{
    private readonly List<ISkillCard> cards = new();
    private readonly Random rng;

    public CardDeck() : this(Array.Empty<ISkillCard>()) { }

    public CardDeck(IEnumerable<ISkillCard> pool, int? seed = null)
    {
        rng = seed.HasValue ? new Random(seed.Value) : new Random();
        cards.AddRange(pool);
        Shuffle();
    }

    private CardDeck(List<ISkillCard> copy, Random rng)
    {
        cards = copy;
        this.rng = rng;
    }

    public int Count => cards.Count;

    /// <summary>화면 표시용. 실제 순서는 비공개이므로 번호순으로 정렬해서 준다.</summary>
    public IEnumerable<ISkillCard> Peek() => cards.OrderBy(c => c.Id);

    public ISkillCard? Draw()
    {
        if (cards.Count == 0) return null;
        var top = cards[^1];
        cards.RemoveAt(cards.Count - 1);
        return top;
    }

    public void Return(ISkillCard card)
    {
        cards.Add(card);
        Shuffle();
    }

    public void Shuffle()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }

    /// <summary>CPU 시뮬레이션용 복사본. 시뮬레이션에서는 드로우하지 않지만 원본 덱 보호를 위해 분리.</summary>
    public CardDeck Clone() => new(new List<ISkillCard>(cards), new Random(rng.Next()));
}
