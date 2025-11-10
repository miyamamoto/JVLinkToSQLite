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
using System.Data.Common;

namespace Urasandesu.JVLinkToSQLite.DatabaseProviders
{
    /// <summary>
    /// Creates and configures database connections for a specific database provider.
    /// </summary>
    /// <remarks>
    /// This interface handles connection string parsing, validation, and
    /// database-specific configuration (e.g., PostgreSQL password injection).
    ///
    /// Constitution Compliance:
    /// - Principle V (Security): Validates connection strings and handles passwords securely
    /// - Principle VII (Observability): Provides clear validation error messages
    /// </remarks>
    public interface IConnectionFactory
    {
        /// <summary>
        /// Gets the type of database this connection factory supports.
        /// </summary>
        DatabaseType DatabaseType { get; }

        /// <summary>
        /// Creates a new database connection from the specified connection string.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <returns>A new <see cref="DbConnection"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when connectionString format is invalid.</exception>
        /// <exception cref="ConnectionException">Thrown when connection creation fails.</exception>
        /// <remarks>
        /// Connection string formats:
        /// - SQLite: "Data Source=race.db" or ":memory:"
        /// - DuckDB: "Data Source=race.duckdb" or ":memory:"
        /// - PostgreSQL: "Server=localhost;Port=5432;Database=jvdata;Username=user"
        ///               (Password read from JVLINK_DB_PASSWORD environment variable)
        ///
        /// The returned connection is not opened.
        /// </remarks>
        DbConnection CreateConnection(string connectionString);

        /// <summary>
        /// Parses the connection string into a structured builder object.
        /// </summary>
        /// <param name="connectionString">The connection string to parse.</param>
        /// <returns>A <see cref="DbConnectionStringBuilder"/> with parsed values.</returns>
        /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when connectionString format is invalid.</exception>
        /// <remarks>
        /// For PostgreSQL, this method injects the password from the JVLINK_DB_PASSWORD
        /// environment variable if not already present in the connection string.
        /// </remarks>
        DbConnectionStringBuilder ParseConnectionString(string connectionString);

        /// <summary>
        /// Validates the connection string format and required parameters.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>A <see cref="ValidationResult"/> indicating success or failure.</returns>
        /// <remarks>
        /// Validation checks:
        /// - SQLite/DuckDB:
        ///   * Valid file path or ":memory:"
        ///   * Directory exists (for file paths)
        ///   * No path traversal attempts (..)
        ///
        /// - PostgreSQL:
        ///   * Contains Server/Host parameter
        ///   * Contains Database parameter
        ///   * Contains Username/User Id parameter
        ///   * JVLINK_DB_PASSWORD environment variable is set
        ///   * Port is numeric (if specified)
        /// </remarks>
        ValidationResult ValidateConnectionString(string connectionString);

        /// <summary>
        /// Applies database-specific configuration settings to the connection.
        /// </summary>
        /// <param name="connection">The database connection to configure.</param>
        /// <exception cref="ArgumentNullException">Thrown when connection is null.</exception>
        /// <remarks>
        /// Configuration examples:
        /// - SQLite: PRAGMA settings for performance (synchronous=OFF, journal_mode=WAL)
        /// - DuckDB: No special configuration needed
        /// - PostgreSQL: Connection pooling (already configured in connection string)
        /// </remarks>
        void ApplyConnectionSettings(DbConnection connection);
    }

    /// <summary>
    /// Represents the result of a connection string validation.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets a value indicating whether the validation succeeded.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets the error message if validation failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets additional validation details (e.g., which parameter is missing).
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// Creates a failed validation result with an error message.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="details">Additional details (optional).</param>
        public static ValidationResult Failure(string errorMessage, string details = null)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage,
                Details = details
            };
        }
    }
}
