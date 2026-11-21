namespace Othello.Core.Model;

/// <summary>
/// 칸에 걸린 초능력 상태. 여러 상태가 동시에 걸릴 수 있으므로 비트 플래그로 둔다.
/// (설계서 3.3.1)
/// </summary>
[Flags]
public enum CellState : byte
{
    None = 0,
    Fixed = 1 << 0,   // 고정: 뒤집히거나 제거되지 않음
    Sealed = 1 << 1   // 봉인: 아무도 둘 수 없음 (빈칸에만 걸림)
    // TODO: 지뢰(Mine), 성역(Sanctuary)은 최종 12종 확정 후 추가
}

/// <summary>
/// 칸 하나. 값 타입 필드만 가지므로 Cell[,]을 Clone()하면 완전히 독립된 복사본이 된다.
/// </summary>
public struct Cell
{
    public Stone Color;
    public CellState State;
    public byte SealTurns;   // 봉인이 풀리기까지 남은 턴 수

    public readonly bool IsEmpty => Color == Stone.Empty;
    public readonly bool Has(CellState flag) => (State & flag) == flag;
}
