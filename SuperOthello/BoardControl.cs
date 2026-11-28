using System.Drawing.Drawing2D;
using Othello.Core.Logic;
using Othello.Core.Model;

namespace SuperOthello;

// 보드 그리기 전용 컨트롤 (GDI+, 더블 버퍼링)
// - 규칙 판단은 하지 않음: 그릴 칸 목록은 GameForm이 Core로 계산해서 넘김
// - 클릭은 CellClicked, 마우스가 올라간 칸은 CellHovered로 전달 (미리보기용)
public class BoardControl : Control {
    private static readonly Color HintColor = Color.FromArgb(150, 10, 30, 20);
    private static readonly Color TargetFill = Color.FromArgb(70, 242, 201, 76);
    private static readonly Color AreaFill = Color.FromArgb(120, 235, 87, 87);
    private static readonly Color FlipMark = Color.FromArgb(230, 242, 201, 76);

    private Board? board;
    private IReadOnlyCollection<(int Row, int Col)> hints = Array.Empty<(int Row, int Col)>();
    private IReadOnlyCollection<(int Row, int Col)> targets = Array.Empty<(int Row, int Col)>();
    private (int Row, int Col)? lastMove;

    // 미리보기 (F6-4): 놓일 돌, 뒤집힐 돌, 카드 효과 범위
    private (int Row, int Col)? ghost;
    private IsOnStone ghostStone;
    private IReadOnlyCollection<(int Row, int Col)> previewFlips = Array.Empty<(int Row, int Col)>();
    private IReadOnlyCollection<(int Row, int Col)> previewArea = Array.Empty<(int Row, int Col)>();

    private (int Row, int Col)? hover;

    public event Action<(int Row, int Col)>? CellClicked;
    public event Action<(int Row, int Col)?>? CellHovered;

    public BoardControl() {
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Back;
    }

    // 그릴 내용을 한 번에 받아서 다시 그리기 (미리보기는 초기화)
    public void Render(
        Board board,
        IReadOnlyCollection<(int Row, int Col)> hints,
        IReadOnlyCollection<(int Row, int Col)> targets,
        (int Row, int Col)? lastMove) {
        this.board = board;
        this.hints = hints;
        this.targets = targets;
        this.lastMove = lastMove;
        ClearPreview();
    }

    public void SetPreview(
        (int Row, int Col)? ghost, IsOnStone ghostStone,
        IReadOnlyCollection<(int Row, int Col)> flips,
        IReadOnlyCollection<(int Row, int Col)> area) {
        this.ghost = ghost;
        this.ghostStone = ghostStone;
        previewFlips = flips;
        previewArea = area;
        Invalidate();
    }

    public void ClearPreview() {
        ghost = null;
        previewFlips = Array.Empty<(int Row, int Col)>();
        previewArea = Array.Empty<(int Row, int Col)>();
        Invalidate();
    }

    // ───────────── 좌표 ─────────────

    private float Side => Math.Min(ClientSize.Width, ClientSize.Height);
    private float Edge => Side * 0.055f;   // 가장자리 좌표 표시 칸
    private float CellSize => (Side - Edge * 2) / Board.Size;

    private RectangleF CellRect(int row, int col)
        => new(Edge + col * CellSize, Edge + row * CellSize, CellSize, CellSize);

    private (int Row, int Col)? CellAt(Point p) {
        float cs = CellSize;
        if (cs <= 0) return null;
        int col = (int)Math.Floor((p.X - Edge) / cs);
        int row = (int)Math.Floor((p.Y - Edge) / cs);
        return BoardLogic.InBounds(row, col) ? (row, col) : null;
    }

    // ───────────── 그리기 ─────────────

    protected override void OnPaint(PaintEventArgs e) {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        float side = Side, m = Edge, cs = CellSize, full = cs * Board.Size;

        // 테두리 + 펠트
        using (var framePath = Theme.Rounded(new RectangleF(0, 0, side - 1, side - 1), m * 0.6f))
        using (var frame = new SolidBrush(Theme.Frame))
            g.FillPath(frame, framePath);

        var feltRect = new RectangleF(m, m, full, full);
        using (var felt = new LinearGradientBrush(feltRect, Theme.Felt, Theme.FeltDark, 45f))
            g.FillRectangle(felt, feltRect);

        // 좌표 (행동 로그의 (행,열) 번호와 같음)
        using (var lf = Theme.F(Math.Max(7f, m * 0.42f)))
        using (var lb = new SolidBrush(Color.FromArgb(170, 200, 225, 205))) {
            var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            for (int i = 0; i < Board.Size; i++) {
                g.DrawString(i.ToString(), lf, lb, new RectangleF(m + i * cs, 0, cs, m), fmt);
                g.DrawString(i.ToString(), lf, lb, new RectangleF(0, m + i * cs, m, cs), fmt);
            }
        }

        if (!board.HasValue || board.Value.Cells == null) return;
        Cell[,] cells = board.Value.Cells;

        // 카드 대상 칸
        using (var tf = new SolidBrush(TargetFill))
        using (var tp = new Pen(Color.FromArgb(200, Theme.Accent), 1.5f)) {
            foreach (var p in targets) {
                var r = CellRect(p.Row, p.Col); r.Inflate(-2, -2);
                g.FillRectangle(tf, r);
                g.DrawRectangle(tp, r.X, r.Y, r.Width, r.Height);
            }
        }

        // 카드 효과 범위 미리보기 (폭탄 3×3 등)
        using (var af = new SolidBrush(AreaFill))
        using (var ap = new Pen(Color.FromArgb(230, Theme.Danger), 2f)) {
            foreach (var p in previewArea) {
                var r = CellRect(p.Row, p.Col); r.Inflate(-1, -1);
                g.FillRectangle(af, r);
                g.DrawRectangle(ap, r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2);
            }
        }

        // 마우스가 올라간 칸
        if (hover is (int hr, int hc) && (hints.Contains((hr, hc)) || targets.Contains((hr, hc)))) {
            using var hb = new SolidBrush(Color.FromArgb(40, 255, 255, 255));
            g.FillRectangle(hb, CellRect(hr, hc));
        }

        // 봉인 칸: 빗금 + 남은 턴
        using (var hatch = new HatchBrush(HatchStyle.WideUpwardDiagonal, Color.FromArgb(150, 10, 10, 10), Color.FromArgb(60, 90, 40, 120)))
        using (var sf = Theme.F(Math.Max(7f, cs * 0.2f), FontStyle.Bold))
        using (var sb = new SolidBrush(Color.FromArgb(230, 230, 210, 255))) {
            var fmt = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far };
            for (int row = 0; row < Board.Size; row++)
                for (int col = 0; col < Board.Size; col++) {
                    if (!CellStateLogic.Has(cells[row, col], CellState.Sealed)) continue;
                    var r = CellRect(row, col);
                    g.FillRectangle(hatch, r);
                    var tr = r; tr.Inflate(-3, -2);
                    g.DrawString($"봉인 {cells[row, col].SealTurns}", sf, sb, tr, fmt);
                }
        }

        // 격자 + 화점
        using (var pen = new Pen(Color.FromArgb(140, 10, 40, 25), 1.2f)) {
            for (int i = 0; i <= Board.Size; i++) {
                g.DrawLine(pen, m, m + i * cs, m + full, m + i * cs);
                g.DrawLine(pen, m + i * cs, m, m + i * cs, m + full);
            }
        }
        using (var dot = new SolidBrush(Color.FromArgb(160, 10, 40, 25))) {
            float d = cs * 0.1f;
            foreach (var (r, c) in new[] { (2, 2), (2, 6), (6, 2), (6, 6) })
                g.FillEllipse(dot, m + c * cs - d / 2, m + r * cs - d / 2, d, d);
        }

        // 돌
        float pad = cs * 0.09f;
        for (int row = 0; row < Board.Size; row++) {
            for (int col = 0; col < Board.Size; col++) {
                Cell cell = cells[row, col];
                if (cell.Stone == IsOnStone.Empty) continue;

                var rect = CellRect(row, col); rect.Inflate(-pad, -pad);
                Theme.DrawStone(g, rect, cell.Stone);

                // 고정 돌: 금색 테두리 + 자물쇠 점
                if (CellStateLogic.Has(cell, CellState.Fixed)) {
                    using var fp = new Pen(Theme.Accent, cs * 0.07f);
                    var fr = rect; fr.Inflate(-cs * 0.04f, -cs * 0.04f);
                    g.DrawEllipse(fp, fr);
                }

                // 뒤집힐 돌 미리보기
                if (previewFlips.Contains((row, col))) {
                    using var mp = new Pen(FlipMark, cs * 0.06f);
                    var mr = rect; mr.Inflate(-cs * 0.22f, -cs * 0.22f);
                    g.DrawEllipse(mp, mr);
                }
            }
        }

        // 놓일 돌 미리보기 (반투명)
        if (ghost is (int gr, int gc)) {
            var rect = CellRect(gr, gc); rect.Inflate(-pad, -pad);
            Theme.DrawStone(g, rect, ghostStone, alpha: 120);
        }

        // 착수 가능 칸: 작은 점
        using (var hintBrush = new SolidBrush(HintColor)) {
            float d = cs * 0.22f;
            foreach (var p in hints) {
                if (ghost == p) continue;
                var r = CellRect(p.Row, p.Col);
                g.FillEllipse(hintBrush, r.X + (cs - d) / 2, r.Y + (cs - d) / 2, d, d);
            }
        }

        // 최근 착수: 빨간 점
        if (lastMove is (int lr, int lc) && cells[lr, lc].Stone != IsOnStone.Empty) {
            var r = CellRect(lr, lc);
            float d = cs * 0.16f;
            using var lb = new SolidBrush(Theme.Danger);
            g.FillEllipse(lb, r.X + (cs - d) / 2, r.Y + (cs - d) / 2, d, d);
        }
    }

    // ───────────── 마우스 ─────────────

    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        var cell = CellAt(e.Location);
        if (cell == hover) return;

        hover = cell;
        Cursor = cell is (int r, int c) && (hints.Contains((r, c)) || targets.Contains((r, c))) ? Cursors.Hand : Cursors.Default;
        CellHovered?.Invoke(cell);
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e) {
        base.OnMouseLeave(e);
        hover = null;
        Cursor = Cursors.Default;
        CellHovered?.Invoke(null);
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e) {
        base.OnMouseClick(e);
        if (CellAt(e.Location) is (int r, int c))
            CellClicked?.Invoke((r, c));
    }
}
