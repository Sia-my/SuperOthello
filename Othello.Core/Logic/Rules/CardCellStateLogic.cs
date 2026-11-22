
using Othello.Core.Model;

namespace Othello.Core.Logic {
    // CellStateLogic의 초능력 부분 (고정·봉인)
    // - 새 칸 상태가 생기면 이 파일의 Apply*Rules에 조건 추가

    public static partial class CellStateLogic {
        // 봉인 칸은 착수 불가
        static partial void ApplyCardPlaceRules(
            in Cell cell, ref bool allowed) {
            if (Has(in cell, CellState.Sealed))
                allowed = false;
        }

        // 고정 돌은 뒤집기 불가
        static partial void ApplyCardFlipRules(
            in Cell cell, ref bool allowed) {
            if (Has(in cell, CellState.Fixed))
                allowed = false;
        }

        // 고정 돌은 제거 불가
        static partial void ApplyCardRemoveRules(
            in Cell cell, ref bool allowed) {
            if (Has(in cell, CellState.Fixed))
                allowed = false;
        }

        // 봉인 해제 시 남은 턴도 0으로
        static partial void AfterCardStateRemoved(
            Data data, int row, int col, CellState removed) {
            if ((removed & CellState.Sealed) != CellState.None) {
                data.Board.Cells[row, col].SealTurns = 0;
            }
        }

        // 상태 전체 초기화 시 남은 턴도 0으로
        static partial void AfterCardStatesCleared(
            Data data, int row, int col) {
            data.Board.Cells[row, col].SealTurns = 0;
        }

        // 턴 종료 처리 (Data로 바로 호출)
        public static void ProcessTurnEnd(Data data) {
            ProcessTurnEnd(data.Board.Cells);
        }

        // 봉인 남은 턴 1 감소, 0이 되면 해제
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
