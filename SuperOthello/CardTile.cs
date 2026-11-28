using System.Drawing.Drawing2D;
using Othello.Core.Cards;

namespace SuperOthello;

// 손패 카드 한 장을 그리는 컨트롤 (유형 색 띠 + 이름 + 분류 + 한 줄 설명)
// - 누를 수 있을 때만 CardClicked 발생. 빈 칸은 점선 테두리
public class CardTile : Control {
    private ISkillCard? card;
    private bool selected;
    private bool clickable;
    private bool blocked;   // 내 차례인데 지금은 쓸 수 없는 카드
    private bool hovering;

    public event Action<ISkillCard>? CardClicked;

    public CardTile() {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Theme.Panel;
    }

    public ISkillCard? Card => card;

    public void Show(ISkillCard? card, bool selected, bool clickable, bool blocked) {
        this.card = card;
        this.selected = selected;
        this.clickable = clickable && card != null;
        this.blocked = blocked;
        Cursor = this.clickable ? Cursors.Hand : Cursors.Default;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e) {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var r = new RectangleF(1.5f, 1.5f, Width - 3, Height - 3);

        if (card == null) {
            using var empty = Theme.Rounded(r, 10);
            using var dash = new Pen(Theme.Border, 1.2f) { DashStyle = DashStyle.Dash };
            g.DrawPath(dash, empty);
            using var ef = Theme.F(9f);
            using var eb = new SolidBrush(Theme.Dim);
            var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("빈 칸", ef, eb, r, fmt);
            return;
        }

        Color typeColor = Theme.TypeColor(card.Type);
        int alpha = blocked ? 110 : 255;
        Color bg = hovering && clickable ? Theme.Border : Theme.PanelLight;

        using (var path = Theme.Rounded(r, 10)) {
            using (var fill = new SolidBrush(Color.FromArgb(alpha, bg)))
                g.FillPath(fill, path);

            // 왼쪽 유형 색 띠
            var old = g.Clip;
            g.SetClip(path);
            using (var strip = new SolidBrush(Color.FromArgb(alpha, typeColor)))
                g.FillRectangle(strip, r.X, r.Y, 6, r.Height);
            g.Clip = old;

            Color borderColor = selected ? Theme.Accent : Color.FromArgb(alpha, Theme.Border);
            using var border = new Pen(borderColor, selected ? 2.5f : 1f);
            g.DrawPath(border, path);
        }

        float x = r.X + 14, y = r.Y + 8, w = r.Width - 22;

        // 이름 + 분류 배지
        using (var nf = Theme.F(11f, FontStyle.Bold))
        using (var nb = new SolidBrush(Color.FromArgb(alpha, Theme.Text))) {
            g.DrawString(card.Name, nf, nb, x, y);
        }
        using (var bf = Theme.F(8f, FontStyle.Bold)) {
            string type = Theme.TypeName(card.Type);
            SizeF ts = g.MeasureString(type, bf);
            Theme.DrawBadge(g, type, bf, Color.FromArgb(alpha, typeColor), new PointF(r.Right - ts.Width - 18, y + 2), out _);
        }

        // 설명 (아래 표시가 있으면 그만큼 줄여서, 넘치면 말줄임 · 전체 문장은 툴팁)
        bool footer = selected || blocked;
        using (var df = Theme.F(8.5f))
        using (var db = new SolidBrush(Color.FromArgb(alpha, Theme.SubText))) {
            var fmt = new StringFormat { Trimming = StringTrimming.EllipsisWord };
            float bottom = r.Bottom - (footer ? 24 : 6);
            g.DrawString(card.Description, df, db, new RectangleF(x, y + 24, w, bottom - (y + 24)), fmt);
        }

        if (selected) {
            using var sf = Theme.F(8f, FontStyle.Bold);
            Theme.DrawBadge(g, "선택됨", sf, Theme.Accent, new PointF(x, r.Bottom - 22), out _);
        }
        else if (blocked) {
            using var sf = Theme.F(8f);
            using var sb = new SolidBrush(Theme.Dim);
            g.DrawString("지금은 사용 불가", sf, sb, x, r.Bottom - 20);
        }
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovering = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovering = false; Invalidate(); }

    protected override void OnMouseClick(MouseEventArgs e) {
        base.OnMouseClick(e);
        if (clickable && card != null && e.Button == MouseButtons.Left)
            CardClicked?.Invoke(card);
    }
}
