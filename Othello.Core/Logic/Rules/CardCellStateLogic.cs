using Othello.Core.Model;

namespace Othello.Core.Logic {
    // CellStateLogic의 초능력 부분: 고정 돌 규칙
    public static partial class CellStateLogic {
        static partial void ApplyCardFlipRules(in Cell cell, ref bool allowed) {
            if (Has(in cell, CellState.Fixed)) allowed = false;
        }

        static partial void ApplyCardRemoveRules(in Cell cell, ref bool allowed) {
            if (Has(in cell, CellState.Fixed)) allowed = false;
        }

        // 기존 호출부와 호환. 봉인이 없어져 턴 종료 시 칸 상태 변화는 없다.
        public static void ProcessTurnEnd(Data data) { }
        public static void ProcessTurnEnd(Cell[,] cells) { }
    }
}
