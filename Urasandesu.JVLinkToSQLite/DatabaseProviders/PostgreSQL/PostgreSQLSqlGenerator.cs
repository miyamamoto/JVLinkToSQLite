using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Urasandesu.JVLinkToSQLite.DatabaseProviders.PostgreSQL
{
    /// <summary>
    /// PostgreSQL SQL generator.
    /// Generates PostgreSQL-specific SQL statements with proper type mappings.
    /// </summary>
    public class PostgreSQLSqlGenerator : ISqlGenerator
    {
        /// <summary>
        /// Gets the database type for PostgreSQL.
        /// </summary>
        public DatabaseType DatabaseType => DatabaseType.PostgreSQL;

        /// <summary>
        /// Gets the identifier quote character (double quote for PostgreSQL).
        /// </summary>
        public string IdentifierQuoteChar => "\"";

        /// <summary>
        /// Gets the parameter prefix (@ for all databases).
        /// </summary>
        public string ParameterPrefix => "@";

        private static readonly Dictionary<Type, string> _typeMappings = new Dictionary<Type, string>
        {
            { typeof(string), "TEXT" },
            { typeof(int), "INTEGER" },
            { typeof(long), "BIGINT" },
            { typeof(short), "SMALLINT" },
            { typeof(byte), "SMALLINT" },  // PostgreSQL doesn't have TINYINT
            { typeof(decimal), "NUMERIC(18,6)" },
            { typeof(double), "DOUBLE PRECISION" },
            { typeof(float), "REAL" },
            { typeof(DateTime), "TIMESTAMP" },
            { typeof(bool), "BOOLEAN" },
            { typeof(byte[]), "BYTEA" }
        };

        /// <summary>
        /// Maps a C# type to PostgreSQL SQL type.
        /// </summary>
        public string MapCSharpTypeToSqlType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (_typeMappings.TryGetValue(underlyingType, out var sqlType))
            {
                return sqlType;
            }

            throw new NotSupportedException($"Type {type.Name} is not supported by PostgreSQL SQL generator.");
        }

        /// <summary>
        /// Quotes an identifier using PostgreSQL double-quote convention.
        /// </summary>
        public string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentException("Identifier cannot be null or empty.", nameof(identifier));

            // Escape internal double quotes by doubling them
            var escaped = identifier.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        /// <summary>
        /// Returns parameter name with @ prefix (PostgreSQL supports @ or :, using @ for consistency).
        /// </summary>
        public string GetParameterName(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

            return $"@{columnName}";
        }

        /// <summary>
        /// Generates CREATE TABLE DDL for the given type.
        /// </summary>
        public string GenerateCreateTableDdl(Type entityType)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));

            var tableName = QuoteIdentifier(entityType.Name);
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var columns = properties
                .Select(p =>
                {
                    var columnName = QuoteIdentifier(p.Name);
                    var sqlType = MapCSharpTypeToSqlType(p.PropertyType);
                    return $"    {columnName} {sqlType}";
                });

            var ddl = new StringBuilder();
            ddl.AppendLine($"CREATE TABLE IF NOT EXISTS {tableName} (");
            ddl.AppendLine(string.Join(",\n", columns));
            ddl.AppendLine(");");

            return ddl.ToString();
        }

        /// <summary>
        /// Generates INSERT statement for the given type.
        /// </summary>
        public string GenerateInsertSql(Type entityType)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));

            var tableName = QuoteIdentifier(entityType.Name);
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var columnNames = properties.Select(p => QuoteIdentifier(p.Name));
            var parameterNames = properties.Select(p => GetParameterName(p.Name));

            var sql = $"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) " +
                      $"VALUES ({string.Join(", ", parameterNames)});";

            return sql;
        }

        /// <summary>
        /// Generates UPDATE statement for the given type.
        /// </summary>
        public string GenerateUpdateSql(Type entityType, string whereClause)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));
            if (string.IsNullOrWhiteSpace(whereClause))
                throw new ArgumentException("WHERE clause cannot be null or empty.", nameof(whereClause));

            var tableName = QuoteIdentifier(entityType.Name);
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var setClause = string.Join(", ",
                properties.Select(p => $"{QuoteIdentifier(p.Name)} = {GetParameterName(p.Name)}"));

            return $"UPDATE {tableName} SET {setClause} WHERE {whereClause};";
        }

        /// <summary>
        /// Generates SELECT statement for the given type.
        /// </summary>
        public string GenerateSelectSql(Type entityType, string whereClause = null)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));

            var tableName = QuoteIdentifier(entityType.Name);
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var columnNames = properties.Select(p => QuoteIdentifier(p.Name));

            var sql = $"SELECT {string.Join(", ", columnNames)} FROM {tableName}";

            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sql += $" WHERE {whereClause}";
            }

            return sql + ";";
        }

        /// <summary>
        /// Generates DELETE statement for the given type.
        /// </summary>
        public string GenerateDeleteSql(Type entityType, string whereClause)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));
            if (string.IsNullOrWhiteSpace(whereClause))
                throw new ArgumentException("WHERE clause cannot be null or empty.", nameof(whereClause));

            var tableName = QuoteIdentifier(entityType.Name);
            return $"DELETE FROM {tableName} WHERE {whereClause};";
        }
    }
}
