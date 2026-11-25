using Othello.Core.Model;

namespace SuperOthello;

// 대전 설정: 1P(흑)·2P(백) 슬롯마다 사람 ID 또는 CPU 난이도 선택 (설계서 3.2)
// - 시작 가능 여부는 PlayerIdRule.GetSetupError로 판단, # 입력은 KeyPress에서 차단
public class MatchSetupForm : Form {
    private readonly SlotPanel blackPanel;
    private readonly SlotPanel whitePanel;
    private readonly Label errorLabel = new() {
        Location = new Point(12, 232),
        Size = new Size(456, 24),
        ForeColor = Color.Firebrick
    };
    private readonly Button startButton = new() { Text = "시작", Location = new Point(276, 264), Size = new Size(92, 34) };
    private readonly Button cancelButton = new() { Text = "취소", Location = new Point(376, 264), Size = new Size(92, 34), DialogResult = DialogResult.Cancel };

    public PlayerSlot Black => blackPanel.ToSlot();
    public PlayerSlot White => whitePanel.ToSlot();

    public MatchSetupForm() {
        Text = "대전 설정";
        ClientSize = new Size(480, 310);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("맑은 고딕", 10f);

        blackPanel = new SlotPanel("1P (흑)", new Point(12, 12), isCpu: false);
        whitePanel = new SlotPanel("2P (백)", new Point(12, 122), isCpu: true);
        blackPanel.Changed += UpdateState;
        whitePanel.Changed += UpdateState;

        startButton.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };
        AcceptButton = startButton;
        CancelButton = cancelButton;

        Controls.AddRange(new Control[] { blackPanel, whitePanel, errorLabel, startButton, cancelButton });
        UpdateState();
    }

    private void UpdateState() {
        string? error = PlayerIdRule.GetSetupError(Black, White);
        startButton.Enabled = error == null;
        errorLabel.Text = error ?? "";
    }

    // 슬롯 하나: 사람/CPU 라디오 + ID 입력칸 + 난이도 콤보박스
    private sealed class SlotPanel : GroupBox {
        private static readonly Difficulty[] Levels = { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard };

        private readonly RadioButton humanRadio = new() { Text = "사람", Location = new Point(14, 28), AutoSize = true };
        private readonly RadioButton cpuRadio = new() { Text = "CPU", Location = new Point(14, 62), AutoSize = true };
        private readonly TextBox idBox = new() { Location = new Point(90, 26), Size = new Size(200, 25), MaxLength = PlayerIdRule.MaxLength };
        private readonly ComboBox levelBox = new() { Location = new Point(90, 60), Size = new Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Label hint = new() { Text = "1~12자 한글·영문·숫자", Location = new Point(298, 29), AutoSize = true, ForeColor = Color.Gray };

        public event Action? Changed;

        public SlotPanel(string title, Point location, bool isCpu) {
            Text = title;
            Location = location;
            Size = new Size(456, 100);

            levelBox.Items.AddRange(new object[] { "쉬움", "보통", "어려움" });
            levelBox.SelectedIndex = 1;

            humanRadio.Checked = !isCpu;
            cpuRadio.Checked = isCpu;

            // # 은 CPU 예약 ID 전용이라 입력 자체를 막음 (설계서 2.4)
            idBox.KeyPress += (_, e) => { if (e.KeyChar == '#') e.Handled = true; };
            idBox.TextChanged += (_, _) => Changed?.Invoke();
            levelBox.SelectedIndexChanged += (_, _) => Changed?.Invoke();
            humanRadio.CheckedChanged += (_, _) => { SyncEnabled(); Changed?.Invoke(); };

            Controls.AddRange(new Control[] { humanRadio, cpuRadio, idBox, levelBox, hint });
            SyncEnabled();
        }

        private void SyncEnabled() {
            idBox.Enabled = humanRadio.Checked;
            hint.Enabled = humanRadio.Checked;
            levelBox.Enabled = cpuRadio.Checked;
        }

        public PlayerSlot ToSlot()
            => cpuRadio.Checked
                ? PlayerSlot.Cpu(Levels[Math.Max(0, levelBox.SelectedIndex)])
                : PlayerSlot.Human(idBox.Text);
    }
}
