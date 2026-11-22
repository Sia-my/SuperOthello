
using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Cpu {
    // CPU용 위치 가중치 (설계서 3.4.3)
    // - 단계는 턴 수가 아니라 빈칸 수로 구분 (폭탄 등으로 돌이 줄어들 수 있어서)
    // - 초반: 기본표 / 중반: 차지된 모서리 주변 C·X칸 감점 0 / 종반: 평탄표

    public static class WeightTables {
        // 기본표: 모서리 100, C칸(모서리 옆) -20, X칸(모서리 대각선) -50
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

        // 종반 평탄표 값: 위치보다 남은 돌 수(종료 보너스)가 중요해지는 시점
        public const int EndCorner = 30;
        public const int EndCX = 0;
        public const int EndOther = 5;

        public enum Phase { Opening, Middle, End }

        // 빈칸 40개 이상 초반, 15~39개 중반, 14개 이하 종반
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

        // 현재 단계에 맞는 칸 가중치
        public static int WeightAt(Board board, int row, int col)
            => WeightAt(board, row, col, PhaseOf(board));

        // 단계를 이미 구했을 때 쓰는 버전 (평가 중 반복 계산 방지)
        public static int WeightAt(Board board, int row, int col, Phase phase) {
            bool isCorner = IsCorner(row, col);
            bool isCX = TryGetNearCorner(row, col, out var corner);

            if (phase == Phase.End) {
                if (isCorner) return EndCorner;
                if (isCX) return EndCX;
                return EndOther;
            }

            // 중반: 모서리를 누군가 차지했으면 주변 C·X칸은 더 이상 위험하지 않음
            if (phase == Phase.Middle && isCX
                && board.Cells[corner.Row, corner.Col].Stone != IsOnStone.Empty)
                return 0;

            return Basic[row, col];
        }

        public static bool IsCorner(int row, int col)
            => (row == 0 || row == Board.Size - 1)
            && (col == 0 || col == Board.Size - 1);

        // C칸·X칸이면 붙어 있는 모서리 좌표를 돌려줌
        public static bool TryGetNearCorner(int row, int col, out (int Row, int Col) corner) {
            int last = Board.Size - 1;
            int cornerRow = row <= 1 ? 0 : row >= last - 1 ? last : -1;
            int cornerCol = col <= 1 ? 0 : col >= last - 1 ? last : -1;

            corner = (cornerRow, cornerCol);
            if (cornerRow < 0 || cornerCol < 0 || IsCorner(row, col))
                return false;

            // 모서리에서 한 칸 거리인 칸만 C·X칸
            return BoardLogic.InBounds(row, col)
                && System.Math.Abs(row - cornerRow) <= 1
                && System.Math.Abs(col - cornerCol) <= 1;
        }
    }
}
