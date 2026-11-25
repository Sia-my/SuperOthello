namespace Othello.Core.Records {
    // Games 테이블 한 행 (설계서 3.3.1)
    // - Winner: 'B' 흑 승, 'W' 백 승, 'D' 무승부

    public sealed record GameRecord(
        long GameId,
        string BlackId,
        string WhiteId,
        int BlackScore,
        int WhiteScore,
        char Winner,
        DateTime PlayedAt);
}
