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
using System.IO;
using System.Text.RegularExpressions;
using Urasandesu.JVLinkToSQLite.DatabaseProviders.SQLite;
using Urasandesu.JVLinkToSQLite.DatabaseProviders.DuckDB;
using Urasandesu.JVLinkToSQLite.DatabaseProviders.PostgreSQL;

namespace Urasandesu.JVLinkToSQLite.DatabaseProviders
{
    /// <summary>
    /// Factory for creating database provider instances based on database type and connection string.
    /// </summary>
    /// <remarks>
    /// Constitution Compliance:
    /// - Principle I (Modularity): Centralizes provider creation logic
    /// - Principle V (Security): Validates connection strings before creating providers
    /// </remarks>
    public static class DatabaseProviderFactory
    {
        /// <summary>
        /// Creates a database provider instance for the specified database type.
        /// </summary>
        /// <param name="databaseType">The type of database.</param>
        /// <param name="connectionString">The database connection string.</param>
        /// <returns>An <see cref="IDatabaseProvider"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when databaseType is invalid.</exception>
        public static IDatabaseProvider Create(DatabaseType databaseType, string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
            }

            switch (databaseType)
            {
                case DatabaseType.SQLite:
                    return new SQLiteDatabaseProvider(connectionString);

                case DatabaseType.DuckDB:
                    return new DuckDBDatabaseProvider(connectionString);

                case DatabaseType.PostgreSQL:
                    return new PostgreSQLDatabaseProvider(connectionString);

                default:
                    throw new ArgumentException($"Unsupported database type: {databaseType}", nameof(databaseType));
            }
        }

        /// <summary>
        /// Automatically detects the database type from the connection string format.
        /// </summary>
        /// <param name="connectionString">The connection string to analyze.</param>
        /// <returns>The detected <see cref="DatabaseType"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when database type cannot be detected.</exception>
        /// <remarks>
        /// Detection rules:
        /// - ".db" or ".sqlite" extension → SQLite
        /// - ".duckdb" extension → DuckDB
        /// - Contains "Host=" or "Server=" → PostgreSQL
        /// </remarks>
        public static DatabaseType DetectDatabaseType(string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));
            }

            // Check for special SQLite in-memory database
            if (connectionString.Trim().Equals(":memory:", StringComparison.OrdinalIgnoreCase))
            {
                return DatabaseType.SQLite;
            }

            // Check for PostgreSQL connection string format (contains Host= or Server=)
            if (Regex.IsMatch(connectionString, @"(Host|Server)\s*=", RegexOptions.IgnoreCase))
            {
                return DatabaseType.PostgreSQL;
            }

            // Check file extension
            var extension = Path.GetExtension(connectionString).ToLowerInvariant();

            switch (extension)
            {
                case ".db":
                case ".sqlite":
                    return DatabaseType.SQLite;

                case ".duckdb":
                    return DatabaseType.DuckDB;

                default:
                    // Check if it might be a Data Source style connection string
                    if (connectionString.Contains("Data Source=") || connectionString.Contains("DataSource="))
                    {
                        // Extract the data source value and check its extension
                        var match = Regex.Match(connectionString, @"Data\s*Source\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            var dataSource = match.Groups[1].Value.Trim();
                            var dsExtension = Path.GetExtension(dataSource).ToLowerInvariant();

                            switch (dsExtension)
                            {
                                case ".db":
                                case ".sqlite":
                                    return DatabaseType.SQLite;
                                case ".duckdb":
                                    return DatabaseType.DuckDB;
                            }
                        }
                    }

                    throw new ArgumentException(
                        $"Unable to detect database type from connection string: '{connectionString}'. " +
                        "Expected: '.db', '.sqlite', '.duckdb' file extension, or PostgreSQL connection string (Host=... or Server=...).",
                        nameof(connectionString));
            }
        }

        /// <summary>
        /// Creates a database provider by auto-detecting the type from the connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>An <see cref="IDatabaseProvider"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when connectionString is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when database type cannot be detected.</exception>
        public static IDatabaseProvider CreateFromConnectionString(string connectionString)
        {
            var databaseType = DetectDatabaseType(connectionString);
            return Create(databaseType, connectionString);
        }
    }
}
