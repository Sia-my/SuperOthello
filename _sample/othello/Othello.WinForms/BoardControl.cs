using System.Drawing.Drawing2D;
using Othello.Core.Model;

namespace Othello.WinForms;

/// <summary>
/// GDI+로 보드를 그리는 사용자 정의 컨트롤 (설계서 3.2). 더블 버퍼링으로 깜빡임을 없앤다.
/// 그리기만 담당하고 규칙은 모른다. 클릭한 칸은 CellClicked 이벤트로 알린다.
/// </summary>
public class BoardControl : Control
{
    private static readonly Color BoardColor = Color.FromArgb(46, 125, 80);
    private static readonly Color LineColor = Color.FromArgb(30, 80, 50);
    private static readonly Color HintColor = Color.FromArgb(140, 0, 0, 0);
    private static readonly Color TargetColor = Color.FromArgb(110, 255, 210, 60);
    private static readonly Color LastMoveColor = Color.FromArgb(220, 40, 40);
    private static readonly Color FixedColor = Color.Gold;

    private Board? board;
    private IReadOnlyCollection<Position> hints = Array.Empty<Position>();
    private IReadOnlyCollection<Position> targets = Array.Empty<Position>();
    private Position? lastMove;

    public event Action<Position>? CellClicked;

    public BoardControl()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    /// <summary>다시 그릴 내용을 한 번에 넘긴다.</summary>
    public void Render(Board board, IReadOnlyCollection<Position> hints,
                     IReadOnlyCollection<Position> targets, Position? lastMove)
    {
        this.board = board;
        this.hints = hints;
        this.targets = targets;
        this.lastMove = lastMove;
        Invalidate();
    }

    private float CellSize => Math.Min(ClientSize.Width, ClientSize.Height) / (float)Board.Size;

    private RectangleF CellRect(int r, int c)
        => new(c * CellSize, r * CellSize, CellSize, CellSize);

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float cs = CellSize;
        float full = cs * Board.Size;

        using (var bg = new SolidBrush(BoardColor)) g.FillRectangle(bg, 0, 0, full, full);
        if (board == null) return;

        // 패 대상 칸 강조
        using (var tb = new SolidBrush(TargetColor))
            foreach (var p in targets) g.FillRectangle(tb, CellRect(p.Row, p.Col));

        // 봉인 칸: 빗금
        using (var hatch = new HatchBrush(HatchStyle.WideUpwardDiagonal, Color.FromArgb(150, 0, 0, 0), Color.Transparent))
            for (int r = 0; r < Board.Size; r++)
                for (int c = 0; c < Board.Size; c++)
                    if (board[r, c].Has(CellState.Sealed)) g.FillRectangle(hatch, CellRect(r, c));

        // 격자
        using (var pen = new Pen(LineColor, 1.5f))
            for (int i = 0; i <= Board.Size; i++)
            {
                g.DrawLine(pen, 0, i * cs, full, i * cs);
                g.DrawLine(pen, i * cs, 0, i * cs, full);
            }

        // 돌
        float pad = cs * 0.1f;
        for (int r = 0; r < Board.Size; r++)
        {
            for (int c = 0; c < Board.Size; c++)
            {
                Cell cell = board[r, c];
                if (cell.IsEmpty) continue;

                var rect = CellRect(r, c);
                rect.Inflate(-pad, -pad);
                using (var brush = new SolidBrush(cell.Color == Stone.Black ? Color.Black : Color.White))
                    g.FillEllipse(brush, rect);
                using (var outline = new Pen(Color.FromArgb(60, 0, 0, 0), 1f))
                    g.DrawEllipse(outline, rect);

                if (cell.Has(CellState.Fixed))   // 고정 돌: 금색 테두리
                    using (var fp = new Pen(FixedColor, cs * 0.07f)) g.DrawEllipse(fp, rect);
            }
        }

        // 착수 가능 칸: 작은 점
        using (var hb = new SolidBrush(HintColor))
            foreach (var p in hints)
            {
                var rect = CellRect(p.Row, p.Col);
                float d = cs * 0.2f;
                g.FillEllipse(hb, rect.X + (cs - d) / 2, rect.Y + (cs - d) / 2, d, d);
            }

        // 최근 착수 돌 하이라이트 (F6-2)
        if (lastMove is Position lm)
        {
            var rect = CellRect(lm.Row, lm.Col);
            float d = cs * 0.18f;
            using var lb = new SolidBrush(LastMoveColor);
            g.FillEllipse(lb, rect.X + (cs - d) / 2, rect.Y + (cs - d) / 2, d, d);
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        int c = (int)(e.X / CellSize);
        int r = (int)(e.Y / CellSize);
        if (Board.InBounds(r, c)) CellClicked?.Invoke(new Position(r, c));
    }
}
