
using System;
using System.Collections.Generic;
using Othello.Core.Cards;
using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Cpu {
    // CPU 수 읽기용 시뮬레이션
    // - 반드시 복제본(Data.Clone())에만 사용
    // - 카드 획득·턴 교체 같은 진행 처리는 하지 않고, 판 변화와 점수만 반영

    public static class CpuSimLogic {
        // 후보를 적용. 카드를 쓸 수 없거나 착수가 막히면 false
        public static bool TryApply(Data data, CpuCandidate cand, IsOnStone me) {
            if (cand.Card is ISkillCard card) {
                if (!card.CanUse(data, me))
                    return false;

                try {
                    card.Apply(data, me, cand.CardTarget);
                }
                catch (InvalidOperationException) {
                    return false;
                }

                data.HandOf(me).Remove(card);
            }

            // 카드 효과로 착수 칸이 막혔을 수 있음 (봉인, 전향 등)
            if (BoardLogic.GetFlips(data, cand.Move.Row, cand.Move.Col, me).Count == 0)
                return false;

            PlaceAndScore(data, cand.Move, me);
            ScoreLogic.ResetTurnModifiers(data);   // 점수 2배·럭키 세븐·마이너스의 손은 이번 착수에만
            return true;
        }

        // 착수 + 점수 반영. 뒤집은 개수 반환
        public static int PlaceAndScore(Data data, (int Row, int Col) move, IsOnStone mover) {
            List<(int Row, int Col)> flips = BoardLogic.Place(data, move.Row, move.Col, mover);
            bool opponentShielded = GameStateLogic.IsShielded(data, GameLogic.Opponent(mover));

            ScoreLogic.OnPlace(data, mover, flips.Count, opponentShielded);
            return flips.Count;
        }
    }
}
