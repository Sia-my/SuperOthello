
namespace Othello.Core.Model {
    /*
     * Data.cs - 오셀로 기본 데이터
     *
     * [사용 안내]
     * 게임 데이터 생성     : new Data()
     * 현재 차례 확인      : data.Current
     * 특정 칸 돌 확인    : data.Board.Cells[row, col].Stone
     * 점수 확인          : data.Scores[0] / data.Scores[1]  // 흑 / 백
     * 턴 번호 확인       : data.TurnNumber
     * 연속 패스 확인     : data.ConsecutivePasses
     * 게임 종료 확인     : data.IsOver
     * 전체 데이터 복제   : data.Clone()
     *
     * [partial 구조]
     * Data.cs     : 기본 데이터와 복제 진입점
     * CardData.cs : 카드/초능력 데이터 및 확장 데이터 복제
     *
     * Cell과 Board의 인스턴스 필드는 이 파일에만 선언한다.
     * CardData.cs는 확장 저장 공간의 타입과 접근 속성을 정의한다.
     * 이 방식은 partial struct 필드 순서 경고(CS0282)를 방지한다.
     * 일반적인 착수와 턴 진행은 GameController를 통해 처리한다.
     */

    // 돌의 종류
    public enum IsOnStone : byte {
        Empty = 0,
        Black = 1,
        White = 2
    }

    // 게임판의 한 칸
    public partial struct Cell {
        public IsOnStone Stone;

        // CardData.cs에서 정의하는 값 타입 확장 저장 공간
        internal CardCellFields CardFields;
    }

    // 게임판 전체
    public partial struct Board {
        public const int Size = 8;

        public Cell[,] Cells;

        // CardData.cs에서 정의하는 값 타입 확장 저장 공간
        internal CardBoardFields CardFields;

        public Board Clone() {
            // 값 타입인 CardFields도 함께 복사된다.
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

            // 게임판과 점수 배열은 원본과 독립적으로 복제한다.
            copy.Board = Board.Clone();
            copy.Scores = (int[])Scores.Clone();

            // 카드/초능력 참조형 데이터는 CardData.cs에서 복제한다.
            copy.CloneExtendedData();
            return copy;
        }

        // CardData.cs에서 구현하는 확장 메서드
        partial void InitializeExtendedData();
        partial void CloneExtendedData();
    }
}
