using Othello.Core.Records;

namespace SuperOthello;

// 전적 화면: 최근 10게임 표 + ID 부분 검색으로 플레이어별 승·패·무·승률 (설계서 3.2, F5-3·F5-4)
// - DB를 읽지 못하면 안내 후 닫음
public class RecordForm : Form {
    private readonly RecordRepository repo;

    private readonly DataGridView grid = new() { Location = new Point(20, 52), Size = new Size(680, 336) };
    private readonly Label emptyLabel = new() {
        Text = "아직 기록이 없습니다 · 대전을 끝까지 두면 여기에 표시됩니다",
        Location = new Point(20, 200), Size = new Size(680, 30),
        TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.SubText, BackColor = Theme.Panel, Visible = false
    };
    private readonly TextBox idBox = new() {
        Location = new Point(36, 440), Size = new Size(200, 28), MaxLength = 12,
        BackColor = Theme.Back, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, Font = Theme.F(11f)
    };
    private readonly Button searchButton = Theme.MakeButton("검색", new Size(76, 30), primary: true);
    private readonly Label statsLabel = new() {
        Location = new Point(330, 438), Size = new Size(360, 32), TextAlign = ContentAlignment.MiddleLeft,
        Font = Theme.F(11f, FontStyle.Bold), ForeColor = Theme.SubText, BackColor = Theme.Panel,
        Text = "ID 일부만 입력해도 맞는 플레이어를 모두 찾습니다"
    };
    private readonly DataGridView searchGrid = new() { Location = new Point(36, 482), Size = new Size(648, 156) };

    public RecordForm(RecordRepository repository) {
        repo = repository;

        Text = "전적 보기";
        Theme.ApplyForm(this);
        DoubleBuffered = true;
        ClientSize = new Size(720, 670);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        Theme.StyleGrid(grid);
        grid.RowTemplate.Height = 30;
        grid.Columns.Add("Black", "흑");
        grid.Columns.Add("White", "백");
        grid.Columns.Add("Winner", "승자");
        grid.Columns.Add("Score", "점수 (흑 : 백)");
        grid.Columns.Add("PlayedAt", "일시");
        foreach (DataGridViewColumn c in grid.Columns) c.SortMode = DataGridViewColumnSortMode.NotSortable;
        grid.Columns["Score"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.Columns["PlayedAt"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.Columns["PlayedAt"]!.FillWeight = 130;
        grid.CellFormatting += (_, e) => {
            if (e.ColumnIndex == grid.Columns["Winner"]!.Index && e.Value is string w && e.CellStyle != null) {
                e.CellStyle.ForeColor = w == "무승부" ? Theme.SubText : Theme.Accent;
                e.CellStyle.Font = Theme.F(10f, FontStyle.Bold);
            }
        };

        // 부분 검색 결과 표 (플레이어별 승·패·무·총 경기·승률)
        Theme.StyleGrid(searchGrid);
        searchGrid.RowTemplate.Height = 30;
        searchGrid.BackgroundColor = Theme.Panel;
        searchGrid.Columns.Add("Id", "플레이어 ID");
        searchGrid.Columns.Add("Wins", "승");
        searchGrid.Columns.Add("Losses", "패");
        searchGrid.Columns.Add("Draws", "무");
        searchGrid.Columns.Add("Games", "총 경기");
        searchGrid.Columns.Add("WinRate", "승률");
        searchGrid.Columns["Id"]!.FillWeight = 160;
        foreach (DataGridViewColumn c in searchGrid.Columns) {
            c.SortMode = DataGridViewColumnSortMode.NotSortable;
            if (c.Name != "Id") c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }
        searchGrid.Columns["WinRate"]!.DefaultCellStyle.ForeColor = Theme.Accent;

        searchButton.Location = new Point(idBox.Right + 8, idBox.Top - 1);
        searchButton.Click += (_, _) => Search();
        idBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { Search(); e.SuppressKeyPress = true; } };

        Controls.AddRange(new Control[] { emptyLabel, grid, idBox, searchButton, statsLabel, searchGrid });
        emptyLabel.BringToFront();
        Load += (_, _) => LoadRecent();
    }

    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using (var tf = Theme.F(13f, FontStyle.Bold))
        using (var tb = new SolidBrush(Theme.Text))
            g.DrawString("최근 10게임", tf, tb, 18, 16);

        // 아래쪽 검색 영역
        var box = new RectangleF(20, 404, 680, 250);
        using (var path = Theme.Rounded(box, 12))
        using (var fill = new SolidBrush(Theme.Panel))
        using (var pen = new Pen(Theme.Border, 1f)) {
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }
        using var lf = Theme.F(9f, FontStyle.Bold);
        using var lb = new SolidBrush(Theme.SubText);
        g.DrawString("플레이어 통계 (ID 일부로 검색)", lf, lb, 34, 414);
    }

    private void LoadRecent() {
        IReadOnlyList<GameRecord> recent;
        try {
            recent = repo.GetRecent(10);
        }
        catch (Exception ex) {
            MessageBox.Show($"전적을 불러오지 못했습니다.\n{ex.Message}", "전적 보기", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            BeginInvoke(new Action(Close));
            return;
        }

        foreach (var g in recent) {
            string winner = g.Winner switch {
                'B' => g.BlackId,
                'W' => g.WhiteId,
                _ => "무승부"
            };
            grid.Rows.Add(g.BlackId, g.WhiteId, winner, $"{g.BlackScore} : {g.WhiteScore}", g.PlayedAt.ToString("yyyy-MM-dd HH:mm"));
        }
        grid.ClearSelection();

        emptyLabel.Visible = recent.Count == 0;
    }

    // ID 부분 문자열 검색 (RecordRepository.SearchStats)
    private void Search() {
        string keyword = idBox.Text.Trim();
        searchGrid.Rows.Clear();
        if (keyword.Length == 0) {
            statsLabel.ForeColor = Theme.SubText;
            statsLabel.Text = "검색할 ID를 입력하세요";
            return;
        }

        try {
            var matches = repo.SearchStats(keyword);
            foreach (var s in matches)
                searchGrid.Rows.Add(s.Id, s.Wins, s.Losses, s.Draws, s.Games, s.WinRateText);
            searchGrid.ClearSelection();

            statsLabel.ForeColor = matches.Count == 0 ? Theme.SubText : Theme.Text;
            statsLabel.Text = matches.Count == 0
                ? "일치하는 플레이어가 없습니다"
                : $"검색 결과: {matches.Count}명";
        }
        catch (Exception ex) {
            statsLabel.ForeColor = Theme.Danger;
            statsLabel.Text = "통계를 불러오지 못했습니다";
            MessageBox.Show(ex.Message, "전적 보기", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
