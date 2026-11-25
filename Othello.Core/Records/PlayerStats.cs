namespace Othello.Core.Records {
    // ID별 승·패·무 통계 (F5-4)
    // - 승률 = 승 / 전체 판수 (무승부 포함), 0판이면 0

    public sealed record PlayerStats(string Id, int Wins, int Losses, int Draws) {
        public int Games => Wins + Losses + Draws;

        public double WinRate => Games == 0 ? 0 : (double)Wins / Games;

        // 화면 표시용: 소수점 1자리 % (예: 75.0%)
        public string WinRateText
            => (WinRate * 100).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%";
    }
}
