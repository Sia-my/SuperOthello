using Othello.Core.Cards;
using Othello.Core.Control;
using Othello.Core.Cpu;
using Othello.Core.Model;
using Othello.Core.Logic;
using Othello.Core.Records;
using System.Linq;

namespace SuperOthello;

/// <summary>
/// 게임 화면 (설계서 3.2). 디자이너 없이 코드로 배치했다.
/// 흐름: 손패 클릭(선택) → 대상 칸 클릭(필요 시) → 착수 칸 클릭.
/// </summary>
public class GameForm : Form {
    private readonly GameController ctrl;
    private readonly RecordRepository? records;   // null이면 전적 기능 비활성
    private readonly CPUPlayer?[] cpus = new CPUPlayer?[2];

    private readonly BoardControl boardView = new() { Location = new Point(12, 12), Size = new Size(480, 480) };
    private readonly Label turnLabel = new() { Location = new Point(510, 12), AutoSize = true, Font = new Font("맑은 고딕", 13f, FontStyle.Bold) };
    private readonly Label infoLabel = new() { Location = new Point(510, 44), AutoSize = true, Font = new Font("맑은 고딕", 10f) };
    private readonly GroupBox[] handBoxes =
    {
        new() { Location = new Point(510, 110), Size = new Size(330, 80) },
        new() { Location = new Point(510, 200), Size = new Size(330, 80) }
    };
    private readonly ListBox logBox = new() { Location = new Point(510, 290), Size = new Size(330, 160), HorizontalScrollbar = true };
    private readonly Button resignButton = new() { Text = "기권", Location = new Point(510, 460), Size = new Size(80, 30) };
    private readonly ToolTip tip = new();

    // 사람의 입력 진행 상태
    private ISkillCard? selectedCard;
    private (int Row, int Col)? selectedTarget;
    private bool cpuThinking;
    private bool resignConfirmed;   // 기권 버튼으로 이미 확인함

    // 결과 화면에서 '다시 하기'를 골랐는지 (창이 닫힌 뒤 MainForm이 확인)
    public bool ReplayRequested { get; private set; }

    public GameForm(GameController controller, RecordRepository? recordRepository = null) {
        ctrl = controller;
        records = recordRepository;
        Text = "초능력 오셀로";
        ClientSize = new Size(856, 504);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        Controls.AddRange(new Control[] { boardView, turnLabel, infoLabel, handBoxes[0], handBoxes[1], logBox, resignButton });

        foreach (IsOnStone s in new[] { IsOnStone.Black, IsOnStone.White }) {
            var slot = ctrl.SlotOf(s);
            if (slot.IsCpu) cpus[ScoreLogic.PlayerIndex(s)] = new CPUPlayer(slot.Difficulty);
        }

        boardView.CellClicked += OnCellClicked;
        resignButton.Click += OnResign;
        ctrl.LogAdded += msg => { logBox.Items.Add(msg); logBox.TopIndex = logBox.Items.Count - 1; };
        ctrl.StateChanged += RefreshView;
        ctrl.GameOver += OnGameOver;
        ctrl.ChooseDiscard = ChooseDiscardDialog;

        Shown += (_, _) => ctrl.Start();
    }

    // ───────────────────────── 화면 갱신 ─────────────────────────

    private static string StoneName(IsOnStone stone) => stone == IsOnStone.Black ? "흑" : "백";

    private void RefreshView() {
        var s = ctrl.Data;
        IsOnStone me = s.Current;

        turnLabel.Text = s.IsOver ? "게임 종료"
                       : cpuThinking ? $"{StoneName(me)} 차례 — 생각 중…"
                       : $"{StoneName(me)} 차례 ({ctrl.CurrentSlot.Id})";
        infoLabel.Text = $"흑 {ctrl.SlotOf(IsOnStone.Black).Id}: {s.Scores[0]}점   " +
                         $"백 {ctrl.SlotOf(IsOnStone.White).Id}: {s.Scores[1]}점\n" +
                         $"턴 {s.TurnNumber} · 덱 {s.Deck.Count}장 · 빈칸 {BoardLogic.EmptyCount(s)}";

        BuildHand(IsOnStone.Black);
        BuildHand(IsOnStone.White);

        bool myInput = !s.IsOver && !cpuThinking && !ctrl.CurrentSlot.IsCpu;
        IReadOnlyCollection<(int Row, int Col)> targets = Array.Empty<(int Row, int Col)>();
        IReadOnlyCollection<(int Row, int Col)> hints = Array.Empty<(int Row, int Col)>();

        if (myInput) {
            if (selectedCard is { NeedsTarget: true } && selectedTarget is null)
                targets = selectedCard.GetTargets(s, me).ToList();
            else
                hints = MovesAfterSelectedCard();
        }

        boardView.Render(s.Board, hints, targets, ctrl.LastMove);
        resignButton.Enabled = !s.IsOver;

        if (!s.IsOver && ctrl.CurrentSlot.IsCpu && !cpuThinking) _ = RunCpuTurnAsync();
    }

    /// <summary>양측 손패 공개 (F2-6). 자기 차례인 사람의 손패만 누를 수 있다.</summary>
    private void BuildHand(IsOnStone owner) {
        var box = handBoxes[ScoreLogic.PlayerIndex(owner)];
        var s = ctrl.Data;
        var status = s.StatusOf(owner);

        box.Text = $"{StoneName(owner)} 손패" + (status.Silenced ? " (침묵)" : "");
        box.Controls.Clear();

        var cards = s.HandOf(owner).Cards;
        for (int i = 0; i < Hand.MaxSize; i++) {
            var btn = new Button { Location = new Point(8 + i * 106, 22), Size = new Size(100, 48) };
            if (i < cards.Count) {
                var card = cards[i];
                btn.Text = $"{card.Name}\n({card.Type})";
                btn.Enabled = owner == s.Current && !ctrl.CurrentSlot.IsCpu && !cpuThinking && ctrl.CanUseCard(card);
                btn.BackColor = ReferenceEquals(card, selectedCard) ? Color.LightSkyBlue : SystemColors.Control;
                tip.SetToolTip(btn, card.Description);   // 손패 툴팁 (F6-3)
                btn.Click += (_, _) => OnCardClicked(card);
            } else {
                btn.Text = "-";
                btn.Enabled = false;
            }
            box.Controls.Add(btn);
        }
    }

    /// <summary>선택한 패를 적용했다고 가정했을 때 둘 수 있는 칸 (복사본에서 계산).</summary>
    private List<(int Row, int Col)> MovesAfterSelectedCard() {
        var s = ctrl.Data;
        if (selectedCard == null) return BoardLogic.GetValidMoves(s, s.Current);

        var copy = s.Clone();
        selectedCard.Apply(copy, copy.Current, selectedTarget);
        return BoardLogic.GetValidMoves(copy, copy.Current);
    }

    // ───────────────────────── 사람 입력 ─────────────────────────

    private void OnCardClicked(ISkillCard card) {
        if (ReferenceEquals(selectedCard, card)) { ClearSelection(); }   // 다시 누르면 취소
        else { selectedCard = card; selectedTarget = null; }

        // 대상이 필요 없는 패인데 적용 후 둘 곳이 없으면 바로 '패 사용 + 패스'
        if (selectedCard is { NeedsTarget: false } && MovesAfterSelectedCard().Count == 0)
            Submit(GameAction.WithCard(selectedCard, null, null));
        else
            RefreshView();
    }

    private void OnCellClicked((int Row, int Col) p) {
        if (ctrl.Data.IsOver || cpuThinking || ctrl.CurrentSlot.IsCpu) return;

        // 대상 지정 단계
        if (selectedCard is { NeedsTarget: true } && selectedTarget is null) {
            if (!selectedCard.GetTargets(ctrl.Data, ctrl.Data.Current).Contains(p)) return;
            selectedTarget = p;
            if (MovesAfterSelectedCard().Count == 0) Submit(GameAction.WithCard(selectedCard, p, null));
            else RefreshView();
            return;
        }

        // 착수 단계
        if (!MovesAfterSelectedCard().Contains(p)) return;   // 둘 수 없는 칸은 무시
        var action = selectedCard == null
            ? GameAction.Place(p)
            : GameAction.WithCard(selectedCard, selectedTarget, p);
        Submit(action);
    }

    private void Submit(GameAction action) {
        ClearSelection();
        if (!ctrl.TryExecuteTurn(action, out string err)) {
            MessageBox.Show(err, "둘 수 없습니다", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshView();
        }
    }

    private void ClearSelection() {
        selectedCard = null;
        selectedTarget = null;
    }

    // ───────────────────────── CPU 차례 ─────────────────────────

    private async Task RunCpuTurnAsync() {
        var cpu = cpus[ScoreLogic.PlayerIndex(ctrl.Data.Current)];
        if (cpu == null) return;

        cpuThinking = true;
        RefreshView();

        var snapshot = ctrl.Data.Clone();
        var action = await Task.Run(() => cpu.ChooseAction(snapshot));   // 계산은 백그라운드 (F4-4)
        await Task.Delay(700);                                           // 0.5~1초 연출 지연

        cpuThinking = false;
        if (IsDisposed || ctrl.Data.IsOver) return;
        if (!ctrl.TryExecuteTurn(action, out string err)) {
            // 설계서 2.5: CPU 행동이 거부되면 카드 없이 가중치 최고 칸에 착수
            logBox.Items.Add($"CPU 오류: {err} → 일반 착수로 대체");
            if (!ctrl.TryExecuteTurn(cpu.FallbackAction(ctrl.Data), out _))
                RefreshView();
        }
    }

    // ───────────────────────── 대화상자 ─────────────────────────

    /// <summary>손패 4장 중 버릴 1장 선택 (F2-3).</summary>
    private ISkillCard ChooseDiscardDialog(IReadOnlyList<ISkillCard> cards) {
        using var dlg = new Form {
            Text = "버릴 패를 고르세요",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(16 + cards.Count * 106, 72),
            ControlBox = false
        };

        ISkillCard chosen = cards[cards.Count - 1];
        for (int i = 0; i < cards.Count; i++) {
            var card = cards[i];
            var btn = new Button { Text = card.Name, Location = new Point(8 + i * 106, 12), Size = new Size(100, 48) };
            btn.Click += (_, _) => { chosen = card; dlg.DialogResult = DialogResult.OK; };
            dlg.Controls.Add(btn);
        }
        dlg.ShowDialog(this);
        return chosen;
    }

    private void OnGameOver(IsOnStone winner) {
        var black = ctrl.SlotOf(IsOnStone.Black);
        var white = ctrl.SlotOf(IsOnStone.White);

        // 끝까지 둔 게임만 여기로 옴 (기권은 GameOver 없이 창을 닫음, 설계서 2.5)
        // 전적 저장은 결과 화면보다 먼저
        bool saveFailed = false;
        if (records != null) {
            try {
                records.Save(black, white, ctrl.Data);
            }
            catch (Exception ex) {
                saveFailed = true;
                logBox.Items.Add($"전적 저장 실패: {ex.Message}");
            }
        }

        BeginInvoke(new Action(() => {
            if (saveFailed)
                MessageBox.Show("전적을 저장하지 못했습니다.", "전적 저장", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            using var result = new ResultForm(ctrl.Data, black, white);
            ReplayRequested = result.ShowDialog(this) == DialogResult.Retry;
            Close();   // 다시 하기는 MainForm이 같은 설정으로 새 게임을 엶
        }));
    }

    private void OnResign(object? sender, EventArgs e) {
        if (ConfirmResign()) {
            resignConfirmed = true;
            Close();
        }
    }

    // 게임 중 창 닫기도 기권과 같은 확인 (설계서 2.5)
    protected override void OnFormClosing(FormClosingEventArgs e) {
        bool byUser = e.CloseReason is CloseReason.UserClosing or CloseReason.None;   // X·Alt+F4·외부 WM_CLOSE
        if (byUser && !ctrl.Data.IsOver && !resignConfirmed && !ConfirmResign())
            e.Cancel = true;
        base.OnFormClosing(e);
    }

    private bool ConfirmResign()
        => MessageBox.Show("기권하면 이번 게임은 전적에 기록되지 않습니다. 기권할까요?",
                           "기권", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
}
