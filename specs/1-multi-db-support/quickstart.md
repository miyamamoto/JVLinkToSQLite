# Quick Start Guide: Multi-Database Support Development

**Target Audience**: Developers implementing the multi-database support feature
**Prerequisites**: Familiarity with C#, .NET Framework, and Entity Framework 6
**Estimated Time**: 15 minutes to understand, 3-5 days to implement

## Overview

This guide walks you through implementing multi-database support for JVLinkToSQLite. By the end, the application will support SQLite, DuckDB, and PostgreSQL as backend databases.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     JVLinkToSQLite (CLI)                    │
│  - Parse --dbtype and --datasource arguments               │
│  - Delegate to JVLinkToSQLiteBootstrap                     │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│              JVLinkToSQLiteBootstrap                        │
│  - Initialize DatabaseProvider from config                 │
│  - Inject into DataBridgeFactory                           │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│              DatabaseProviderFactory                        │
│  - Detect database type from file extension or             │
│    connection string                                       │
│  - Create appropriate provider instance                    │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
         ┌──────────────────┴──────────────────┐
         │                                     │
         ▼                                     ▼
┌──────────────────┐              ┌──────────────────────┐
│ IDatabaseProvider│              │   ISqlGenerator      │
│ Implementation:  │◄─────────────┤   Implementation:    │
│ - SQLite         │              │   - SQLiteSql        │
│ - DuckDB         │              │   - DuckDBSql        │
│ - PostgreSQL     │              │   - PostgreSQLSql    │
└─────────┬────────┘              └──────────────────────┘
          │
          ▼
┌──────────────────────────────────────────────────────────┐
│              DataBridgeFactory                           │
│  - Use provider's SqlGenerator                          │
│  - Create DataBridge<T> instances                       │
└─────────────────┬────────────────────────────────────────┘
                  │
                  ▼
          ┌───────────────┐
          │ DataBridge<T> │
          │ (40+ types)   │
          └───────────────┘
```

## Step-by-Step Implementation

### Step 1: Add NuGet Packages

**File**: `Urasandesu.JVLinkToSQLite/packages.config` (or use Package Manager)

```xml
<!-- Add these packages -->
<package id="DuckDB.NET.Data.Full" version="1.4.1" targetFramework="net48" />
<package id="Npgsql" version="4.1.13" targetFramework="net48" />
<package id="EntityFramework6.Npgsql" version="6.4.3" targetFramework="net48" />
```

**Commands**:
```powershell
# In Package Manager Console
Install-Package DuckDB.NET.Data.Full -Version 1.4.1 -ProjectName Urasandesu.JVLinkToSQLite
Install-Package Npgsql -Version 4.1.13 -ProjectName Urasandesu.JVLinkToSQLite
Install-Package EntityFramework6.Npgsql -Version 6.4.3 -ProjectName Urasandesu.JVLinkToSQLite
```

### Step 2: Create Database Provider Infrastructure

**Directory**: `Urasandesu.JVLinkToSQLite/DatabaseProviders/`

Create the following files in order:

1. **DatabaseType.cs** (enum) - See `contracts/DatabaseType.cs`
2. **IDatabaseProvider.cs** (interface) - See `contracts/IDatabaseProvider.cs`
3. **ISqlGenerator.cs** (interface) - See `contracts/ISqlGenerator.cs`
4. **IConnectionFactory.cs** (interface) - See `contracts/IConnectionFactory.cs`

### Step 3: Implement SQLite Provider (Refactor Existing Code)

**File**: `Urasandesu.JVLinkToSQLite/DatabaseProviders/SQLite/SQLiteDatabaseProvider.cs`

```csharp
public class SQLiteDatabaseProvider : IDatabaseProvider
{
    private readonly string _connectionString;
    private readonly ISqlGenerator _sqlGenerator;
    private readonly IConnectionFactory _connectionFactory;

    public SQLiteDatabaseProvider(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _sqlGenerator = new SQLiteSqlGenerator();
        _connectionFactory = new SQLiteConnectionFactory();
    }

    public DatabaseType DatabaseType => DatabaseType.SQLite;
    public string ConnectionString => _connectionString;
    public bool IsConnected => false; // Stateless
    public bool SupportsTransactions => true;

    public DbConnection CreateConnection()
    {
        return _connectionFactory.CreateConnection(_connectionString);
    }

    public DbTransaction BeginTransaction(DbConnection connection, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (connection.State != ConnectionState.Open)
            throw new InvalidOperationException("Connection must be open to begin a transaction.");

        return connection.BeginTransaction(isolationLevel);
    }

    public ISqlGenerator GetSqlGenerator() => _sqlGenerator;
    public IConnectionFactory GetConnectionFactory() => _connectionFactory;

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
        // No resources to dispose for SQLite provider
    }
}
```

**Tip**: Start with SQLite since you're refactoring existing code. Copy patterns to DuckDB and PostgreSQL providers.

### Step 4: Implement SQL Generators

**File**: `Urasandesu.JVLinkToSQLite/DatabaseProviders/SQLite/SQLiteSqlGenerator.cs`

```csharp
public class SQLiteSqlGenerator : ISqlGenerator
{
    private static readonly Dictionary<Type, string> _typeMappings = new Dictionary<Type, string>
    {
        { typeof(string), "TEXT" },
        { typeof(int), "INTEGER" },
        { typeof(int?), "INTEGER" },
        { typeof(long), "INTEGER" },
        { typeof(long?), "INTEGER" },
        { typeof(decimal), "REAL" },
        { typeof(decimal?), "REAL" },
        { typeof(DateTime), "TEXT" },
        { typeof(DateTime?), "TEXT" },
        { typeof(bool), "INTEGER" },
        { typeof(bool?), "INTEGER" },
        { typeof(byte[]), "BLOB" }
    };

    public DatabaseType DatabaseType => DatabaseType.SQLite;
    public string IdentifierQuoteChar => "\"";
    public string ParameterPrefix => "@";

    public string GenerateCreateTableDdl(Type jvDataType)
    {
        if (jvDataType == null) throw new ArgumentNullException(nameof(jvDataType));

        var tableName = QuoteIdentifier(jvDataType.Name);
        var properties = jvDataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var columns = new StringBuilder();
        foreach (var prop in properties)
        {
            var columnName = QuoteIdentifier(prop.Name);
            var sqlType = MapCSharpTypeToSqlType(prop.PropertyType);
            var nullable = IsNullable(prop.PropertyType) ? "" : " NOT NULL";

            columns.AppendLine($"    {columnName} {sqlType}{nullable},");
        }

        // Remove trailing comma
        if (columns.Length > 0)
            columns.Length -= 3; // Remove ",\r\n"

        return $"CREATE TABLE IF NOT EXISTS {tableName} (\n{columns}\n);";
    }

    public string GenerateInsertSql(Type jvDataType)
    {
        if (jvDataType == null) throw new ArgumentNullException(nameof(jvDataType));

        var tableName = QuoteIdentifier(jvDataType.Name);
        var properties = jvDataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var columnNames = string.Join(", ", properties.Select(p => QuoteIdentifier(p.Name)));
        var parameterNames = string.Join(", ", properties.Select(p => GetParameterName(p.Name)));

        return $"INSERT INTO {tableName} ({columnNames}) VALUES ({parameterNames});";
    }

    public string MapCSharpTypeToSqlType(Type csharpType)
    {
        if (csharpType == null) throw new ArgumentNullException(nameof(csharpType));

        if (_typeMappings.TryGetValue(csharpType, out var sqlType))
            return sqlType;

        // Handle Nullable<T>
        var underlyingType = Nullable.GetUnderlyingType(csharpType);
        if (underlyingType != null && _typeMappings.TryGetValue(underlyingType, out sqlType))
            return sqlType;

        throw new NotSupportedException($"Type {csharpType.Name} is not supported.");
    }

    public string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            throw new ArgumentNullException(nameof(identifier));

        return $"\"{identifier}\"";
    }

    public string GetParameterName(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
            throw new ArgumentNullException(nameof(fieldName));

        return $"{ParameterPrefix}{fieldName}";
    }

    private bool IsNullable(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
    }
}
```

**DuckDB and PostgreSQL**: Copy this structure and update `_typeMappings` according to `data-model.md`.

### Step 5: Update MainOptions.cs

**File**: `JVLinkToSQLite/MainOptions.cs`

```csharp
[Verb("main", isDefault: true, HelpText = "メイン処理")]
internal class MainOptions : Options
{
    // ... existing options ...

    [Option("dbtype", Required = false, HelpText =
        "データベースタイプ。以下を指定可能:\n" +
        "* sqlite (デフォルト)\n" +
        "* duckdb\n" +
        "* postgresql\n" +
        "省略時はファイル拡張子から自動検出")]
    public string DatabaseType { get; set; }

    // Existing --datasource option continues to work
}
```

### Step 6: Update JVLinkToSQLiteBootstrap.cs

**File**: `Urasandesu.JVLinkToSQLite/JVLinkToSQLiteBootstrap.cs`

```csharp
public class JVLinkToSQLiteBootstrap
{
    private IDatabaseProvider _databaseProvider;

    public void Initialize(string databaseTypeString, string dataSource)
    {
        // Detect or parse database type
        var databaseType = string.IsNullOrEmpty(databaseTypeString)
            ? DatabaseProviderFactory.DetectDatabaseType(dataSource)
            : ParseDatabaseType(databaseTypeString);

        // Create provider
        _databaseProvider = DatabaseProviderFactory.Create(databaseType, dataSource);

        // Validate connection
        if (!_databaseProvider.ValidateConnection())
            throw new ConnectionException("Failed to connect to database. Please check connection settings.");

        // ... existing initialization code ...
    }

    public IDatabaseProvider GetDatabaseProvider() => _databaseProvider;

    private DatabaseType ParseDatabaseType(string typeString)
    {
        if (Enum.TryParse<DatabaseType>(typeString, true, out var result))
            return result;

        throw new ArgumentException($"Invalid database type: {typeString}");
    }
}
```

### Step 7: Update DataBridgeFactory.cs

**File**: `Urasandesu.JVLinkToSQLite/JVLinkWrappers/DataBridges/DataBridgeFactory.cs`

```csharp
public class DataBridgeFactory
{
    private readonly IDatabaseProvider _databaseProvider;

    public DataBridgeFactory(IDatabaseProvider databaseProvider)
    {
        _databaseProvider = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
    }

    public DataBridge CreateDataBridge(string recordSpec)
    {
        var sqlGenerator = _databaseProvider.GetSqlGenerator();

        // Example: Create appropriate DataBridge based on record spec
        switch (recordSpec)
        {
            case "RA":
                return new JV_RA_RACEDataBridge(sqlGenerator);
            case "UM":
                return new JV_UM_UMADataBridge(sqlGenerator);
            // ... 40+ other cases ...
            default:
                throw new NotSupportedException($"Record spec {recordSpec} is not supported.");
        }
    }
}
```

### Step 8: Update DataBridge Base Class

**File**: `Urasandesu.JVLinkToSQLite/JVLinkWrappers/DataBridges/DataBridge.cs`

```csharp
public abstract class DataBridge<T> : DataBridge where T : class, new()
{
    protected readonly ISqlGenerator SqlGenerator;

    protected DataBridge(ISqlGenerator sqlGenerator)
    {
        SqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
    }

    protected override string CreateTableSql => SqlGenerator.GenerateCreateTableDdl(typeof(T));
    protected override string InsertSql => SqlGenerator.GenerateInsertSql(typeof(T));

    // ... rest of existing implementation ...
}
```

### Step 9: Write Tests

**File**: `Test.Urasandesu.JVLinkToSQLite/DatabaseProviders/SQLiteDatabaseProviderTests.cs`

```csharp
[TestFixture]
public class SQLiteDatabaseProviderTests
{
    [Test]
    public void CreateConnection_WithValidConnectionString_ReturnsConnection()
    {
        // Arrange
        var provider = new SQLiteDatabaseProvider("Data Source=:memory:");

        // Act
        using (var connection = provider.CreateConnection())
        {
            // Assert
            Assert.IsNotNull(connection);
            Assert.IsInstanceOf<SQLiteConnection>(connection);

            connection.Open();
            Assert.AreEqual(ConnectionState.Open, connection.State);
        }
    }

    [Test]
    public void GenerateCreateTableDdl_ForJV_RA_RACE_ReturnsValidSql()
    {
        // Arrange
        var provider = new SQLiteDatabaseProvider("Data Source=:memory:");
        var sqlGenerator = provider.GetSqlGenerator();

        // Act
        var ddl = sqlGenerator.GenerateCreateTableDdl(typeof(JV_RA_RACE));

        // Assert
        StringAssert.Contains("CREATE TABLE IF NOT EXISTS", ddl);
        StringAssert.Contains("\"JV_RA_RACE\"", ddl);
        Assert.IsTrue(ddl.Contains("TEXT") || ddl.Contains("INTEGER")); // Has columns
    }
}
```

**Repeat for DuckDB and PostgreSQL providers.**

## Testing Strategy

### Unit Tests (Fast, Isolated)
```bash
# Run all database provider unit tests
dotnet test --filter "FullyQualifiedName~DatabaseProviders"
```

### Integration Tests (Real Databases)
```bash
# Requires PostgreSQL server running on localhost:5432
# Set environment variable: JVLINK_DB_PASSWORD=yourpassword

dotnet test --filter "FullyQualifiedName~Integration"
```

### Performance Tests (Benchmarks)
```bash
# Test 100k record insertion
dotnet test --filter "FullyQualifiedName~Performance"
```

## Debugging Tips

### Common Issues

**Issue 1**: "Unable to load DLL 'duckdb.dll'"
- **Solution**: Ensure DuckDB.NET.Data.Full is installed (includes native library)
- **Workaround**: Copy `duckdb.dll` to output directory manually

**Issue 2**: "Npgsql: 53300: sorry, too many clients already"
- **Solution**: Check PostgreSQL `max_connections` setting
- **Workaround**: Reduce `MaxPoolSize` in connection string

**Issue 3**: "Case sensitivity mismatch in table names"
- **Solution**: Always quote identifiers in SQL generation
- **Verify**: Check `QuoteIdentifier()` implementation

### Logging

Enable detailed logging for database operations:

```csharp
// In Bootstrap
var listener = new ConsoleListener(LogLevels.Debug);
serviceOperationNotifier.Subscribe(listener);
```

## Performance Optimization

### SQLite
```csharp
// Apply PRAGMA settings for bulk inserts
using (var command = connection.CreateCommand())
{
    command.CommandText = "PRAGMA synchronous = OFF; PRAGMA journal_mode = WAL;";
    command.ExecuteNonQuery();
}
```

### DuckDB
```csharp
// Use prepared statements
using (var command = connection.CreateCommand())
{
    command.CommandText = insertSql;
    command.Prepare();

    foreach (var record in records)
    {
        // Set parameters and execute
    }
}
```

### PostgreSQL
```csharp
// Use COPY for fastest bulk insert
using (var writer = connection.BeginBinaryImport($"COPY {tableName} FROM STDIN (FORMAT BINARY)"))
{
    foreach (var record in records)
    {
        writer.StartRow();
        // Write fields
    }
    writer.Complete();
}
```

## Example Usage

### SQLite (Default, Backward Compatible)
```bash
JVLinkToSQLite.exe --datasource race.db --mode Exec
```

### DuckDB (Auto-detected from extension)
```bash
JVLinkToSQLite.exe --datasource race.duckdb --mode Exec
```

### PostgreSQL (Explicit Type)
```bash
# Set password first
set JVLINK_DB_PASSWORD=mypassword

JVLinkToSQLite.exe --dbtype postgresql --datasource "Server=localhost;Database=jvdata;Username=jvuser" --mode Exec
```

## Next Steps

1. ✅ Review this guide
2. ⏩ Implement Phase 2 tasks (see `tasks.md` after running `/speckit.tasks`)
3. ⏩ Write comprehensive tests
4. ⏩ Benchmark performance against requirements
5. ⏩ Update documentation (README.md, Wiki)

## Resources

- **DuckDB.NET Documentation**: https://duckdb.net/docs/
- **Npgsql Documentation**: https://www.npgsql.org/doc/
- **Entity Framework 6**: https://docs.microsoft.com/ef/ef6/
- **Constitution**: `.specify/memory/constitution.md`
- **Data Model**: `specs/1-multi-db-support/data-model.md`
- **Research**: `specs/1-multi-db-support/research.md`

---

**Quick Start Status**: ✅ Complete
**Ready for Implementation**: ✅ Yes
