using Othello.Core.Cards;

namespace Othello.Core.Model;

/// <summary>플레이어별로 다음 턴에만 유효한 상태 (침묵, 방패 등).</summary>
public struct PlayerStatus
{
    public bool Silenced;   // 침묵: 이번 턴 패 사용 불가
    public bool Shielded;   // 방패: 상대 턴 동안 받는 감점 절반(내림)
}

/// <summary>한 시점의 게임 전체 상태 (설계서 3.3). CPU는 Clone()한 복사본 위에서 수를 둬 본다.</summary>
public class GameState
{
    public Board Board { get; private set; } = new();
    public int[] Scores { get; private set; } = new int[2];          // [흑, 백], 음수 허용
    public Hand[] Hands { get; private set; } = new[] { new Hand(), new Hand() };
    public CardDeck Deck { get; private set; } = new();
    public PlayerStatus[] Status { get; private set; } = new PlayerStatus[2];

    public Stone Current { get; set; } = Stone.Black;
    public int TurnNumber { get; set; } = 1;
    public int ConsecutivePasses { get; set; }
    public bool IsOver { get; set; }

    /// <summary>이번 턴 착수 점수 배율 (점수 2배 패). 턴이 끝나면 1로 되돌린다.</summary>
    public int TurnScoreMultiplier { get; set; } = 1;

    public Hand HandOf(Stone s) => Hands[s.Index()];
    public int ScoreOf(Stone s) => Scores[s.Index()];
    public ref PlayerStatus StatusOf(Stone s) => ref Status[s.Index()];

    /// <summary>새 게임: 판 초기화, 덱 섞기, 양측 1장씩 지급 (F2-2).</summary>
    public static GameState NewGame(IEnumerable<ISkillCard> cardPool, int? seed = null)
    {
        var s = new GameState();
        s.Board.Reset();
        s.Deck = new CardDeck(cardPool, seed);
        foreach (var hand in s.Hands)
        {
            var card = s.Deck.Draw();
            if (card != null) hand.TryAdd(card);
        }
        return s;
    }

    public GameState Clone()
    {
        return new GameState
        {
            Board = Board.Clone(),
            Scores = (int[])Scores.Clone(),
            Hands = new[] { Hands[0].Clone(), Hands[1].Clone() },
            Deck = Deck.Clone(),
            Status = (PlayerStatus[])Status.Clone(),
            Current = Current,
            TurnNumber = TurnNumber,
            ConsecutivePasses = ConsecutivePasses,
            IsOver = IsOver,
            TurnScoreMultiplier = TurnScoreMultiplier
        };
    }
}
