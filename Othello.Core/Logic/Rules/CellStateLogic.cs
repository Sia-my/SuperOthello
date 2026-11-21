
using Othello.Core.Model;

namespace Othello.Core.Logic {
    /*
     * CellStateLogic.cs - 칸 상태의 공통 관리 및 기본 판정
     *
     * [사용 안내]
     * 특정 상태 확인      : CellStateLogic.Has(data, row, col, flag)
     * 상태 추가           : CellStateLogic.Add(data, row, col, flag)
     * 상태 제거           : CellStateLogic.Remove(data, row, col, flag)
     * 모든 칸 상태 제거   : CellStateLogic.Clear(data, row, col)
     * 돌 놓기 가능 여부   : CellStateLogic.CanPlace(cell)
     * 돌 뒤집기 가능 여부 : CellStateLogic.CanFlip(cell)
     * 돌 제거 가능 여부   : CellStateLogic.CanRemove(cell)
     *
     * [파일 분리]
     * CellStateLogic.cs     : 기본 칸 상태 관리와 돌 유무 판정
     * CardCellStateLogic.cs : 고정/봉인 효과 및 봉인 지속시간 처리
     *
     * 두 파일은 같은 partial class로 컴파일된다.
     * BoardLogic은 지금처럼 CanPlace / CanFlip만 호출하면 된다.
     * 새 카드가 기본 판정에 영향을 준다면 확장 파일의
     * ApplyCard*Rules 구현을 수정한다.
     *
     * 일반 착수는 GameController를 통해 처리한다.
     */
    public static partial class CellStateLogic {
        // 지정한 플래그가 모두 설정되어 있는지 확인한다.
        // flag == CellState.None인 경우 항상 true이므로 별도로 비교한다.
        public static bool Has(in Cell cell, CellState flag)
            => (cell.CellFlags & flag) == flag;

        public static bool Has(Data data, int row, int col, CellState flag)
            => Has(data.Board.Cells[row, col], flag);

        // 기본 판정은 돌의 유무이며, 카드 효과는 partial 메서드로 확장한다.
        public static bool CanPlace(in Cell cell) {
            bool allowed = cell.Stone == IsOnStone.Empty;
            ApplyCardPlaceRules(in cell, ref allowed);
            return allowed;
        }

        public static bool CanFlip(in Cell cell) {
            bool allowed = cell.Stone != IsOnStone.Empty;
            ApplyCardFlipRules(in cell, ref allowed);
            return allowed;
        }

        public static bool CanRemove(in Cell cell) {
            bool allowed = cell.Stone != IsOnStone.Empty;
            ApplyCardRemoveRules(in cell, ref allowed);
            return allowed;
        }

        public static void Add(Data data, int row, int col, CellState flag)
            => data.Board.Cells[row, col].CellFlags |= flag;

        public static void Remove(Data data, int row, int col, CellState flag) {
            data.Board.Cells[row, col].CellFlags &= ~flag;
            AfterCardStateRemoved(data, row, col, flag);
        }

        public static void Clear(Data data, int row, int col) {
            data.Board.Cells[row, col].CellFlags = CellState.None;
            AfterCardStatesCleared(data, row, col);
        }

        // CardCellStateLogic.cs가 구현하는 확장 지점.
        // 구현이 없는 동안은 해당 호출이 컴파일 시 제거된다.
        static partial void ApplyCardPlaceRules(in Cell cell, ref bool allowed);
        static partial void ApplyCardFlipRules(in Cell cell, ref bool allowed);
        static partial void ApplyCardRemoveRules(in Cell cell, ref bool allowed);
        static partial void AfterCardStateRemoved(Data data, int row, int col, CellState removed);
        static partial void AfterCardStatesCleared(Data data, int row, int col);
    }
}
