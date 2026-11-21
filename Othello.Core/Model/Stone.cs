namespace Othello.Core.Model;

/// <summary>칸에 놓인 돌의 색.</summary>
public enum Stone : byte
{
    Empty,
    Black,
    White
}

public static class StoneExtensions
{
    /// <summary>상대 색을 돌려준다. Empty는 Empty 그대로.</summary>
    public static Stone Opponent(this Stone s) => s switch
    {
        Stone.Black => Stone.White,
        Stone.White => Stone.Black,
        _ => Stone.Empty
    };

    /// <summary>Scores, Hands 같은 [흑, 백] 배열의 인덱스. 흑 = 0, 백 = 1.</summary>
    public static int Index(this Stone s) => s switch
    {
        Stone.Black => 0,
        Stone.White => 1,
        _ => throw new ArgumentException("Empty에는 인덱스가 없습니다.", nameof(s))
    };

    public static string ToKorean(this Stone s) => s switch
    {
        Stone.Black => "흑",
        Stone.White => "백",
        _ => "빈칸"
    };
}
