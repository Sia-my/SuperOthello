using Othello.Core.Records;

namespace SuperOthello;

// 전적 화면: 최근 10게임 표 + ID별 승·패·무·승률 검색 (설계서 3.2, F5-3·F5-4)
// - DB를 읽지 못하면 안내 후 닫음
public class RecordForm : Form {
    private readonly RecordRepository repo;

    private readonly DataGridView grid = new() {
        Location = new Point(12, 12),
        Size = new Size(596, 270),
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = SystemColors.Window
    };
    private readonly Label emptyLabel = new() {
        Text = "아직 기록이 없습니다",
        Location = new Point(12, 140),
        Size = new Size(596, 30),
        TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = Color.Gray,
        Visible = false
    };
    private readonly TextBox idBox = new() { Location = new Point(12, 300), Size = new Size(180, 25) };
    private readonly Button searchButton = new() { Text = "검색", Location = new Point(200, 298), Size = new Size(70, 30) };
    private readonly Label statsLabel = new() { Location = new Point(280, 303), Size = new Size(328, 25) };

    public RecordForm(RecordRepository repository) {
        repo = repository;

        Text = "전적 보기";
        ClientSize = new Size(620, 340);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("맑은 고딕", 10f);

        grid.Columns.Add("Black", "흑 ID");
        grid.Columns.Add("White", "백 ID");
        grid.Columns.Add("Winner", "승자");
        grid.Columns.Add("Score", "점수");
        grid.Columns.Add("PlayedAt", "일시");
        grid.Columns["Score"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.Columns["PlayedAt"]!.FillWeight = 140;

        searchButton.Click += (_, _) => Search();
        idBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { Search(); e.SuppressKeyPress = true; } };

        Controls.AddRange(new Control[] { emptyLabel, grid, idBox, searchButton, statsLabel });
        Load += (_, _) => LoadRecent();
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

        bool empty = recent.Count == 0;
        emptyLabel.Visible = empty;
        if (empty) emptyLabel.BringToFront();
    }

    private void Search() {
        string id = idBox.Text.Trim();
        if (id.Length == 0) {
            statsLabel.Text = "검색할 ID를 입력하세요.";
            return;
        }

        try {
            var s = repo.GetStats(id);
            statsLabel.Text = s.Games == 0
                ? $"{id}: 기록이 없습니다"
                : $"{id}: {s.Wins}승 {s.Losses}패 {s.Draws}무 / 승률 {s.WinRateText}";
        }
        catch (Exception ex) {
            statsLabel.Text = "통계를 불러오지 못했습니다.";
            MessageBox.Show(ex.Message, "전적 보기", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
