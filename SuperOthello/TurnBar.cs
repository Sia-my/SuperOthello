using System.Drawing.Drawing2D;
using Othello.Core.Model;

namespace SuperOthello;

// 게임 화면 상단: 현재 차례·안내 문구 + 턴·덱·빈칸 수 (설계서 3.2)
public class TurnBar : Control {
    private IsOnStone current = IsOnStone.Black;
    private string title = "";
    private string guide = "";
    private (string Label, string Value)[] chips = Array.Empty<(string, string)>();
    private bool over;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int ChipsRightMargin { get; set; } = 110;   // 오른쪽 버튼 자리

    public TurnBar() {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Back;
    }

    public void Show(IsOnStone current, string title, string guide, bool over, params (string, string)[] chips) {
        this.current = current;
        this.title = title;
        this.guide = guide;
        this.over = over;
        this.chips = chips;
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

        float x = 16;
        if (!over) {
            Theme.DrawStone(g, new RectangleF(x, (Height - 30) / 2f, 30, 30), current);
            x += 42;
        }

        using (var tf = Theme.F(14f, FontStyle.Bold))
        using (var tb = new SolidBrush(Theme.Text))
            g.DrawString(title, tf, tb, x, 6);
        using (var gf = Theme.F(9.5f))
        using (var gb = new SolidBrush(Theme.Accent))
            g.DrawString(guide, gf, gb, x + 1, 32);

        // 오른쪽 정보 칩
        using var lf = Theme.F(8.5f);
        using var vf = Theme.F(12f, FontStyle.Bold);
        using var lb = new SolidBrush(Theme.SubText);
        using var vb = new SolidBrush(Theme.Text);
        float cx = Width - ChipsRightMargin;
        for (int i = chips.Length - 1; i >= 0; i--) {
            var (label, value) = chips[i];
            float w = Math.Max(g.MeasureString(label, lf).Width, g.MeasureString(value, vf).Width) + 18;
            cx -= w;
            var chip = new RectangleF(cx, 8, w, Height - 18);
            using (var cp = Theme.Rounded(chip, 8))
            using (var cfill = new SolidBrush(Theme.PanelLight))
                g.FillPath(cfill, cp);
            var fmt = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString(label, lf, lb, new RectangleF(chip.X, chip.Y + 3, chip.Width, 16), fmt);
            g.DrawString(value, vf, vb, new RectangleF(chip.X, chip.Y + 18, chip.Width, 22), fmt);
            cx -= 8;
        }
    }
}
