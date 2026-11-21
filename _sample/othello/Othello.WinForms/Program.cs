using Othello.Core.Control;

namespace Othello.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // TODO: MainForm → MatchSetupForm 완성 전까지는 바로 게임 화면을 띄운다.
        // 사람 vs 사람으로 시험하려면 두 번째 인자를 PlayerSlot.Human("player2")로 바꾼다.
        var black = PlayerSlot.Human("player1");
        var white = PlayerSlot.Cpu(Difficulty.Normal);

        Application.Run(new GameForm(new GameController(black, white)));
    }
}
