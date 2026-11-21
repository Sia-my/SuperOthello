using Othello.Core.Rules;

namespace Othello.Core.Model;

/// <summary>8×8 판과 오셀로 기본 규칙 (설계서 F1).</summary>
public class Board
{
    public const int Size = 8;

    private static readonly (int dr, int dc)[] Directions =
    {
        (-1, -1), (-1, 0), (-1, 1),
        ( 0, -1),          ( 0, 1),
        ( 1, -1), ( 1, 0), ( 1, 1)
    };

    private Cell[,] cells = new Cell[Size, Size];

    /// <summary>패 효과처럼 칸을 직접 바꿔야 하는 경우에만 사용한다.</summary>
    public Cell[,] Cells => cells;

    public ref Cell this[int r, int c] => ref cells[r, c];
    public ref Cell this[Position p] => ref cells[p.Row, p.Col];

    /// <summary>F1-1 판 초기화: 중앙 4칸에 흑백 2개씩.</summary>
    public void Reset()
    {
        cells = new Cell[Size, Size];
        cells[3, 3].Color = Stone.White;
        cells[4, 4].Color = Stone.White;
        cells[3, 4].Color = Stone.Black;
        cells[4, 3].Color = Stone.Black;
    }

    public static bool InBounds(int r, int c) => r >= 0 && r < Size && c >= 0 && c < Size;

    /// <summary>
    /// (r, c)에 color를 두었을 때 뒤집힐 칸 목록. 비어 있으면 둘 수 없는 칸이다.
    /// 고정 돌은 줄을 막지는 않지만 뒤집히지도 않는다.
    /// </summary>
    public List<Position> GetFlips(int r, int c, Stone color)
    {
        var result = new List<Position>();
        if (!InBounds(r, c) || !StatusEffects.CanPlace(cells[r, c])) return result;

        Stone opp = color.Opponent();
        var line = new List<Position>();

        foreach (var (dr, dc) in Directions)
        {
            line.Clear();
            int nr = r + dr, nc = c + dc;

            while (InBounds(nr, nc) && cells[nr, nc].Color == opp)
            {
                if (StatusEffects.CanFlip(cells[nr, nc])) line.Add(new Position(nr, nc));
                nr += dr;
                nc += dc;
            }

            // 상대 돌 줄 끝에 내 돌이 있어야 성립
            if (InBounds(nr, nc) && cells[nr, nc].Color == color)
                result.AddRange(line);
        }
        return result;
    }

    /// <summary>F1-2 착수 가능 위치 계산.</summary>
    public List<Position> GetValidMoves(Stone color)
    {
        var moves = new List<Position>();
        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
                if (GetFlips(r, c, color).Count > 0)
                    moves.Add(new Position(r, c));
        return moves;
    }

    public bool HasValidMove(Stone color) => GetValidMoves(color).Count > 0;

    /// <summary>F1-3 착수 및 뒤집기. 뒤집은 칸 목록을 돌려준다. 둘 수 없으면 예외.</summary>
    public List<Position> Place(int r, int c, Stone color)
    {
        var flips = GetFlips(r, c, color);
        if (flips.Count == 0)
            throw new InvalidOperationException($"({r},{c})에는 {color.ToKorean()}이(가) 둘 수 없습니다.");

        cells[r, c].Color = color;
        foreach (var p in flips) cells[p.Row, p.Col].Color = color;
        return flips;
    }

    /// <summary>규칙 무시하고 돌만 놓는다 (자유 착수 패, 테스트용).</summary>
    public void PutStone(int r, int c, Stone color) => cells[r, c].Color = color;

    public int Count(Stone color)
    {
        int n = 0;
        foreach (var cell in cells) if (cell.Color == color) n++;
        return n;
    }

    public int EmptyCount => Count(Stone.Empty);
    public bool IsFull => EmptyCount == 0;

    public Board Clone()
    {
        var b = new Board();
        b.cells = (Cell[,])cells.Clone();   // Cell이 값 타입이라 이것만으로 깊은 복사
        return b;
    }
}
