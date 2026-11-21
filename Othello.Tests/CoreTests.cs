using Othello.Core.Cards;
using Othello.Core.Control;
using Othello.Core.Model;
using Othello.Core.Rules;

namespace Othello.Tests;

/// <summary>설계서 3.5 테스트 설계의 TC 번호를 메서드 이름에 붙였다.</summary>
[TestClass]
public class CoreTests
{
    private static Board NewBoard()
    {
        var b = new Board();
        b.Reset();
        return b;
    }

    [TestMethod]
    public void TC01_판초기화_중앙4칸()
    {
        var b = NewBoard();
        Assert.AreEqual(Stone.White, b[3, 3].Color);
        Assert.AreEqual(Stone.Black, b[3, 4].Color);
        Assert.AreEqual(Stone.Black, b[4, 3].Color);
        Assert.AreEqual(Stone.White, b[4, 4].Color);
        Assert.AreEqual(60, b.EmptyCount);
    }

    [TestMethod]
    public void TC02_초기상태_흑착수가능_4곳()
    {
        var moves = NewBoard().GetValidMoves(Stone.Black);
        CollectionAssert.AreEquivalent(
            new[] { new Position(2, 3), new Position(3, 2), new Position(4, 5), new Position(5, 4) },
            moves);
    }

    [TestMethod]
    public void TC03_고정돌은_뒤집히지_않음()
    {
        // 행 0: 흑 백(고정) 백 [빈칸]  → 흑이 (0,3)에 두면 (0,2)만 뒤집힌다
        var b = new Board();
        b.PutStone(0, 0, Stone.Black);
        b.PutStone(0, 1, Stone.White);
        b.PutStone(0, 2, Stone.White);
        b[0, 1].State |= CellState.Fixed;

        var flips = b.Place(0, 3, Stone.Black);

        Assert.AreEqual(1, flips.Count);
        Assert.AreEqual(Stone.White, b[0, 1].Color);
        Assert.AreEqual(Stone.Black, b[0, 2].Color);
    }

    [TestMethod]
    public void TC03b_봉인칸에는_둘_수_없음()
    {
        var b = NewBoard();
        b[2, 3].State |= CellState.Sealed;
        b[2, 3].SealTurns = 3;
        Assert.IsFalse(b.GetValidMoves(Stone.Black).Contains(new Position(2, 3)));
    }

    [TestMethod]
    public void TC05_점수계산_3개뒤집기()
    {
        var s = new GameState();
        ScoreRule.OnPlace(s, Stone.Black, 3);
        Assert.AreEqual(3, s.ScoreOf(Stone.Black));
        Assert.AreEqual(-3, s.ScoreOf(Stone.White));
    }

    [TestMethod]
    public void TC05b_점수2배는_득점에만_적용()
    {
        var s = new GameState { TurnScoreMultiplier = 2 };
        ScoreRule.OnPlace(s, Stone.Black, 3);
        Assert.AreEqual(6, s.ScoreOf(Stone.Black));
        Assert.AreEqual(-3, s.ScoreOf(Stone.White));
    }

    [TestMethod]
    public void TC06_종료보너스_남은돌수만큼()
    {
        var s = GameState.NewGame(Array.Empty<ISkillCard>());
        ScoreRule.ApplyFinalBonus(s);
        Assert.AreEqual(2, s.ScoreOf(Stone.Black));
        Assert.AreEqual(2, s.ScoreOf(Stone.White));
    }

    [TestMethod]
    public void TC07_전향_상대돌을_내돌로_사용자_마이너스2()
    {
        var s = GameState.NewGame(Array.Empty<ISkillCard>());
        var card = new ConvertCard();

        card.Apply(s, Stone.Black, new Position(3, 3));

        Assert.AreEqual(Stone.Black, s.Board[3, 3].Color);
        Assert.AreEqual(-2, s.ScoreOf(Stone.Black));
    }

    [TestMethod]
    public void TC07b_봉인_3턴뒤_해제()
    {
        var s = GameState.NewGame(Array.Empty<ISkillCard>());
        new SealCard().Apply(s, Stone.Black, new Position(0, 0));

        for (int i = 0; i < SealCard.SealDuration; i++)
        {
            Assert.IsTrue(s.Board[0, 0].Has(CellState.Sealed));
            StatusEffects.ProcessTurnEnd(s.Board.Cells);
        }
        Assert.IsFalse(s.Board[0, 0].Has(CellState.Sealed));
    }

    [TestMethod]
    public void TC09_손패한도_3장()
    {
        var h = new Hand();
        Assert.IsTrue(h.TryAdd(new FixCard()));
        Assert.IsTrue(h.TryAdd(new FixCard()));
        Assert.IsTrue(h.TryAdd(new FixCard()));
        Assert.IsFalse(h.TryAdd(new FixCard()));
    }

    [TestMethod]
    public void TC10_사용한패는_덱으로_돌아감()
    {
        var ctrl = new GameController(PlayerSlot.Human("a"), PlayerSlot.Human("b"),
                                      new ISkillCard[] { new SilenceCard(), new SilenceCard(), new SilenceCard() }, seed: 1);
        int before = ctrl.State.Deck.Count;   // 3장 중 2장 지급 → 1장
        var card = ctrl.State.HandOf(Stone.Black).Cards[0];

        bool ok = ctrl.TryExecuteTurn(GameAction.WithCard(card, null, new Position(2, 3)), out string err);

        Assert.IsTrue(ok, err);
        Assert.AreEqual(before + 1, ctrl.State.Deck.Count);
    }

    [TestMethod]
    public void TC11_Clone은_원본과_독립()
    {
        var s = GameState.NewGame(CardPool.Current(), seed: 1);
        var copy = s.Clone();

        copy.Board.Place(2, 3, Stone.Black);
        copy.Scores[0] = 99;
        copy.HandOf(Stone.Black).Remove(copy.HandOf(Stone.Black).Cards[0]);

        Assert.AreEqual(Stone.Empty, s.Board[2, 3].Color);
        Assert.AreEqual(0, s.Scores[0]);
        Assert.AreEqual(1, s.HandOf(Stone.Black).Count);
    }

    [TestMethod]
    public void 잘못된착수는_상태를_바꾸지_않음()
    {
        var ctrl = new GameController(PlayerSlot.Human("a"), PlayerSlot.Human("b"), seed: 1);
        bool ok = ctrl.TryExecuteTurn(GameAction.Place(new Position(0, 0)), out _);

        Assert.IsFalse(ok);
        Assert.AreEqual(Stone.Black, ctrl.State.Current);
        Assert.AreEqual(60, ctrl.State.Board.EmptyCount);
    }
}
