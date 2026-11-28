using System.Drawing.Drawing2D;
using Othello.Core.Cards;
using Othello.Core.Model;

namespace SuperOthello;

// 화면 공용 색·글꼴·그리기 도우미 (어두운 테마, 글꼴은 맑은 고딕)
// - 모든 폼이 같은 모양을 쓰도록 색과 버튼·표 스타일을 여기서만 정의
public static class Theme {
    // 배경·패널·글자
    public static readonly Color Back = Color.FromArgb(24, 27, 38);
    public static readonly Color Panel = Color.FromArgb(36, 40, 55);
    public static readonly Color PanelLight = Color.FromArgb(48, 53, 72);
    public static readonly Color Border = Color.FromArgb(62, 68, 90);
    public static readonly Color Text = Color.FromArgb(236, 239, 244);
    public static readonly Color SubText = Color.FromArgb(160, 168, 190);
    public static readonly Color Dim = Color.FromArgb(105, 112, 135);

    // 강조
    public static readonly Color Accent = Color.FromArgb(242, 201, 76);    // 현재 차례·선택
    public static readonly Color Good = Color.FromArgb(111, 207, 151);
    public static readonly Color Danger = Color.FromArgb(235, 87, 87);
    public static readonly Color Primary = Color.FromArgb(86, 140, 245);

    // 보드
    public static readonly Color Felt = Color.FromArgb(39, 110, 72);
    public static readonly Color FeltDark = Color.FromArgb(27, 84, 54);
    public static readonly Color Frame = Color.FromArgb(20, 46, 32);

    // 카드 유형별 색 (A 판 변경 · B 점수 · C 운영)
    public static Color TypeColor(CardType type) => type switch {
        CardType.A => Color.FromArgb(76, 154, 255),
        CardType.B => Color.FromArgb(242, 153, 74),
        _ => Color.FromArgb(171, 120, 240)
    };

    public static string TypeName(CardType type) => type switch {
        CardType.A => "A 판 변경",
        CardType.B => "B 점수",
        _ => "C 운영"
    };

    public static string StoneName(IsOnStone stone) => stone == IsOnStone.Black ? "흑" : "백";

    // 글꼴
    public const string FontName = "맑은 고딕";
    public static Font F(float size, FontStyle style = FontStyle.Regular) => new(FontName, size, style);

    // 폼 기본 모양
    public static void ApplyForm(Form form) {
        form.BackColor = Back;
        form.ForeColor = Text;
        form.Font = F(10f);
    }

    // 평면 버튼. primary면 강조색 채움
    public static Button MakeButton(string text, Size size, bool primary = false) {
        var b = new Button { Text = text, Size = size };
        StyleButton(b, primary);
        return b;
    }

    public static void StyleButton(Button b, bool primary = false) {
        b.FlatStyle = FlatStyle.Flat;
        b.Font = F(10.5f, primary ? FontStyle.Bold : FontStyle.Regular);
        b.Cursor = Cursors.Hand;
        b.BackColor = primary ? Primary : PanelLight;
        b.ForeColor = Text;
        b.FlatAppearance.BorderColor = primary ? Primary : Border;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.MouseOverBackColor = primary ? ControlPaint.Light(Primary, 0.15f) : Border;
        b.FlatAppearance.MouseDownBackColor = primary ? ControlPaint.Dark(Primary, 0.05f) : Panel;
        b.UseVisualStyleBackColor = false;
    }

    // 읽기 전용 표 (전적·도감)
    public static void StyleGrid(DataGridView grid) {
        grid.BackgroundColor = Panel;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Border;
        grid.EnableHeadersVisualStyles = false;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 36;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle {
            BackColor = PanelLight, ForeColor = SubText, Font = F(9.5f, FontStyle.Bold),
            SelectionBackColor = PanelLight, SelectionForeColor = SubText,
            Alignment = DataGridViewContentAlignment.MiddleCenter, Padding = new Padding(6, 0, 6, 0)
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle {
            BackColor = Panel, ForeColor = Text, Font = F(10f),
            SelectionBackColor = Color.FromArgb(58, 72, 110), SelectionForeColor = Text,
            Padding = new Padding(6, 0, 6, 0)
        };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(40, 44, 60) };
        grid.RowTemplate.Height = 34;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AllowUserToResizeColumns = false;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.MultiSelect = false;
    }

    // 둥근 사각형 경로
    public static GraphicsPath Rounded(RectangleF r, float radius) {
        var path = new GraphicsPath();
        float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        if (d <= 0) { path.AddRectangle(r); return path; }
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    // 입체감 있는 돌 (보드·패널·결과 화면 공용)
    public static void DrawStone(Graphics g, RectangleF rect, IsOnStone stone, int alpha = 255) {
        if (stone == IsOnStone.Empty) return;

        // 그림자
        var shadow = rect; shadow.Offset(rect.Width * 0.04f, rect.Height * 0.06f);
        using (var sb = new SolidBrush(Color.FromArgb(alpha * 90 / 255, 0, 0, 0)))
            g.FillEllipse(sb, shadow);

        bool black = stone == IsOnStone.Black;
        Color baseColor = black ? Color.FromArgb(alpha, 28, 28, 32) : Color.FromArgb(alpha, 240, 240, 236);
        Color hiColor = black ? Color.FromArgb(alpha, 95, 98, 110) : Color.FromArgb(alpha, 255, 255, 255);

        using (var path = new GraphicsPath()) {
            path.AddEllipse(rect);
            using var pg = new PathGradientBrush(path) {
                CenterPoint = new PointF(rect.X + rect.Width * 0.35f, rect.Y + rect.Height * 0.32f),
                CenterColor = hiColor,
                SurroundColors = new[] { baseColor }
            };
            g.FillEllipse(pg, rect);
        }

        using var edge = new Pen(Color.FromArgb(alpha * 120 / 255, black ? 0 : 150, black ? 0 : 150, black ? 0 : 150), 1f);
        g.DrawEllipse(edge, rect);
    }

    // 작은 배지 (침묵·방패·CPU 등)
    public static void DrawBadge(Graphics g, string text, Font font, Color color, PointF at, out float width) {
        SizeF sz = g.MeasureString(text, font);
        var r = new RectangleF(at.X, at.Y, sz.Width + 10, sz.Height + 2);
        using (var path = Rounded(r, r.Height / 2))
        using (var fill = new SolidBrush(Color.FromArgb(55, color)))
        using (var pen = new Pen(color, 1f)) {
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }
        using (var tb = new SolidBrush(color))
            g.DrawString(text, font, tb, r.X + 5, r.Y + 1);
        width = r.Width;
    }
}
