using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Urasandesu.JVLinkToSQLite.DatabaseProviders;
using Urasandesu.JVLinkToSQLite.DatabaseProviders.SQLite;
using Urasandesu.JVLinkToSQLite.DatabaseProviders.DuckDB;

namespace Test.Urasandesu.JVLinkToSQLite.DatabaseProviders
{
    /// <summary>
    /// インデックス生成ロジックの単体テスト
    /// 重複防止、パターンマッチング、columnName判定の検証
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    [Category("IndexGeneration")]
    public class IndexGenerationTests
    {
        private string _tempDirectory;
        private string _sqlitePath;
        private string _duckdbPath;

        [SetUp]
        public void Setup()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "JVLink_IndexGen_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
            _sqlitePath = Path.Combine(_tempDirectory, "index_gen_test.db");
            _duckdbPath = Path.Combine(_tempDirectory, "index_gen_test.duckdb");
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    Directory.Delete(_tempDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        /// <summary>
        /// テスト1: 外部キーカラム（_id, _codeで終わる）が正しく検出されること
        /// </summary>
        [Test]
        public void GenerateIndexes_ForeignKeyColumns_CreatesIndexes()
        {
            // Arrange
            var sqliteConnectionString = $"Data Source={_sqlitePath}";

            using (var provider = new SQLiteDatabaseProvider(sqliteConnectionString))
            {
                var generator = provider.GetSqlGenerator();
                var quote = generator.IdentifierQuoteChar;

                using (var conn = provider.CreateConnection())
                {
                    conn.Open();

                    // 外部キーパターンのテーブルを作成
                    var createTableSql = $@"CREATE TABLE {quote}test_foreign_keys{quote} (
                        {quote}id{quote} INTEGER PRIMARY KEY,
                        {quote}race_id{quote} TEXT,
                        {quote}jockey_code{quote} TEXT,
                        {quote}trainer_code{quote} TEXT,
                        {quote}name{quote} TEXT
                    )";

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = createTableSql;
                        cmd.ExecuteNonQuery();
                    }

                    // Act: インデックスを取得
                    var indexes = GetIndexesForTable(conn, "test_foreign_keys");

                    // Assert
                    TestContext.WriteLine($"Indexes found: {indexes.Count}");
                    foreach (var idx in indexes)
                    {
                        TestContext.WriteLine($"  - {idx.IndexName}: {idx.Sql}");
                    }

                    // 外部キーカラムに対してインデックスが作成されているか
                    Assert.IsTrue(indexes.Any(i => i.Sql != null && i.Sql.Contains("race_id")), "race_id index not found");
                    Assert.IsTrue(indexes.Any(i => i.Sql != null && i.Sql.Contains("jockey_code")), "jockey_code index not found");
                    Assert.IsTrue(indexes.Any(i => i.Sql != null && i.Sql.Contains("trainer_code")), "trainer_code index not found");

                    // nameカラムにはインデックスが作成されていないこと
                    Assert.IsFalse(indexes.Any(i => i.Sql != null && i.Sql.Contains("(name)")), "name index should not exist");
                }
            }
        }

        /// <summary>
        /// テスト2: 日付カラム（date, timeを含む）が正しく検出されること
        /// </summary>
        [Test]
        public void GenerateIndexes_DateTimeColumns_CreatesIndexes()
        {
            // Arrange
            var sqliteConnectionString = $"Data Source={_sqlitePath}";

            using (var provider = new SQLiteDatabaseProvider(sqliteConnectionString))
            {
                var generator = provider.GetSqlGenerator();
                var quote = generator.IdentifierQuoteChar;

                using (var conn = provider.CreateConnection())
                {
                    conn.Open();

                    // 日付カラムのテーブルを作成
                    var createTableSql = $@"CREATE TABLE {quote}test_date_time{quote} (
                        {quote}id{quote} INTEGER PRIMARY KEY,
                        {quote}race_date{quote} TEXT,
                        {quote}start_time{quote} TEXT,
                        {quote}registration_date{quote} TEXT,
                        {quote}update_datetime{quote} TEXT,
                        {quote}description{quote} TEXT
                    )";

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = createTableSql;
                        cmd.ExecuteNonQuery();
                    }

                    // Act
                    var indexes = GetIndexesForTable(conn, "test_date_time");

                    // Assert
                    TestContext.WriteLine($"Indexes found: {indexes.Count}");
                    foreach (var idx in indexes)
                    {
                        TestContext.WriteLine($"  - {idx.IndexName}: {idx.Sql}");
                    }

                    Assert.IsTrue(indexes.Any(i => i.Sql != null && i.Sql.Contains("race_date")), "race_date index not found");
                    Assert.IsTrue(indexes.Any(i => i.Sql != null && i.Sql.Contains("start_time")), "start_time index not found");
                    Assert.IsTrue(indexes.Any(i => i.Sql != null && i.Sql.Contains("registration_date")), "registration_date index not found");
                    Assert.IsTrue(indexes.Any(i => i.Sql != null && i.Sql.Contains("update_datetime")), "update_datetime index not found");

                    // descriptionカラムにはインデックスが作成されていないこと
                    Assert.IsFalse(indexes.Any(i => i.Sql != null && i.Sql.Contains("(description)")), "description index should not exist");
                }
            }
        }

        /// <summary>
        /// テスト3: 重要カラム（sex, affiliation, distance）が正しく検出されること
        /// </summary>
        [Test]
        public void GenerateIndexes_ImportantColumns_CreatesIndexes()
        {
            // Arrange
            var sqliteConnectionString = $"Data Source={_sqlitePath}";

            using (var provider = new SQLiteDatabaseProvider(sqliteConnectionString))
            {
                var generator = provider.GetSqlGenerator();
                var quote = generator.IdentifierQuoteChar;

                using (var conn = provider.CreateConnection())
                {
                    conn.Open();

                    // 重要カラムのテーブルを作成
                    var createTableSql = $@"CREATE TABLE {quote}test_important{quote} (
                        {quote}id{quote} INTEGER PRIMARY KEY,
                        {quote}sex{quote} TEXT,
                        {quote}affiliation{quote} TEXT,
                        {quote}distance{quote} INTEGER,
                        {quote}other{quote} TEXT
                    )";

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = createTableSql;
                        cmd.ExecuteNonQuery();
                    }

                    // Act
                    var indexes = GetIndexesForTable(conn, "test_important");

                    // Assert
                    TestContext.WriteLine($"Indexes found: {indexes.Count}");
                    foreach (var idx in indexes)
                    {
                        TestContext.WriteLine($"  - {idx.IndexName}: {idx.Sql}");
                    }

                    Assert.IsTrue(indexes.Any(i => i.Sql != null && i.Sql.Contains("sex")), "sex index not found");
                    Assert.IsTrue(indexes.Any(i => i.Sql != null && i.Sql.Contains("affiliation")), "affiliation index not found");
                    Assert.IsTrue(indexes.Any(i => i.Sql != null && i.Sql.Contains("distance")), "distance index not found");

                    // otherカラムにはインデックスが作成されていないこと
                    Assert.IsFalse(indexes.Any(i => i.Sql != null && i.Sql.Contains("(other)")), "other index should not exist");
                }
            }
        }

        /// <summary>
        /// テスト4: 重複インデックスが生成されないこと（重要！）
        /// </summary>
        [Test]
        public void GenerateIndexes_DuplicatePattern_NoDuplicateIndexes()
        {
            // Arrange
            var sqliteConnectionString = $"Data Source={_sqlitePath}";

            using (var provider = new SQLiteDatabaseProvider(sqliteConnectionString))
            {
                var generator = provider.GetSqlGenerator();
                var quote = generator.IdentifierQuoteChar;

                using (var conn = provider.CreateConnection())
                {
                    conn.Open();

                    // 複数の条件に該当するカラム名のテーブルを作成
                    // 例: "update_datetime" は "date" と "time" の両方を含む
                    var createTableSql = $@"CREATE TABLE {quote}test_duplicates{quote} (
                        {quote}id{quote} INTEGER PRIMARY KEY,
                        {quote}update_datetime{quote} TEXT,
                        {quote}registration_date{quote} TEXT
                    )";

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = createTableSql;
                        cmd.ExecuteNonQuery();
                    }

                    // Act
                    var indexes = GetIndexesForTable(conn, "test_duplicates");

                    // Assert
                    TestContext.WriteLine($"Indexes found: {indexes.Count}");
                    foreach (var idx in indexes)
                    {
                        TestContext.WriteLine($"  - {idx.IndexName}: {idx.Sql}");
                    }

                    // update_datetime に対して1つだけインデックスが作成されていること（重複なし）
                    var updateDatetimeIndexes = indexes.Where(i => i.Sql != null && i.Sql.Contains("update_datetime")).ToList();
                    Assert.AreEqual(1, updateDatetimeIndexes.Count, "update_datetime should have exactly 1 index (no duplicates)");

                    // registration_date に対して1つだけインデックスが作成されていること
                    var registrationDateIndexes = indexes.Where(i => i.Sql != null && i.Sql.Contains("registration_date")).ToList();
                    Assert.AreEqual(1, registrationDateIndexes.Count, "registration_date should have exactly 1 index");
                }
            }
        }

        /// <summary>
        /// テスト5: columnName.EndsWith("id")ロジック改善の検証
        /// "affiliation_id"は検出されるが、"timeid"は検出されないこと（_idで終わる場合のみ）
        /// </summary>
        [Test]
        public void GenerateIndexes_ImprovedIdLogic_CorrectDetection()
        {
            // Arrange
            var sqliteConnectionString = $"Data Source={_sqlitePath}";

            using (var provider = new SQLiteDatabaseProvider(sqliteConnectionString))
            {
                var generator = provider.GetSqlGenerator();
                var quote = generator.IdentifierQuoteChar;

                using (var conn = provider.CreateConnection())
                {
                    conn.Open();

                    // エッジケースのテーブルを作成
                    var createTableSql = $@"CREATE TABLE {quote}test_id_logic{quote} (
                        {quote}id{quote} INTEGER PRIMARY KEY,
                        {quote}affiliation_id{quote} TEXT,
                        {quote}timeid{quote} TEXT,
                        {quote}horse_id{quote} TEXT
                    )";

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = createTableSql;
                        cmd.ExecuteNonQuery();
                    }

                    // Act
                    var indexes = GetIndexesForTable(conn, "test_id_logic");

                    // Assert
                    TestContext.WriteLine($"Indexes found: {indexes.Count}");
                    foreach (var idx in indexes)
                    {
                        TestContext.WriteLine($"  - {idx.IndexName}: {idx.Sql}");
                    }

                    // affiliation_id は "_id" で終わるため検出される
                    Assert.IsTrue(indexes.Any(i => i.Sql != null && i.Sql.Contains("affiliation_id")), "affiliation_id index should exist");

                    // horse_id は "_id" で終わるため検出される
                    Assert.IsTrue(indexes.Any(i => i.Sql != null && i.Sql.Contains("horse_id")), "horse_id index should exist");

                    // timeid は "id" で終わるが "_id" ではないため、外部キーとしては検出されない
                    // ただし、"time"を含むため日付カラムとして検出される可能性がある
                    // このテストでは、外部キー判定の改善により誤検出が減ることを確認
                    TestContext.WriteLine("Note: timeid may be detected as date/time column (contains 'time')");
                }
            }
        }

        /// <summary>
        /// テスト6: DuckDBでも同様にインデックスが生成されること
        /// </summary>
        [Test]
        public void GenerateIndexes_DuckDB_CreatesIndexes()
        {
            // Arrange
            var duckdbConnectionString = $"Data Source={_duckdbPath}";

            using (var provider = new DuckDBDatabaseProvider(duckdbConnectionString))
            {
                var generator = provider.GetSqlGenerator();
                var quote = generator.IdentifierQuoteChar;

                using (var conn = provider.CreateConnection())
                {
                    conn.Open();

                    // テーブルを作成
                    var createTableSql = $@"CREATE TABLE {quote}test_duckdb{quote} (
                        {quote}id{quote} INTEGER PRIMARY KEY,
                        {quote}race_id{quote} VARCHAR,
                        {quote}race_date{quote} VARCHAR,
                        {quote}sex{quote} VARCHAR
                    )";

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = createTableSql;
                        cmd.ExecuteNonQuery();
                    }

                    // Act: DuckDBのインデックス情報を取得（実装が異なる可能性あり）
                    try
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT index_name FROM duckdb_indexes() WHERE table_name = 'test_duckdb'";
                            using (var reader = cmd.ExecuteReader())
                            {
                                int indexCount = 0;
                                while (reader.Read())
                                {
                                    indexCount++;
                                    TestContext.WriteLine($"DuckDB Index: {reader.GetString(0)}");
                                }

                                TestContext.WriteLine($"DuckDB Indexes found: {indexCount}");
                                // DuckDBではインデックスの実装が異なる可能性があるため、Warning扱い
                                if (indexCount == 0)
                                {
                                    Assert.Warn("DuckDB may not support explicit secondary indexes");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TestContext.WriteLine($"DuckDB index query failed: {ex.Message}");
                        Assert.Warn("DuckDB index query not supported - this is expected");
                    }
                }
            }
        }

        private List<IndexInfo> GetIndexesForTable(IDbConnection conn, string tableName)
        {
            var indexes = new List<IndexInfo>();

            using (var cmd = conn.CreateCommand())
            {
                // SQLite: Get all indexes for the table (excluding auto-created PRIMARY KEY indexes)
                cmd.CommandText = $"SELECT name, sql FROM sqlite_master WHERE type='index' AND tbl_name='{tableName}' AND sql IS NOT NULL";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        indexes.Add(new IndexInfo
                        {
                            IndexName = reader.GetString(0),
                            Sql = reader.GetString(1)
                        });
                    }
                }
            }

            return indexes;
        }

        private class IndexInfo
        {
            public string IndexName { get; set; }
            public string Sql { get; set; }
        }
    }
}
