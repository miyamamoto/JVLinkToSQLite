using System;
using System.Data;
using System.Data.Common;

namespace Urasandesu.JVLinkToSQLite.DatabaseProviders.PostgreSQL
{
    /// <summary>
    /// PostgreSQL database provider.
    /// Implements database operations for PostgreSQL using Npgsql.
    /// </summary>
    public class PostgreSQLDatabaseProvider : IDatabaseProvider
    {
        private readonly PostgreSQLConnectionFactory _connectionFactory;
        private readonly PostgreSQLSqlGenerator _sqlGenerator;
        private bool _disposed = false;

        public DatabaseType DatabaseType => DatabaseType.PostgreSQL;
        public string ConnectionString { get; }
        public bool SupportsTransactions => true;
        public bool IsConnected { get; private set; }

        public PostgreSQLDatabaseProvider(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null.");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));

            ConnectionString = connectionString;
            _connectionFactory = new PostgreSQLConnectionFactory(connectionString);
            _sqlGenerator = new PostgreSQLSqlGenerator();
        }

        /// <summary>
        /// Creates a new PostgreSQL connection.
        /// </summary>
        public DbConnection CreateConnection()
        {
            var connection = _connectionFactory.CreateConnection(ConnectionString);
            IsConnected = true;
            return connection;
        }

        /// <summary>
        /// Begins a new transaction with the specified isolation level.
        /// </summary>
        public DbTransaction BeginTransaction(DbConnection connection, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            return connection.BeginTransaction(isolationLevel);
        }

        /// <summary>
        /// Validates the connection to the database.
        /// </summary>
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

        /// <summary>
        /// Gets the SQL generator for PostgreSQL.
        /// </summary>
        public ISqlGenerator GetSqlGenerator()
        {
            return _sqlGenerator;
        }

        /// <summary>
        /// Gets the connection factory for PostgreSQL.
        /// </summary>
        public IConnectionFactory GetConnectionFactory()
        {
            return _connectionFactory;
        }

        /// <summary>
        /// Executes a non-query SQL command.
        /// </summary>
        public int ExecuteNonQuery(string sql, DbConnection connection, DbTransaction transaction = null)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL cannot be null or empty.", nameof(sql));
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Transaction = transaction;

                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Executes a scalar SQL command and returns the result.
        /// </summary>
        public object ExecuteScalar(string sql, DbConnection connection, DbTransaction transaction = null)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL cannot be null or empty.", nameof(sql));
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Transaction = transaction;

                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                return command.ExecuteScalar();
            }
        }

        /// <summary>
        /// Tests the connection to verify it works.
        /// </summary>
        public bool TestConnection(out string errorMessage)
        {
            errorMessage = null;

            try
            {
                using (var connection = CreateConnection())
                {
                    connection.Open();

                    // Execute a simple query to verify connection
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT 1;";
                        var result = command.ExecuteScalar();

                        if (result == null || !result.Equals(1))
                        {
                            errorMessage = "Connection test query returned unexpected result.";
                            return false;
                        }
                    }

                    IsConnected = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to connect to PostgreSQL: {ex.Message}";
                IsConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Disposes the provider and releases resources.
        /// </summary>
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
                    // Dispose managed resources
                    IsConnected = false;
                }

                _disposed = true;
            }
        }
    }
}
