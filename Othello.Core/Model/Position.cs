namespace Othello.Core.Model;

/// <summary>보드 좌표 (행, 열). 0~7.</summary>
public readonly record struct Position(int Row, int Col)
{
    public override string ToString() => $"({Row},{Col})";
}
