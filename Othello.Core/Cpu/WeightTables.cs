using Othello.Core.Model;

namespace Othello.Core.Cpu;

/// <summary>위치 가중치표 (설계서 3.4.3). 단계는 빈칸 수로 구분한다.</summary>
public static class WeightTables
{
    public static readonly int[,] Basic =
    {
        { 100, -20,  10,   5,   5,  10, -20, 100 },
        { -20, -50,  -2,  -2,  -2,  -2, -50, -20 },
        {  10,  -2,   1,   1,   1,   1,  -2,  10 },
        {   5,  -2,   1,   0,   0,   1,  -2,   5 },
        {   5,  -2,   1,   0,   0,   1,  -2,   5 },
        {  10,  -2,   1,   1,   1,   1,  -2,  10 },
        { -20, -50,  -2,  -2,  -2,  -2, -50, -20 },
        { 100, -20,  10,   5,   5,  10, -20, 100 }
    };

    public enum Phase { Opening, Middle, End }

    public static Phase PhaseOf(Board b) => b.EmptyCount switch
    {
        >= 40 => Phase.Opening,
        >= 15 => Phase.Middle,
        _ => Phase.End
    };

    /// <summary>
    /// 지금 판에 맞는 가중치 값.
    /// TODO(3주차): 중반 규칙(점유된 모서리 주변 C·X칸 0), 종반 평탄표(모서리 30, C·X 0, 나머지 5)
    /// </summary>
    public static int WeightAt(Board b, int r, int c) => Basic[r, c];
}
