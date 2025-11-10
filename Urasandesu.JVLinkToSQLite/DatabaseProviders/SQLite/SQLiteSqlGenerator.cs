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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Urasandesu.JVLinkToSQLite.DatabaseProviders.SQLite
{
    /// <summary>
    /// Generates SQLite-specific SQL statements.
    /// </summary>
    public class SQLiteSqlGenerator : ISqlGenerator
    {
        private static readonly Dictionary<Type, string> _typeMappings = new Dictionary<Type, string>
        {
            { typeof(string), "TEXT" },
            { typeof(int), "INTEGER" },
            { typeof(long), "INTEGER" },
            { typeof(short), "INTEGER" },
            { typeof(byte), "INTEGER" },
            { typeof(decimal), "REAL" },
            { typeof(double), "REAL" },
            { typeof(float), "REAL" },
            { typeof(DateTime), "TEXT" },
            { typeof(bool), "INTEGER" },
            { typeof(byte[]), "BLOB" }
        };

        public DatabaseType DatabaseType => DatabaseType.SQLite;

        public string IdentifierQuoteChar => "\"";

        public string ParameterPrefix => "@";

        public string MapCSharpTypeToSqlType(Type csharpType)
        {
            if (csharpType == null)
            {
                throw new ArgumentNullException(nameof(csharpType), "Type cannot be null.");
            }

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(csharpType) ?? csharpType;

            if (_typeMappings.TryGetValue(underlyingType, out var sqlType))
            {
                return sqlType;
            }

            throw new NotSupportedException($"Type '{csharpType.FullName}' is not supported for SQLite mapping.");
        }

        public string QuoteIdentifier(string identifier)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier), "Identifier cannot be null.");
            }

            if (identifier == string.Empty)
            {
                throw new ArgumentException("Identifier cannot be empty.", nameof(identifier));
            }

            return $"\"{identifier}\"";
        }

        public string GetParameterName(string fieldName)
        {
            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName), "Field name cannot be null.");
            }

            if (fieldName == string.Empty)
            {
                throw new ArgumentException("Field name cannot be empty.", nameof(fieldName));
            }

            return $"@{fieldName}";
        }

        public string GenerateCreateTableDdl(Type jvDataType)
        {
            if (jvDataType == null)
            {
                throw new ArgumentNullException(nameof(jvDataType), "JV-Data type cannot be null.");
            }

            var tableName = QuoteIdentifier(jvDataType.Name);
            var properties = jvDataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var columns = new List<string>();
            foreach (var property in properties)
            {
                var columnName = QuoteIdentifier(property.Name);
                var sqlType = MapCSharpTypeToSqlType(property.PropertyType);
                columns.Add($"{columnName} {sqlType}");
            }

            var columnDefinitions = string.Join(", ", columns);
            return $"CREATE TABLE IF NOT EXISTS {tableName} ({columnDefinitions})";
        }

        public string GenerateInsertSql(Type jvDataType)
        {
            if (jvDataType == null)
            {
                throw new ArgumentNullException(nameof(jvDataType), "JV-Data type cannot be null.");
            }

            var tableName = QuoteIdentifier(jvDataType.Name);
            var properties = jvDataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var columnNames = properties.Select(p => QuoteIdentifier(p.Name));
            var parameterNames = properties.Select(p => GetParameterName(p.Name));

            var columnsClause = string.Join(", ", columnNames);
            var valuesClause = string.Join(", ", parameterNames);

            return $"INSERT INTO {tableName} ({columnsClause}) VALUES ({valuesClause})";
        }
    }
}
