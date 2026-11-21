using Othello.Core.Model;

namespace Othello.Core.Rules;

/// <summary>
/// 칸 상태 플래그에 따른 판정을 한곳에 모은 클래스 (설계서 3.3 StatusEffects).
/// Board와 패 클래스는 상태를 직접 검사하지 말고 반드시 여기를 거친다.
/// </summary>
public static class StatusEffects
{
    /// <summary>이 칸에 새 돌을 둘 수 있는가.</summary>
    public static bool CanPlace(in Cell cell) => cell.IsEmpty && !cell.Has(CellState.Sealed);

    /// <summary>이 칸의 돌이 뒤집힐 수 있는가.</summary>
    public static bool CanFlip(in Cell cell) => !cell.IsEmpty && !cell.Has(CellState.Fixed);

    /// <summary>폭탄·등가교환 등으로 이 칸의 돌을 제거할 수 있는가.</summary>
    public static bool CanRemove(in Cell cell) => !cell.IsEmpty && !cell.Has(CellState.Fixed);

    /// <summary>턴이 끝날 때 호출. 봉인 턴을 1 줄이고 0이 되면 봉인을 푼다.</summary>
    public static void ProcessTurnEnd(Cell[,] cells)
    {
        for (int r = 0; r < Board.Size; r++)
        {
            for (int c = 0; c < Board.Size; c++)
            {
                ref Cell cell = ref cells[r, c];
                if (!cell.Has(CellState.Sealed)) continue;

                if (cell.SealTurns > 0) cell.SealTurns--;
                if (cell.SealTurns == 0) cell.State &= ~CellState.Sealed;
            }
        }
    }
}
