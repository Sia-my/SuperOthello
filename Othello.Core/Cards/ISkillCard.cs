using System;
using System.Collections.Generic;
using System.Linq;
using Othello.Core.Model;

namespace Othello.Core.Cards {
    /*
     * ISkillCard.cs - 초능력 카드 공통 규약 및 기본 클래스
     *
     * [새 카드를 만들 때]
     *   SkillCardBase를 상속하고 Id, Name, Description, Type,
     *   Apply()를 구현한다.
     *   대상 칸이 필요한 카드는 GetTargets()를 재정의한다.
     *   대상 칸이 필요 없는 카드는 NeedsTarget => false로 설정한다.
     *
     * [카드를 사용할 수 있는지 확인]
     *   card.CanUse(data, player)
     *
     * [카드 대상 칸 목록 조회]
     *   card.GetTargets(data, player)
     *
     * [카드 효과 실행]
     *   card.Apply(data, player, target)
     *   // target은 (Row, Col) 튜플이며, 대상이 없으면 null
     *
     * [주의]
     *   - 이 파일은 카드의 공통 인터페이스만 정의한다.
     *   - 카드 사용 순서, 손패 소모, 턴 처리는 GameController가 맡는다.
     *   - 기존 GameState 클래스 -> Data 클래스,
     *     Stone -> IsOnStone, Position -> (int Row, int Col)로 변경했다.
     *   - 카드는 내부에 변하는 상태를 저장하지 않는 것을 전제로 한다.
     */

    // A: 판 변경형, B: 점수형, C: 운영형
    public enum CardType {
        A,
        B,
        C
    }

    /// <summary>모든 초능력 카드가 구현하는 공통 인터페이스.</summary>
    public interface ISkillCard {
        int Id { get; }
        string Name { get; }
        string Description { get; }
        CardType Type { get; }
        bool NeedsTarget { get; }

        /// <summary>현재 플레이어가 카드를 사용할 수 있는지 확인한다.</summary>
        bool CanUse(Data data, IsOnStone user);

        /// <summary>선택 가능한 대상 칸. 대상이 없는 카드는 빈 목록을 반환한다.</summary>
        IReadOnlyList<(int Row, int Col)> GetTargets(Data data, IsOnStone user);

        /// <summary>카드 효과를 적용하고 행동 로그용 문장을 반환한다.</summary>
        string Apply(Data data, IsOnStone user, (int Row, int Col)? target);
    }

    /// <summary>카드 구현 시 공통 동작을 제공하는 기본 클래스.</summary>
    public abstract class SkillCardBase : ISkillCard {
        public abstract int Id { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract CardType Type { get; }
        public virtual bool NeedsTarget => true;

        public virtual bool CanUse(Data data, IsOnStone user)
            => !NeedsTarget || GetTargets(data, user).Count > 0;

        public virtual IReadOnlyList<(int Row, int Col)> GetTargets(
            Data data, IsOnStone user)
            => Array.Empty<(int Row, int Col)>();

        public abstract string Apply(
            Data data, IsOnStone user, (int Row, int Col)? target);

        // 선택한 좌표가 실제 카드 대상 목록에 포함되는지 검사한다.
        protected (int Row, int Col) RequireTarget(
            Data data, IsOnStone user, (int Row, int Col)? target) {
            if (!target.HasValue || !GetTargets(data, user).Contains(target.Value))
                throw new InvalidOperationException(
                    $"[{Name}] 대상이 올바르지 않습니다.");

            return target.Value;
        }

        public override string ToString() => Name;
    }
}
