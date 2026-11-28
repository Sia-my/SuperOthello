using System.Drawing.Drawing2D;
using Othello.Core.Cards;
using Othello.Core.Model;

namespace SuperOthello;

// 행동 로그 표시 (F3-5): 줄마다 흑/백 돌 아이콘, 카드 사용 줄은 카드 유형 색
// - 새 줄이 오면 맨 아래로 이동, 마우스 휠로 지난 기록 보기
public class LogView : Control {
    private const int LineHeight = 19;
    private const int HeaderHeight = 22;

    private static readonly Dictionary<string, CardType> CardTypes =
        CardPool.Current().GroupBy(c => c.Name).ToDictionary(g => g.Key, g => g.First().Type);

    private readonly List<string> lines = new();
    private int scroll;   // 맨 아래에서 위로 올린 줄 수

    public LogView() {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        BackColor = Theme.Back;
    }

    private int VisibleLines => Math.Max(1, (Height - HeaderHeight - 6) / LineHeight);

    public void Add(string line) {
        lines.Add(line);
        scroll = 0;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); Focus(); }

    protected override void OnMouseWheel(MouseEventArgs e) {
        base.OnMouseWheel(e);
        int max = Math.Max(0, lines.Count - VisibleLines);
        scroll = Math.Clamp(scroll + (e.Delta > 0 ? 2 : -2), 0, max);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e) {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var r = new RectangleF(1, 1, Width - 3, Height - 3);
        using (var path = Theme.Rounded(r, 12)) {
            using (var fill = new SolidBrush(Theme.Panel)) g.FillPath(fill, path);
            using var pen = new Pen(Theme.Border, 1f);
            g.DrawPath(pen, path);
        }

        using (var hf = Theme.F(9f, FontStyle.Bold))
        using (var hb = new SolidBrush(Theme.SubText))
            g.DrawString(scroll > 0 ? $"행동 로그 (지난 기록 보는 중 · 휠을 내리면 최신)" : "행동 로그", hf, hb, 12, 4);

        int visible = VisibleLines;
        int last = lines.Count - 1 - scroll;
        int first = Math.Max(0, last - visible + 1);

        using var font = Theme.F(9.5f);
        float y = HeaderHeight;
        for (int i = first; i <= last; i++) {
            DrawLine(g, lines[i], font, 12, y, Width - 30);
            y += LineHeight;
        }

        // 얇은 스크롤 표시
        if (lines.Count > visible) {
            float trackTop = HeaderHeight, trackH = Height - HeaderHeight - 8;
            float thumbH = Math.Max(14, trackH * visible / lines.Count);
            float thumbTop = trackTop + (trackH - thumbH) * first / Math.Max(1, lines.Count - visible);
            using var thumb = new SolidBrush(Theme.Border);
            using var tp = Theme.Rounded(new RectangleF(Width - 12, thumbTop, 5, thumbH), 2.5f);
            g.FillPath(thumb, tp);
        }
    }

    private static void DrawLine(Graphics g, string text, Font font, float x, float y, float width) {
        Color color = Theme.Text;
        IsOnStone who = IsOnStone.Empty;

        // "[n턴] 흑 ..." 형식: 돌 아이콘, 카드 사용 줄은 유형 색, 패스는 흐리게
        int close = text.IndexOf("] ", StringComparison.Ordinal);
        if (text.StartsWith('[') && close > 0 && text.Length > close + 3) {
            string side = text.Substring(close + 2, 1);
            who = side == "흑" ? IsOnStone.Black : side == "백" ? IsOnStone.White : IsOnStone.Empty;
            string rest = text[(close + 4)..];
            if (rest.StartsWith('[')) {
                int end = rest.IndexOf(']');
                if (end > 1 && CardTypes.TryGetValue(rest[1..end], out var type) && !rest[(end + 1)..].StartsWith(" 획득"))
                    color = ControlPaint.Light(Theme.TypeColor(type), 0.35f);
            }
            if (rest.Contains("패스")) color = Theme.SubText;
        }
        else if (text.StartsWith("게임") || text.StartsWith("종료 보너스")) color = Theme.Good;
        else if (text.Contains("실패") || text.Contains("오류")) color = Theme.Danger;

        if (who != IsOnStone.Empty) {
            Theme.DrawStone(g, new RectangleF(x, y + 3, 12, 12), who);
            x += 18;
        }
        using var b = new SolidBrush(color);
        var fmt = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString(text, font, b, new RectangleF(x, y, width - x, LineHeight), fmt);
    }
}
