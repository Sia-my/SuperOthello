using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Cards {
    // 기본 카드 4종(전향·점수 2배·고정·침묵)과 덱 카드 풀 (번호 = 설계서 후보 24종 표의 번호)
    // - 나머지 8종은 BoardCards.cs(A)·ScoreCards.cs(B)·HandCards.cs(C)
    // - 칸 상태 변경은 CellStateLogic, 점수 변경은 ScoreLogic 사용

    /// <summary>4. 전향 (A): 상대 돌 1개를 내 돌로 전향. 모서리 불가, 사용자 -2점.</summary>
    public sealed class ConvertCard : SkillCardBase {
        public override int Id => 4;
        public override string Name => "전향";
        public override string Description => "첫 턴에는 사용 불가. 상대 돌 1개를 내 돌로 바꾼다 (모서리 불가, 사용자 -2점)";
        public override CardType Type => CardType.A;
        public override string ScoreEffect => "사용자 -2";

        public override bool CanUse(Data data, IsOnStone user)
            => data.TurnNumber > 1 && base.CanUse(data, user);

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

            // 방패 상태면 감점 절반
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
        public override string ScoreEffect => "획득 점수 ×2";
        public override bool NeedsTarget => false;

        public override string Apply(
            Data data, IsOnStone user,
            (int Row, int Col)? target) {
            data.TurnScoreMultiplier = 2;

            return "이번 착수 점수 2배";
        }
    }

    /// <summary>7. 고정 (A): 내 돌 1개를 게임 끝까지 뒤집기·제거 불가로</summary>
    public sealed class FixCard : SkillCardBase {
        public override int Id => 7;
        public override string Name => "고정";
        public override string Description => "내 돌 1개를 게임 끝까지 뒤집히거나 제거되지 않게 한다";
        public override CardType Type => CardType.A;
        public override string ScoreEffect => "없음";

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

    /// <summary>11. 침묵 (C): 상대는 다음 턴에 카드 사용 불가</summary>
    public sealed class SilenceCard : SkillCardBase {
        public override int Id => 11;
        public override string Name => "침묵";
        public override string Description => "상대가 다음 턴에 패를 쓸 수 없게 만든다";
        public override CardType Type => CardType.C;
        public override string ScoreEffect => "없음";
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

    // 공용 덱 카드 풀 (F2-1)
    public static class CardPool {
        /// <summary>최종 12종 × 1장 = 12장 (유형별 4종)</summary>
        public static IEnumerable<ISkillCard> Current()
            => new ISkillCard[]
            {
                // A 판 변경
                new ConvertCard(),
                new BombCard(),
                new FixCard(),
                new GiftCard(),

                // B 점수
                new DoubleScoreCard(),
                new ShieldCard(),
                new OmokCard(),
                new MinusHandCard(),

                // C 운영
                new PlunderCard(),
                new SilenceCard(),
                new SupplyCard(),
                new BlackKnightCard()
            };
    }
}
