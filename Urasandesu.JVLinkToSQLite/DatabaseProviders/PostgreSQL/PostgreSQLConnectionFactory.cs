using System;
using System.Data.Common;
using Npgsql;

namespace Urasandesu.JVLinkToSQLite.DatabaseProviders.PostgreSQL
{
    /// <summary>
    /// PostgreSQL connection factory.
    /// Creates and validates Npgsql connections.
    /// </summary>
    public class PostgreSQLConnectionFactory : IConnectionFactory
    {
        private readonly string _defaultConnectionString;

        /// <summary>
        /// Initializes a new instance of PostgreSQLConnectionFactory.
        /// </summary>
        public PostgreSQLConnectionFactory()
        {
            _defaultConnectionString = null;
        }

        /// <summary>
        /// Initializes a new instance of PostgreSQLConnectionFactory with a default connection string.
        /// </summary>
        /// <param name="connectionString">The default connection string to use.</param>
        public PostgreSQLConnectionFactory(string connectionString)
        {
            _defaultConnectionString = connectionString;
        }

        /// <summary>
        /// Gets the database type for PostgreSQL.
        /// </summary>
        public DatabaseType DatabaseType => DatabaseType.PostgreSQL;

        /// <summary>
        /// Creates a new Npgsql connection.
        /// </summary>
        public DbConnection CreateConnection(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            // Support environment variable for password
            var processedConnectionString = ProcessConnectionString(connectionString);
            return new NpgsqlConnection(processedConnectionString);
        }

        /// <summary>
        /// Validates the PostgreSQL connection string.
        /// </summary>
        public ValidationResult ValidateConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return ValidationResult.Failure("Connection string cannot be null or empty.");

            try
            {
                var builder = new NpgsqlConnectionStringBuilder(connectionString);

                // Required parameters
                if (string.IsNullOrWhiteSpace(builder.Host))
                {
                    return ValidationResult.Failure(
                        "Host is required in PostgreSQL connection string.",
                        "PostgreSQL connection string must contain Server or Host parameter."
                    );
                }

                if (string.IsNullOrWhiteSpace(builder.Database))
                {
                    return ValidationResult.Failure(
                        "Database is required in PostgreSQL connection string.",
                        "PostgreSQL connection string must contain Database parameter."
                    );
                }

                // Username is typically required (unless using trust authentication)
                if (string.IsNullOrWhiteSpace(builder.Username))
                {
                    return ValidationResult.Failure(
                        "Username is required in PostgreSQL connection string.",
                        "PostgreSQL connection string must contain Username or User Id parameter."
                    );
                }

                return ValidationResult.Success();
            }
            catch (ArgumentException ex)
            {
                return ValidationResult.Failure(
                    $"Invalid PostgreSQL connection string: {ex.Message}",
                    ex.ToString()
                );
            }
        }

        /// <summary>
        /// Parses the connection string into a DbConnectionStringBuilder.
        /// </summary>
        public DbConnectionStringBuilder ParseConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            var builder = new NpgsqlConnectionStringBuilder(connectionString);

            // Inject password from environment variable if not present
            if (string.IsNullOrEmpty(builder.Password))
            {
                var envPassword = Environment.GetEnvironmentVariable("JVLINK_DB_PASSWORD");
                if (!string.IsNullOrEmpty(envPassword))
                {
                    builder.Password = envPassword;
                }
            }

            return builder;
        }

        /// <summary>
        /// Processes connection string to support environment variable password injection.
        /// If Password is empty but JVLINK_DB_PASSWORD environment variable exists, use it.
        /// </summary>
        private string ProcessConnectionString(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);

            // If password is not specified in connection string, check environment variable
            if (string.IsNullOrEmpty(builder.Password))
            {
                var envPassword = Environment.GetEnvironmentVariable("JVLINK_DB_PASSWORD");
                if (!string.IsNullOrEmpty(envPassword))
                {
                    builder.Password = envPassword;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Applies PostgreSQL-specific connection settings (future use).
        /// </summary>
        /// <remarks>
        /// Placeholder for future optimizations:
        /// - Connection pooling configuration
        /// - Timeout settings
        /// - SSL/TLS configuration
        /// </remarks>
        public void ApplyConnectionSettings(DbConnection connection)
        {
            // Future: Apply PostgreSQL-specific settings
            // Example:
            // - SET statement_timeout = 30000;
            // - SET idle_in_transaction_session_timeout = 60000;
            // - Configure connection pooling parameters
        }
    }
}
