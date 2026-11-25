using Othello.Core.Cards;
using Othello.Core.Control;
using Othello.Core.Model;
using Othello.Core.Records;

namespace SuperOthello
{
    internal static class Program
    {
        /// <summary>
        /// 프로그램 시작점
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 고해상도(DPI)·기본 글꼴 등 기본 설정
            ApplicationConfiguration.Initialize();

            // TODO: MatchSetupForm 완성 전까지는 바로 게임 화면을 띄운다.
            // 사람 vs 사람으로 시험하려면 두 번째 인자를 PlayerSlot.Human("player2")로 바꾼다.
            var black = PlayerSlot.Human("player1");
            var white = PlayerSlot.Cpu(Difficulty.Normal);

            Application.Run(new GameForm(new GameController(black, white), OpenRecords()));
        }

        // 전적 DB 준비. 실패해도 게임은 실행 (전적 기능만 비활성, 설계서 2.5)
        private static RecordRepository? OpenRecords()
        {
            try
            {
                var repo = new RecordRepository(RecordRepository.DefaultPath);
                repo.EnsureCreated();
                repo.SyncPsionics(CardPool.Current());
                return repo;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"전적 DB를 열지 못했습니다. 전적은 저장되지 않습니다.\n{ex.Message}",
                                "전적 DB", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
        }
    }
}
