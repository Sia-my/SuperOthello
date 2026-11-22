
using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Cpu {
    // 확정돌 간이 판정 (설계서 3.4.3)
    // - 확정돌 = 일반 착수로는 다시 뒤집히지 않는 돌
    // - 모서리 돌 + 그 모서리에서 변을 따라 같은 색으로 빈틈없이 이어진 돌 + 고정 돌
    // - 폭탄·전향 같은 카드 효과는 판정에 넣지 않음

    public static class StabilityLogic {
        // 모서리 4곳과, 각 모서리에서 변을 따라가는 두 방향
        private static readonly (int Row, int Col, int dr1, int dc1, int dr2, int dc2)[] Corners =
        {
            (0, 0, 0, 1, 1, 0),
            (0, Board.Size - 1, 0, -1, 1, 0),
            (Board.Size - 1, 0, 0, 1, -1, 0),
            (Board.Size - 1, Board.Size - 1, 0, -1, -1, 0)
        };

        // stone 색의 확정돌 개수
        public static int Count(Data data, IsOnStone stone) {
            bool[,] stable = Mark(data, stone);
            int count = 0;

            foreach (bool s in stable) {
                if (s) count++;
            }

            return count;
        }

        // 칸이 확정돌인지 (고정 카드 대상 고를 때 사용)
        public static bool IsStable(Data data, int row, int col) {
            IsOnStone stone = data.Board.Cells[row, col].Stone;
            return stone != IsOnStone.Empty && Mark(data, stone)[row, col];
        }

        // 확정돌 위치 표시 (같은 칸을 두 번 세지 않도록 배열로 표시)
        private static bool[,] Mark(Data data, IsOnStone stone) {
            Cell[,] cells = data.Board.Cells;
            var stable = new bool[Board.Size, Board.Size];

            // 고정 돌
            for (int row = 0; row < Board.Size; row++) {
                for (int col = 0; col < Board.Size; col++) {
                    if (cells[row, col].Stone == stone
                        && CellStateLogic.Has(cells[row, col], CellState.Fixed))
                        stable[row, col] = true;
                }
            }

            // 모서리 + 변을 따라 이어진 같은 색 돌
            foreach (var (row, col, dr1, dc1, dr2, dc2) in Corners) {
                if (cells[row, col].Stone != stone)
                    continue;

                stable[row, col] = true;
                MarkLine(cells, stable, stone, row, col, dr1, dc1);
                MarkLine(cells, stable, stone, row, col, dr2, dc2);
            }

            return stable;
        }

        private static void MarkLine(
            Cell[,] cells, bool[,] stable, IsOnStone stone,
            int row, int col, int dr, int dc) {
            int r = row + dr, c = col + dc;

            while (BoardLogic.InBounds(r, c) && cells[r, c].Stone == stone) {
                stable[r, c] = true;
                r += dr;
                c += dc;
            }
        }
    }
}
