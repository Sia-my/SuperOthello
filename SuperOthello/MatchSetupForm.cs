using System.Drawing.Drawing2D;
using Othello.Core.Model;

namespace SuperOthello;

// 대전 설정: 1P(흑)·2P(백) 슬롯마다 사람 ID 또는 CPU 난이도 선택 (설계서 3.2)
// - 시작 가능 여부는 PlayerIdRule.GetSetupError로 판단, # 입력은 KeyPress에서 차단
public class MatchSetupForm : Form {
    private readonly SlotPanel blackPanel;
    private readonly SlotPanel whitePanel;
    private readonly Label statusLabel = new() {
        Location = new Point(20, 256),
        Size = new Size(392, 40),
        Font = Theme.F(9.5f, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly Button startButton = Theme.MakeButton("시작", new Size(104, 40), primary: true);
    private readonly Button cancelButton = Theme.MakeButton("취소", new Size(92, 40));

    public PlayerSlot Black => blackPanel.ToSlot();
    public PlayerSlot White => whitePanel.ToSlot();

    public MatchSetupForm() {
        Text = "대전 설정";
        Theme.ApplyForm(this);
        ClientSize = new Size(640, 310);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        blackPanel = new SlotPanel(IsOnStone.Black, "1P · 흑 (먼저 둠)", isCpu: false) { Bounds = new Rectangle(20, 20, 290, 226) };
        whitePanel = new SlotPanel(IsOnStone.White, "2P · 백", isCpu: true) { Bounds = new Rectangle(330, 20, 290, 226) };
        blackPanel.Changed += UpdateState;
        whitePanel.Changed += UpdateState;

        startButton.Location = new Point(ClientSize.Width - 20 - startButton.Width, 256);
        cancelButton.Location = new Point(startButton.Left - 10 - cancelButton.Width, 256);
        cancelButton.DialogResult = DialogResult.Cancel;
        startButton.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };
        AcceptButton = startButton;
        CancelButton = cancelButton;

        Controls.AddRange(new Control[] { blackPanel, whitePanel, statusLabel, startButton, cancelButton });
        UpdateState();
    }

    private void UpdateState() {
        string? error = PlayerIdRule.GetSetupError(Black, White);
        startButton.Enabled = error == null;
        startButton.BackColor = error == null ? Theme.Primary : Theme.PanelLight;
        statusLabel.Text = error ?? "준비 완료 · 시작을 누르세요";
        statusLabel.ForeColor = error == null ? Theme.Good : Theme.Danger;
    }

    // 슬롯 하나: 사람/CPU 선택 + ID 입력칸 + 난이도 선택
    private sealed class SlotPanel : Panel {
        private static readonly Difficulty[] Levels = { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard };

        private readonly IsOnStone stone;
        private readonly string title;
        private readonly Panel modeGroup = new() { Bounds = new Rectangle(16, 64, 258, 34), BackColor = Theme.Panel };
        private readonly Panel levelGroup = new() { Bounds = new Rectangle(16, 178, 258, 32), BackColor = Theme.Panel };
        private readonly RadioButton humanRadio = Toggle("사람");
        private readonly RadioButton cpuRadio = Toggle("CPU");
        private readonly RadioButton[] levelRadios = { Toggle("쉬움"), Toggle("보통"), Toggle("어려움") };
        private readonly TextBox idBox = new() {
            Bounds = new Rectangle(16, 126, 258, 28), MaxLength = PlayerIdRule.MaxLength,
            BackColor = Theme.Back, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, Font = Theme.F(11f)
        };

        public event Action? Changed;

        public SlotPanel(IsOnStone stone, string title, bool isCpu) {
            this.stone = stone;
            this.title = title;
            DoubleBuffered = true;
            BackColor = Theme.Panel;

            humanRadio.Bounds = new Rectangle(0, 0, 126, 34);
            cpuRadio.Bounds = new Rectangle(132, 0, 126, 34);
            modeGroup.Controls.AddRange(new Control[] { humanRadio, cpuRadio });

            for (int i = 0; i < levelRadios.Length; i++) {
                levelRadios[i].Bounds = new Rectangle(i * 88, 0, 82, 32);
                levelRadios[i].CheckedChanged += (_, _) => { Restyle(); Changed?.Invoke(); };
                levelGroup.Controls.Add(levelRadios[i]);
            }
            levelRadios[1].Checked = true;

            humanRadio.Checked = !isCpu;
            cpuRadio.Checked = isCpu;

            // # 은 CPU 예약 ID 전용이라 입력 자체를 막음 (설계서 2.4)
            idBox.KeyPress += (_, e) => { if (e.KeyChar == '#') e.Handled = true; };
            idBox.TextChanged += (_, _) => Changed?.Invoke();
            humanRadio.CheckedChanged += (_, _) => { SyncEnabled(); Changed?.Invoke(); };

            Controls.AddRange(new Control[] { modeGroup, idBox, levelGroup });
            SyncEnabled();
        }

        private static RadioButton Toggle(string text) {
            var r = new RadioButton {
                Text = text, Appearance = Appearance.Button, FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter, Font = Theme.F(10f, FontStyle.Bold),
                Cursor = Cursors.Hand, ForeColor = Theme.Text, BackColor = Theme.PanelLight
            };
            r.FlatAppearance.BorderColor = Theme.Border;
            r.FlatAppearance.CheckedBackColor = Theme.Primary;
            r.FlatAppearance.MouseOverBackColor = Theme.Border;
            return r;
        }

        private void SyncEnabled() {
            idBox.Enabled = humanRadio.Checked;
            idBox.BackColor = humanRadio.Checked ? Theme.Back : Theme.PanelLight;
            levelGroup.Enabled = cpuRadio.Checked;
            Restyle();
            Invalidate();
            if (humanRadio.Checked && IsHandleCreated) idBox.Focus();
        }

        // 꺼진 그룹은 흐리게
        private void Restyle() {
            foreach (var r in levelRadios) {
                r.ForeColor = levelGroup.Enabled ? Theme.Text : Theme.Dim;
                r.FlatAppearance.CheckedBackColor = levelGroup.Enabled ? Theme.Primary : Theme.Border;
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var pen = new Pen(Theme.Border, 1f))
            using (var path = Theme.Rounded(new RectangleF(0.5f, 0.5f, Width - 2, Height - 2), 12))
                g.DrawPath(pen, path);

            Theme.DrawStone(g, new RectangleF(16, 16, 32, 32), stone);
            using (var tf = Theme.F(12f, FontStyle.Bold))
            using (var tb = new SolidBrush(Theme.Text))
                g.DrawString(title, tf, tb, 56, 20);

            using var lf = Theme.F(9f);
            using var on = new SolidBrush(Theme.SubText);
            using var off = new SolidBrush(Theme.Dim);
            g.DrawString("ID (1~12자 한글·영문·숫자)", lf, humanRadio.Checked ? on : off, 14, 106);
            g.DrawString("CPU 난이도", lf, cpuRadio.Checked ? on : off, 14, 158);
        }

        public PlayerSlot ToSlot() {
            int level = Array.FindIndex(levelRadios, r => r.Checked);
            return cpuRadio.Checked
                ? PlayerSlot.Cpu(Levels[Math.Max(0, level)])
                : PlayerSlot.Human(idBox.Text);
        }
    }
}
