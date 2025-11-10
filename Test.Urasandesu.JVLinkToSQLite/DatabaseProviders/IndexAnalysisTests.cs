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

namespace Test.Urasandesu.JVLinkToSQLite.DatabaseProviders
{
    /// <summary>
    /// Tests to analyze index coverage across all database providers
    /// </summary>
    [TestFixture]
    [Category("Performance")]
    [Category("Analysis")]
    public class IndexAnalysisTests
    {
        private string _tempDirectory;
        private string _sqlitePath;
        private string _duckdbPath;

        [SetUp]
        public void Setup()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "JVLink_IndexAnalysis_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
            _sqlitePath = Path.Combine(_tempDirectory, "index_test.db");
            _duckdbPath = Path.Combine(_tempDirectory, "index_test.duckdb");
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

        [Test]
        public void AnalyzeIndexes_AllDatabases_ReportCoverage()
        {
            var report = new IndexCoverageReport();

            // Test SQLite
            TestContext.WriteLine("=== SQLite Index Analysis ===");
            var sqliteConnectionString = $"Data Source={_sqlitePath}";
            using (var provider = new SQLiteDatabaseProvider(sqliteConnectionString))
            {
                var sqliteIndexes = AnalyzeDatabaseIndexes("SQLite", provider);
                report.AddDatabase("SQLite", sqliteIndexes);
            }

            // Test DuckDB
            TestContext.WriteLine("\n=== DuckDB Index Analysis ===");
            var duckdbConnectionString = $"Data Source={_duckdbPath}";
            using (var provider = new DuckDBDatabaseProvider(duckdbConnectionString))
            {
                var duckdbIndexes = AnalyzeDatabaseIndexes("DuckDB", provider);
                report.AddDatabase("DuckDB", duckdbIndexes);
            }

            // Test PostgreSQL (if available)
            var postgresqlConnectionString = Environment.GetEnvironmentVariable("JVLINK_TEST_POSTGRESQL_CONNECTION");
            if (!string.IsNullOrEmpty(postgresqlConnectionString))
            {
                TestContext.WriteLine("\n=== PostgreSQL Index Analysis ===");
                try
                {
                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JVLINK_DB_PASSWORD")))
                    {
                        Environment.SetEnvironmentVariable("JVLINK_DB_PASSWORD", "testpass");
                    }

                    using (var provider = new PostgreSQLDatabaseProvider(postgresqlConnectionString))
                    {
                        var postgresIndexes = AnalyzeDatabaseIndexes("PostgreSQL", provider);
                        report.AddDatabase("PostgreSQL", postgresIndexes);
                    }
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine($"PostgreSQL analysis skipped: {ex.Message}");
                }
            }

            // Generate and display report
            TestContext.WriteLine("\n" + report.GenerateReport());

            // Save report to file
            var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "INDEX_COVERAGE_REPORT.md");
            File.WriteAllText(reportPath, report.GenerateDetailedReport());
            TestContext.WriteLine($"\nDetailed report saved to: {reportPath}");
        }

        private List<IndexInfo> AnalyzeDatabaseIndexes(string dbName, IDatabaseProvider provider)
        {
            var indexes = new List<IndexInfo>();

            using (var conn = provider.CreateConnection())
            {
                conn.Open();
                var generator = provider.GetSqlGenerator();
                var quote = generator.IdentifierQuoteChar;

                // Create a sample JVLink table structure for testing
                var tableName = "JV_RA_RACE_TEST";
                var createTableSql = $@"CREATE TABLE {quote}{tableName}{quote} (
                    {quote}id{quote} INTEGER PRIMARY KEY,
                    {quote}race_id{quote} TEXT NOT NULL,
                    {quote}race_name{quote} TEXT,
                    {quote}race_date{quote} TEXT,
                    {quote}distance{quote} INTEGER,
                    {quote}prize_money{quote} REAL
                )";

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = createTableSql;
                    cmd.ExecuteNonQuery();
                }

                // Get index information
                indexes = GetIndexesForTable(conn, tableName, dbName);

                TestContext.WriteLine($"\n{dbName} - Table: {tableName}");
                TestContext.WriteLine($"  Total indexes found: {indexes.Count}");
                foreach (var idx in indexes)
                {
                    TestContext.WriteLine($"    - {idx.IndexName} on {idx.ColumnName} ({idx.IndexType})");
                }
            }

            return indexes;
        }

        private List<IndexInfo> GetIndexesForTable(IDbConnection conn, string tableName, string dbType)
        {
            var indexes = new List<IndexInfo>();

            using (var cmd = conn.CreateCommand())
            {
                if (dbType == "SQLite")
                {
                    // SQLite: Get indexes from sqlite_master (including auto-created PRIMARY KEY indexes)
                    cmd.CommandText = $"SELECT name, sql FROM sqlite_master WHERE type='index' AND tbl_name='{tableName}'";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var indexName = reader.GetString(0);
                            var sql = reader.IsDBNull(1) ? null : reader.GetString(1);

                            // sqlite_autoindex_* are automatically created for PRIMARY KEY
                            var isPrimaryKey = indexName.StartsWith("sqlite_autoindex");
                            var columnName = ExtractColumnFromIndexSql(sql ?? "");

                            // If no SQL (auto-index), extract from table info
                            if (string.IsNullOrEmpty(sql) && isPrimaryKey)
                            {
                                columnName = "id (auto)";
                            }

                            indexes.Add(new IndexInfo
                            {
                                IndexName = indexName,
                                TableName = tableName,
                                ColumnName = columnName,
                                IndexType = (sql != null && sql.Contains("UNIQUE")) ? "UNIQUE" : (isPrimaryKey ? "PRIMARY KEY" : "INDEX"),
                                IsPrimaryKey = isPrimaryKey
                            });
                        }
                    }
                }
                else if (dbType == "DuckDB")
                {
                    // DuckDB: Query information schema for indexes
                    try
                    {
                        cmd.CommandText = $@"
                            SELECT
                                index_name,
                                is_unique,
                                is_primary
                            FROM duckdb_indexes()
                            WHERE table_name = '{tableName}'";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var indexName = reader.GetString(0);
                                var isUnique = reader.GetBoolean(1);
                                var isPrimary = reader.GetBoolean(2);

                                indexes.Add(new IndexInfo
                                {
                                    IndexName = indexName,
                                    TableName = tableName,
                                    ColumnName = "auto",
                                    IndexType = isUnique ? "UNIQUE" : "INDEX",
                                    IsPrimaryKey = isPrimary
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // DuckDB may not have index support or different query syntax
                        TestContext.WriteLine($"DuckDB index query failed: {ex.Message}");
                    }
                }
                else if (dbType == "PostgreSQL")
                {
                    // PostgreSQL: Query pg_indexes
                    cmd.CommandText = $@"
                        SELECT
                            indexname,
                            indexdef
                        FROM pg_indexes
                        WHERE tablename = '{tableName.ToLower()}'";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var indexName = reader.GetString(0);
                            var indexDef = reader.GetString(1);

                            indexes.Add(new IndexInfo
                            {
                                IndexName = indexName,
                                TableName = tableName,
                                ColumnName = ExtractColumnFromIndexSql(indexDef),
                                IndexType = indexDef.Contains("UNIQUE") ? "UNIQUE" : "INDEX",
                                IsPrimaryKey = indexName.EndsWith("_pkey")
                            });
                        }
                    }
                }
            }

            return indexes;
        }

        private string ExtractColumnFromIndexSql(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                return "unknown";

            // Extract column name from CREATE INDEX statement
            // Example: "CREATE INDEX idx_name ON table (column)"
            var startIdx = sql.IndexOf("(");
            var endIdx = sql.IndexOf(")");

            if (startIdx >= 0 && endIdx > startIdx)
            {
                return sql.Substring(startIdx + 1, endIdx - startIdx - 1).Trim();
            }

            return "unknown";
        }
    }

    public class IndexInfo
    {
        public string IndexName { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string IndexType { get; set; }
        public bool IsPrimaryKey { get; set; }
    }

    public class IndexCoverageReport
    {
        private Dictionary<string, List<IndexInfo>> _databaseIndexes = new Dictionary<string, List<IndexInfo>>();

        public void AddDatabase(string dbName, List<IndexInfo> indexes)
        {
            _databaseIndexes[dbName] = indexes;
        }

        public string GenerateReport()
        {
            var report = "=== Index Coverage Summary ===\n\n";

            foreach (var db in _databaseIndexes)
            {
                report += $"{db.Key}:\n";
                report += $"  Total indexes: {db.Value.Count}\n";
                report += $"  Primary keys: {db.Value.Count(_ => _.IsPrimaryKey)}\n";
                report += $"  Additional indexes: {db.Value.Count(_ => !_.IsPrimaryKey)}\n\n";
            }

            return report;
        }

        public string GenerateDetailedReport()
        {
            var report = "# Index Coverage Analysis Report\n\n";
            report += "**Generated:** " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n\n";
            report += "## Summary\n\n";

            // Summary table
            report += "| Database | Total Indexes | Primary Keys | Additional Indexes | Coverage |\n";
            report += "|----------|---------------|--------------|-------------------|----------|\n";

            foreach (var db in _databaseIndexes)
            {
                var total = db.Value.Count;
                var pk = db.Value.Count(_ => _.IsPrimaryKey);
                var additional = total - pk;
                var coverage = additional > 0 ? "Partial" : "Basic (PK only)";

                report += $"| {db.Key} | {total} | {pk} | {additional} | {coverage} |\n";
            }

            report += "\n## Detailed Analysis\n\n";

            foreach (var db in _databaseIndexes)
            {
                report += $"### {db.Key}\n\n";

                if (db.Value.Count == 0)
                {
                    report += "No indexes found.\n\n";
                    continue;
                }

                report += "| Index Name | Table | Column | Type | Is PK |\n";
                report += "|------------|-------|--------|------|-------|\n";

                foreach (var idx in db.Value)
                {
                    report += $"| {idx.IndexName} | {idx.TableName} | {idx.ColumnName} | {idx.IndexType} | {(idx.IsPrimaryKey ? "Yes" : "No")} |\n";
                }

                report += "\n";
            }

            // Recommendations
            report += "## Recommendations\n\n";
            report += "### Missing Indexes\n\n";
            report += "Based on JVLink table structure analysis, the following indexes are recommended for performance:\n\n";
            report += "1. **race_id**: Frequently used for lookups and joins\n";
            report += "2. **race_date**: Used for date range queries\n";
            report += "3. **distance**: Used for filtering by distance\n";
            report += "4. **Composite indexes**: (race_date, distance) for complex queries\n\n";

            report += "### Index Implementation Status\n\n";
            report += "❌ **Current Status**: Only PRIMARY KEY indexes are automatically created.\n\n";
            report += "✅ **Recommended**: Implement automatic secondary index creation for:\n";
            report += "- Foreign key columns (race_id, horse_id, jockey_code, etc.)\n";
            report += "- Date columns (race_date, birth_date, etc.)\n";
            report += "- Frequently filtered columns (distance, prize_money, etc.)\n\n";

            return report;
        }
    }
}
