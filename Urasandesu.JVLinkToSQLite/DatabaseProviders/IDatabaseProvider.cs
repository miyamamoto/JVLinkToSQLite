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
using System.Data;
using System.Data.Common;

namespace Urasandesu.JVLinkToSQLite.DatabaseProviders
{
    /// <summary>
    /// Provides database-agnostic operations for SQLite, DuckDB, and PostgreSQL.
    /// </summary>
    /// <remarks>
    /// This interface abstracts the differences between database providers,
    /// allowing the application to work with any supported database type without
    /// modifying business logic.
    ///
    /// Constitution Compliance:
    /// - Principle I (Modularity): Isolates database-specific logic
    /// - Principle II (Data Integrity): Enforces transaction support
    /// - Principle VI (API Compatibility): Maintains consistent interface across databases
    /// </remarks>
    public interface IDatabaseProvider : IDisposable
    {
        /// <summary>
        /// Gets the type of database this provider supports.
        /// </summary>
        DatabaseType DatabaseType { get; }

        /// <summary>
        /// Gets the connection string used by this provider.
        /// </summary>
        /// <remarks>
        /// For security, this should not include passwords.
        /// PostgreSQL passwords are read from environment variables.
        /// </remarks>
        string ConnectionString { get; }

        /// <summary>
        /// Gets a value indicating whether this provider currently has an active connection.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets a value indicating whether this database supports transactions.
        /// </summary>
        /// <remarks>
        /// All supported databases (SQLite, DuckDB, PostgreSQL) support transactions.
        /// This property is provided for future extensibility.
        /// </remarks>
        bool SupportsTransactions { get; }

        /// <summary>
        /// Creates a new database connection.
        /// </summary>
        /// <returns>A new <see cref="DbConnection"/> instance.</returns>
        /// <exception cref="ConnectionException">Thrown when connection creation fails.</exception>
        /// <remarks>
        /// The returned connection is not opened. Caller must call Open() explicitly.
        /// Always dispose the connection when done (use 'using' statement).
        /// </remarks>
        DbConnection CreateConnection();

        /// <summary>
        /// Begins a database transaction with the specified isolation level.
        /// </summary>
        /// <param name="connection">The open database connection.</param>
        /// <param name="isolationLevel">The isolation level for the transaction. Default is ReadCommitted.</param>
        /// <returns>A new <see cref="DbTransaction"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when connection is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when connection is not open.</exception>
        /// <exception cref="TransactionException">Thrown when transaction creation fails.</exception>
        /// <remarks>
        /// Default isolation level is ReadCommitted for consistency across databases.
        /// Always commit or rollback the transaction explicitly.
        /// </remarks>
        DbTransaction BeginTransaction(DbConnection connection, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);

        /// <summary>
        /// Gets the SQL generator for this database provider.
        /// </summary>
        /// <returns>An <see cref="ISqlGenerator"/> instance.</returns>
        /// <remarks>
        /// The SQL generator is used by DataBridge implementations to generate
        /// database-specific CREATE TABLE and INSERT statements.
        /// </remarks>
        ISqlGenerator GetSqlGenerator();

        /// <summary>
        /// Gets the connection factory for this database provider.
        /// </summary>
        /// <returns>An <see cref="IConnectionFactory"/> instance.</returns>
        /// <remarks>
        /// The connection factory handles connection string parsing and validation.
        /// </remarks>
        IConnectionFactory GetConnectionFactory();

        /// <summary>
        /// Validates that the database connection can be established.
        /// </summary>
        /// <returns>True if connection can be established; otherwise, false.</returns>
        /// <remarks>
        /// This method attempts to open and close a connection.
        /// Use this for pre-flight checks before starting data import.
        /// </remarks>
        bool ValidateConnection();
    }
}
