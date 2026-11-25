using Othello.Core.Cards;
using Othello.Core.Records;

namespace SuperOthello;

// 카드 도감: 이름 / 분류 / 효과 설명 / 점수 영향 (설계서 3.2, F6-3)
// - 이름·설명은 Psionics 테이블, 분류·점수 영향은 카드 클래스 (Id로 매칭). DB를 못 쓰면 CardPool로 표시
public class CardBookForm : Form {
    public CardBookForm(RecordRepository? repository) {
        Text = "카드 도감";
        ClientSize = new Size(720, 330);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("맑은 고딕", 10f);

        var grid = new DataGridView {
            Location = new Point(12, 12),
            Size = new Size(696, 306),
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window
        };
        grid.Columns.Add("Name", "이름");
        grid.Columns.Add("Type", "분류");
        grid.Columns.Add("Description", "효과 설명");
        grid.Columns.Add("ScoreEffect", "점수 영향");
        grid.Columns["Name"]!.FillWeight = 60;
        grid.Columns["Type"]!.FillWeight = 60;
        grid.Columns["Description"]!.FillWeight = 220;
        grid.Columns["ScoreEffect"]!.FillWeight = 70;
        grid.Columns["Description"]!.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

        foreach (var row in BuildRows(repository))
            grid.Rows.Add(row.Name, row.Type, row.Description, row.ScoreEffect);

        Controls.Add(grid);
    }

    // 같은 카드가 덱에 여러 장 있어도 1행
    private static IEnumerable<(string Name, string Type, string Description, string ScoreEffect)> BuildRows(RecordRepository? repo) {
        var cards = CardPool.Current()
            .GroupBy(c => c.Id)
            .ToDictionary(g => g.Key, g => g.First());

        IReadOnlyList<(int Id, string Name, string Description)> entries;
        try {
            entries = repo?.GetPsionics() ?? Array.Empty<(int, string, string)>();
        }
        catch (Exception) {
            entries = Array.Empty<(int, string, string)>();
        }
        if (entries.Count == 0)
            entries = cards.Values.OrderBy(c => c.Id).Select(c => (c.Id, c.Name, c.Description)).ToList();

        // 현재 덱에 없는 옛 카드 행은 표시하지 않음
        foreach (var (id, name, description) in entries) {
            if (!cards.TryGetValue(id, out var card)) continue;
            yield return (
                name,
                TypeName(card.Type),
                description,
                (card as SkillCardBase)?.ScoreEffect ?? "없음");
        }
    }

    public static string TypeName(CardType type) => type switch {
        CardType.A => "A 판 변경",
        CardType.B => "B 점수",
        _ => "C 운영"
    };
}
