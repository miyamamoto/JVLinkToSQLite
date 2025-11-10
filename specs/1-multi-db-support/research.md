# Research: Multi-Database Support Technology Decisions

**Date**: 2025-11-09
**Feature**: Multi-Database Support (DuckDB and PostgreSQL)
**Purpose**: Resolve technical unknowns and establish best practices for implementation

## 1. DuckDB.NET Integration

### Decision
Use **DuckDB.NET.Data.Full 1.4.1** NuGet package for DuckDB support.

### Rationale
- **ADO.NET Compatible**: Provides standard ADO.NET provider model, similar to existing SQLite usage
- **Full Package**: Includes native DuckDB library, simplifying deployment
- **.NET Framework Compatibility**: Targets .NET Standard 2.0, compatible with .NET Framework 4.6.1+
- **Latest Stable**: Version 1.4.1 (as of 2025) is stable and well-maintained
- **MIT License**: Compatible with GPLv3

### API Overview
```csharp
// Connection
using var connection = new DuckDBConnection("Data Source=race.duckdb");
connection.Open();

// Command execution (similar to SQLite)
using var command = connection.CreateCommand();
command.CommandText = "CREATE TABLE ...";
command.ExecuteNonQuery();

// Transactions
using var transaction = connection.BeginTransaction();
// ... execute commands
transaction.Commit();
```

### Alternatives Considered
- **DuckDB.NET.Data** (managed-only): Rejected because it requires separate native library deployment
- **DuckDB.NET.Bindings**: Low-level API, more complex than needed

### Integration Points
- Use ADO.NET `DbConnection`, `DbCommand`, `DbTransaction` abstractions
- Compatible with Entity Framework 6 provider model
- Connection string format: `Data Source=race.duckdb`

## 2. Npgsql and Entity Framework 6 Integration

### Decision
Use **Npgsql 4.1.x** and **EntityFramework6.Npgsql 6.4.x** NuGet packages for PostgreSQL support.

### Rationale
- **Entity Framework 6 Support**: Dedicated EF6 provider package
- **.NET Framework Compatibility**: Npgsql 4.x targets .NET Framework 4.6.1+
- **Mature and Stable**: Widely used in production applications
- **PostgreSQL License**: PostgreSQL and Npgsql are both permissive licenses, compatible with GPLv3

### Configuration
```csharp
// App.config / Web.config (auto-configured by NuGet)
<connectionStrings>
  <add name="PostgreSQLConnection"
       connectionString="Server=localhost;Port=5432;Database=jvdata;User Id=user"
       providerName="Npgsql" />
</connectionStrings>

<entityFramework>
  <providers>
    <provider invariantName="Npgsql"
              type="Npgsql.NpgsqlServices, EntityFramework6.Npgsql" />
  </providers>
</entityFramework>

// Code-based configuration
public class NpgsqlConfiguration : DbConfiguration
{
    public NpgsqlConfiguration()
    {
        SetProviderServices("Npgsql", NpgsqlServices.Instance);
        SetProviderFactory("Npgsql", NpgsqlFactory.Instance);
        SetDefaultConnectionFactory(new NpgsqlConnectionFactory());
    }
}
```

### Password Management
- **Environment Variable**: Read from `JVLINK_DB_PASSWORD` instead of connection string
- **Runtime**: Inject password at runtime: `connectionStringBuilder.Password = Environment.GetEnvironmentVariable("JVLINK_DB_PASSWORD")`

### Alternatives Considered
- **Npgsql 6.x+ (for .NET Core)**: Rejected due to .NET Framework 4.x requirement
- **Direct PostgreSQL driver**: Npgsql is the official .NET PostgreSQL driver

### Integration Points
- Use ADO.NET and Entity Framework 6 abstractions
- Connection pooling built-in (default: min=1, max=100)
- Prepared statements for performance
- Connection string format: `Server=localhost;Port=5432;Database=jvdata;User Id=user`

## 3. SQL Dialect Differences

### Decision
Create database-specific SQL generators with explicit data type mapping tables.

### DuckDB vs SQLite vs PostgreSQL Differences

#### Data Type Mapping

| C# Type | SQLite | DuckDB | PostgreSQL |
|---------|--------|--------|------------|
| `string` | TEXT | VARCHAR | VARCHAR(n) or TEXT |
| `int` | INTEGER | INTEGER | INTEGER |
| `long` | INTEGER | BIGINT | BIGINT |
| `decimal` | REAL | DECIMAL(p,s) | NUMERIC(p,s) |
| `DateTime` | TEXT (ISO8601) | TIMESTAMP | TIMESTAMP |
| `bool` | INTEGER (0/1) | BOOLEAN | BOOLEAN |
| `byte[]` | BLOB | BLOB | BYTEA |

#### DDL Differences

**SQLite**:
```sql
CREATE TABLE IF NOT EXISTS JV_RA_RACE (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    race_id TEXT NOT NULL,
    race_date TEXT,
    race_name TEXT
);
```

**DuckDB** (PostgreSQL-compatible):
```sql
CREATE TABLE IF NOT EXISTS JV_RA_RACE (
    id INTEGER PRIMARY KEY,
    race_id VARCHAR NOT NULL,
    race_date TIMESTAMP,
    race_name VARCHAR
);
```

**PostgreSQL**:
```sql
CREATE TABLE IF NOT EXISTS JV_RA_RACE (
    id SERIAL PRIMARY KEY,
    race_id VARCHAR(50) NOT NULL,
    race_date TIMESTAMP,
    race_name VARCHAR(200)
);
```

#### Case Sensitivity
- **SQLite**: Case-insensitive by default
- **DuckDB**: Case-sensitive identifiers, but case-insensitive matching
- **PostgreSQL**: Lowercases unquoted identifiers

**Solution**: Always use double quotes for identifiers in SQL generation to ensure consistency:
```sql
CREATE TABLE "JV_RA_RACE" ("race_id" VARCHAR NOT NULL, ...)
```

#### Transaction Isolation
- **SQLite**: SERIALIZABLE (default)
- **DuckDB**: READ COMMITTED (default)
- **PostgreSQL**: READ COMMITTED (default)

**Solution**: Explicitly set isolation level for consistency across databases.

### Rationale
- **Explicit Mapping**: Prevents implicit conversions and data loss
- **Database-Specific Generators**: Each database has a dedicated SQL generator class
- **Quoted Identifiers**: Avoids case sensitivity issues

## 4. Connection Pooling Strategies

### Decision
- **SQLite/DuckDB**: No connection pooling (file-based, single connection)
- **PostgreSQL**: Use Npgsql built-in connection pooling

### PostgreSQL Connection Pool Configuration

```csharp
// Default settings (sufficient for most cases)
var connectionString = new NpgsqlConnectionStringBuilder
{
    Host = "localhost",
    Database = "jvdata",
    Username = "user",
    Password = Environment.GetEnvironmentVariable("JVLINK_DB_PASSWORD"),
    Pooling = true,              // Enable pooling (default: true)
    MinPoolSize = 1,             // Minimum connections (default: 1)
    MaxPoolSize = 100,           // Maximum connections (default: 100)
    ConnectionLifetime = 0,      // Connection lifetime in seconds (0 = unlimited)
    ConnectionIdleLifetime = 300, // Idle timeout (default: 300s)
    Timeout = 30                 // Connection timeout (default: 15s)
};
```

### Rationale
- **SQLite/DuckDB**: Embedded databases, no network overhead
- **PostgreSQL**: Network-based, connection pooling improves performance
- **Built-in Pooling**: Npgsql handles pooling automatically, no custom implementation needed

### Best Practices
- Dispose connections properly (`using` statements)
- Avoid long-lived connections
- Monitor pool exhaustion (MaxPoolSize reached)

## 5. Transaction Management

### Decision
Implement a unified transaction abstraction that delegates to database-specific implementations.

### Implementation Pattern

```csharp
public interface IDatabaseProvider
{
    DbConnection CreateConnection();
    DbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    void CommitTransaction(DbTransaction transaction);
    void RollbackTransaction(DbTransaction transaction);
}

// Usage
using (var connection = databaseProvider.CreateConnection())
{
    connection.Open();
    using (var transaction = databaseProvider.BeginTransaction())
    {
        try
        {
            // Execute commands
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
```

### Isolation Levels
- **Default**: `IsolationLevel.ReadCommitted` for all databases
- **Override**: Allow users to configure via setting.xml (future enhancement)

### Rationale
- **Consistency**: Same transaction semantics across databases
- **ADO.NET Standard**: Use standard `DbTransaction` abstraction
- **Error Handling**: Automatic rollback in `finally` block

## 6. Error Handling and Retry Logic

### Decision
Implement database-specific error handling with retry logic for transient errors.

### Transient Errors (PostgreSQL)

PostgreSQL error codes requiring retry:
- `53000`: Insufficient resources
- `53300`: Too many connections
- `40001`: Serialization failure
- `40P01`: Deadlock detected
- Network errors (connection timeout, network failure)

### Retry Strategy

```csharp
public async Task<T> ExecuteWithRetry<T>(
    Func<Task<T>> operation,
    int maxRetries = 3,
    int delayMilliseconds = 1000)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return await operation();
        }
        catch (NpgsqlException ex) when (IsTransientError(ex) && i < maxRetries - 1)
        {
            await Task.Delay(delayMilliseconds * (i + 1)); // Exponential backoff
        }
    }
    throw new DatabaseException("Max retries exceeded");
}

private bool IsTransientError(NpgsqlException ex)
{
    return ex.ErrorCode == "53000" ||
           ex.ErrorCode == "53300" ||
           ex.ErrorCode == "40001" ||
           ex.ErrorCode == "40P01";
}
```

### Error Messages

User-friendly error messages:
- **Connection Failed**: "Could not connect to PostgreSQL server at {Host}:{Port}. Please check that the server is running and accessible."
- **Authentication Failed**: "Authentication failed for user '{Username}'. Please check the JVLINK_DB_PASSWORD environment variable."
- **Database Not Found**: "Database '{Database}' not found. Please create the database first."

### Rationale
- **Resilience**: Handle transient network and resource errors
- **User Experience**: Provide actionable error messages
- **Exponential Backoff**: Avoid overwhelming the server during recovery

## 7. Performance Optimization

### Decision
Implement database-specific optimizations while maintaining a unified interface.

### Batch Insert Strategies

**SQLite**:
```csharp
// Use PRAGMA for performance
using (var transaction = connection.BeginTransaction())
{
    command.CommandText = "PRAGMA synchronous = OFF";
    command.ExecuteNonQuery();
    command.CommandText = "PRAGMA journal_mode = MEMORY";
    command.ExecuteNonQuery();

    // Batch inserts in transaction
    // ...

    transaction.Commit();
}
```

**DuckDB**:
```csharp
// DuckDB optimizes batch inserts automatically
// Use prepared statements
using (var command = connection.CreateCommand())
{
    command.CommandText = "INSERT INTO table VALUES (?, ?, ?)";
    command.Prepare();

    foreach (var row in rows)
    {
        // Set parameters and execute
    }
}
```

**PostgreSQL**:
```csharp
// Use COPY for bulk inserts (fastest)
using (var writer = connection.BeginBinaryImport("COPY table FROM STDIN (FORMAT BINARY)"))
{
    foreach (var row in rows)
    {
        writer.StartRow();
        writer.Write(row.Field1);
        writer.Write(row.Field2);
        // ...
    }
    writer.Complete();
}

// Alternative: Prepared statements with batching
using (var batch = new NpgsqlBatch(connection))
{
    foreach (var row in rows)
    {
        var command = new NpgsqlBatchCommand("INSERT INTO table VALUES ($1, $2)");
        command.Parameters.AddWithValue(row.Field1);
        command.Parameters.AddWithValue(row.Field2);
        batch.BatchCommands.Add(command);
    }
    await batch.ExecuteNonQueryAsync();
}
```

### Benchmarks (Target: 100k records in 10 minutes)

| Database | Strategy | Expected Performance |
|----------|----------|---------------------|
| SQLite | Transaction + PRAGMA | ~5k records/sec |
| DuckDB | Prepared statements | ~10k records/sec (2x SQLite) |
| PostgreSQL | COPY command | ~20k records/sec (4x SQLite) |

### Rationale
- **Database-Specific**: Each database has optimal insertion strategy
- **Measurable**: Benchmark against 100k records / 10 min requirement
- **Abstracted**: Hide implementation details behind provider interface

## 8. Testing Strategy

### Decision
Implement three-tier testing: Unit, Integration, and Performance.

### Test Structure

**Unit Tests** (Mock database connections):
```csharp
[TestFixture]
public class SQLiteDatabaseProviderTests
{
    [Test]
    public void CreateConnection_ReturnsValidConnection()
    {
        var provider = new SQLiteDatabaseProvider("Data Source=:memory:");
        using (var connection = provider.CreateConnection())
        {
            Assert.IsInstanceOf<SQLiteConnection>(connection);
            connection.Open();
            Assert.AreEqual(ConnectionState.Open, connection.State);
        }
    }
}
```

**Integration Tests** (Real databases):
```csharp
[TestFixture]
public class DuckDBIntegrationTests
{
    private string _testDbPath;

    [SetUp]
    public void SetUp()
    {
        _testDbPath = Path.GetTempFileName() + ".duckdb";
    }

    [Test]
    public void InsertAndQuery_100kRecords_CompletesInTime()
    {
        var provider = new DuckDBDatabaseProvider($"Data Source={_testDbPath}");
        var stopwatch = Stopwatch.StartNew();

        // Insert 100k records
        // ...

        stopwatch.Stop();
        Assert.Less(stopwatch.Elapsed.TotalMinutes, 10, "Should complete in under 10 minutes");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }
}
```

**Performance Tests**:
- Measure insertion rates for each database
- Compare against baseline (SQLite)
- Verify DuckDB is 3x faster for analytics queries

### Test Data
- Use real JV-Data structures (from existing test fixtures)
- Generate 100k+ synthetic records for performance tests
- Test all 40+ DataBridge types

### Rationale
- **Comprehensive**: Cover all database types and scenarios
- **Automated**: Run on CI/CD pipeline
- **Measurable**: Quantify performance improvements

## Summary of Decisions

| Area | Decision | Rationale |
|------|----------|-----------|
| **DuckDB Package** | DuckDB.NET.Data.Full 1.4.1 | ADO.NET compatible, includes native library, .NET Framework support |
| **PostgreSQL Package** | Npgsql 4.1.x + EntityFramework6.Npgsql 6.4.x | EF6 support, mature, .NET Framework compatible |
| **SQL Dialect** | Database-specific SQL generators with explicit type mapping | Avoid implicit conversions, handle case sensitivity |
| **Connection Pooling** | Built-in Npgsql pooling for PostgreSQL only | File-based DBs don't need pooling |
| **Transactions** | Unified `IDatabaseProvider` abstraction with `IsolationLevel.ReadCommitted` | Consistent semantics across databases |
| **Error Handling** | Retry logic for transient PostgreSQL errors with exponential backoff | Resilience and better user experience |
| **Performance** | Database-specific batch strategies (PRAGMA for SQLite, COPY for PostgreSQL) | Meet 100k records / 10 min requirement |
| **Testing** | Three-tier: Unit (mocked), Integration (real DBs), Performance (benchmarks) | Comprehensive coverage and validation |

## Next Steps

1. ✅ Research complete - all technical unknowns resolved
2. ⏩ Proceed to Phase 1: Design data model and contracts
3. ⏩ Create interface definitions (`IDatabaseProvider`, `ISqlGenerator`, `IConnectionFactory`)
4. ⏩ Document data type mapping tables
5. ⏩ Write quick start guide for developers

---

**Research Status**: ✅ Complete
**Ready for Phase 1**: ✅ Yes
