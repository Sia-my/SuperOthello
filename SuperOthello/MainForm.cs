using Othello.Core.Control;
using Othello.Core.Model;
using Othello.Core.Records;

namespace SuperOthello;

// 메인 메뉴: 대전 / 전적 보기 / 카드 도감 / 종료 (설계서 3.2, 이어하기 없음)
// - 게임 중에는 숨겼다가 게임 창이 닫히면 다시 표시
public class MainForm : Form {
    private readonly RecordRepository? records;   // null이면 전적 기능 비활성

    public MainForm(RecordRepository? recordRepository) {
        records = recordRepository;

        Text = "초능력 오셀로";
        ClientSize = new Size(320, 330);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("맑은 고딕", 10f);

        var title = new Label {
            Text = "초능력 오셀로",
            Font = new Font("맑은 고딕", 18f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 20),
            Size = new Size(320, 50)
        };
        Controls.Add(title);

        AddButton("대전", 90, OnMatch);
        AddButton("전적 보기", 145, (_, _) => NotReady("전적 보기"));
        AddButton("카드 도감", 200, (_, _) => NotReady("카드 도감"));
        AddButton("종료", 255, (_, _) => Close());
    }

    private void AddButton(string text, int top, EventHandler onClick) {
        var btn = new Button { Text = text, Location = new Point(70, top), Size = new Size(180, 42) };
        btn.Click += onClick;
        Controls.Add(btn);
    }

    private void NotReady(string name)
        => MessageBox.Show($"{name}는 준비 중입니다.", name, MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void OnMatch(object? sender, EventArgs e) {
        using var setup = new MatchSetupForm();
        if (setup.ShowDialog(this) != DialogResult.OK)
            return;

        StartGame(setup.Black, setup.White);
    }

    // 게임 창을 열고 메뉴는 숨김. 다시 하기면 같은 슬롯으로 새 게임
    private void StartGame(PlayerSlot black, PlayerSlot white) {
        var game = new GameForm(new GameController(black, white), records) {
            StartPosition = FormStartPosition.CenterScreen
        };
        game.FormClosed += (_, _) => {
            if (game.ReplayRequested) StartGame(black, white);
            else Show();
        };
        Hide();
        game.Show();
    }
}
