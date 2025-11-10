// JVLinkToSQLite は、JRA-VAN データラボが提供する競馬データを SQLite データベースに変換するツールです。
//
// Copyright (C) 2023 Akira Sugiura
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using NUnit.Framework;
using System;
using System.Data;
using System.IO;
using Urasandesu.JVLinkToSQLite.DatabaseProviders;
using Urasandesu.JVLinkToSQLite.DatabaseProviders.DuckDB;

namespace Test.Urasandesu.JVLinkToSQLite.DatabaseProviders.DuckDB
{
    [TestFixture]
    public class DuckDBDatabaseProviderTests
    {
        private string _testDbPath;

        [SetUp]
        public void SetUp()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.duckdb");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_testDbPath))
            {
                try
                {
                    File.Delete(_testDbPath);
                }
                catch
                {
                    // DuckDB may have file lock, ignore
                }
            }
        }

        [Test]
        [Category("Unit")]
        [Category("DuckDB")]
        public void Constructor_WithValidConnectionString_CreatesProvider()
        {
            // Arrange
            var connectionString = $"Data Source={_testDbPath}";

            // Act
            using (var provider = new DuckDBDatabaseProvider(connectionString))
            {
                // Assert
                Assert.That(provider, Is.Not.Null);
                Assert.That(provider.DatabaseType, Is.EqualTo(DatabaseType.DuckDB));
                Assert.That(provider.ConnectionString, Is.EqualTo(connectionString));
            }
        }

        [Test]
        [Category("Unit")]
        [Category("DuckDB")]
        public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
        {
            // Arrange
            string connectionString = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DuckDBDatabaseProvider(connectionString));
        }

        [Test]
        [Category("Unit")]
        [Category("DuckDB")]
        public void DatabaseType_ReturnsCorrectType()
        {
            // Arrange
            var connectionString = $"Data Source={_testDbPath}";

            // Act
            using (var provider = new DuckDBDatabaseProvider(connectionString))
            {
                // Assert
                Assert.That(provider.DatabaseType, Is.EqualTo(DatabaseType.DuckDB));
            }
        }

        [Test]
        [Category("Unit")]
        [Category("DuckDB")]
        public void SupportsTransactions_ReturnsTrue()
        {
            // Arrange
            var connectionString = $"Data Source={_testDbPath}";

            // Act
            using (var provider = new DuckDBDatabaseProvider(connectionString))
            {
                // Assert
                Assert.That(provider.SupportsTransactions, Is.True);
            }
        }

        [Test]
        [Category("Unit")]
        [Category("DuckDB")]
        public void CreateConnection_ReturnsValidConnection()
        {
            // Arrange
            var connectionString = $"Data Source={_testDbPath}";

            // Act
            using (var provider = new DuckDBDatabaseProvider(connectionString))
            using (var connection = provider.CreateConnection())
            {
                // Assert
                Assert.That(connection, Is.Not.Null);
                Assert.That(connection.State, Is.EqualTo(ConnectionState.Closed));
            }
        }

        [Test]
        [Category("Unit")]
        [Category("DuckDB")]
        public void CreateConnection_WithMemoryDatabase_ReturnsValidConnection()
        {
            // Arrange
            var connectionString = "Data Source=:memory:";

            // Act
            using (var provider = new DuckDBDatabaseProvider(connectionString))
            using (var connection = provider.CreateConnection())
            {
                // Assert
                Assert.That(connection, Is.Not.Null);
                Assert.That(connection.State, Is.EqualTo(ConnectionState.Closed));
            }
        }

        [Test]
        [Category("Unit")]
        [Category("DuckDB")]
        public void BeginTransaction_WithOpenConnection_ReturnsTransaction()
        {
            // Arrange
            var connectionString = "Data Source=:memory:";

            // Act
            using (var provider = new DuckDBDatabaseProvider(connectionString))
            using (var connection = provider.CreateConnection())
            {
                connection.Open();
                using (var transaction = provider.BeginTransaction(connection))
                {
                    // Assert
                    Assert.That(transaction, Is.Not.Null);
                    Assert.That(transaction.Connection, Is.SameAs(connection));
                }
            }
        }

        [Test]
        [Category("Unit")]
        [Category("DuckDB")]
        public void BeginTransaction_WithIsolationLevel_ReturnsTransaction()
        {
            // Arrange
            var connectionString = "Data Source=:memory:";

            // Act
            using (var provider = new DuckDBDatabaseProvider(connectionString))
            using (var connection = provider.CreateConnection())
            {
                connection.Open();
                using (var transaction = provider.BeginTransaction(connection, IsolationLevel.ReadCommitted))
                {
                    // Assert
                    Assert.That(transaction, Is.Not.Null);
                }
            }
        }

        [Test]
        [Category("Unit")]
        [Category("DuckDB")]
        public void BeginTransaction_WithClosedConnection_ThrowsInvalidOperationException()
        {
            // Arrange
            var connectionString = "Data Source=:memory:";

            // Act & Assert
            using (var provider = new DuckDBDatabaseProvider(connectionString))
            using (var connection = provider.CreateConnection())
            {
                Assert.Throws<InvalidOperationException>(() =>
                    provider.BeginTransaction(connection));
            }
        }

        [Test]
        [Category("Unit")]
        [Category("DuckDB")]
        public void GetSqlGenerator_ReturnsDuckDBGenerator()
        {
            // Arrange
            var connectionString = $"Data Source={_testDbPath}";

            // Act
            using (var provider = new DuckDBDatabaseProvider(connectionString))
            {
                var generator = provider.GetSqlGenerator();

                // Assert
                Assert.That(generator, Is.Not.Null);
                Assert.That(generator, Is.InstanceOf<DuckDBSqlGenerator>());
                Assert.That(generator.DatabaseType, Is.EqualTo(DatabaseType.DuckDB));
            }
        }

        [Test]
        [Category("Unit")]
        [Category("DuckDB")]
        public void GetConnectionFactory_ReturnsDuckDBFactory()
        {
            // Arrange
            var connectionString = $"Data Source={_testDbPath}";

            // Act
            using (var provider = new DuckDBDatabaseProvider(connectionString))
            {
                var factory = provider.GetConnectionFactory();

                // Assert
                Assert.That(factory, Is.Not.Null);
                Assert.That(factory, Is.InstanceOf<DuckDBConnectionFactory>());
                Assert.That(factory.DatabaseType, Is.EqualTo(DatabaseType.DuckDB));
            }
        }

        [Test]
        [Category("Unit")]
        [Category("DuckDB")]
        public void ValidateConnection_WithValidConnectionString_ReturnsTrue()
        {
            // Arrange
            var connectionString = "Data Source=:memory:";

            // Act
            using (var provider = new DuckDBDatabaseProvider(connectionString))
            {
                var result = provider.ValidateConnection();

                // Assert
                Assert.That(result, Is.True);
            }
        }

        [Test]
        [Category("Unit")]
        [Category("DuckDB")]
        public void IsConnected_InitiallyReturnsFalse()
        {
            // Arrange
            var connectionString = "Data Source=:memory:";

            // Act
            using (var provider = new DuckDBDatabaseProvider(connectionString))
            {
                // Assert
                Assert.That(provider.IsConnected, Is.False);
            }
        }
    }
}
