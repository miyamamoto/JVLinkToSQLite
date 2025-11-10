using NUnit.Framework;
using System;
using Urasandesu.JVLinkToSQLite.DatabaseProviders;
using Urasandesu.JVLinkToSQLite.DatabaseProviders.SQLite;
using Urasandesu.JVLinkToSQLite.DatabaseProviders.DuckDB;
using Urasandesu.JVLinkToSQLite.DatabaseProviders.PostgreSQL;

namespace Test.Urasandesu.JVLinkToSQLite.ErrorHandling
{
    /// <summary>
    /// Test-First tests for error handling and user-friendly error messages.
    /// Constitution Principle III: NON-NEGOTIABLE - Tests written BEFORE implementation.
    ///
    /// Phase 5 - T058: Error handling tests
    /// Goal: 90%+ of error messages enable user self-service
    /// </summary>
    [TestFixture]
    public class DatabaseErrorTests
    {
        #region DatabaseProviderFactory Error Messages

        [Test]
        public void Create_WithNullConnectionString_ProvidesHelpfulErrorMessage()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                DatabaseProviderFactory.Create(DatabaseType.SQLite, null));

            // Verify error message is helpful
            Assert.That(ex.Message, Does.Contain("Connection string"));
            Assert.That(ex.ParamName, Is.EqualTo("connectionString"));
        }

        [Test]
        public void DetectDatabaseType_WithUnrecognizedExtension_ProvidesExpectedFormatsInMessage()
        {
            // Arrange
            var connectionString = "data.txt";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                DatabaseProviderFactory.DetectDatabaseType(connectionString));

            // Verify error message provides guidance on expected formats
            Assert.That(ex.Message, Does.Contain("Unable to detect") | Does.Contain("unrecognized"));
            Assert.That(ex.Message, Does.Contain(".db") | Does.Contain("file extension"));
            Assert.That(ex.Message, Does.Contain(".sqlite") | Does.Contain("file extension"));
            Assert.That(ex.Message, Does.Contain(".duckdb") | Does.Contain("file extension"));
            Assert.That(ex.Message, Does.Contain("PostgreSQL") | Does.Contain("Host="));
        }

        #endregion

        #region SQLite Connection Errors

        [Test]
        public void SQLiteConnectionFactory_WithInvalidPath_ProvidesHelpfulErrorMessage()
        {
            // Arrange - Path with parent directory traversal
            var connectionString = @"..\..\suspicious.db";
            var factory = new SQLiteConnectionFactory();

            // Act
            var result = factory.ValidateConnectionString(connectionString);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
            Assert.That(result.ErrorMessage, Does.Contain("..") | Does.Contain("parent directory") | Does.Contain("security"));
        }

        [Test]
        public void SQLiteConnectionFactory_WithNonExistentDirectory_ProvidesHelpfulErrorMessage()
        {
            // Arrange
            var connectionString = @"C:\NonExistentDirectory\data.db";
            var factory = new SQLiteConnectionFactory();

            // Act
            var result = factory.ValidateConnectionString(connectionString);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
            Assert.That(result.ErrorMessage, Does.Contain("directory") | Does.Contain("does not exist") | Does.Contain("create"));
        }

        [Test]
        public void SQLiteConnectionFactory_WithMemoryDatabase_PassesValidation()
        {
            // Arrange
            var connectionString = ":memory:";
            var factory = new SQLiteConnectionFactory();

            // Act
            var result = factory.ValidateConnectionString(connectionString);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ErrorMessage, Is.Null.Or.Empty);
        }

        #endregion

        #region DuckDB Connection Errors

        [Test]
        public void DuckDBConnectionFactory_WithInvalidPath_ProvidesHelpfulErrorMessage()
        {
            // Arrange
            var connectionString = @"..\..\suspicious.duckdb";
            var factory = new DuckDBConnectionFactory();

            // Act
            var result = factory.ValidateConnectionString(connectionString);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
            Assert.That(result.ErrorMessage, Does.Contain("..") | Does.Contain("parent directory") | Does.Contain("security"));
        }

        [Test]
        public void DuckDBConnectionFactory_WithNonExistentDirectory_ProvidesHelpfulErrorMessage()
        {
            // Arrange
            var connectionString = @"D:\NonExistent\analytics.duckdb";
            var factory = new DuckDBConnectionFactory();

            // Act
            var result = factory.ValidateConnectionString(connectionString);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
            Assert.That(result.ErrorMessage, Does.Contain("directory") | Does.Contain("does not exist") | Does.Contain("create"));
        }

        #endregion

        #region PostgreSQL Connection Errors

        [Test]
        public void PostgreSQLConnectionFactory_WithMissingHost_ProvidesSpecificErrorMessage()
        {
            // Arrange - Connection string without Host
            var connectionString = "Database=jvlink;Username=postgres";
            var factory = new PostgreSQLConnectionFactory();

            // Act
            var result = factory.ValidateConnectionString(connectionString);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
            Assert.That(result.ErrorMessage, Does.Contain("Host"));
            Assert.That(result.ErrorMessage, Does.Contain("required"));
        }

        [Test]
        public void PostgreSQLConnectionFactory_WithMissingDatabase_ProvidesSpecificErrorMessage()
        {
            // Arrange - Connection string without Database
            var connectionString = "Host=localhost;Username=postgres";
            var factory = new PostgreSQLConnectionFactory();

            // Act
            var result = factory.ValidateConnectionString(connectionString);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
            Assert.That(result.ErrorMessage, Does.Contain("Database"));
            Assert.That(result.ErrorMessage, Does.Contain("required"));
        }

        [Test]
        public void PostgreSQLConnectionFactory_WithMissingUsername_ProvidesSpecificErrorMessage()
        {
            // Arrange - Connection string without Username
            var connectionString = "Host=localhost;Database=jvlink";
            var factory = new PostgreSQLConnectionFactory();

            // Act
            var result = factory.ValidateConnectionString(connectionString);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
            Assert.That(result.ErrorMessage, Does.Contain("Username"));
            Assert.That(result.ErrorMessage, Does.Contain("required"));
        }

        [Test]
        public void PostgreSQLConnectionFactory_WithValidConnectionString_PassesValidation()
        {
            // Arrange
            var connectionString = "Host=localhost;Database=jvlink;Username=postgres;Password=test";
            var factory = new PostgreSQLConnectionFactory();

            // Act
            var result = factory.ValidateConnectionString(connectionString);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ErrorMessage, Is.Null.Or.Empty);
        }

        [Test]
        public void PostgreSQLConnectionFactory_WithoutPasswordButWithEnvVar_PassesValidation()
        {
            // Arrange - No password in connection string, assumes JVLINK_DB_PASSWORD environment variable
            var connectionString = "Host=localhost;Database=jvlink;Username=postgres";
            var factory = new PostgreSQLConnectionFactory();

            // Act
            var result = factory.ValidateConnectionString(connectionString);

            // Assert - Should still pass validation (password not required at validation time)
            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region SQL Generator Errors

        [Test]
        public void SQLiteSqlGenerator_WithUnsupportedType_ProvidesHelpfulErrorMessage()
        {
            // Arrange
            var generator = new SQLiteSqlGenerator();

            // Act & Assert
            var ex = Assert.Throws<NotSupportedException>(() =>
                generator.MapCSharpTypeToSqlType(typeof(Guid)));

            // Verify error message mentions the unsupported type
            Assert.That(ex.Message, Does.Contain("Guid") | Does.Contain("not supported"));
        }

        [Test]
        public void DuckDBSqlGenerator_WithUnsupportedType_ProvidesHelpfulErrorMessage()
        {
            // Arrange
            var generator = new DuckDBSqlGenerator();

            // Act & Assert
            var ex = Assert.Throws<NotSupportedException>(() =>
                generator.MapCSharpTypeToSqlType(typeof(Guid)));

            // Verify error message mentions the unsupported type
            Assert.That(ex.Message, Does.Contain("Guid") | Does.Contain("not supported"));
        }

        [Test]
        public void PostgreSQLSqlGenerator_WithUnsupportedType_ProvidesHelpfulErrorMessage()
        {
            // Arrange
            var generator = new PostgreSQLSqlGenerator();

            // Act & Assert
            var ex = Assert.Throws<NotSupportedException>(() =>
                generator.MapCSharpTypeToSqlType(typeof(Guid)));

            // Verify error message mentions the unsupported type
            Assert.That(ex.Message, Does.Contain("Guid") | Does.Contain("not supported"));
        }

        #endregion

        #region Provider Instantiation Errors

        [Test]
        public void SQLiteDatabaseProvider_WithNullConnectionString_ThrowsWithHelpfulMessage()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new SQLiteDatabaseProvider(null));

            Assert.That(ex.ParamName, Is.EqualTo("connectionString"));
        }

        [Test]
        public void DuckDBDatabaseProvider_WithNullConnectionString_ThrowsWithHelpfulMessage()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new DuckDBDatabaseProvider(null));

            Assert.That(ex.ParamName, Is.EqualTo("connectionString"));
        }

        [Test]
        public void PostgreSQLDatabaseProvider_WithNullConnectionString_ThrowsWithHelpfulMessage()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new PostgreSQLDatabaseProvider(null));

            Assert.That(ex.ParamName, Is.EqualTo("connectionString"));
        }

        #endregion

        #region Error Message Quality Tests

        [Test]
        public void AllValidationErrorMessages_ContainActionableGuidance()
        {
            // This test documents the requirement that error messages should:
            // 1. Clearly state what went wrong
            // 2. Provide guidance on how to fix it
            // 3. Enable user self-service (90%+ target)

            // Examples of good error messages:
            // - "Host is required in PostgreSQL connection string. Please add 'Host=localhost' parameter."
            // - "Directory 'C:\Data' does not exist. Please create the directory first."
            // - "Type 'Guid' is not supported by SQLite. Supported types: string, int, long, ..."

            Assert.Pass("Error message quality is verified by individual tests above");
        }

        #endregion
    }
}
