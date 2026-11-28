using System.Drawing.Drawing2D;
using Othello.Core.Logic;
using Othello.Core.Model;

namespace SuperOthello;

// 결과 화면: 흑·백 뒤집기·카드 점수 / 남은 돌 보너스 / 최종 점수, 승자 (설계서 3.2, F3-3·F3-4)
// - 다시 하기 → DialogResult.Retry, 메인 메뉴·창 닫기 → DialogResult.OK
public class ResultForm : Form {
    private readonly IsOnStone winner;
    private readonly string headline;
    private readonly (PlayerSlot Slot, IsOnStone Stone, (int Play, int Bonus, int Final) Score)[] sides;

    public ResultForm(Data finished, PlayerSlot black, PlayerSlot white) {
        Text = "게임 결과";
        Theme.ApplyForm(this);
        DoubleBuffered = true;
        ClientSize = new Size(520, 380);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        winner = ScoreLogic.Winner(finished);
        headline = winner switch {
            IsOnStone.Black => $"{black.Id} 승리!",
            IsOnStone.White => $"{white.Id} 승리!",
            _ => "무승부"
        };
        sides = new[] {
            (black, IsOnStone.Black, ScoreLogic.BreakdownOf(finished, IsOnStone.Black)),
            (white, IsOnStone.White, ScoreLogic.BreakdownOf(finished, IsOnStone.White))
        };

        var retry = Theme.MakeButton("다시 하기", new Size(130, 42), primary: true);
        var menu = Theme.MakeButton("메인 메뉴", new Size(130, 42));
        retry.DialogResult = DialogResult.Retry;
        menu.DialogResult = DialogResult.OK;
        retry.Location = new Point(ClientSize.Width / 2 - retry.Width - 6, ClientSize.Height - 62);
        menu.Location = new Point(ClientSize.Width / 2 + 6, ClientSize.Height - 62);
        Controls.AddRange(new Control[] { retry, menu });
        AcceptButton = menu;
        CancelButton = menu;   // Esc·창 닫기는 메인 메뉴로
    }

    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        var center = new StringFormat { Alignment = StringAlignment.Center };

        // 승자 문구
        using (var sub = Theme.F(10f))
        using (var sb = new SolidBrush(Theme.SubText))
            g.DrawString("게임 종료", sub, sb, new RectangleF(0, 18, ClientSize.Width, 22), center);
        using (var hf = Theme.F(20f, FontStyle.Bold))
        using (var hb = new SolidBrush(winner == IsOnStone.Empty ? Theme.Text : Theme.Accent))
            g.DrawString(headline, hf, hb, new RectangleF(0, 40, ClientSize.Width, 40), center);

        // 흑·백 점수 카드
        float cardW = (ClientSize.Width - 60) / 2f, top = 96, cardH = 200;
        for (int i = 0; i < 2; i++) {
            var (slot, stone, score) = sides[i];
            bool won = stone == winner;
            var r = new RectangleF(20 + i * (cardW + 20), top, cardW, cardH);

            using (var path = Theme.Rounded(r, 14)) {
                using (var fill = new SolidBrush(Theme.Panel)) g.FillPath(fill, path);
                using var pen = new Pen(won ? Theme.Accent : Theme.Border, won ? 2.5f : 1f);
                g.DrawPath(pen, path);
            }

            Theme.DrawStone(g, new RectangleF(r.X + 16, r.Y + 16, 34, 34), stone);
            using (var nf = Theme.F(12f, FontStyle.Bold))
            using (var nb = new SolidBrush(Theme.Text)) {
                var fmt = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(slot.Id, nf, nb, new RectangleF(r.X + 58, r.Y + 22, r.Width - 66, 24), fmt);
            }
            if (won) {
                using var bf = Theme.F(8.5f, FontStyle.Bold);
                Theme.DrawBadge(g, "승리", bf, Theme.Accent, new PointF(r.Right - 52, r.Y + 8), out _);
            }

            Row(g, r, 68, "뒤집기·카드 점수", score.Play.ToString(), false);
            Row(g, r, 98, "남은 돌 보너스", $"+{score.Bonus}", false);
            using (var line = new Pen(Theme.Border, 1f))
                g.DrawLine(line, r.X + 16, r.Y + 132, r.Right - 16, r.Y + 132);
            Row(g, r, 142, "최종 점수", score.Final.ToString(), true);
        }
    }

    private static void Row(Graphics g, RectangleF card, float y, string label, string value, bool big) {
        using var lf = Theme.F(big ? 10.5f : 9.5f, big ? FontStyle.Bold : FontStyle.Regular);
        using var lb = new SolidBrush(big ? Theme.Text : Theme.SubText);
        using var vf = Theme.F(big ? 20f : 12f, FontStyle.Bold);
        using var vb = new SolidBrush(Theme.Text);
        g.DrawString(label, lf, lb, card.X + 16, card.Y + y + (big ? 10 : 2));
        var right = new StringFormat { Alignment = StringAlignment.Far };
        g.DrawString(value, vf, vb, new RectangleF(card.X, card.Y + y, card.Width - 16, 40), right);
    }
}
