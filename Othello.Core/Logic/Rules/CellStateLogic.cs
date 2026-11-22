
using Othello.Core.Model;

namespace Othello.Core.Logic {
    // 칸 상태 판정: 착수·뒤집기·제거 가능 여부
    // - 기본 판정은 돌 유무만 확인
    // - 초능력 규칙은 CardCellStateLogic.cs에서 추가 (같은 partial 클래스)

    public static partial class CellStateLogic {
        // 플래그 보유 여부 (None은 항상 true)
        public static bool Has(in Cell cell, CellState flag)
            => (cell.CellFlags & flag) == flag;

        public static bool Has(Data data, int row, int col, CellState flag)
            => Has(data.Board.Cells[row, col], flag);

        // 기본 판정(돌 유무) → 초능력 규칙으로 덮어쓰기
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

        // 초능력 확장 지점 → 구현은 CardCellStateLogic.cs (구현이 없으면 호출 자체가 빠짐)
        static partial void ApplyCardPlaceRules(in Cell cell, ref bool allowed);
        static partial void ApplyCardFlipRules(in Cell cell, ref bool allowed);
        static partial void ApplyCardRemoveRules(in Cell cell, ref bool allowed);
        static partial void AfterCardStateRemoved(Data data, int row, int col, CellState removed);
        static partial void AfterCardStatesCleared(Data data, int row, int col);
    }
}
