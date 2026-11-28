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
        Theme.ApplyForm(this);
        DoubleBuffered = true;
        ClientSize = new Size(400, 500);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        AddButton("대전", 196, OnMatch, primary: true);
        AddButton("전적 보기", 256, OnRecords);
        AddButton("카드 도감", 316, (_, _) => { using var book = new CardBookForm(records); book.ShowDialog(this); });
        AddButton("종료", 376, (_, _) => Close());
    }

    private void AddButton(string text, int top, EventHandler onClick, bool primary = false) {
        var btn = Theme.MakeButton(text, new Size(240, 46), primary);
        btn.Location = new Point((ClientSize.Width - btn.Width) / 2, top);
        btn.Font = Theme.F(11.5f, primary ? FontStyle.Bold : FontStyle.Regular);
        btn.Click += onClick;
        Controls.Add(btn);
    }

    // 로고(흑·백 돌) + 제목 + 부제 + 아래쪽 팀 이름
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        float cx = ClientSize.Width / 2f;
        Theme.DrawStone(g, new RectangleF(cx - 46, 34, 52, 52), IsOnStone.Black);
        Theme.DrawStone(g, new RectangleF(cx - 6, 34, 52, 52), IsOnStone.White);
        using (var spark = new SolidBrush(Theme.Accent)) {
            PointF c = new(cx + 44, 34);
            g.FillPolygon(spark, new[] {
                new PointF(c.X, c.Y - 11), new PointF(c.X + 3, c.Y - 3), new PointF(c.X + 11, c.Y),
                new PointF(c.X + 3, c.Y + 3), new PointF(c.X, c.Y + 11), new PointF(c.X - 3, c.Y + 3),
                new PointF(c.X - 11, c.Y), new PointF(c.X - 3, c.Y - 3)
            });
        }

        var center = new StringFormat { Alignment = StringAlignment.Center };
        using (var tf = Theme.F(22f, FontStyle.Bold))
        using (var tb = new SolidBrush(Theme.Text))
            g.DrawString("초능력 오셀로", tf, tb, new RectangleF(0, 100, ClientSize.Width, 44), center);
        using (var sf = Theme.F(10f))
        using (var sb = new SolidBrush(Theme.SubText))
            g.DrawString("초능력 패로 판을 뒤집는 점수제 오셀로", sf, sb, new RectangleF(0, 146, ClientSize.Width, 24), center);
        using (var ff = Theme.F(8.5f))
        using (var fb = new SolidBrush(Theme.Dim))
            g.DrawString("소프트웨어 프로젝트 · 팀 리턴즈", ff, fb, new RectangleF(0, ClientSize.Height - 30, ClientSize.Width, 20), center);
    }

    // 전적 DB가 없으면 안내만 (설계서 2.5)
    private void OnRecords(object? sender, EventArgs e) {
        if (records == null) {
            MessageBox.Show("전적 DB를 사용할 수 없어 전적을 볼 수 없습니다.", "전적 보기", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        using var form = new RecordForm(records);
        form.ShowDialog(this);
    }

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
