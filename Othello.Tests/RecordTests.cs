using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Othello.Core.Cards;
using Othello.Core.Model;
using Othello.Core.Records;

namespace Othello.Tests {
    // 전적 DB 테스트 (설계서 3.3.1, 3.5 TC-31~32)
    // - 테스트마다 임시 파일 DB 사용, 실제 records.db는 건드리지 않음

    [TestClass]
    public class RecordTests {
        private string dbPath = "";
        private RecordRepository repo = null!;

        [TestInitialize]
        public void Init() {
            dbPath = Path.GetTempFileName();
            repo = new RecordRepository(dbPath);
            repo.EnsureCreated();
        }

        [TestCleanup]
        public void Cleanup() {
            // 연결 풀이 파일을 잡고 있으므로 먼저 해제
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        private static Data Finished(int black, int white) {
            var data = new Data { IsOver = true };
            data.Scores[0] = black;
            data.Scores[1] = white;
            return data;
        }

        private long Scalar(string sql, params (string Name, object Value)[] args) {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value);
            return (long)cmd.ExecuteScalar()!;
        }

        [TestMethod]
        public void DB01_테이블_생성() {
            foreach (string table in new[] { "Players", "Games", "Psionics" }) {
                long n = Scalar("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @n", ("@n", table));
                Assert.AreEqual(1L, n, table);
            }

            repo.EnsureCreated(); // 두 번 불러도 오류 없음
        }

        [TestMethod]
        public void DB02_저장후_최근목록에_표시() {
            repo.Save(PlayerSlot.Human("홍정호"), PlayerSlot.Cpu(Difficulty.Normal), Finished(32, 27));

            var recent = repo.GetRecent();
            Assert.AreEqual(1, recent.Count);
            var g = recent[0];
            Assert.AreEqual("홍정호", g.BlackId);
            Assert.AreEqual("#CPU-보통", g.WhiteId);
            Assert.AreEqual(32, g.BlackScore);
            Assert.AreEqual(27, g.WhiteScore);
            Assert.AreEqual('B', g.Winner);
            Assert.IsTrue((DateTime.Now - g.PlayedAt).TotalMinutes < 1);
        }

        [TestMethod]
        public void DB03_최근목록은_최신순_최대10개() {
            for (int i = 0; i < 12; i++)
                repo.Save(PlayerSlot.Human("a"), PlayerSlot.Human("b"), Finished(i, 0));

            var recent = repo.GetRecent();
            Assert.AreEqual(10, recent.Count);
            CollectionAssert.AreEqual(
                recent.Select(r => r.GameId).OrderByDescending(id => id).ToList(),
                recent.Select(r => r.GameId).ToList());
            Assert.AreEqual(11, recent[0].BlackScore); // 마지막에 저장한 판이 맨 앞
            Assert.AreEqual(2, recent[9].BlackScore);
        }

        [TestMethod]
        public void DB04_ID별_승패무_집계() {
            var a = PlayerSlot.Human("a");
            var b = PlayerSlot.Human("b");
            repo.Save(a, b, Finished(30, 20)); // a 승
            repo.Save(b, a, Finished(30, 20)); // a 패
            repo.Save(b, a, Finished(10, 40)); // a 승
            repo.Save(a, b, Finished(25, 25)); // 무

            var stats = repo.GetStats("a");
            Assert.AreEqual(2, stats.Wins);
            Assert.AreEqual(1, stats.Losses);
            Assert.AreEqual(1, stats.Draws);
            Assert.AreEqual(0.5, stats.WinRate, 1e-9);

            var none = repo.GetStats("nobody");
            Assert.AreEqual(0, none.Games);
            Assert.AreEqual(0, none.WinRate);
        }

        [TestMethod]
        public void DB05_처음보는_ID는_자동등록() {
            Assert.AreEqual(0L, Scalar("SELECT COUNT(*) FROM Players"));

            repo.Save(PlayerSlot.Human("a"), PlayerSlot.Human("b"), Finished(1, 0));
            repo.Save(PlayerSlot.Human("a"), PlayerSlot.Human("c"), Finished(1, 0));

            Assert.AreEqual(3L, Scalar("SELECT COUNT(*) FROM Players"));
            Assert.AreEqual(0L, Scalar("SELECT IsCpu FROM Players WHERE Id = @id", ("@id", "a")));
        }

        [TestMethod]
        public void DB06_CPU는_IsCpu_1로_등록() {
            repo.Save(PlayerSlot.Human("a"), PlayerSlot.Cpu(Difficulty.Normal), Finished(1, 0));

            Assert.AreEqual(1L, Scalar("SELECT IsCpu FROM Players WHERE Id = @id", ("@id", "#CPU-보통")));
            Assert.AreEqual("#CPU-보통", repo.GetRecent()[0].WhiteId);
        }

        [TestMethod]
        public void DB07_특수문자_ID도_쿼리가_깨지지_않음() {
            string tricky = "a'b\"; DROP TABLE Games; --";
            repo.Save(PlayerSlot.Human(tricky), PlayerSlot.Human("b"), Finished(5, 3));

            Assert.AreEqual(tricky, repo.GetRecent()[0].BlackId);
            Assert.AreEqual(1, repo.GetStats(tricky).Wins);
            Assert.AreEqual(1L, Scalar("SELECT COUNT(*) FROM Games"));
        }

        [TestMethod]
        public void DB08_Psionics_동기화() {
            var pool = CardPool.Current().ToList();
            repo.SyncPsionics(pool);
            repo.SyncPsionics(pool); // 다시 불러도 중복 없음

            var expected = pool.GroupBy(c => c.Id).Select(g => g.First()).OrderBy(c => c.Id).ToList();
            var rows = repo.GetPsionics();
            Assert.AreEqual(12, rows.Count, "최종 12종");
            Assert.AreEqual(expected.Count, rows.Count);
            for (int i = 0; i < rows.Count; i++) {
                Assert.AreEqual(expected[i].Id, rows[i].Id);
                Assert.AreEqual(expected[i].Name, rows[i].Name);
                Assert.AreEqual(expected[i].Description, rows[i].Description);
            }
        }

        [TestMethod]
        public void DB09_승률_계산() {
            var a = PlayerSlot.Human("a");
            var b = PlayerSlot.Human("b");
            for (int i = 0; i < 3; i++) repo.Save(a, b, Finished(30, 20)); // a 3승
            repo.Save(a, b, Finished(10, 20));                             // a 1패

            var stats = repo.GetStats("a");
            Assert.AreEqual(0.75, stats.WinRate, 1e-9);
            Assert.AreEqual("75.0%", stats.WinRateText);

            var none = repo.GetStats("nobody");
            Assert.AreEqual(0, none.WinRate);
            Assert.AreEqual("0.0%", none.WinRateText);
        }
    }
}
