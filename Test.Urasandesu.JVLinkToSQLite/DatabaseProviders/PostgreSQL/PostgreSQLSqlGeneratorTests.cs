using NUnit.Framework;
using System;
using Urasandesu.JVLinkToSQLite.DatabaseProviders.PostgreSQL;

namespace Test.Urasandesu.JVLinkToSQLite.DatabaseProviders.PostgreSQL
{
    /// <summary>
    /// Test-First tests for PostgreSQLSqlGenerator.
    /// Constitution Principle III: NON-NEGOTIABLE - Tests written BEFORE implementation.
    /// </summary>
    [TestFixture]
    public class PostgreSQLSqlGeneratorTests
    {
        private PostgreSQLSqlGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _generator = new PostgreSQLSqlGenerator();
        }

        #region Type Mapping Tests

        [Test]
        public void MapCSharpTypeToSqlType_String_ReturnsText()
        {
            // Act
            var sqlType = _generator.MapCSharpTypeToSqlType(typeof(string));

            // Assert
            Assert.That(sqlType, Is.EqualTo("TEXT"));
        }

        [Test]
        public void MapCSharpTypeToSqlType_Int_ReturnsInteger()
        {
            // Act
            var sqlType = _generator.MapCSharpTypeToSqlType(typeof(int));

            // Assert
            Assert.That(sqlType, Is.EqualTo("INTEGER"));
        }

        [Test]
        public void MapCSharpTypeToSqlType_Long_ReturnsBigInt()
        {
            // Act
            var sqlType = _generator.MapCSharpTypeToSqlType(typeof(long));

            // Assert
            Assert.That(sqlType, Is.EqualTo("BIGINT"));
        }

        [Test]
        public void MapCSharpTypeToSqlType_Short_ReturnsSmallInt()
        {
            // Act
            var sqlType = _generator.MapCSharpTypeToSqlType(typeof(short));

            // Assert
            Assert.That(sqlType, Is.EqualTo("SMALLINT"));
        }

        [Test]
        public void MapCSharpTypeToSqlType_Byte_ReturnsSmallInt()
        {
            // Act
            var sqlType = _generator.MapCSharpTypeToSqlType(typeof(byte));

            // Assert
            // PostgreSQL doesn't have TINYINT, uses SMALLINT instead
            Assert.That(sqlType, Is.EqualTo("SMALLINT"));
        }

        [Test]
        public void MapCSharpTypeToSqlType_Decimal_ReturnsNumeric()
        {
            // Act
            var sqlType = _generator.MapCSharpTypeToSqlType(typeof(decimal));

            // Assert
            Assert.That(sqlType, Is.EqualTo("NUMERIC(18,6)"));
        }

        [Test]
        public void MapCSharpTypeToSqlType_Double_ReturnsDoublePrecision()
        {
            // Act
            var sqlType = _generator.MapCSharpTypeToSqlType(typeof(double));

            // Assert
            Assert.That(sqlType, Is.EqualTo("DOUBLE PRECISION"));
        }

        [Test]
        public void MapCSharpTypeToSqlType_Float_ReturnsReal()
        {
            // Act
            var sqlType = _generator.MapCSharpTypeToSqlType(typeof(float));

            // Assert
            Assert.That(sqlType, Is.EqualTo("REAL"));
        }

        [Test]
        public void MapCSharpTypeToSqlType_DateTime_ReturnsTimestamp()
        {
            // Act
            var sqlType = _generator.MapCSharpTypeToSqlType(typeof(DateTime));

            // Assert
            Assert.That(sqlType, Is.EqualTo("TIMESTAMP"));
        }

        [Test]
        public void MapCSharpTypeToSqlType_Bool_ReturnsBoolean()
        {
            // Act
            var sqlType = _generator.MapCSharpTypeToSqlType(typeof(bool));

            // Assert
            Assert.That(sqlType, Is.EqualTo("BOOLEAN"));
        }

        [Test]
        public void MapCSharpTypeToSqlType_ByteArray_ReturnsBytea()
        {
            // Act
            var sqlType = _generator.MapCSharpTypeToSqlType(typeof(byte[]));

            // Assert
            Assert.That(sqlType, Is.EqualTo("BYTEA"));
        }

        [Test]
        public void MapCSharpTypeToSqlType_UnsupportedType_ThrowsNotSupportedException()
        {
            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                _generator.MapCSharpTypeToSqlType(typeof(Guid)));
        }

        #endregion

        #region Identifier Quoting Tests

        [Test]
        public void QuoteIdentifier_SimpleIdentifier_ReturnsDoubleQuoted()
        {
            // Act
            var quoted = _generator.QuoteIdentifier("table_name");

            // Assert
            Assert.That(quoted, Is.EqualTo("\"table_name\""));
        }

        [Test]
        public void QuoteIdentifier_WithSpaces_ReturnsDoubleQuoted()
        {
            // Act
            var quoted = _generator.QuoteIdentifier("my table");

            // Assert
            Assert.That(quoted, Is.EqualTo("\"my table\""));
        }

        [Test]
        public void QuoteIdentifier_WithDoubleQuotes_EscapesQuotes()
        {
            // Act
            var quoted = _generator.QuoteIdentifier("my\"table");

            // Assert
            Assert.That(quoted, Is.EqualTo("\"my\"\"table\""));
        }

        #endregion

        #region Parameter Name Tests

        [Test]
        public void GetParameterName_ReturnsAtPrefixedName()
        {
            // Act
            var paramName = _generator.GetParameterName("column1");

            // Assert
            Assert.That(paramName, Is.EqualTo("@column1"));
        }

        #endregion

        #region DDL Generation Tests

        [Test]
        public void GenerateCreateTableDdl_SimpleClass_ReturnsValidDdl()
        {
            // Arrange
            var testType = typeof(TestEntity);

            // Act
            var ddl = _generator.GenerateCreateTableDdl(testType);

            // Assert
            Assert.That(ddl, Does.Contain("CREATE TABLE"));
            Assert.That(ddl, Does.Contain("\"TestEntity\""));
            Assert.That(ddl, Does.Contain("\"Id\" INTEGER"));
            Assert.That(ddl, Does.Contain("\"Name\" TEXT"));
        }

        [Test]
        public void GenerateInsertSql_SimpleClass_ReturnsValidInsert()
        {
            // Arrange
            var testType = typeof(TestEntity);

            // Act
            var sql = _generator.GenerateInsertSql(testType);

            // Assert
            Assert.That(sql, Does.Contain("INSERT INTO"));
            Assert.That(sql, Does.Contain("\"TestEntity\""));
            Assert.That(sql, Does.Contain("@Id"));
            Assert.That(sql, Does.Contain("@Name"));
        }

        [Test]
        public void MapCSharpTypeToSqlType_NullableInt_ReturnsInteger()
        {
            // Act
            var sqlType = _generator.MapCSharpTypeToSqlType(typeof(int?));

            // Assert
            Assert.That(sqlType, Is.EqualTo("INTEGER"));
        }

        #endregion

        #region Test Helper Classes

        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        #endregion
    }
}
