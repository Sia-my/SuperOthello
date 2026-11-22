using System;
using System.Collections.Generic;
using System.Linq;
using Othello.Core.Model;

namespace Othello.Core.Cards {
    // 초능력 카드 공통 규칙
    // - 새 카드 = SkillCardBase를 상속한 클래스 1개 추가
    // - 카드 객체는 상태를 갖지 않음 → 복제본끼리 공유해도 안전

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
