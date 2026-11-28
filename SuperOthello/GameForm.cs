using Othello.Core.Cards;
using Othello.Core.Control;
using Othello.Core.Cpu;
using Othello.Core.Model;
using Othello.Core.Logic;
using Othello.Core.Records;

namespace SuperOthello;

// 게임 화면 (설계서 3.2: 상단 턴 표시 · 좌우 플레이어 패널 · 중앙 보드 · 하단 행동 로그, 코드로 배치)
// - 조작: 손패 클릭(선택) → 대상 칸 클릭(필요 시) → 착수 칸 클릭. 마우스를 올리면 뒤집힐 돌 미리보기 (F6-4)
// - 상태는 GameController 이벤트로만 받음. CPU 계산은 백그라운드 + 0.7초 연출 지연
public class GameForm : Form {
    private const int Pad = 14;
    private const int BoardSide = 460;
    private const int PanelWidth = 260;
    private const int BarHeight = 60;
    private const int LogHeight = 106;

    private readonly GameController ctrl;
    private readonly RecordRepository? records;   // null이면 전적 기능 비활성
    private readonly CPUPlayer?[] cpus = new CPUPlayer?[2];

    private readonly TurnBar turnBar = new();
    private readonly BoardControl boardView = new();
    private readonly PlayerPanel[] panels = { new(IsOnStone.Black), new(IsOnStone.White) };
    private readonly LogView logView = new();
    private readonly Button resignButton = Theme.MakeButton("기권", new Size(84, 36));

    // 사람의 입력 진행 상태
    private ISkillCard? selectedCard;
    private (int Row, int Col)? selectedTarget;
    private bool cpuThinking;
    private bool resignConfirmed;   // 기권 버튼으로 이미 확인함

    // 미리보기용: 선택한 카드를 적용한 복제본과 그 상태의 착수 가능 칸
    private Data? previewBase;
    private List<(int Row, int Col)> previewMoves = new();
    private IReadOnlyList<(int Row, int Col)> currentTargets = Array.Empty<(int Row, int Col)>();

    // 결과 화면에서 '다시 하기'를 골랐는지 (창이 닫힌 뒤 MainForm이 확인)
    public bool ReplayRequested { get; private set; }

    public GameForm(GameController controller, RecordRepository? recordRepository = null) {
        ctrl = controller;
        records = recordRepository;

        Text = "초능력 오셀로";
        Theme.ApplyForm(this);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ClientSize = new Size(Pad * 4 + PanelWidth * 2 + BoardSide, Pad + BarHeight + Pad + BoardSide + Pad + LogHeight + Pad);

        LayoutControls();

        foreach (IsOnStone s in new[] { IsOnStone.Black, IsOnStone.White }) {
            var slot = ctrl.SlotOf(s);
            if (slot.IsCpu) cpus[ScoreLogic.PlayerIndex(s)] = new CPUPlayer(slot.Difficulty);
        }

        boardView.CellClicked += OnCellClicked;
        boardView.CellHovered += OnCellHovered;
        foreach (var panel in panels) panel.CardClicked += OnCardClicked;
        resignButton.Click += OnResign;

        ctrl.LogAdded += AddLog;
        ctrl.StateChanged += RefreshView;
        ctrl.GameOver += OnGameOver;
        ctrl.ChooseDiscard = ChooseDiscardDialog;

        Shown += (_, _) => ctrl.Start();
    }

    private void LayoutControls() {
        int top = Pad + BarHeight + Pad;
        turnBar.Bounds = new Rectangle(Pad, Pad, ClientSize.Width - Pad * 2, BarHeight);
        resignButton.Location = new Point(turnBar.Right - resignButton.Width - 12, Pad + (BarHeight - resignButton.Height) / 2);
        turnBar.ChipsRightMargin = resignButton.Width + 24;

        panels[0].Bounds = new Rectangle(Pad, top, PanelWidth, BoardSide);
        boardView.Bounds = new Rectangle(Pad * 2 + PanelWidth, top, BoardSide, BoardSide);
        panels[1].Bounds = new Rectangle(boardView.Right + Pad, top, PanelWidth, BoardSide);
        logView.Bounds = new Rectangle(Pad, top + BoardSide + Pad, ClientSize.Width - Pad * 2, LogHeight);

        Controls.AddRange(new Control[] { turnBar, panels[0], boardView, panels[1], logView, resignButton });
        resignButton.BringToFront();
    }

    // ───────────────────────── 화면 갱신 ─────────────────────────

    private bool MyInput => !ctrl.Data.IsOver && !cpuThinking && !ctrl.CurrentSlot.IsCpu;

    private void RefreshView() {
        var s = ctrl.Data;
        IsOnStone me = s.Current;
        bool myInput = MyInput;

        // 플레이어 패널 (양측 손패 공개, F2-6)
        foreach (IsOnStone owner in new[] { IsOnStone.Black, IsOnStone.White }) {
            int i = ScoreLogic.PlayerIndex(owner);
            bool turn = !s.IsOver && owner == me;
            panels[i].Show(ctrl.SlotOf(owner), s.Scores[i], turn, turn && cpuThinking, s.StatusOf(owner),
                s.HandOf(owner).Cards, owner == me ? selectedCard : null,
                canClick: myInput && owner == me, canUse: ctrl.CanUseCard);
        }

        // 보드: 대상 지정 단계면 대상 칸, 아니면 (카드 적용 후) 착수 가능 칸
        IReadOnlyCollection<(int Row, int Col)> hints = Array.Empty<(int Row, int Col)>();
        currentTargets = Array.Empty<(int Row, int Col)>();
        previewBase = null;
        previewMoves = new();
        Board shown = s.Board;

        if (myInput) {
            if (selectedCard is { NeedsTarget: true } && selectedTarget is null) {
                currentTargets = selectedCard.GetTargets(s, me).ToList();
            }
            else {
                previewBase = AfterSelectedCard();
                previewMoves = BoardLogic.GetValidMoves(previewBase, me);
                hints = previewMoves;
                if (selectedCard != null) shown = previewBase.Board;   // 카드 효과를 미리 보여 줌
            }
        }

        boardView.Render(shown, hints, currentTargets, ctrl.LastMove);
        UpdateTurnBar();
        resignButton.Enabled = !s.IsOver;

        if (!s.IsOver && ctrl.CurrentSlot.IsCpu && !cpuThinking) _ = RunCpuTurnAsync();
    }

    private void UpdateTurnBar() {
        var s = ctrl.Data;
        IsOnStone me = s.Current;
        string title, guide;

        if (s.IsOver) {
            title = "게임 종료";
            guide = "결과를 확인하세요";
        }
        else {
            title = $"{Theme.StoneName(me)} 차례 · {ctrl.CurrentSlot.Id}";
            if (cpuThinking || ctrl.CurrentSlot.IsCpu)
                guide = "CPU가 다음 수를 고르는 중입니다…";
            else if (selectedCard is { NeedsTarget: true } && selectedTarget is null)
                guide = $"[{selectedCard.Name}] 효과를 줄 칸을 고르세요 · 카드를 다시 누르면 취소";
            else if (selectedCard != null)
                guide = $"[{selectedCard.Name}] 사용 후 둘 칸을 고르세요 · 카드를 다시 누르면 취소";
            else if (GameStateLogic.IsSilenced(s, me))
                guide = "침묵 상태라 이번 턴에는 카드를 쓸 수 없습니다 · 둘 칸을 고르세요";
            else
                guide = "점으로 표시된 칸에 두세요 · 착수 전에 손패 카드를 쓸 수 있습니다";
        }

        turnBar.Show(me, title, guide, s.IsOver,
            ("턴", s.TurnNumber.ToString()),
            ("덱", $"{s.Deck.Count}장"),
            ("빈칸", BoardLogic.EmptyCount(s).ToString()));
    }

    // 선택한 패를 적용했다고 가정한 복제본 (카드가 없으면 그냥 복제본)
    private Data AfterSelectedCard() {
        var copy = ctrl.Data.Clone();
        if (selectedCard != null)
            selectedCard.Apply(copy, copy.Current, selectedTarget);
        return copy;
    }

    private List<(int Row, int Col)> MovesAfterSelectedCard() {
        var copy = AfterSelectedCard();
        return BoardLogic.GetValidMoves(copy, copy.Current);
    }

    // 마우스를 올린 칸 미리보기 (F6-4): 착수면 뒤집힐 돌, 대상 지정이면 효과 범위
    private void OnCellHovered((int Row, int Col)? cell) {
        if (!MyInput || cell is not (int r, int c)) { boardView.ClearPreview(); return; }
        IsOnStone me = ctrl.Data.Current;

        if (currentTargets.Count > 0) {
            if (!currentTargets.Contains((r, c))) { boardView.ClearPreview(); return; }
            IReadOnlyCollection<(int Row, int Col)> area = selectedCard is BombCard
                ? BombCard.Area(r, c).ToList()
                : new[] { (r, c) };
            boardView.SetPreview(null, me, Array.Empty<(int Row, int Col)>(), area);
            return;
        }

        if (previewBase != null && previewMoves.Contains((r, c))) {
            var flips = BoardLogic.GetFlips(previewBase, r, c, me);
            boardView.SetPreview((r, c), me, flips, Array.Empty<(int Row, int Col)>());
        }
        else {
            boardView.ClearPreview();
        }
    }

    // ───────────────────────── 행동 로그 ─────────────────────────

    private void AddLog(string msg) => logView.Add(msg);

    // ───────────────────────── 사람 입력 ─────────────────────────

    private void OnCardClicked(ISkillCard card) {
        if (!MyInput) return;
        if (ReferenceEquals(selectedCard, card)) { ClearSelection(); }   // 다시 누르면 취소
        else { selectedCard = card; selectedTarget = null; }

        // 대상이 필요 없는 패인데 적용 후 둘 곳이 없으면 바로 '패 사용 + 패스'
        if (selectedCard is { NeedsTarget: false } && MovesAfterSelectedCard().Count == 0)
            Submit(GameAction.WithCard(selectedCard, null, null));
        else
            RefreshView();
    }

    private void OnCellClicked((int Row, int Col) p) {
        if (!MyInput) return;

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
            AddLog($"CPU 오류: {err} → 일반 착수로 대체");
            if (!ctrl.TryExecuteTurn(cpu.FallbackAction(ctrl.Data), out _))
                RefreshView();
        }
    }

    // ───────────────────────── 대화상자 ─────────────────────────

    // 손패 4장 중 버릴 1장 선택 (F2-3)
    private ISkillCard ChooseDiscardDialog(IReadOnlyList<ISkillCard> cards) {
        const int tileW = 200, tileH = 96, gap = 10;
        using var dlg = new Form {
            Text = "버릴 패 선택",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ControlBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(24 + cards.Count * tileW + (cards.Count - 1) * gap, 160)
        };
        Theme.ApplyForm(dlg);
        dlg.Controls.Add(new Label {
            Text = "손패가 가득 찼습니다. 버릴 카드 1장을 고르세요 (버린 카드는 덱으로 돌아갑니다)",
            ForeColor = Theme.Text, Font = Theme.F(10.5f, FontStyle.Bold),
            Location = new Point(12, 14), AutoSize = true
        });

        ISkillCard chosen = cards[cards.Count - 1];
        for (int i = 0; i < cards.Count; i++) {
            var tile = new CardTile { Bounds = new Rectangle(12 + i * (tileW + gap), 48, tileW, tileH), BackColor = Theme.Back };
            tile.Show(cards[i], selected: i == cards.Count - 1, clickable: true, blocked: false);
            tile.CardClicked += c => { chosen = c; dlg.DialogResult = DialogResult.OK; };
            dlg.Controls.Add(tile);
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
                AddLog($"전적 저장 실패: {ex.Message}");
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
