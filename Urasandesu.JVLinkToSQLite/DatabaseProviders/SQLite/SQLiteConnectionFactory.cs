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
using System.Data.SQLite;
using System.IO;

namespace Urasandesu.JVLinkToSQLite.DatabaseProviders.SQLite
{
    /// <summary>
    /// Creates and configures SQLite database connections.
    /// </summary>
    public class SQLiteConnectionFactory : IConnectionFactory
    {
        public DatabaseType DatabaseType => DatabaseType.SQLite;

        public DbConnection CreateConnection(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
            }

            var validation = ValidateConnectionString(connectionString);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.ErrorMessage, nameof(connectionString));
            }

            return new SQLiteConnection(connectionString);
        }

        public DbConnectionStringBuilder ParseConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
            }

            var builder = new SQLiteConnectionStringBuilder(connectionString);
            return builder;
        }

        public ValidationResult ValidateConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return ValidationResult.Failure("Connection string cannot be null or empty.");
            }

            try
            {
                var builder = new SQLiteConnectionStringBuilder(connectionString);

                // Check for :memory: database (always valid)
                if (builder.DataSource == ":memory:")
                {
                    return ValidationResult.Success();
                }

                // Check for path traversal attempts
                if (builder.DataSource.Contains(".."))
                {
                    return ValidationResult.Failure(
                        "Connection string contains path traversal attempt (..).",
                        "Path traversal is not allowed for security reasons.");
                }

                // Check if directory exists (only if not :memory:)
                if (!string.IsNullOrEmpty(builder.DataSource) && builder.DataSource != ":memory:")
                {
                    var directory = Path.GetDirectoryName(builder.DataSource);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        return ValidationResult.Failure(
                            $"Directory does not exist: {directory}",
                            "Please create the directory before running the application.");
                    }
                }

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure(
                    "Invalid SQLite connection string format.",
                    ex.Message);
            }
        }

        public void ApplyConnectionSettings(DbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection), "Connection cannot be null.");
            }

            if (!(connection is SQLiteConnection sqliteConnection))
            {
                throw new ArgumentException("Connection must be a SQLiteConnection.", nameof(connection));
            }

            // SQLite-specific performance settings can be applied via PRAGMA statements
            // after the connection is opened. This method is a placeholder for future
            // optimizations like:
            // - PRAGMA synchronous = OFF
            // - PRAGMA journal_mode = WAL
            // - PRAGMA cache_size = 10000
            // These should be applied by the caller after opening the connection.
        }
    }
}
