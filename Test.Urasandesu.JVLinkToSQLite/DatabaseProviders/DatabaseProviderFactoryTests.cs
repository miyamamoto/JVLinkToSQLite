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
using Urasandesu.JVLinkToSQLite.DatabaseProviders;

namespace Test.Urasandesu.JVLinkToSQLite.DatabaseProviders
{
    [TestFixture]
    public class DatabaseProviderFactoryTests
    {
        [Test]
        [Category("Unit")]
        [Category("DatabaseProvider")]
        public void DetectDatabaseType_WithSqliteDbExtension_ReturnsSQLite()
        {
            // Arrange
            var connectionString = "race.db";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.SQLite));
        }

        [Test]
        [Category("Unit")]
        [Category("DatabaseProvider")]
        public void DetectDatabaseType_WithSqliteExtension_ReturnsSQLite()
        {
            // Arrange
            var connectionString = "data.sqlite";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.SQLite));
        }

        [Test]
        [Category("Unit")]
        [Category("DatabaseProvider")]
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
        [Category("Unit")]
        [Category("DatabaseProvider")]
        public void DetectDatabaseType_WithHostParameter_ReturnsPostgreSQL()
        {
            // Arrange
            var connectionString = "Host=localhost;Database=jvdata;Username=test";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.PostgreSQL));
        }

        [Test]
        [Category("Unit")]
        [Category("DatabaseProvider")]
        public void DetectDatabaseType_WithServerParameter_ReturnsPostgreSQL()
        {
            // Arrange
            var connectionString = "Server=localhost;Database=jvdata;Username=test";

            // Act
            var result = DatabaseProviderFactory.DetectDatabaseType(connectionString);

            // Assert
            Assert.That(result, Is.EqualTo(DatabaseType.PostgreSQL));
        }

        [Test]
        [Category("Unit")]
        [Category("DatabaseProvider")]
        public void DetectDatabaseType_WithUnrecognizedFormat_ThrowsArgumentException()
        {
            // Arrange
            var connectionString = "unknown.xyz";

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                DatabaseProviderFactory.DetectDatabaseType(connectionString));
        }

        [Test]
        [Category("Unit")]
        [Category("DatabaseProvider")]
        public void DetectDatabaseType_WithNullConnectionString_ThrowsArgumentNullException()
        {
            // Arrange
            string connectionString = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                DatabaseProviderFactory.DetectDatabaseType(connectionString));
        }

        [Test]
        [Category("Unit")]
        [Category("DatabaseProvider")]
        public void DetectDatabaseType_WithEmptyConnectionString_ThrowsArgumentException()
        {
            // Arrange
            var connectionString = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                DatabaseProviderFactory.DetectDatabaseType(connectionString));
        }

        [Test]
        [Category("Unit")]
        [Category("DatabaseProvider")]
        public void Create_WithSQLiteType_ReturnsSQLiteProvider()
        {
            // Arrange
            var dbType = DatabaseType.SQLite;
            var connectionString = "Data Source=test.db";

            // Act
            using (var provider = DatabaseProviderFactory.Create(dbType, connectionString))
            {
                // Assert
                Assert.That(provider, Is.Not.Null);
                Assert.That(provider.DatabaseType, Is.EqualTo(DatabaseType.SQLite));
            }
        }

        [Test]
        [Category("Unit")]
        [Category("DatabaseProvider")]
        public void Create_WithDuckDBType_ReturnsDuckDBProvider()
        {
            // Arrange
            var dbType = DatabaseType.DuckDB;
            var connectionString = "Data Source=test.duckdb";

            // Act
            using (var provider = DatabaseProviderFactory.Create(dbType, connectionString))
            {
                // Assert
                Assert.That(provider, Is.Not.Null);
                Assert.That(provider.DatabaseType, Is.EqualTo(DatabaseType.DuckDB));
            }
        }

        [Test]
        [Category("Unit")]
        [Category("DatabaseProvider")]
        public void Create_WithPostgreSQLType_ReturnsPostgreSQLProvider()
        {
            // Arrange
            var dbType = DatabaseType.PostgreSQL;
            var connectionString = "Host=localhost;Database=test;Username=user";
            Environment.SetEnvironmentVariable("JVLINK_DB_PASSWORD", "testpass");

            try
            {
                // Act
                using (var provider = DatabaseProviderFactory.Create(dbType, connectionString))
                {
                    // Assert
                    Assert.That(provider, Is.Not.Null);
                    Assert.That(provider.DatabaseType, Is.EqualTo(DatabaseType.PostgreSQL));
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("JVLINK_DB_PASSWORD", null);
            }
        }

        [Test]
        [Category("Unit")]
        [Category("DatabaseProvider")]
        public void Create_WithInvalidDatabaseType_ThrowsArgumentException()
        {
            // Arrange
            var dbType = (DatabaseType)999;
            var connectionString = "test.db";

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                DatabaseProviderFactory.Create(dbType, connectionString));
        }

        [Test]
        [Category("Unit")]
        [Category("DatabaseProvider")]
        public void Create_WithNullConnectionString_ThrowsArgumentNullException()
        {
            // Arrange
            var dbType = DatabaseType.SQLite;
            string connectionString = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                DatabaseProviderFactory.Create(dbType, connectionString));
        }
    }
}
