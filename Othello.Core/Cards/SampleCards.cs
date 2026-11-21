
using System.Collections.Generic;
using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Cards {
    /*
     * SampleCards.cs - 기존 초능력 카드 5종 및 기본 카드 풀
     *
     * [사용 안내]
     * 기본 카드 풀 생성   : CardPool.Current()
     * 사용 가능 확인     : card.CanUse(data, user)
     * 대상 칸 목록       : card.GetTargets(data, user)
     * 카드 효과 적용     : card.Apply(data, user, target)
     *
     * [카드 목록]
     * ConvertCard      : 상대 돌 전향 (모서리 제외), 사용자 -2점
     * DoubleScoreCard  : 이번 착수 점수 2배
     * FixCard          : 내 돌 고정
     * SealCard         : 빈칸 3턴 봉인
     * SilenceCard      : 상대의 다음 턴 카드 사용 금지
     *
     * CardData.cs의 확장 상태값과 Logic 메서드를 사용한다.
     * 카드 사용 순서, 손패 제거, 턴 처리는 GameController에서 담당한다.
     */

    /// <summary>4. 전향 (A): 상대 돌 1개를 내 돌로 전향. 모서리 불가, 사용자 -2점.</summary>
    public sealed class ConvertCard : SkillCardBase {
        public override int Id => 4;
        public override string Name => "전향";
        public override string Description => "상대 돌 1개를 내 돌로 바꾼다 (모서리 불가, 사용자 -2점)";
        public override CardType Type => CardType.A;

        private static bool IsCorner(int row, int col)
            => (row == 0 || row == Board.Size - 1)
                && (col == 0 || col == Board.Size - 1);

        public override IReadOnlyList<(int Row, int Col)> GetTargets(
            Data data, IsOnStone user) {
            var targets = new List<(int Row, int Col)>();
            IsOnStone opponent = GameLogic.Opponent(user);

            for (int row = 0; row < Board.Size; row++) {
                for (int col = 0; col < Board.Size; col++) {
                    Cell cell = data.Board.Cells[row, col];

                    if (cell.Stone == opponent
                        && !IsCorner(row, col)
                        && CellStateLogic.CanFlip(cell)) {
                        targets.Add((row, col));
                    }
                }
            }

            return targets;
        }

        public override string Apply(
            Data data, IsOnStone user,
            (int Row, int Col)? target) {
            var (row, col) = RequireTarget(data, user, target);

            BoardLogic.PutStone(data, row, col, user);

            // 기존 ScoreRule.OnCard의 방패 적용 방식 보존.
            bool shielded =
                data.Status[ScoreLogic.PlayerIndex(user)].Shielded;

            ScoreLogic.OnCard(data, user, -2, shielded);

            return $"({row},{col})의 돌을 전향 (-2점)";
        }
    }

    /// <summary>6. 점수 2배 (B): 이번 착수의 획득 점수에만 2배 적용.</summary>
    public sealed class DoubleScoreCard : SkillCardBase {
        public override int Id => 6;
        public override string Name => "점수 2배";
        public override string Description => "이번 착수로 얻는 점수를 2배로 계산한다";
        public override CardType Type => CardType.B;
        public override bool NeedsTarget => false;

        public override string Apply(
            Data data, IsOnStone user,
            (int Row, int Col)? target) {
            data.TurnScoreMultiplier = 2;

            return "이번 착수 점수 2배";
        }
    }

    /// <summary>7. 고정 (A): 자기 돌을 게임 끝까지 뒤집거나 제거할 수 없게 한다.</summary>
    public sealed class FixCard : SkillCardBase {
        public override int Id => 7;
        public override string Name => "고정";
        public override string Description => "내 돌 1개를 게임 끝까지 뒤집히거나 제거되지 않게 한다";
        public override CardType Type => CardType.A;

        public override IReadOnlyList<(int Row, int Col)> GetTargets(
            Data data, IsOnStone user) {
            var targets = new List<(int Row, int Col)>();

            for (int row = 0; row < Board.Size; row++) {
                for (int col = 0; col < Board.Size; col++) {
                    Cell cell = data.Board.Cells[row, col];

                    if (cell.Stone == user
                        && !CellStateLogic.Has(cell, CellState.Fixed)) {
                        targets.Add((row, col));
                    }
                }
            }

            return targets;
        }

        public override string Apply(
            Data data, IsOnStone user,
            (int Row, int Col)? target) {
            var (row, col) = RequireTarget(data, user, target);

            CellStateLogic.Add(
                data, row, col, CellState.Fixed);

            return $"({row},{col})의 돌을 고정";
        }
    }

    /// <summary>9. 봉인 (A): 빈칸 하나를 3턴 동안 착수 불가 상태로 만든다.</summary>
    public sealed class SealCard : SkillCardBase {
        // 양쪽 턴 모두 계산:
        // 내 턴 끝 2 -> 상대 턴 끝 1 -> 내 턴 끝 0.
        public const byte SealDuration = 3;

        public override int Id => 9;
        public override string Name => "봉인";
        public override string Description => "빈칸 1개를 3턴 동안 아무도 둘 수 없게 막는다";
        public override CardType Type => CardType.A;

        public override IReadOnlyList<(int Row, int Col)> GetTargets(
            Data data, IsOnStone user) {
            var targets = new List<(int Row, int Col)>();

            for (int row = 0; row < Board.Size; row++) {
                for (int col = 0; col < Board.Size; col++) {
                    if (CellStateLogic.CanPlace(
                        data.Board.Cells[row, col])) {
                        targets.Add((row, col));
                    }
                }
            }

            return targets;
        }

        public override string Apply(
            Data data, IsOnStone user,
            (int Row, int Col)? target) {
            var (row, col) = RequireTarget(data, user, target);

            CellStateLogic.Add(
                data, row, col, CellState.Sealed);

            data.Board.Cells[row, col].SealTurns = SealDuration;

            return $"({row},{col}) 칸을 {SealDuration}턴 봉인";
        }
    }

    /// <summary>11. 침묵 (C): 상대가 다음 턴에 카드를 쓰지 못하게 한다.</summary>
    public sealed class SilenceCard : SkillCardBase {
        public override int Id => 11;
        public override string Name => "침묵";
        public override string Description => "상대가 다음 턴에 패를 쓸 수 없게 만든다";
        public override CardType Type => CardType.C;
        public override bool NeedsTarget => false;

        public override string Apply(
            Data data, IsOnStone user,
            (int Row, int Col)? target) {
            IsOnStone opponent = GameLogic.Opponent(user);

            data.Status[
                ScoreLogic.PlayerIndex(opponent)
            ].Silenced = true;

            return "상대를 침묵시킴";
        }
    }

    // 현재 사용 중인 임시 카드 풀
    public static class CardPool {
        /// <summary>기존 임시 덱 구성: 5종 2장씩 총 10장.</summary>
        public static IEnumerable<ISkillCard> Current()
            => new ISkillCard[]
            {
                new ConvertCard(),
                new DoubleScoreCard(),
                new FixCard(),
                new SealCard(),
                new SilenceCard(),

                new ConvertCard(),
                new DoubleScoreCard(),
                new FixCard(),
                new SealCard(),
                new SilenceCard()
            };
    }
}
