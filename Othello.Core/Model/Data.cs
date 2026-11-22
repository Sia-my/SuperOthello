
namespace Othello.Core.Model {
    // 게임 기본 데이터: 보드, 점수, 차례, 턴 정보
    // - 데이터만 보관, 계산은 Logic 클래스에서
    // - new Data()는 빈 보드 → 초기 배치는 GameLogic.Reset()

    // 돌의 종류
    public enum IsOnStone : byte {
        Empty = 0,
        Black = 1,
        White = 2
    }

    // 게임판의 한 칸
    public partial struct Cell {
        public IsOnStone Stone;

        // 초능력 상태 저장 공간 (구조는 CardData.cs)
        internal CardCellFields CardFields;
    }

    // 게임판 전체
    public partial struct Board {
        public const int Size = 8;

        public Cell[,] Cells;

        // 초능력 상태 저장 공간 (구조는 CardData.cs)
        internal CardBoardFields CardFields;

        public Board Clone() {
            // 배열만 새로 복사 (CardFields는 값 타입이라 자동 복사)
            Board copy = this;
            copy.Cells = Cells == null ? null! : (Cell[,])Cells.Clone();
            return copy;
        }
    }

    // 게임 전체 데이터
    public partial class Data {
        public Board Board;
        public int[] Scores;          // [0] 흑, [1] 백
        public IsOnStone Current;     // 현재 차례
        public int TurnNumber;        // 첫 턴은 1
        public int ConsecutivePasses; // 연속 패스 횟수
        public bool IsOver;           // 게임 종료 여부

        public Data() {
            Board = new Board {
                Cells = new Cell[global::Othello.Core.Model.Board.Size,
                                 global::Othello.Core.Model.Board.Size]
            };

            Scores = new int[2];
            Current = IsOnStone.Black;
            TurnNumber = 1;
            ConsecutivePasses = 0;
            IsOver = false;

            InitializeExtendedData();
        }

        public Data Clone() {
            Data copy = (Data)MemberwiseClone();

            // 보드·점수는 원본과 분리해서 복제
            copy.Board = Board.Clone();
            copy.Scores = (int[])Scores.Clone();

            // 손패·덱·상태는 CardData.cs에서 복제
            copy.CloneExtendedData();
            return copy;
        }

        // 구현은 CardData.cs
        partial void InitializeExtendedData();
        partial void CloneExtendedData();
    }
}
