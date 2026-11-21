
using Othello.Core.Model;

namespace Othello.Core.Logic {
    /*
     * CardCellStateLogic.cs - 개별 칸의 초능력 효과
     *
     * [사용 안내]
     *
     * 고정 상태 부여:
     *   CellStateLogic.Add(data, row, col, CellState.Fixed);
     *
     * 봉인 상태 부여:
     *   CellStateLogic.Add(data, row, col, CellState.Sealed);
     *
     * 봉인 지속시간 설정:
     *   data.Board.Cells[row, col].SealTurns = 3;
     *
     * 턴 종료 시 상태 처리:
     *   CellStateLogic.ProcessTurnEnd(data);
     *
     * [역할 분리]
     *
     * CellStateLogic.cs:
     *   기본적인 칸 상태 관리와 판정
     *
     * CardCellStateLogic.cs:
     *   고정, 봉인 등 초능력에 의한 판정 변경
     *
     * 두 파일은 하나의 partial class로 결합된다.
     */

    public static partial class CellStateLogic {
        // 봉인된 칸에는 돌을 놓을 수 없음
        static partial void ApplyCardPlaceRules(
            in Cell cell, ref bool allowed) {
            if (Has(in cell, CellState.Sealed))
                allowed = false;
        }

        // 고정된 돌은 뒤집을 수 없음
        static partial void ApplyCardFlipRules(
            in Cell cell, ref bool allowed) {
            if (Has(in cell, CellState.Fixed))
                allowed = false;
        }

        // 고정된 돌은 제거할 수 없음
        static partial void ApplyCardRemoveRules(
            in Cell cell, ref bool allowed) {
            if (Has(in cell, CellState.Fixed))
                allowed = false;
        }

        // 봉인 상태가 제거되면 지속시간도 초기화
        static partial void AfterCardStateRemoved(
            Data data, int row, int col, CellState removed) {
            if ((removed & CellState.Sealed) != CellState.None) {
                data.Board.Cells[row, col].SealTurns = 0;
            }
        }

        // 모든 상태가 초기화되면 봉인 시간도 초기화
        static partial void AfterCardStatesCleared(
            Data data, int row, int col) {
            data.Board.Cells[row, col].SealTurns = 0;
        }

        // Data를 전달하는 편의 메서드
        public static void ProcessTurnEnd(Data data) {
            ProcessTurnEnd(data.Board.Cells);
        }

        // 기존 StatusEffects의 봉인 지속시간 처리
        public static void ProcessTurnEnd(Cell[,] cells) {
            for (int row = 0; row < Board.Size; row++) {
                for (int col = 0; col < Board.Size; col++) {
                    ref Cell cell = ref cells[row, col];

                    if (!Has(in cell, CellState.Sealed))
                        continue;

                    if (cell.SealTurns > 0)
                        cell.SealTurns--;

                    if (cell.SealTurns == 0)
                        cell.CellFlags &= ~CellState.Sealed;
                }
            }
        }
    }
}
