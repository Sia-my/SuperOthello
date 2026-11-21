
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Othello.Core.Logic;
using Othello.Core.Model;

namespace SuperOthello;

/*
 * BoardControl.cs - 오셀로 게임판 표시 및 클릭 입력
 *
 * [UI 사용 안내]
 * 보드 표시 및 갱신:
 *   boardControl.Render(controller.Data.Board, hints, targets, controller.LastMove);
 *
 * 칸 클릭 이벤트:
 *   boardControl.CellClicked += p =>
 *       controller.TryPlace(p.Row, p.Col, out _);
 *
 * hints/targets: IReadOnlyCollection<(int Row, int Col)>
 * lastMove    : (int Row, int Col)?
 *
 * 화면에 돌, 봉인 빗금, 고정 테두리, 착수 힌트, 카드 대상,
 * 마지막 착수 표시만 그린다.
 * 오셀로 규칙 및 게임 데이터 변경은 하지 않는다.
 */
public class BoardControl : Control {
    private static readonly Color BoardColor = Color.FromArgb(46, 125, 80);
    private static readonly Color LineColor = Color.FromArgb(30, 80, 50);
    private static readonly Color HintColor = Color.FromArgb(140, 0, 0, 0);
    private static readonly Color TargetColor = Color.FromArgb(110, 255, 210, 60);
    private static readonly Color LastMoveColor = Color.FromArgb(220, 40, 40);
    private static readonly Color FixedColor = Color.Gold;

    private Board? board;
    private IReadOnlyCollection<(int Row, int Col)> hints =
        Array.Empty<(int Row, int Col)>();
    private IReadOnlyCollection<(int Row, int Col)> targets =
        Array.Empty<(int Row, int Col)>();
    private (int Row, int Col)? lastMove;

    public event Action<(int Row, int Col)>? CellClicked;

    public BoardControl() {
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw, true);
    }

    // 현재 보드와 강조할 좌표들을 받아 화면을 갱신한다.
    public void Render(
        Board board,
        IReadOnlyCollection<(int Row, int Col)> hints,
        IReadOnlyCollection<(int Row, int Col)> targets,
        (int Row, int Col)? lastMove) {
        this.board = board;
        this.hints = hints;
        this.targets = targets;
        this.lastMove = lastMove;

        Invalidate();
    }

    private float CellSize
        => Math.Min(ClientSize.Width, ClientSize.Height)
           / (float)Board.Size;

    private RectangleF CellRect(int row, int col)
        => new(
            col * CellSize,
            row * CellSize,
            CellSize,
            CellSize);

    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float cs = CellSize;
        float full = cs * Board.Size;

        // 게임판 배경
        using (var bg = new SolidBrush(BoardColor))
            g.FillRectangle(bg, 0, 0, full, full);

        if (!board.HasValue || board.Value.Cells == null)
            return;

        Cell[,] cells = board.Value.Cells;

        // 카드 대상 칸 강조
        using (var targetBrush = new SolidBrush(TargetColor)) {
            foreach (var p in targets)
                g.FillRectangle(
                    targetBrush,
                    CellRect(p.Row, p.Col));
        }

        // 봉인된 칸: 빗금
        using (var hatch = new HatchBrush(
            HatchStyle.WideUpwardDiagonal,
            Color.FromArgb(150, 0, 0, 0),
            Color.Transparent)) {
            for (int row = 0; row < Board.Size; row++) {
                for (int col = 0; col < Board.Size; col++) {
                    if (CellStateLogic.Has(
                        cells[row, col], CellState.Sealed)) {
                        g.FillRectangle(
                            hatch,
                            CellRect(row, col));
                    }
                }
            }
        }

        // 격자
        using (var pen = new Pen(LineColor, 1.5f)) {
            for (int i = 0; i <= Board.Size; i++) {
                g.DrawLine(pen, 0, i * cs, full, i * cs);
                g.DrawLine(pen, i * cs, 0, i * cs, full);
            }
        }

        // 돌 및 고정 상태 표시
        float pad = cs * 0.1f;

        for (int row = 0; row < Board.Size; row++) {
            for (int col = 0; col < Board.Size; col++) {
                Cell cell = cells[row, col];

                if (cell.Stone == IsOnStone.Empty)
                    continue;

                var rect = CellRect(row, col);
                rect.Inflate(-pad, -pad);

                using (var brush = new SolidBrush(
                    cell.Stone == IsOnStone.Black
                        ? Color.Black
                        : Color.White)) {
                    g.FillEllipse(brush, rect);
                }

                using (var outline = new Pen(
                    Color.FromArgb(60, 0, 0, 0), 1f)) {
                    g.DrawEllipse(outline, rect);
                }

                // 고정 돌: 금색 테두리
                if (CellStateLogic.Has(
                    cell, CellState.Fixed)) {
                    using var fixedPen =
                        new Pen(FixedColor, cs * 0.07f);

                    g.DrawEllipse(fixedPen, rect);
                }
            }
        }

        // 착수 가능한 칸: 작은 점
        using (var hintBrush = new SolidBrush(HintColor)) {
            foreach (var p in hints) {
                var rect = CellRect(p.Row, p.Col);
                float diameter = cs * 0.2f;

                g.FillEllipse(
                    hintBrush,
                    rect.X + (cs - diameter) / 2,
                    rect.Y + (cs - diameter) / 2,
                    diameter, diameter);
            }
        }

        // 마지막 착수 위치 표시
        if (lastMove.HasValue) {
            var p = lastMove.Value;
            var rect = CellRect(p.Row, p.Col);

            float diameter = cs * 0.18f;

            using var lastBrush =
                new SolidBrush(LastMoveColor);

            g.FillEllipse(
                lastBrush,
                rect.X + (cs - diameter) / 2,
                rect.Y + (cs - diameter) / 2,
                diameter, diameter);
        }
    }

    protected override void OnMouseClick(MouseEventArgs e) {
        base.OnMouseClick(e);

        float cs = CellSize;

        if (cs <= 0 || e.X < 0 || e.Y < 0)
            return;

        int col = (int)(e.X / cs);
        int row = (int)(e.Y / cs);

        if (BoardLogic.InBounds(row, col))
            CellClicked?.Invoke((row, col));
    }
}
