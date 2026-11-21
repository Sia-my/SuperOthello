using Othello.Core.Control;

namespace SuperOthello
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // TODO: MatchSetupForm 완성 전까지는 바로 게임 화면을 띄운다.
            // 사람 vs 사람으로 시험하려면 두 번째 인자를 PlayerSlot.Human("player2")로 바꾼다.
            var black = PlayerSlot.Human("player1");
            var white = PlayerSlot.Cpu(Difficulty.Normal);

            Application.Run(new GameForm(new GameController(black, white)));
        }
    }
}
