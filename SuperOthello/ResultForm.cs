using Othello.Core.Logic;
using Othello.Core.Model;

namespace SuperOthello;

// 결과 화면: 흑·백 뒤집기·카드 점수 / 남은 돌 보너스 / 최종 점수, 승자 (설계서 3.2, F3-3·F3-4)
// - 다시 하기 → DialogResult.Retry, 메인 메뉴·창 닫기 → DialogResult.OK
public class ResultForm : Form {
    public ResultForm(Data finished, PlayerSlot black, PlayerSlot white) {
        Text = "게임 결과";
        ClientSize = new Size(400, 290);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("맑은 고딕", 10f);

        IsOnStone winner = ScoreLogic.Winner(finished);
        string headline = winner switch {
            IsOnStone.Black => $"{black.Id} 승리!",
            IsOnStone.White => $"{white.Id} 승리!",
            _ => "무승부"
        };

        Controls.Add(new Label {
            Text = headline,
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 14),
            Size = new Size(400, 40)
        });

        var table = new TableLayoutPanel {
            Location = new Point(20, 66),
            Size = new Size(360, 150),
            ColumnCount = 3,
            RowCount = 4,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        for (int i = 0; i < 4; i++) table.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        var b = ScoreLogic.BreakdownOf(finished, IsOnStone.Black);
        var w = ScoreLogic.BreakdownOf(finished, IsOnStone.White);

        AddRow(table, 0, "", $"흑 {black.Id}", $"백 {white.Id}", bold: true);
        AddRow(table, 1, "뒤집기·카드 점수", b.Play.ToString(), w.Play.ToString());
        AddRow(table, 2, "남은 돌 보너스", $"+{b.Bonus}", $"+{w.Bonus}");
        AddRow(table, 3, "최종 점수", b.Final.ToString(), w.Final.ToString(), bold: true);
        Controls.Add(table);

        var retry = new Button { Text = "다시 하기", Location = new Point(100, 234), Size = new Size(96, 36), DialogResult = DialogResult.Retry };
        var menu = new Button { Text = "메인 메뉴", Location = new Point(204, 234), Size = new Size(96, 36), DialogResult = DialogResult.OK };
        Controls.AddRange(new Control[] { retry, menu });
        AcceptButton = menu;
        CancelButton = menu;   // Esc·창 닫기는 메인 메뉴로
    }

    private void AddRow(TableLayoutPanel table, int row, string head, string black, string white, bool bold = false) {
        var font = bold ? new Font(Font, FontStyle.Bold) : Font;
        string[] texts = { head, black, white };
        for (int col = 0; col < 3; col++) {
            table.Controls.Add(new Label {
                Text = texts[col],
                Font = font,
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                TextAlign = col == 0 ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter
            }, col, row);
        }
    }
}
