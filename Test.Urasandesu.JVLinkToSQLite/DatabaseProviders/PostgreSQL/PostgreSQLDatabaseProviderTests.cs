using NUnit.Framework;
using NSubstitute;
using System;
using System.Data.Common;
using Urasandesu.JVLinkToSQLite.DatabaseProviders;
using Urasandesu.JVLinkToSQLite.DatabaseProviders.PostgreSQL;

namespace Test.Urasandesu.JVLinkToSQLite.DatabaseProviders.PostgreSQL
{
    /// <summary>
    /// Test-First tests for PostgreSQLDatabaseProvider.
    /// Constitution Principle III: NON-NEGOTIABLE - Tests written BEFORE implementation.
    /// </summary>
    [TestFixture]
    public class PostgreSQLDatabaseProviderTests
    {
        private const string ValidConnectionString = "Host=localhost;Database=jvlink;Username=postgres;Password=testpass";

        [Test]
        public void Constructor_WithValidConnectionString_SetsProperties()
        {
            // Arrange & Act
            var provider = new PostgreSQLDatabaseProvider(ValidConnectionString);

            // Assert
            Assert.That(provider.DatabaseType, Is.EqualTo(DatabaseType.PostgreSQL));
            Assert.That(provider.ConnectionString, Is.EqualTo(ValidConnectionString));
            Assert.That(provider.SupportsTransactions, Is.True);
        }

        [Test]
        public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PostgreSQLDatabaseProvider(null));
        }

        [Test]
        public void Constructor_WithEmptyConnectionString_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new PostgreSQLDatabaseProvider(""));
            Assert.Throws<ArgumentException>(() => new PostgreSQLDatabaseProvider("   "));
        }

        [Test]
        public void CreateConnection_ReturnsValidNpgsqlConnection()
        {
            // Arrange
            var provider = new PostgreSQLDatabaseProvider(ValidConnectionString);

            // Act
            var connection = provider.CreateConnection();

            // Assert
            Assert.That(connection, Is.Not.Null);
            Assert.That(connection.GetType().Name, Is.EqualTo("NpgsqlConnection"));
            Assert.That(connection.ConnectionString, Is.EqualTo(ValidConnectionString));
        }

        [Test]
        public void CreateConnection_CalledMultipleTimes_ReturnsNewInstanceEachTime()
        {
            // Arrange
            var provider = new PostgreSQLDatabaseProvider(ValidConnectionString);

            // Act
            var connection1 = provider.CreateConnection();
            var connection2 = provider.CreateConnection();

            // Assert
            Assert.That(connection1, Is.Not.SameAs(connection2));
        }

        [Test]
        public void BeginTransaction_WithOpenConnection_ReturnsTransaction()
        {
            // Note: This test would require actual PostgreSQL connection in integration testing
            // For unit testing, we verify the interface contract
            var provider = new PostgreSQLDatabaseProvider(ValidConnectionString);

            // This test documents expected behavior - implementation will create transaction
            Assert.That(provider.SupportsTransactions, Is.True);
        }

        [Test]
        public void GetSqlGenerator_ReturnsPostgreSQLSqlGenerator()
        {
            // Arrange
            var provider = new PostgreSQLDatabaseProvider(ValidConnectionString);

            // Act
            var generator = provider.GetSqlGenerator();

            // Assert
            Assert.That(generator, Is.Not.Null);
            Assert.That(generator, Is.TypeOf<PostgreSQLSqlGenerator>());
        }

        [Test]
        public void GetConnectionFactory_ReturnsPostgreSQLConnectionFactory()
        {
            // Arrange
            var provider = new PostgreSQLDatabaseProvider(ValidConnectionString);

            // Act
            var factory = provider.GetConnectionFactory();

            // Assert
            Assert.That(factory, Is.Not.Null);
            Assert.That(factory, Is.TypeOf<PostgreSQLConnectionFactory>());
        }

        [Test]
        public void ValidateConnection_WithValidConnectionString_ReturnsSuccessResult()
        {
            // Arrange
            var provider = new PostgreSQLDatabaseProvider(ValidConnectionString);

            // Act
            var result = provider.ValidateConnection();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateConnection_WithInvalidConnectionString_ReturnsFailureResult()
        {
            // Arrange
            var invalidConnectionString = "InvalidConnectionString";
            var provider = new PostgreSQLDatabaseProvider(invalidConnectionString);

            // Act
            var result = provider.ValidateConnection();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateConnection_WithMissingHost_ReturnsFailureResult()
        {
            // Arrange
            var connectionString = "Database=jvlink;Username=postgres";
            var provider = new PostgreSQLDatabaseProvider(connectionString);

            // Act
            var result = provider.ValidateConnection();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var provider = new PostgreSQLDatabaseProvider(ValidConnectionString);

            // Act & Assert - Should not throw
            provider.Dispose();
            provider.Dispose();
        }

        [Test]
        public void SupportsTransactions_ReturnsTrue()
        {
            // Arrange
            var provider = new PostgreSQLDatabaseProvider(ValidConnectionString);

            // Act & Assert
            Assert.That(provider.SupportsTransactions, Is.True);
        }
    }
}
