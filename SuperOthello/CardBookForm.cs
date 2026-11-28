using System.Drawing.Drawing2D;
using Othello.Core.Cards;
using Othello.Core.Records;

namespace SuperOthello;

// 카드 도감: 유형별 3열로 이름 / 분류 / 효과 설명 / 점수 영향 표시 (설계서 3.2, F6-3)
// - 이름·설명은 Psionics 테이블, 분류·점수 영향은 카드 클래스 (Id로 매칭). DB를 못 쓰면 CardPool로 표시
public class CardBookForm : Form {
    private const int ColWidth = 290, TileHeight = 104, Gap = 12, GridTop = 104;

    private static readonly (CardType Type, string Summary)[] Columns = {
        (CardType.A, "판 위 돌 배치를 바꾸는 패"),
        (CardType.B, "이번 착수의 점수를 바꾸는 패"),
        (CardType.C, "손패·차례를 다루는 패")
    };

    private readonly List<(string Name, CardType Type, string Description, string ScoreEffect)> rows;

    public CardBookForm(RecordRepository? repository) {
        Text = "카드 도감";
        Theme.ApplyForm(this);
        DoubleBuffered = true;
        rows = BuildRows(repository).ToList();

        int maxPerType = Math.Max(1, Columns.Max(c => rows.Count(r => r.Type == c.Type)));
        ClientSize = new Size(Gap + Columns.Length * (ColWidth + Gap), GridTop + maxPerType * (TileHeight + Gap) + 8);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
    }

    // 같은 카드가 덱에 여러 장 있어도 1번만
    private static IEnumerable<(string Name, CardType Type, string Description, string ScoreEffect)> BuildRows(RecordRepository? repo) {
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
            yield return (name, card.Type, description, (card as SkillCardBase)?.ScoreEffect ?? "없음");
        }
    }

    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using (var tf = Theme.F(14f, FontStyle.Bold))
        using (var tb = new SolidBrush(Theme.Text))
            g.DrawString($"카드 도감 · {rows.Count}종", tf, tb, Gap + 2, 14);
        using (var sf = Theme.F(9.5f))
        using (var sb = new SolidBrush(Theme.SubText))
            g.DrawString("한 턴에 1장, 착수 전에 사용 · 3개 이상 뒤집으면 1장 획득 · 손패는 최대 3장", sf, sb, Gap + 2, 42);

        using var headFont = Theme.F(11f, FontStyle.Bold);
        using var headSub = Theme.F(8.5f);
        using var nameFont = Theme.F(11.5f, FontStyle.Bold);
        using var descFont = Theme.F(9f);
        using var effFont = Theme.F(8.5f, FontStyle.Bold);
        using var textBrush = new SolidBrush(Theme.Text);
        using var subBrush = new SolidBrush(Theme.SubText);

        for (int col = 0; col < Columns.Length; col++) {
            var (type, summary) = Columns[col];
            Color color = Theme.TypeColor(type);
            float x = Gap + col * (ColWidth + Gap);

            // 열 제목
            using (var cb = new SolidBrush(color))
                g.FillRectangle(cb, x, 70, 4, 26);
            using (var hb = new SolidBrush(color))
                g.DrawString(Theme.TypeName(type), headFont, hb, x + 10, 68);
            g.DrawString(summary, headSub, subBrush, x + 10, 86);

            // 카드
            float y = GridTop;
            foreach (var row in rows.Where(r => r.Type == type)) {
                var r = new RectangleF(x, y, ColWidth, TileHeight);
                using (var path = Theme.Rounded(r, 10)) {
                    using (var fill = new SolidBrush(Theme.Panel)) g.FillPath(fill, path);
                    var old = g.Clip;
                    g.SetClip(path);
                    using (var strip = new SolidBrush(color)) g.FillRectangle(strip, r.X, r.Y, 6, r.Height);
                    g.Clip = old;
                    using var pen = new Pen(Theme.Border, 1f);
                    g.DrawPath(pen, path);
                }

                g.DrawString(row.Name, nameFont, textBrush, r.X + 16, r.Y + 10);
                var fmt = new StringFormat { Trimming = StringTrimming.EllipsisWord };
                g.DrawString(row.Description, descFont, subBrush, new RectangleF(r.X + 16, r.Y + 36, r.Width - 28, 38), fmt);
                Theme.DrawBadge(g, "점수 영향 · " + row.ScoreEffect, effFont, row.ScoreEffect == "없음" ? Theme.Dim : Theme.Accent,
                    new PointF(r.X + 16, r.Bottom - 26), out _);

                y += TileHeight + Gap;
            }
        }
    }
}
