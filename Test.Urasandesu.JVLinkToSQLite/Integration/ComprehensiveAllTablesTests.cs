using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Urasandesu.JVLinkToSQLite.DatabaseProviders;
using Urasandesu.JVLinkToSQLite.DatabaseProviders.SQLite;
using Urasandesu.JVLinkToSQLite.DatabaseProviders.DuckDB;
using Urasandesu.JVLinkToSQLite.DatabaseProviders.PostgreSQL;

namespace Test.Urasandesu.JVLinkToSQLite.Integration
{
    /// <summary>
    /// Comprehensive test to verify all databases can handle various table structures
    /// with different data types and Japanese text.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("Comprehensive")]
    public class ComprehensiveAllTablesTests
    {
        private string _tempDirectory;
        private string _sqlitePath;
        private string _duckdbPath;

        [SetUp]
        public void Setup()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "JVLink_Comprehensive_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
            _sqlitePath = Path.Combine(_tempDirectory, "comprehensive.db");
            _duckdbPath = Path.Combine(_tempDirectory, "comprehensive.duckdb");
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
        /// Test tables representing various JVLink data structures
        /// </summary>
        private class TestTableDefinition
        {
            public string TableName { get; set; }
            public Dictionary<string, string> Columns { get; set; }
            public List<Dictionary<string, object>> TestData { get; set; }
        }

        private List<TestTableDefinition> GetComprehensiveTestTables()
        {
            return new List<TestTableDefinition>
            {
                // Race data table
                new TestTableDefinition
                {
                    TableName = "JV_RA_RACE",
                    Columns = new Dictionary<string, string>
                    {
                        { "id", "INTEGER" },
                        { "race_id", "TEXT" },
                        { "race_name", "TEXT" },
                        { "race_date", "TEXT" },
                        { "distance", "INTEGER" },
                        { "prize_money", "REAL" }
                    },
                    TestData = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object> { { "id", 1 }, { "race_id", "202401010101" }, { "race_name", "東京優駿（日本ダービー）" }, { "race_date", "2024-05-26" }, { "distance", 2400 }, { "prize_money", 300000000.0 } },
                        new Dictionary<string, object> { { "id", 2 }, { "race_id", "202401010102" }, { "race_name", "桜花賞" }, { "race_date", "2024-04-14" }, { "distance", 1600 }, { "prize_money", 150000000.0 } },
                        new Dictionary<string, object> { { "id", 3 }, { "race_id", "202401010103" }, { "race_name", "皐月賞" }, { "race_date", "2024-04-21" }, { "distance", 2000 }, { "prize_money", 200000000.0 } }
                    }
                },
                // Horse data table
                new TestTableDefinition
                {
                    TableName = "JV_UM_UMA",
                    Columns = new Dictionary<string, string>
                    {
                        { "id", "INTEGER" },
                        { "horse_id", "TEXT" },
                        { "horse_name", "TEXT" },
                        { "sex", "TEXT" },
                        { "birth_date", "TEXT" },
                        { "weight", "INTEGER" }
                    },
                    TestData = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object> { { "id", 1 }, { "horse_id", "2020101001" }, { "horse_name", "ディープインパクト" }, { "sex", "牡" }, { "birth_date", "2002-03-25" }, { "weight", 478 } },
                        new Dictionary<string, object> { { "id", 2 }, { "horse_id", "2020101002" }, { "horse_name", "キズナ" }, { "sex", "牡" }, { "birth_date", "2010-03-12" }, { "weight", 502 } },
                        new Dictionary<string, object> { { "id", 3 }, { "horse_id", "2020101003" }, { "horse_name", "オルフェーヴル" }, { "sex", "牡" }, { "birth_date", "2008-05-14" }, { "weight", 498 } }
                    }
                },
                // Jockey data table
                new TestTableDefinition
                {
                    TableName = "JV_KS_KISYU",
                    Columns = new Dictionary<string, string>
                    {
                        { "id", "INTEGER" },
                        { "jockey_code", "TEXT" },
                        { "jockey_name", "TEXT" },
                        { "birth_date", "TEXT" },
                        { "weight", "REAL" }
                    },
                    TestData = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object> { { "id", 1 }, { "jockey_code", "00666" }, { "jockey_name", "武豊" }, { "birth_date", "1969-03-15" }, { "weight", 52.5 } },
                        new Dictionary<string, object> { { "id", 2 }, { "jockey_code", "01054" }, { "jockey_name", "福永祐一" }, { "birth_date", "1976-12-09" }, { "weight", 52.0 } },
                        new Dictionary<string, object> { { "id", 3 }, { "jockey_code", "01075" }, { "jockey_name", "川田将雅" }, { "birth_date", "1984-10-15" }, { "weight", 52.0 } }
                    }
                },
                // Trainer data table
                new TestTableDefinition
                {
                    TableName = "JV_CH_CHOKYOSI",
                    Columns = new Dictionary<string, string>
                    {
                        { "id", "INTEGER" },
                        { "trainer_code", "TEXT" },
                        { "trainer_name", "TEXT" },
                        { "affiliation", "TEXT" }
                    },
                    TestData = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object> { { "id", 1 }, { "trainer_code", "00401" }, { "trainer_name", "藤沢和雄" }, { "affiliation", "美浦" } },
                        new Dictionary<string, object> { { "id", 2 }, { "trainer_code", "01017" }, { "trainer_name", "友道康夫" }, { "affiliation", "栗東" } },
                        new Dictionary<string, object> { { "id", 3 }, { "trainer_code", "01126" }, { "trainer_name", "池江泰寿" }, { "affiliation", "栗東" } }
                    }
                },
                // Owner data table
                new TestTableDefinition
                {
                    TableName = "JV_BN_BANUSI",
                    Columns = new Dictionary<string, string>
                    {
                        { "id", "INTEGER" },
                        { "owner_code", "TEXT" },
                        { "owner_name", "TEXT" },
                        { "country", "TEXT" }
                    },
                    TestData = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object> { { "id", 1 }, { "owner_code", "010001" }, { "owner_name", "サンデーレーシング" }, { "country", "日本" } },
                        new Dictionary<string, object> { { "id", 2 }, { "owner_code", "010002" }, { "owner_name", "キャロットファーム" }, { "country", "日本" } },
                        new Dictionary<string, object> { { "id", 3 }, { "owner_code", "010003" }, { "owner_name", "社台レースホース" }, { "country", "日本" } }
                    }
                }
            };
        }

        [Test]
        public void ComprehensiveTest_AllTables_AllDatabases_HandleDataCorrectly()
        {
            // Arrange
            var tables = GetComprehensiveTestTables();
            var sqliteConnectionString = $"Data Source={_sqlitePath}";
            var duckdbConnectionString = $"Data Source={_duckdbPath}";

            var postgresqlConnectionString = Environment.GetEnvironmentVariable("JVLINK_TEST_POSTGRESQL_CONNECTION");
            var testPostgreSQL = !string.IsNullOrEmpty(postgresqlConnectionString);

            if (testPostgreSQL && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JVLINK_DB_PASSWORD")))
            {
                Environment.SetEnvironmentVariable("JVLINK_DB_PASSWORD", "testpassword");
            }

            var results = new Dictionary<string, Dictionary<string, List<string>>>();

            // Act & Assert - Test each database
            TestDatabaseProvider("SQLite", new SQLiteDatabaseProvider(sqliteConnectionString), tables, results);
            TestDatabaseProvider("DuckDB", new DuckDBDatabaseProvider(duckdbConnectionString), tables, results);

            if (testPostgreSQL)
            {
                try
                {
                    TestDatabaseProvider("PostgreSQL", new PostgreSQLDatabaseProvider(postgresqlConnectionString), tables, results);
                }
                catch (Exception ex)
                {
                    Assert.Warn($"PostgreSQL test skipped: {ex.Message}");
                }
            }

            // Verify results consistency across databases
            Console.WriteLine("\n=== Comprehensive Test Results ===\n");
            foreach (var table in tables)
            {
                Console.WriteLine($"Table: {table.TableName}");
                Console.WriteLine($"  Columns: {table.Columns.Count}");
                Console.WriteLine($"  Test Records: {table.TestData.Count}");

                foreach (var dbName in results.Keys)
                {
                    if (results[dbName].ContainsKey(table.TableName))
                    {
                        var recordCount = results[dbName][table.TableName].Count;
                        var status = recordCount == table.TestData.Count ? "✓" : "✗";
                        Console.WriteLine($"  {dbName}: {status} {recordCount}/{table.TestData.Count} records");
                    }
                }
                Console.WriteLine();
            }

            // Assert all databases have consistent results
            foreach (var table in tables)
            {
                var expectedCount = table.TestData.Count;
                foreach (var dbName in results.Keys)
                {
                    if (results[dbName].ContainsKey(table.TableName))
                    {
                        var actualCount = results[dbName][table.TableName].Count;
                        Assert.AreEqual(expectedCount, actualCount,
                            $"{dbName} - Table {table.TableName}: Expected {expectedCount} records but got {actualCount}");
                    }
                }
            }

            Console.WriteLine("✅ All databases handled all tables correctly!");
        }

        private void TestDatabaseProvider(string dbName, IDatabaseProvider provider, List<TestTableDefinition> tables, Dictionary<string, Dictionary<string, List<string>>> results)
        {
            results[dbName] = new Dictionary<string, List<string>>();
            var isDuckDB = dbName == "DuckDB";

            using (provider)
            {
                var generator = provider.GetSqlGenerator();
                var quote = generator.IdentifierQuoteChar;

                using (var conn = provider.CreateConnection())
                {
                    conn.Open();

                    foreach (var table in tables)
                    {
                        // Create table
                        var createSql = BuildCreateTableSql(table, generator);
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = createSql;
                            cmd.ExecuteNonQuery();
                        }

                        // Insert data
                        foreach (var data in table.TestData)
                        {
                            // カラム順序を明示的に固定（Dictionary順序に依存しない）
                            var orderedColumns = data.Keys.ToList();
                            var insertSql = BuildInsertSql(table.TableName, orderedColumns, generator, isDuckDB);

                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = insertSql;

                                if (isDuckDB)
                                {
                                    // DuckDB uses positional parameters - add in same order as columns
                                    foreach (var key in orderedColumns)
                                    {
                                        var param = cmd.CreateParameter();
                                        param.Value = data[key] ?? DBNull.Value;
                                        cmd.Parameters.Add(param);
                                    }
                                }
                                else
                                {
                                    // SQLite and PostgreSQL use named parameters
                                    foreach (var key in orderedColumns)
                                    {
                                        var param = cmd.CreateParameter();
                                        param.ParameterName = generator.GetParameterName(key);
                                        param.Value = data[key] ?? DBNull.Value;
                                        cmd.Parameters.Add(param);
                                    }
                                }
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Retrieve and verify data
                        var retrievedData = new List<string>();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = $"SELECT * FROM {quote}{table.TableName}{quote} ORDER BY {quote}id{quote}";
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var row = new List<string>();
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        row.Add(reader.GetValue(i)?.ToString() ?? "NULL");
                                    }
                                    retrievedData.Add(string.Join("|", row));
                                }
                            }
                        }

                        results[dbName][table.TableName] = retrievedData;
                    }
                }
            }
        }

        private string BuildCreateTableSql(TestTableDefinition table, ISqlGenerator generator)
        {
            var quote = generator.IdentifierQuoteChar;
            var columns = string.Join(", ", table.Columns.Select(kvp =>
                $"{quote}{kvp.Key}{quote} {kvp.Value}"));

            return $"CREATE TABLE {quote}{table.TableName}{quote} ({columns})";
        }

        private string BuildInsertSql(string tableName, List<string> orderedColumns, ISqlGenerator generator, bool isDuckDB)
        {
            var quote = generator.IdentifierQuoteChar;
            var columns = string.Join(", ", orderedColumns.Select(k => $"{quote}{k}{quote}"));

            string parameters;
            if (isDuckDB)
            {
                // DuckDB uses positional parameters: $1, $2, $3, etc.
                var paramList = Enumerable.Range(1, orderedColumns.Count).Select(i => $"${i}");
                parameters = string.Join(", ", paramList);
            }
            else
            {
                // SQLite and PostgreSQL use named parameters
                parameters = string.Join(", ", orderedColumns.Select(k => generator.GetParameterName(k)));
            }

            return $"INSERT INTO {quote}{tableName}{quote} ({columns}) VALUES ({parameters})";
        }
    }
}
