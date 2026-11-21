
using Othello.Core.Model;

namespace Othello.Core.Cpu {
    /*
     * WeightTables.cs - CPU용 위치 가중치 및 게임 단계 판정
     *
     * [사용 안내]
     * 특정 좌표의 가중치 : WeightTables.WeightAt(data.Board, row, col)
     * 게임 진행 단계     : WeightTables.PhaseOf(data.Board)
     * 기본 가중치표 확인 : WeightTables.Basic[row, col]
     *
     * PhaseOf 반환값:
     *   Opening : 빈칸 40개 이상
     *   Middle  : 빈칸 15~39개
     *   End     : 빈칸 14개 이하
     *
     * WeightAt은 현재 게임 단계와 무관하게 Basic 값을 반환한다.
     * 향후 단계별 가중치 조정은 별도로 구현할 예정이다.
     * 실제 착수 선택은 CPUPlayer가 담당한다.
     */

    public static class WeightTables {
        // 기존 위치 가중치표 그대로 유지
        public static readonly int[,] Basic =
        {
            { 100, -20,  10,   5,   5,  10, -20, 100 },
            { -20, -50,  -2,  -2,  -2,  -2, -50, -20 },
            {  10,  -2,   1,   1,   1,   1,  -2,  10 },
            {   5,  -2,   1,   0,   0,   1,  -2,   5 },
            {   5,  -2,   1,   0,   0,   1,  -2,   5 },
            {  10,  -2,   1,   1,   1,   1,  -2,  10 },
            { -20, -50,  -2,  -2,  -2,  -2, -50, -20 },
            { 100, -20,  10,   5,   5,  10, -20, 100 }
        };

        public enum Phase { Opening, Middle, End }

        // 기존 Board.EmptyCount 속성 대신 새 Board.Cells를 직접 순회한다.
        public static Phase PhaseOf(Board board) {
            int emptyCount = 0;

            foreach (Cell cell in board.Cells) {
                if (cell.Stone == IsOnStone.Empty)
                    emptyCount++;
            }

            return emptyCount switch {
                >= 40 => Phase.Opening,
                >= 15 => Phase.Middle,
                _ => Phase.End
            };
        }

        /// <summary>
        /// 현재는 기본 가중치표만 사용한다.
        /// TODO(3주차): 중반 규칙(점유된 모서리 주변 C·X칸 0),
        /// 종반 평탄표(모서리 30, C·X 0, 나머지 5)
        /// </summary>
        public static int WeightAt(Board board, int row, int col)
            => Basic[row, col];
    }
}
