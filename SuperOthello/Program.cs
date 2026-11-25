using Othello.Core.Cards;
using Othello.Core.Records;

namespace SuperOthello
{
    // 프로그램 시작점: 전적 DB 준비 후 메인 메뉴 표시
    // - DB를 못 열어도 게임은 실행 (전적 기능만 비활성)

    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // 고해상도(DPI)·기본 글꼴 등 기본 설정
            ApplicationConfiguration.Initialize();

            // 메인 메뉴로 시작 (메뉴 창이 닫히면 프로그램 종료)
            Application.Run(new MainForm(OpenRecords()));
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
