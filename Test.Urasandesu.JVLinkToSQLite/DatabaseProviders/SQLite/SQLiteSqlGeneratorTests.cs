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
using Urasandesu.JVLinkToSQLite.DatabaseProviders.SQLite;

namespace Test.Urasandesu.JVLinkToSQLite.DatabaseProviders.SQLite
{
    [TestFixture]
    public class SQLiteSqlGeneratorTests
    {
        private SQLiteSqlGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _generator = new SQLiteSqlGenerator();
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void DatabaseType_ReturnsSQLite()
        {
            // Assert
            Assert.That(_generator.DatabaseType, Is.EqualTo(DatabaseType.SQLite));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void IdentifierQuoteChar_ReturnsDoubleQuote()
        {
            // Assert
            Assert.That(_generator.IdentifierQuoteChar, Is.EqualTo("\""));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void ParameterPrefix_ReturnsAtSign()
        {
            // Assert
            Assert.That(_generator.ParameterPrefix, Is.EqualTo("@"));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void MapCSharpTypeToSqlType_String_ReturnsText()
        {
            // Act
            var result = _generator.MapCSharpTypeToSqlType(typeof(string));

            // Assert
            Assert.That(result, Is.EqualTo("TEXT"));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void MapCSharpTypeToSqlType_Int_ReturnsInteger()
        {
            // Act
            var result = _generator.MapCSharpTypeToSqlType(typeof(int));

            // Assert
            Assert.That(result, Is.EqualTo("INTEGER"));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void MapCSharpTypeToSqlType_Long_ReturnsInteger()
        {
            // Act
            var result = _generator.MapCSharpTypeToSqlType(typeof(long));

            // Assert
            Assert.That(result, Is.EqualTo("INTEGER"));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void MapCSharpTypeToSqlType_Decimal_ReturnsReal()
        {
            // Act
            var result = _generator.MapCSharpTypeToSqlType(typeof(decimal));

            // Assert
            Assert.That(result, Is.EqualTo("REAL"));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void MapCSharpTypeToSqlType_Double_ReturnsReal()
        {
            // Act
            var result = _generator.MapCSharpTypeToSqlType(typeof(double));

            // Assert
            Assert.That(result, Is.EqualTo("REAL"));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void MapCSharpTypeToSqlType_DateTime_ReturnsText()
        {
            // Act
            var result = _generator.MapCSharpTypeToSqlType(typeof(DateTime));

            // Assert
            Assert.That(result, Is.EqualTo("TEXT"));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void MapCSharpTypeToSqlType_Bool_ReturnsInteger()
        {
            // Act
            var result = _generator.MapCSharpTypeToSqlType(typeof(bool));

            // Assert
            Assert.That(result, Is.EqualTo("INTEGER"));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void MapCSharpTypeToSqlType_ByteArray_ReturnsBlob()
        {
            // Act
            var result = _generator.MapCSharpTypeToSqlType(typeof(byte[]));

            // Assert
            Assert.That(result, Is.EqualTo("BLOB"));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void MapCSharpTypeToSqlType_NullableInt_ReturnsInteger()
        {
            // Act
            var result = _generator.MapCSharpTypeToSqlType(typeof(int?));

            // Assert
            Assert.That(result, Is.EqualTo("INTEGER"));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void MapCSharpTypeToSqlType_NullableDateTime_ReturnsText()
        {
            // Act
            var result = _generator.MapCSharpTypeToSqlType(typeof(DateTime?));

            // Assert
            Assert.That(result, Is.EqualTo("TEXT"));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void MapCSharpTypeToSqlType_NullType_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _generator.MapCSharpTypeToSqlType(null));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void QuoteIdentifier_ValidIdentifier_ReturnsQuoted()
        {
            // Act
            var result = _generator.QuoteIdentifier("table_name");

            // Assert
            Assert.That(result, Is.EqualTo("\"table_name\""));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void QuoteIdentifier_IdentifierWithUnderscore_ReturnsQuoted()
        {
            // Act
            var result = _generator.QuoteIdentifier("JV_RA_RACE");

            // Assert
            Assert.That(result, Is.EqualTo("\"JV_RA_RACE\""));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void QuoteIdentifier_NullIdentifier_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _generator.QuoteIdentifier(null));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void QuoteIdentifier_EmptyIdentifier_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _generator.QuoteIdentifier(""));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void GetParameterName_ValidFieldName_ReturnsParameterized()
        {
            // Act
            var result = _generator.GetParameterName("race_id");

            // Assert
            Assert.That(result, Is.EqualTo("@race_id"));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void GetParameterName_NullFieldName_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _generator.GetParameterName(null));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void GetParameterName_EmptyFieldName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _generator.GetParameterName(""));
        }

        // Simple test class for DDL generation
        private class TestTable
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void GenerateCreateTableDdl_ValidType_ReturnsValidDdl()
        {
            // Act
            var result = _generator.GenerateCreateTableDdl(typeof(TestTable));

            // Assert
            Assert.That(result, Does.Contain("CREATE TABLE IF NOT EXISTS"));
            Assert.That(result, Does.Contain("\"TestTable\""));
            Assert.That(result, Does.Contain("\"Id\""));
            Assert.That(result, Does.Contain("\"Name\""));
            Assert.That(result, Does.Contain("\"CreatedAt\""));
            Assert.That(result, Does.Contain("INTEGER"));
            Assert.That(result, Does.Contain("TEXT"));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void GenerateCreateTableDdl_NullType_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _generator.GenerateCreateTableDdl(null));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void GenerateInsertSql_ValidType_ReturnsValidSql()
        {
            // Act
            var result = _generator.GenerateInsertSql(typeof(TestTable));

            // Assert
            Assert.That(result, Does.Contain("INSERT INTO"));
            Assert.That(result, Does.Contain("\"TestTable\""));
            Assert.That(result, Does.Contain("\"Id\""));
            Assert.That(result, Does.Contain("\"Name\""));
            Assert.That(result, Does.Contain("\"CreatedAt\""));
            Assert.That(result, Does.Contain("@Id"));
            Assert.That(result, Does.Contain("@Name"));
            Assert.That(result, Does.Contain("@CreatedAt"));
            Assert.That(result, Does.Contain("VALUES"));
        }

        [Test]
        [Category("Unit")]
        [Category("SQLite")]
        public void GenerateInsertSql_NullType_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _generator.GenerateInsertSql(null));
        }
    }
}
