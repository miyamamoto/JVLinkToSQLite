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

namespace Urasandesu.JVLinkToSQLite.DatabaseProviders
{
    /// <summary>
    /// Generates database-specific SQL statements for CREATE TABLE and INSERT operations.
    /// </summary>
    /// <remarks>
    /// This interface abstracts SQL dialect differences between SQLite, DuckDB, and PostgreSQL.
    /// Each database has different data types, quoting rules, and DDL syntax.
    ///
    /// Constitution Compliance:
    /// - Principle I (Modularity): Separates SQL generation from business logic
    /// - Principle II (Data Integrity): Ensures correct data type mapping
    /// - Principle V (Security): Prevents SQL injection via proper quoting and parameterization
    /// </remarks>
    public interface ISqlGenerator
    {
        /// <summary>
        /// Gets the type of database this SQL generator targets.
        /// </summary>
        DatabaseType DatabaseType { get; }

        /// <summary>
        /// Gets the character used to quote identifiers (table names, column names).
        /// </summary>
        /// <remarks>
        /// All supported databases use double quotes (") for identifier quoting.
        /// This ensures case-sensitivity is handled consistently.
        /// </remarks>
        string IdentifierQuoteChar { get; }

        /// <summary>
        /// Gets the prefix character for parameter placeholders.
        /// </summary>
        /// <remarks>
        /// All supported databases use @ for parameter names (e.g., @race_id).
        /// </remarks>
        string ParameterPrefix { get; }

        /// <summary>
        /// Generates a CREATE TABLE DDL statement for the specified JV-Data type.
        /// </summary>
        /// <param name="jvDataType">The type of JV-Data structure (e.g., typeof(JV_RA_RACE)).</param>
        /// <returns>A CREATE TABLE SQL statement.</returns>
        /// <exception cref="ArgumentNullException">Thrown when jvDataType is null.</exception>
        /// <exception cref="ArgumentException">Thrown when jvDataType is not a valid JV-Data type.</exception>
        /// <remarks>
        /// The generated SQL includes:
        /// - CREATE TABLE IF NOT EXISTS clause
        /// - Quoted table and column names
        /// - Database-specific data types
        /// - Primary key definition (if applicable)
        /// - NOT NULL constraints
        ///
        /// Example output (PostgreSQL):
        /// CREATE TABLE IF NOT EXISTS "JV_RA_RACE" (
        ///     "race_id" VARCHAR(50) NOT NULL,
        ///     "race_date" TIMESTAMP,
        ///     "race_name" VARCHAR(200)
        /// );
        /// </remarks>
        string GenerateCreateTableDdl(Type jvDataType);

        /// <summary>
        /// Generates an INSERT statement with parameter placeholders for the specified JV-Data type.
        /// </summary>
        /// <param name="jvDataType">The type of JV-Data structure (e.g., typeof(JV_RA_RACE)).</param>
        /// <returns>An INSERT SQL statement with parameter placeholders.</returns>
        /// <exception cref="ArgumentNullException">Thrown when jvDataType is null.</exception>
        /// <exception cref="ArgumentException">Thrown when jvDataType is not a valid JV-Data type.</exception>
        /// <remarks>
        /// The generated SQL includes:
        /// - INSERT INTO clause with quoted table name
        /// - Quoted column names
        /// - Parameter placeholders (@param_name)
        ///
        /// Example output:
        /// INSERT INTO "JV_RA_RACE" ("race_id", "race_date", "race_name")
        /// VALUES (@race_id, @race_date, @race_name);
        /// </remarks>
        string GenerateInsertSql(Type jvDataType);

        /// <summary>
        /// Maps a C# type to the corresponding SQL data type for this database.
        /// </summary>
        /// <param name="csharpType">The C# type (e.g., typeof(string), typeof(int), typeof(DateTime?)).</param>
        /// <returns>The SQL data type as a string (e.g., "VARCHAR(500)", "INTEGER", "TIMESTAMP").</returns>
        /// <exception cref="ArgumentNullException">Thrown when csharpType is null.</exception>
        /// <exception cref="NotSupportedException">Thrown when the type cannot be mapped to SQL.</exception>
        /// <remarks>
        /// Type mappings vary by database:
        /// - SQLite: TEXT, INTEGER, REAL, BLOB
        /// - DuckDB: VARCHAR, INTEGER, BIGINT, DECIMAL, TIMESTAMP, BOOLEAN, BLOB
        /// - PostgreSQL: VARCHAR(n), INTEGER, BIGINT, NUMERIC, TIMESTAMP, BOOLEAN, BYTEA
        ///
        /// Nullable types (e.g., int?) are handled by omitting the NOT NULL constraint.
        /// </remarks>
        string MapCSharpTypeToSqlType(Type csharpType);

        /// <summary>
        /// Quotes an identifier (table name or column name) to handle case sensitivity and reserved words.
        /// </summary>
        /// <param name="identifier">The unquoted identifier.</param>
        /// <returns>The quoted identifier (e.g., "JV_RA_RACE").</returns>
        /// <exception cref="ArgumentNullException">Thrown when identifier is null or empty.</exception>
        /// <remarks>
        /// All identifiers are quoted to ensure consistent case handling across databases:
        /// - SQLite: Case-insensitive by default
        /// - DuckDB: Case-sensitive identifiers, case-insensitive matching
        /// - PostgreSQL: Lowercases unquoted identifiers
        /// </remarks>
        string QuoteIdentifier(string identifier);

        /// <summary>
        /// Gets the parameter name with the appropriate prefix.
        /// </summary>
        /// <param name="fieldName">The field or column name.</param>
        /// <returns>The parameter name (e.g., @race_id).</returns>
        /// <exception cref="ArgumentNullException">Thrown when fieldName is null or empty.</exception>
        /// <remarks>
        /// All databases use the @ prefix for consistency with ADO.NET conventions.
        /// </remarks>
        string GetParameterName(string fieldName);
    }
}
