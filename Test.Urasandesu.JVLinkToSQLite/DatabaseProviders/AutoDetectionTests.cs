using NUnit.Framework;
using System;
using Urasandesu.JVLinkToSQLite.DatabaseProviders;

namespace Test.Urasandesu.JVLinkToSQLite.DatabaseProviders
{
    /// <summary>
    /// Test-First tests for database type auto-detection.
    /// Constitution Principle III: NON-NEGOTIABLE - Tests written BEFORE implementation.
    ///
    /// Phase 5 - T054: Comprehensive auto-detection testing
    /// </summary>
    [TestFixture]
    public class AutoDetectionTests
    {
        #region SQLite Detection Tests

        [Test]
        public void DetectDatabaseType_WithDbExtension_ReturnsSQLite()
        {
            // Arrange
            var connectionString = "race.db";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.SQLite));
        }

        [Test]
        public void DetectDatabaseType_WithSqliteExtension_ReturnsSQLite()
        {
            // Arrange
            var connectionString = "race.sqlite";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.SQLite));
        }

        [Test]
        public void DetectDatabaseType_WithFullPathDb_ReturnsSQLite()
        {
            // Arrange
            var connectionString = @"C:\Data\JVLink\race.db";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.SQLite));
        }

        [Test]
        public void DetectDatabaseType_WithDataSourceParameter_ReturnsSQLite()
        {
            // Arrange
            var connectionString = "Data Source=race.db";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.SQLite));
        }

        [Test]
        public void DetectDatabaseType_WithMemoryDatabase_ReturnsSQLite()
        {
            // Arrange
            var connectionString = ":memory:";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.SQLite));
        }

        #endregion

        #region DuckDB Detection Tests

        [Test]
        public void DetectDatabaseType_WithDuckDbExtension_ReturnsDuckDB()
        {
            // Arrange
            var connectionString = "analytics.duckdb";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.DuckDB));
        }

        [Test]
        public void DetectDatabaseType_WithFullPathDuckDb_ReturnsDuckDB()
        {
            // Arrange
            var connectionString = @"D:\Analytics\data.duckdb";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.DuckDB));
        }

        [Test]
        public void DetectDatabaseType_WithDataSourceDuckDb_ReturnsDuckDB()
        {
            // Arrange
            var connectionString = "Data Source=analytics.duckdb";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.DuckDB));
        }

        #endregion

        #region PostgreSQL Detection Tests

        [Test]
        public void DetectDatabaseType_WithHostParameter_ReturnsPostgreSQL()
        {
            // Arrange
            var connectionString = "Host=localhost;Database=jvlink;Username=postgres";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.PostgreSQL));
        }

        [Test]
        public void DetectDatabaseType_WithServerParameter_ReturnsPostgreSQL()
        {
            // Arrange
            var connectionString = "Server=localhost;Database=jvlink;Username=postgres";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.PostgreSQL));
        }

        [Test]
        public void DetectDatabaseType_WithHostAndPort_ReturnsPostgreSQL()
        {
            // Arrange
            var connectionString = "Host=192.168.1.100;Port=5432;Database=jvlink";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.PostgreSQL));
        }

        [Test]
        public void DetectDatabaseType_WithHostCaseInsensitive_ReturnsPostgreSQL()
        {
            // Arrange
            var connectionString = "host=localhost;database=jvlink";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.PostgreSQL));
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void DetectDatabaseType_WithNullConnectionString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                DatabaseProviderFactory.DetectDatabaseType(null));
        }

        [Test]
        public void DetectDatabaseType_WithEmptyConnectionString_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                DatabaseProviderFactory.DetectDatabaseType(""));
        }

        [Test]
        public void DetectDatabaseType_WithWhitespaceConnectionString_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                DatabaseProviderFactory.DetectDatabaseType("   "));
        }

        [Test]
        public void DetectDatabaseType_WithUnrecognizedExtension_ThrowsArgumentException()
        {
            // Arrange
            var connectionString = "data.txt";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                DatabaseProviderFactory.DetectDatabaseType(connectionString));

            // Verify error message is helpful
            Assert.That(ex.Message, Does.Contain("Unable to detect"));
            Assert.That(ex.Message, Does.Contain(".db"));
            Assert.That(ex.Message, Does.Contain(".sqlite"));
            Assert.That(ex.Message, Does.Contain(".duckdb"));
        }

        [Test]
        public void DetectDatabaseType_WithUnrecognizedFormat_ThrowsArgumentExceptionWithHelpfulMessage()
        {
            // Arrange
            var connectionString = "invalid_format_string";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                DatabaseProviderFactory.DetectDatabaseType(connectionString));

            // Verify error message provides guidance
            Assert.That(ex.Message, Does.Contain("Unable to detect") | Does.Contain("unrecognized"));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void DetectDatabaseType_WithMixedCaseExtension_ReturnsSQLite()
        {
            // Arrange
            var connectionString = "race.DB";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.SQLite));
        }

        [Test]
        public void DetectDatabaseType_WithSpacesInPath_ReturnsSQLite()
        {
            // Arrange
            var connectionString = @"C:\My Data\race data.db";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.SQLite));
        }

        [Test]
        public void DetectDatabaseType_WithRelativePath_ReturnsSQLite()
        {
            // Arrange
            var connectionString = @"..\data\race.db";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.SQLite));
        }

        [Test]
        public void DetectDatabaseType_WithPostgreSQLAndWhitespace_ReturnsPostgreSQL()
        {
            // Arrange
            var connectionString = "Host = localhost ; Database = jvlink";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.PostgreSQL));
        }

        #endregion

        #region Priority/Precedence Tests

        [Test]
        public void DetectDatabaseType_PostgreSQLTakesPrecedenceOverFileExtension()
        {
            // Arrange - If someone has a file called "Host=localhost.db" (weird but possible)
            // PostgreSQL pattern should take precedence
            var connectionString = "Host=localhost;Database=mydb.db";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.PostgreSQL),
                "PostgreSQL connection string pattern should take precedence over file extension");
        }

        #endregion

        #region CreateFromConnectionString Integration

        [Test]
        public void CreateFromConnectionString_WithSqliteFile_ReturnsSQLiteProvider()
        {
            // Arrange
            var connectionString = "test.db";

            // Act
            var provider = DatabaseProviderFactory.CreateFromConnectionString(connectionString);

            // Assert
            Assert.That(provider, Is.Not.Null);
            Assert.That(provider.DatabaseType, Is.EqualTo(DatabaseType.SQLite));
        }

        [Test]
        public void CreateFromConnectionString_WithDuckDbFile_ReturnsDuckDBProvider()
        {
            // Arrange
            var connectionString = "test.duckdb";

            // Act
            var provider = DatabaseProviderFactory.CreateFromConnectionString(connectionString);

            // Assert
            Assert.That(provider, Is.Not.Null);
            Assert.That(provider.DatabaseType, Is.EqualTo(DatabaseType.DuckDB));
        }

        [Test]
        public void CreateFromConnectionString_WithPostgreSQLString_ReturnsPostgreSQLProvider()
        {
            // Arrange
            var connectionString = "Host=localhost;Database=test;Username=user";

            // Act
            var provider = DatabaseProviderFactory.CreateFromConnectionString(connectionString);

            // Assert
            Assert.That(provider, Is.Not.Null);
            Assert.That(provider.DatabaseType, Is.EqualTo(DatabaseType.PostgreSQL));
        }

        #endregion
    }
}
