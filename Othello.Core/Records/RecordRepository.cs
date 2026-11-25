using System.Globalization;
using Microsoft.Data.Sqlite;
using Othello.Core.Cards;
using Othello.Core.Logic;
using Othello.Core.Model;

namespace Othello.Core.Records {
    // 전적 SQLite 저장소 (F5, 설계서 3.3.1)
    // - 모든 쿼리는 매개변수화 (설계서 2.3 보안성), 날짜는 yyyy-MM-dd HH:mm:ss 문자열

    public sealed class RecordRepository {
        public const string DateFormat = "yyyy-MM-dd HH:mm:ss";

        private readonly string connectionString;

        public string DbPath { get; }

        public RecordRepository(string dbPath) {
            DbPath = dbPath;
            connectionString = new SqliteConnectionStringBuilder {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();
        }

        // %LocalAppData%\SuperOthello\records.db (폴더 없으면 생성)
        public static string DefaultPath {
            get {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SuperOthello");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "records.db");
            }
        }

        private SqliteConnection Open() {
            var conn = new SqliteConnection(connectionString);
            conn.Open();
            return conn;
        }

        // 설계서 3.3.1 스키마
        public void EnsureCreated() {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Players (
                    Id        TEXT PRIMARY KEY,
                    IsCpu     INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Games (
                    GameId     INTEGER PRIMARY KEY AUTOINCREMENT,
                    BlackId    TEXT NOT NULL REFERENCES Players(Id),
                    WhiteId    TEXT NOT NULL REFERENCES Players(Id),
                    BlackScore INTEGER NOT NULL,
                    WhiteScore INTEGER NOT NULL,
                    Winner     TEXT,
                    PlayedAt   TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Psionics (
                    PsionicId   INTEGER PRIMARY KEY,
                    Name        TEXT NOT NULL,
                    Description TEXT NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();
        }

        // 도감용 카드 정보 동기화 (같은 카드가 여러 장이면 1행)
        public void SyncPsionics(IEnumerable<ISkillCard> cards) {
            using var conn = Open();
            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT OR REPLACE INTO Psionics (PsionicId, Name, Description)
                VALUES (@id, @name, @desc);
                """;
            var pId = cmd.Parameters.Add("@id", SqliteType.Integer);
            var pName = cmd.Parameters.Add("@name", SqliteType.Text);
            var pDesc = cmd.Parameters.Add("@desc", SqliteType.Text);

            foreach (var card in cards.GroupBy(c => c.Id).Select(g => g.First())) {
                pId.Value = card.Id;
                pName.Value = card.Name;
                pDesc.Value = card.Description;
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }

        // 끝난 게임 1판 저장 (처음 보는 ID는 자동 등록, F5-1·F5-2)
        public void Save(PlayerSlot black, PlayerSlot white, Data finished) {
            string now = DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture);

            using var conn = Open();
            using var tx = conn.BeginTransaction();

            using (var cmd = conn.CreateCommand()) {
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT OR IGNORE INTO Players (Id, IsCpu, CreatedAt)
                    VALUES (@id, @cpu, @at);
                    """;
                var pId = cmd.Parameters.Add("@id", SqliteType.Text);
                var pCpu = cmd.Parameters.Add("@cpu", SqliteType.Integer);
                cmd.Parameters.AddWithValue("@at", now);

                foreach (var slot in new[] { black, white }) {
                    pId.Value = slot.Id;
                    pCpu.Value = slot.IsCpu ? 1 : 0;
                    cmd.ExecuteNonQuery();
                }
            }

            using (var cmd = conn.CreateCommand()) {
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO Games (BlackId, WhiteId, BlackScore, WhiteScore, Winner, PlayedAt)
                    VALUES (@black, @white, @bs, @ws, @winner, @at);
                    """;
                cmd.Parameters.AddWithValue("@black", black.Id);
                cmd.Parameters.AddWithValue("@white", white.Id);
                cmd.Parameters.AddWithValue("@bs", finished.Scores[0]);
                cmd.Parameters.AddWithValue("@ws", finished.Scores[1]);
                cmd.Parameters.AddWithValue("@winner", WinnerCode(ScoreLogic.Winner(finished)).ToString());
                cmd.Parameters.AddWithValue("@at", now);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }

        public static char WinnerCode(IsOnStone winner) => winner switch {
            IsOnStone.Black => 'B',
            IsOnStone.White => 'W',
            _ => 'D'
        };

        // 최근 게임 (최신순, F5-3)
        public IReadOnlyList<GameRecord> GetRecent(int count = 10) {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT GameId, BlackId, WhiteId, BlackScore, WhiteScore, Winner, PlayedAt
                FROM Games ORDER BY GameId DESC LIMIT @count;
                """;
            cmd.Parameters.AddWithValue("@count", count);

            var list = new List<GameRecord>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) {
                string winner = r.IsDBNull(5) ? "D" : r.GetString(5);
                list.Add(new GameRecord(
                    r.GetInt64(0),
                    r.GetString(1),
                    r.GetString(2),
                    r.GetInt32(3),
                    r.GetInt32(4),
                    winner.Length > 0 ? winner[0] : 'D',
                    DateTime.ParseExact(r.GetString(6), DateFormat, CultureInfo.InvariantCulture)));
            }
            return list;
        }

        // ID별 승·패·무 집계 (F5-4)
        public PlayerStats GetStats(string id) {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT
                    COALESCE(SUM(CASE WHEN (BlackId = @id AND Winner = 'B')
                                        OR (WhiteId = @id AND Winner = 'W') THEN 1 ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN (BlackId = @id AND Winner = 'W')
                                        OR (WhiteId = @id AND Winner = 'B') THEN 1 ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN Winner = 'D' THEN 1 ELSE 0 END), 0)
                FROM Games
                WHERE BlackId = @id OR WhiteId = @id;
                """;
            cmd.Parameters.AddWithValue("@id", id);

            using var r = cmd.ExecuteReader();
            r.Read();
            return new PlayerStats(id, r.GetInt32(0), r.GetInt32(1), r.GetInt32(2));
        }

        // 패 도감 목록 (Id 순)
        public IReadOnlyList<(int Id, string Name, string Description)> GetPsionics() {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT PsionicId, Name, Description FROM Psionics ORDER BY PsionicId;";

            var list = new List<(int, string, string)>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add((r.GetInt32(0), r.GetString(1), r.GetString(2)));
            return list;
        }
    }
}
