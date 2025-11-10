# Data Model: Multi-Database Support

**Date**: 2025-11-09
**Feature**: Multi-Database Support (DuckDB and PostgreSQL)
**Purpose**: Define data structures and abstractions for database provider layer

## Entity Model

### 1. Database Provider Hierarchy

```
IDatabaseProvider (Interface)
├── SQLiteDatabaseProvider
├── DuckDBDatabaseProvider
└── PostgreSQLDatabaseProvider
```

**Responsibilities**:
- Create database connections
- Begin/commit/rollback transactions
- Provide SQL generator instance
- Provide connection factory instance
- Handle database-specific configurations

**Attributes**:
- `DatabaseType`: Enum (SQLite, DuckDB, PostgreSQL)
- `ConnectionString`: string
- `IsConnected`: bool
- `SupportsTransactions`: bool

**Operations**:
- `CreateConnection()`: DbConnection
- `BeginTransaction(IsolationLevel)`: DbTransaction
- `GetSqlGenerator()`: ISqlGenerator
- `GetConnectionFactory()`: IConnectionFactory
- `ValidateConnection()`: bool
- `Dispose()`: void

---

### 2. SQL Generator Hierarchy

```
ISqlGenerator (Interface)
├── SQLiteSqlGenerator
├── DuckDBSqlGenerator
└── PostgreSQLSqlGenerator
```

**Responsibilities**:
- Generate CREATE TABLE DDL for JV-Data structures
- Generate INSERT statements with parameter placeholders
- Map C# types to database-specific SQL types
- Handle identifier quoting (case sensitivity)

**Attributes**:
- `DatabaseType`: Enum
- `IdentifierQuoteChar`: string (" for all databases)
- `ParameterPrefix`: string (@ for all)
- `TypeMappings`: Dictionary<Type, string>

**Operations**:
- `GenerateCreateTableDdl(Type jvDataType)`: string
- `GenerateInsertSql(Type jvDataType)`: string
- `MapCSharpTypeToSqlType(Type type)`: string
- `QuoteIdentifier(string identifier)`: string
- `GetParameterName(string fieldName)`: string

---

### 3. Connection Factory Hierarchy

```
IConnectionFactory (Interface)
├── SQLiteConnectionFactory
├── DuckDBConnectionFactory
└── PostgreSQLConnectionFactory
```

**Responsibilities**:
- Create and configure database connections
- Parse and validate connection strings
- Apply database-specific connection settings
- Handle environment variable injection (PostgreSQL password)

**Attributes**:
- `DatabaseType`: Enum
- `ConnectionStringTemplate`: string

**Operations**:
- `CreateConnection(string connectionString)`: DbConnection
- `ParseConnectionString(string connectionString)`: DbConnectionStringBuilder
- `ValidateConnectionString(string connectionString)`: ValidationResult
- `ApplyConnectionSettings(DbConnection connection)`: void

---

### 4. Database Provider Factory

```
DatabaseProviderFactory (Static Class)
```

**Responsibilities**:
- Detect database type from file extension or connection string
- Create appropriate database provider instance
- Validate database type parameter

**Operations**:
- `Create(DatabaseType type, string connectionString)`: IDatabaseProvider
- `DetectDatabaseType(string dataSource)`: DatabaseType
- `IsValidDatabaseType(string typeString)`: bool

**Detection Logic**:
```
Input: dataSource string
├── Ends with .db or .sqlite → SQLite
├── Ends with .duckdb → DuckDB
├── Contains "Host=" or "Server=" → PostgreSQL
└── Otherwise → Error (cannot detect)
```

---

### 5. Database Type Enumeration

```csharp
public enum DatabaseType
{
    SQLite = 0,
    DuckDB = 1,
    PostgreSQL = 2
}
```

---

### 6. Connection Configuration

```
DatabaseConnectionInfo (Replaces SQLiteConnectionInfo)
```

**Attributes**:
- `DatabaseType`: DatabaseType
- `DataSource`: string (file path or connection string)
- `ConnectionString`: string (computed property)
- `AdditionalOptions`: Dictionary<string, string>

**Relationships**:
- Used by `JVLinkToSQLiteBootstrap` during initialization
- Serialized to/from XML (setting.xml compatibility)

**Operations**:
- `BuildConnectionString()`: string
- `Validate()`: ValidationResult
- `ApplyEnvironmentVariables()`: void (for PostgreSQL password)

---

### 7. Data Type Mapping

Each SQL generator maintains a type mapping table:

#### SQLite Type Mappings

| C# Type | SQL Type | Notes |
|---------|----------|-------|
| `string` | TEXT | No length limit |
| `int` | INTEGER | 32-bit |
| `long` | INTEGER | 64-bit (SQLite INTEGER is variable) |
| `decimal` | REAL | Approximation |
| `DateTime` | TEXT | ISO8601 format |
| `bool` | INTEGER | 0 = false, 1 = true |
| `byte[]` | BLOB | Binary data |

#### DuckDB Type Mappings

| C# Type | SQL Type | Notes |
|---------|----------|-------|
| `string` | VARCHAR | Variable length |
| `int` | INTEGER | 32-bit |
| `long` | BIGINT | 64-bit |
| `decimal` | DECIMAL(18,6) | Precision/scale configurable |
| `DateTime` | TIMESTAMP | Microsecond precision |
| `bool` | BOOLEAN | Native boolean type |
| `byte[]` | BLOB | Binary data |

#### PostgreSQL Type Mappings

| C# Type | SQL Type | Notes |
|---------|----------|-------|
| `string` | VARCHAR(500) | Length configurable, TEXT for long strings |
| `int` | INTEGER | 32-bit |
| `long` | BIGINT | 64-bit |
| `decimal` | NUMERIC(18,6) | Exact decimal |
| `DateTime` | TIMESTAMP | Microsecond precision |
| `bool` | BOOLEAN | Native boolean type |
| `byte[]` | BYTEA | Binary data |

**Special Handling**:
- Nullable types (`int?`, `DateTime?`): Add `NULL` constraint handling
- Strings: Default to VARCHAR(500), use TEXT for long fields (>1000 chars)
- Primary keys: Auto-increment handling varies by database

---

### 8. DataBridge Extension

**Current**: `DataBridge<T>` generates SQL using embedded strings

**Modified**: `DataBridge<T>` delegates SQL generation to `ISqlGenerator`

**Changes**:
```csharp
// Before (embedded SQL)
public class JV_RA_RACEDataBridge : DataBridge<JV_RA_RACE>
{
    protected override string CreateTableSql =>
        "CREATE TABLE IF NOT EXISTS JV_RA_RACE (...)";

    protected override string InsertSql =>
        "INSERT INTO JV_RA_RACE (...) VALUES (...)";
}

// After (delegated SQL generation)
public class JV_RA_RACEDataBridge : DataBridge<JV_RA_RACE>
{
    private readonly ISqlGenerator _sqlGenerator;

    public JV_RA_RACEDataBridge(ISqlGenerator sqlGenerator)
    {
        _sqlGenerator = sqlGenerator;
    }

    protected override string CreateTableSql =>
        _sqlGenerator.GenerateCreateTableDdl(typeof(JV_RA_RACE));

    protected override string InsertSql =>
        _sqlGenerator.GenerateInsertSql(typeof(JV_RA_RACE));
}
```

**Impact**:
- All 40+ DataBridge implementations need constructor injection of `ISqlGenerator`
- `DataBridgeFactory` needs to receive `IDatabaseProvider` to create DataBridges
- No changes to JV-Data structure definitions

---

### 9. Bootstrap Configuration Extension

**Current**: `JVLinkToSQLiteBootstrap` initializes with SQLite connection

**Modified**: `JVLinkToSQLiteBootstrap` initializes with database provider

**Changes**:
```csharp
// Before
public class JVLinkToSQLiteBootstrap
{
    public void Initialize(SQLiteConnectionInfo connectionInfo)
    {
        // ...
    }
}

// After
public class JVLinkToSQLiteBootstrap
{
    private IDatabaseProvider _databaseProvider;

    public void Initialize(DatabaseConnectionInfo connectionInfo)
    {
        _databaseProvider = DatabaseProviderFactory.Create(
            connectionInfo.DatabaseType,
            connectionInfo.ConnectionString);
        // ...
    }

    public IDatabaseProvider GetDatabaseProvider() => _databaseProvider;
}
```

---

### 10. Validation Rules

#### Connection String Validation

**SQLite**:
- Must be a valid file path or `:memory:`
- Directory must exist (if file path)
- File extension should be `.db` or `.sqlite`

**DuckDB**:
- Must be a valid file path or `:memory:`
- Directory must exist (if file path)
- File extension should be `.duckdb`

**PostgreSQL**:
- Must contain `Host` or `Server`
- Must contain `Database`
- Must contain `Username` or `User Id`
- Password read from `JVLINK_DB_PASSWORD` environment variable
- Port defaults to 5432 if not specified

#### Transaction Validation

- Ensure transaction is not already active before beginning new one
- Validate isolation level is supported by database
- Ensure connection is open before starting transaction

---

### 11. Error Handling Model

#### Error Categories

1. **Connection Errors**:
   - Database file not found (SQLite/DuckDB)
   - Server not reachable (PostgreSQL)
   - Authentication failed (PostgreSQL)
   - Permission denied

2. **SQL Execution Errors**:
   - Syntax error (malformed SQL)
   - Constraint violation (primary key, foreign key)
   - Data type mismatch

3. **Transaction Errors**:
   - Deadlock (PostgreSQL)
   - Serialization failure (PostgreSQL)
   - Transaction timeout

4. **Configuration Errors**:
   - Invalid database type
   - Missing environment variable (PostgreSQL password)
   - Invalid connection string format

#### Exception Hierarchy

```
DatabaseException (Base)
├── ConnectionException
│   ├── DatabaseNotFoundException
│   ├── ServerUnreachableException
│   └── AuthenticationException
├── SqlExecutionException
│   ├── SyntaxErrorException
│   └── ConstraintViolationException
└── TransactionException
    ├── DeadlockException
    └── SerializationFailureException
```

---

## State Transitions

### Database Provider State Machine

```
[Created] → Initialize() → [Initialized]
[Initialized] → CreateConnection() → [Connected]
[Connected] → BeginTransaction() → [InTransaction]
[InTransaction] → Commit/Rollback() → [Connected]
[Connected] → Dispose() → [Disposed]
```

**Invariants**:
- Cannot begin transaction without connection
- Cannot commit/rollback without active transaction
- Cannot execute SQL without connection

---

## Relationships

### Component Dependencies

```
JVLinkToSQLite (CLI)
    ↓
JVLinkToSQLiteBootstrap
    ↓
DatabaseProviderFactory
    ↓
IDatabaseProvider ←── ISqlGenerator
    ↓                      ↓
DataBridgeFactory  →  DataBridge<T>
    ↓
JV-Data Structures
```

**Dependency Injection**:
- `IDatabaseProvider` injected into `DataBridgeFactory`
- `ISqlGenerator` injected into `DataBridge<T>` instances
- `IConnectionFactory` used internally by `IDatabaseProvider`

---

## Performance Considerations

### Caching Strategy

1. **SQL Generation**: Cache generated DDL/INSERT SQL per database type
   - Key: `(DatabaseType, JVDataType)`
   - Value: Generated SQL string
   - Invalidation: Never (static schema)

2. **Type Mappings**: Pre-compute type mapping dictionaries at startup
   - One-time initialization in each SQL generator
   - No runtime lookups needed

3. **Connection Pooling**: PostgreSQL only
   - SQLite/DuckDB: Single connection per database file
   - PostgreSQL: Npgsql built-in pooling (max 100 connections)

### Memory Management

- Dispose database connections in `finally` blocks
- Use `using` statements for transactions
- Avoid holding connections during JVLink API calls (network latency)

---

## Security Considerations

### Password Management

**PostgreSQL**:
- Never log connection strings containing passwords
- Read password from environment variable at runtime
- Clear password from memory after use (SecureString not available in .NET Framework)

### SQL Injection Prevention

- Use parameterized queries (ADO.NET `DbParameter`)
- Quote all identifiers to prevent injection via table/column names
- Validate user input (database type, data source path)

### Path Traversal Prevention

**SQLite/DuckDB**:
- Validate file paths before creating connections
- Reject paths containing `..` or absolute paths outside allowed directories
- Use `Path.GetFullPath()` to resolve and validate paths

---

## Next Steps

1. ✅ Data model complete
2. ⏩ Create interface contracts (see `contracts/` directory)
3. ⏩ Write quick start guide for developers
4. ⏩ Generate implementation tasks with `/speckit.tasks`

---

**Data Model Status**: ✅ Complete
**Ready for Contracts**: ✅ Yes
