
using System;
using Othello.Core.Cards;

namespace Othello.Core.Model {
    // 카드·초능력 데이터 (Data.cs의 partial 확장)
    // - 새 칸 상태: CardCellFields에 필드 + Cell에 속성 추가
    // - 새 참조형 필드: CloneExtendedData()에서 따로 복제

    [Flags]
    public enum CellState : byte {
        None = 0,
        Fixed = 1 << 0,  // 고정: 뒤집거나 제거할 수 없음
        Sealed = 1 << 1  // 봉인: 빈칸에 착수할 수 없음
    }

    [Flags]
    public enum GameState {
        None = 0
        // 보드 전체 효과가 생기면 여기에 추가
    }

    // 플레이어별 일시 상태
    public struct PlayerStatus {
        public bool Silenced;
        public bool Shielded;
    }

    // 칸의 초능력 상태 묶음
    internal struct CardCellFields {
        public CellState CellFlags;
        public byte SealTurns;
    }

    // 보드의 초능력 상태 묶음
    internal struct CardBoardFields {
        public GameState GameFlags;
    }

    // 필드는 Data.cs에만 두고 여기선 속성만 추가 (partial struct 경고 CS0282 방지)
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
        // 선언과 동시에 생성 (null 경고 방지)
        public Hand[] Hands = { new Hand(), new Hand() };
        public CardDeck Deck = new CardDeck();
        public PlayerStatus[] Status = new PlayerStatus[2];

        // 이번 턴에만 유효한 점수 효과 (초기화는 ScoreLogic.ResetTurnModifiers)
        public int TurnScoreMultiplier;     // 점수 2배 = 2
        public int TurnPenaltyMultiplier;   // 마이너스의 손 = 2
        public bool TurnLuckySeven;         // 럭키 세븐

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
            // Hands, Deck, Status는 선언 시 이미 생성됨
            TurnScoreMultiplier = 1;
            TurnPenaltyMultiplier = 1;
            TurnLuckySeven = false;
            Board.GameFlags = GameState.None;
        }

        partial void CloneExtendedData() {
            // Hand 객체까지 따로 복제 (카드 객체는 공유)
            Hands = Array.ConvertAll(Hands, hand => hand.Clone());
            Deck = Deck.Clone();
            Status = (PlayerStatus[])Status.Clone();

            // 값 타입(배율, CardFields)은 MemberwiseClone에서 이미 복사됨
        }
    }
}
