namespace Othello.Core.Cards;

/// <summary>손패. 최대 3장 (F2-3).</summary>
public class Hand
{
    public const int MaxSize = 3;
    private readonly List<ISkillCard> cards = new();

    public IReadOnlyList<ISkillCard> Cards => cards;
    public int Count => cards.Count;
    public bool IsFull => cards.Count >= MaxSize;

    public bool TryAdd(ISkillCard card)
    {
        if (IsFull) return false;
        cards.Add(card);
        return true;
    }

    /// <summary>한도를 넘겨서라도 추가. 사람이 4장 중 1장을 버리는 동안만 쓴다.</summary>
    public void ForceAdd(ISkillCard card) => cards.Add(card);

    public bool Remove(ISkillCard card) => cards.Remove(card);

    public Hand Clone()
    {
        var h = new Hand();
        h.cards.AddRange(cards);
        return h;
    }
}
