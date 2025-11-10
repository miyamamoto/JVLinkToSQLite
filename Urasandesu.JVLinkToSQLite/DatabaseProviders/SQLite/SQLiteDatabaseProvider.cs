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

namespace Urasandesu.JVLinkToSQLite.DatabaseProviders.SQLite
{
    /// <summary>
    /// Provides SQLite database operations.
    /// </summary>
    public class SQLiteDatabaseProvider : IDatabaseProvider
    {
        private readonly string _connectionString;
        private readonly SQLiteConnectionFactory _connectionFactory;
        private readonly SQLiteSqlGenerator _sqlGenerator;
        private bool _disposed;

        public SQLiteDatabaseProvider(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
            }

            _connectionString = connectionString;
            _connectionFactory = new SQLiteConnectionFactory();
            _sqlGenerator = new SQLiteSqlGenerator();
        }

        public DatabaseType DatabaseType => DatabaseType.SQLite;

        public string ConnectionString => _connectionString;

        public bool IsConnected => false; // SQLite doesn't maintain persistent connections

        public bool SupportsTransactions => true;

        public DbConnection CreateConnection()
        {
            return _connectionFactory.CreateConnection(_connectionString);
        }

        public DbTransaction BeginTransaction(DbConnection connection, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection), "Connection cannot be null.");
            }

            if (connection.State != ConnectionState.Open)
            {
                throw new InvalidOperationException("Connection must be open before beginning a transaction.");
            }

            return connection.BeginTransaction(isolationLevel);
        }

        public ISqlGenerator GetSqlGenerator()
        {
            return _sqlGenerator;
        }

        public IConnectionFactory GetConnectionFactory()
        {
            return _connectionFactory;
        }

        public bool ValidateConnection()
        {
            try
            {
                using (var connection = CreateConnection())
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // SQLite doesn't require explicit cleanup for the provider itself
                    // Connections are managed by callers
                }

                _disposed = true;
            }
        }
    }
}
