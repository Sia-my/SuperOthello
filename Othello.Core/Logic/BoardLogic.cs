
using System;
using System.Collections.Generic;
using Othello.Core.Model;
using Othello.Core.Logic;

namespace Othello.Core.Logic {
    // 오셀로 기본 규칙: 초기 배치, 뒤집기 계산, 착수, 돌 개수
    // - 좌표는 (Row, Col) 튜플
    // - 착수·뒤집기 가능 여부는 CellStateLogic 판정을 따름 (봉인·고정 반영)

    public static class BoardLogic {
        // 오셀로의 8방향
        private static readonly (int dr, int dc)[] Directions =
        {
            (-1, -1), (-1, 0), (-1, 1),
            ( 0, -1),          ( 0, 1),
            ( 1, -1), ( 1, 0), ( 1, 1)
        };

        // 보드 범위 검사
        public static bool InBounds(int row, int col)
            => row >= 0 && row < Board.Size
            && col >= 0 && col < Board.Size;

        // 게임판 초기 배치
        public static void Reset(Data data) {
            data.Board.Cells = new Cell[Board.Size, Board.Size];

            data.Board.Cells[3, 3].Stone = IsOnStone.White;
            data.Board.Cells[4, 4].Stone = IsOnStone.White;
            data.Board.Cells[3, 4].Stone = IsOnStone.Black;
            data.Board.Cells[4, 3].Stone = IsOnStone.Black;
        }

        // 8방향 탐색으로 뒤집을 돌 목록 계산
        public static List<(int Row, int Col)> GetFlips(
            Data data, int row, int col, IsOnStone stone) {
            var result = new List<(int Row, int Col)>();

            if (stone == IsOnStone.Empty || !InBounds(row, col))
                return result;

            Cell[,] cells = data.Board.Cells;

            if (!CellStateLogic.CanPlace(cells[row, col]))
                return result;

            IsOnStone opponent = stone == IsOnStone.Black
                ? IsOnStone.White
                : IsOnStone.Black;

            var line = new List<(int Row, int Col)>();

            foreach (var (dr, dc) in Directions) {
                line.Clear();

                int nextRow = row + dr;
                int nextCol = col + dc;

                // 해당 방향으로 이어진 상대 돌 탐색
                while (InBounds(nextRow, nextCol)
                    && cells[nextRow, nextCol].Stone == opponent) {
                    if (CellStateLogic.CanFlip(cells[nextRow, nextCol]))
                        line.Add((nextRow, nextCol));

                    nextRow += dr;
                    nextCol += dc;
                }

                // 상대 돌 줄 끝에 내 돌이 있어야 성립
                if (InBounds(nextRow, nextCol)
                    && cells[nextRow, nextCol].Stone == stone) {
                    result.AddRange(line);
                }
            }

            return result;
        }

        // 착수 가능한 위치 목록
        public static List<(int Row, int Col)> GetValidMoves(
            Data data, IsOnStone stone) {
            var moves = new List<(int Row, int Col)>();

            for (int row = 0; row < Board.Size; row++) {
                for (int col = 0; col < Board.Size; col++) {
                    if (GetFlips(data, row, col, stone).Count > 0)
                        moves.Add((row, col));
                }
            }

            return moves;
        }

        // 착수 가능한 위치가 있는지 확인
        public static bool HasValidMove(Data data, IsOnStone stone)
            => GetValidMoves(data, stone).Count > 0;

        // 돌 배치 및 뒤집기
        public static List<(int Row, int Col)> Place(
            Data data, int row, int col, IsOnStone stone) {
            List<(int Row, int Col)> flips =
                GetFlips(data, row, col, stone);

            if (flips.Count == 0)
                throw new InvalidOperationException(
                    $"({row},{col})에는 {stone} 돌을 둘 수 없습니다.");

            Cell[,] cells = data.Board.Cells;

            cells[row, col].Stone = stone;

            foreach (var (flipRow, flipCol) in flips) {
                cells[flipRow, flipCol].Stone = stone;
            }

            return flips;
        }

        // 규칙을 무시하고 돌 직접 변경
        public static void PutStone(
            Data data, int row, int col, IsOnStone stone) {
            data.Board.Cells[row, col].Stone = stone;
        }

        // 특정 돌의 개수
        public static int Count(Data data, IsOnStone stone) {
            int count = 0;

            foreach (Cell cell in data.Board.Cells) {
                if (cell.Stone == stone)
                    count++;
            }

            return count;
        }

        // 빈칸 개수
        public static int EmptyCount(Data data)
            => Count(data, IsOnStone.Empty);

        // 보드가 가득 찼는지 확인
        public static bool IsFull(Data data)
            => EmptyCount(data) == 0;
    }
}
