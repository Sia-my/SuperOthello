
using System;
using Othello.Core.Cards;

namespace Othello.Core.Model {
    /*
     * CardData.cs - Data.cs의 카드/초능력 확장 데이터
     *
     * [사용 안내]
     * 칸 상태 확인       : data.Board.Cells[row, col].CellFlags
     * 봉인 남은 턴      : data.Board.Cells[row, col].SealTurns
     * 보드 전체 상태    : data.Board.GameFlags
     * 흑/백 손패        : data.Hands[0] / data.Hands[1]
     * 손패 편의 접근    : data.HandOf(IsOnStone.Black)
     * 공용 카드 덱      : data.Deck
     * 플레이어별 상태   : data.Status[0] / data.Status[1]
     * 상태 편의 접근    : data.StatusOf(IsOnStone.White)
     * 이번 턴 점수 배율 : data.TurnScoreMultiplier
     *
     * [확장 규칙]
     * - Data/Board/Cell은 Data.cs와 동일한 partial 자료형이다.
     * - Cell/Board의 실제 필드는 Data.cs에 모아 두었다.
     * - 새 칸/보드 상태는 아래 CardCellFields/CardBoardFields에 추가하고
     *   partial struct에 읽기/쓰기 속성을 추가한다.
     * - 새 참조형 Data 필드는 선언 시 초기화하고,
     *   CloneExtendedData()에서 독립적으로 복제한다.
     * - 실제 카드 효과 및 상태 규칙은 Logic에서 처리한다.
     */

    [Flags]
    public enum CellState : byte {
        None = 0,
        Fixed = 1 << 0,  // 고정: 뒤집거나 제거할 수 없음
        Sealed = 1 << 1  // 봉인: 빈칸에 착수할 수 없음
    }

    [Flags]
    public enum GameState {
        None = 0
        // 게임/보드 전체 효과가 추가되면 이곳에 선언
    }

    // 플레이어 한 명의 카드 상태
    public struct PlayerStatus {
        public bool Silenced;
        public bool Shielded;
    }

    // Cell 내부 확장 필드들을 하나의 값 타입으로 관리
    internal struct CardCellFields {
        public CellState CellFlags;
        public byte SealTurns;
    }

    // Board 내부 확장 필드들을 하나의 값 타입으로 관리
    internal struct CardBoardFields {
        public GameState GameFlags;
    }

    // 필드를 추가하지 않고 접근 속성만 확장한다. (CS0282 방지)
    public partial struct Cell {
        public CellState CellFlags {
            get => CardFields.CellFlags;
            set => CardFields.CellFlags = value;
        }

        public byte SealTurns {
            get => CardFields.SealTurns;
            set => CardFields.SealTurns = value;
        }
    }

    public partial struct Board {
        public GameState GameFlags {
            get => CardFields.GameFlags;
            set => CardFields.GameFlags = value;
        }
    }

    public partial class Data {
        // 선언 시 초기화하여 nullable 경고(CS8618)를 방지한다.
        public Hand[] Hands = { new Hand(), new Hand() };
        public CardDeck Deck = new CardDeck();
        public PlayerStatus[] Status = new PlayerStatus[2];

        public int TurnScoreMultiplier;

        public Hand HandOf(IsOnStone stone)
            => Hands[PlayerIndex(stone)];

        public ref PlayerStatus StatusOf(IsOnStone stone)
            => ref Status[PlayerIndex(stone)];

        private static int PlayerIndex(IsOnStone stone)
            => stone switch {
                IsOnStone.Black => 0,
                IsOnStone.White => 1,
                _ => throw new ArgumentException(
                    "Empty는 플레이어가 아닙니다.", nameof(stone))
            };

        partial void InitializeExtendedData() {
            // Hands, Deck, Status는 필드 선언 시 이미 생성된다.
            TurnScoreMultiplier = 1;
            Board.GameFlags = GameState.None;
        }

        partial void CloneExtendedData() {
            // 배열뿐 아니라 내부 Hand 객체까지 독립적으로 복제한다.
            Hands = Array.ConvertAll(Hands, hand => hand.Clone());
            Deck = Deck.Clone();
            Status = (PlayerStatus[])Status.Clone();

            // TurnScoreMultiplier, CardFields 등 값 타입은
            // Data.MemberwiseClone() / Board.Clone()에서 이미 복사된다.
        }
    }
}
