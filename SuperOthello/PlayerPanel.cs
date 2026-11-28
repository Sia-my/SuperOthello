using System.Drawing.Drawing2D;
using Othello.Core.Cards;
using Othello.Core.Model;

namespace SuperOthello;

// 게임 화면 좌·우 플레이어 패널: 돌·ID·점수·상태 효과 + 손패 3칸 (설계서 3.2, F2-6)
// - 현재 차례면 금색 테두리, CPU가 생각 중이면 "생각 중…" 표시
public class PlayerPanel : Control {
    public const int HeaderHeight = 150;
    private const int TileHeight = 88;
    private const int TileGap = 8;

    private readonly IsOnStone stone;
    private readonly CardTile[] tiles = new CardTile[Hand.MaxSize];
    private readonly ToolTip tip = new() { InitialDelay = 300 };

    private string id = "";
    private string role = "";
    private int score;
    private bool isTurn, thinking, silenced, shielded;

    public event Action<ISkillCard>? CardClicked;

    public PlayerPanel(IsOnStone stone) {
        this.stone = stone;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Back;

        for (int i = 0; i < tiles.Length; i++) {
            tiles[i] = new CardTile();
            tiles[i].CardClicked += c => CardClicked?.Invoke(c);
            Controls.Add(tiles[i]);
        }
        LayoutTiles();
    }

    protected override void OnResize(EventArgs e) {
        base.OnResize(e);
        LayoutTiles();
    }

    private void LayoutTiles() {
        if (tiles[0] == null) return;
        int top = HeaderHeight + 30;
        for (int i = 0; i < tiles.Length; i++)
            tiles[i].Bounds = new Rectangle(12, top + i * (TileHeight + TileGap), Width - 24, TileHeight);
    }

    // 표시할 상태를 한 번에 받기
    public void Show(
        PlayerSlot slot, int score, bool isTurn, bool thinking, PlayerStatus status,
        IReadOnlyList<ISkillCard> cards, ISkillCard? selected,
        bool canClick, Func<ISkillCard, bool> canUse) {
        id = slot.Id;
        role = slot.IsCpu ? "CPU · " + (slot.Difficulty switch { Difficulty.Easy => "쉬움", Difficulty.Normal => "보통", _ => "어려움" }) : "사람";
        this.score = score;
        this.isTurn = isTurn;
        this.thinking = thinking;
        silenced = status.Silenced;
        shielded = status.Shielded;

        for (int i = 0; i < tiles.Length; i++) {
            ISkillCard? card = i < cards.Count ? cards[i] : null;
            bool usable = card != null && canClick && canUse(card);
            bool blocked = card != null && canClick && !usable;
            tiles[i].Show(card, card != null && ReferenceEquals(card, selected), usable, blocked);
            tip.SetToolTip(tiles[i], card == null ? "" : $"{card.Name} ({Theme.TypeName(card.Type)})\n{card.Description}");   // 손패 툴팁 (F6-3)
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e) {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var r = new RectangleF(1, 1, Width - 3, Height - 3);
        using (var path = Theme.Rounded(r, 14)) {
            using (var fill = new SolidBrush(Theme.Panel)) g.FillPath(fill, path);
            using var pen = new Pen(isTurn ? Theme.Accent : Theme.Border, isTurn ? 2.5f : 1f);
            g.DrawPath(pen, path);
        }

        // 돌 아이콘 + 흑/백
        var icon = new RectangleF(16, 16, 44, 44);
        Theme.DrawStone(g, icon, stone);
        using (var sideFont = Theme.F(9f, FontStyle.Bold))
        using (var sideBrush = new SolidBrush(Theme.SubText))
            g.DrawString(stone == IsOnStone.Black ? "1P · 흑" : "2P · 백", sideFont, sideBrush, 70, 16);

        // ID
        using (var idFont = Theme.F(13f, FontStyle.Bold))
        using (var idBrush = new SolidBrush(Theme.Text)) {
            var fmt = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            g.DrawString(id, idFont, idBrush, new RectangleF(70, 34, r.Width - 80, 26), fmt);
        }

        // 역할 배지 + 차례 배지
        using (var bf = Theme.F(8.5f, FontStyle.Bold)) {
            Theme.DrawBadge(g, role, bf, Theme.SubText, new PointF(16, 70), out float w);
            if (isTurn)
                Theme.DrawBadge(g, thinking ? "생각 중…" : "내 차례", bf, Theme.Accent, new PointF(16 + w + 6, 70), out _);
        }

        // 점수: 큰 숫자 + 오른쪽에 "점" (숫자 아래쪽에 맞춤)
        using (var sf = Theme.F(24f, FontStyle.Bold))
        using (var sb = new SolidBrush(score < 0 ? Theme.Danger : Theme.Text))
        using (var lf = Theme.F(10f))
        using (var lb = new SolidBrush(Theme.SubText)) {
            string text = score.ToString();
            SizeF ss = g.MeasureString(text, sf);
            g.DrawString(text, sf, sb, 12, 96);
            g.DrawString("점", lf, lb, 12 + ss.Width - 6, 96 + ss.Height - 26);
        }

        // 상태 효과
        using (var bf = Theme.F(8.5f, FontStyle.Bold)) {
            float x = r.Right - 12;
            if (shielded) { x -= Measure(g, "방패", bf); Theme.DrawBadge(g, "방패", bf, Theme.Primary, new PointF(x, 104), out _); x -= 6; }
            if (silenced) { x -= Measure(g, "침묵", bf); Theme.DrawBadge(g, "침묵", bf, Theme.TypeColor(CardType.C), new PointF(x, 104), out _); }
        }

        // 손패 제목
        using (var hf = Theme.F(9.5f, FontStyle.Bold))
        using (var hb = new SolidBrush(Theme.SubText))
            g.DrawString($"손패 {tiles.Count(t => t.Card != null)} / {Hand.MaxSize}", hf, hb, 14, HeaderHeight + 6);
        using (var line = new Pen(Theme.Border, 1f))
            g.DrawLine(line, 14, HeaderHeight, r.Right - 14, HeaderHeight);
    }

    private static float Measure(Graphics g, string text, Font font) => g.MeasureString(text, font).Width + 10;
}
