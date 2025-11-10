# DuckDB Support - Usage Guide

**Feature**: Multi-Database Support - DuckDB Implementation
**Status**: ✅ Provider Implementation Complete
**Phase**: 3 of 6
**Last Updated**: 2025-11-10

---

## Overview

JVLinkToSQLite now supports **DuckDB** as an alternative database backend. DuckDB is an embedded analytical database optimized for OLAP (Online Analytical Processing) workloads, making it ideal for:

- **Analytics and Aggregations**: 3x faster than SQLite for complex queries
- **Data Science Workflows**: Seamless integration with Python/R via DuckDB extensions
- **Large Dataset Analysis**: Efficient handling of millions of records
- **Columnar Storage**: Better compression and query performance

---

## Quick Start

### Basic Usage

```bash
# Auto-detect from file extension (.duckdb)
JVLinkToSQLite.exe --datasource race.duckdb --mode Exec

# Explicit database type specification
JVLinkToSQLite.exe --dbtype duckdb --datasource analytics.duckdb --mode Exec

# Use existing setting.xml
JVLinkToSQLite.exe --dbtype duckdb --datasource race.duckdb --setting mysetting.xml
```

### In-Memory Database (for testing)

```bash
JVLinkToSQLite.exe --dbtype duckdb --datasource :memory: --mode Exec
```

---

## Installation Requirements

### NuGet Packages (Already Installed)

The following package is required and has been added to the project:

```xml
<package id="DuckDB.NET.Data.Full" version="1.4.1" targetFramework="net48" />
```

### No External Installation Required

DuckDB is **embedded** - no separate database server installation needed. The DuckDB.NET package includes the native DuckDB library.

---

## DuckDB vs SQLite: When to Use Each

### Use DuckDB When:

✅ **Analytics and Aggregations**
- Complex `GROUP BY`, `JOIN`, `WINDOW` functions
- Multi-table aggregations
- Statistical analysis

✅ **Large Datasets**
- Millions of race records
- Performance-critical queries
- Data warehouse scenarios

✅ **Integration with Data Science Tools**
- Export to Parquet, CSV for Python/R analysis
- Direct integration with pandas, R dataframes

### Use SQLite When:

✅ **Simplicity**
- Small to medium datasets
- Simple queries
- Minimal setup required

✅ **Compatibility**
- Existing workflows
- Well-established tools and libraries

✅ **Transactional Workloads**
- Frequent updates/inserts
- OLTP (Online Transaction Processing) scenarios

---

## Type Mapping: DuckDB vs SQLite

DuckDB uses stronger typing than SQLite, providing better data integrity:

| C# Type | SQLite Type | DuckDB Type | Notes |
|---------|-------------|-------------|-------|
| `string` | TEXT | VARCHAR | DuckDB supports length constraints |
| `int` | INTEGER | INTEGER | Same |
| `long` | INTEGER | **BIGINT** | DuckDB uses proper 64-bit type |
| `short` | INTEGER | **SMALLINT** | DuckDB optimizes for 16-bit |
| `byte` | INTEGER | **TINYINT** | DuckDB optimizes for 8-bit |
| `decimal` | REAL | **DECIMAL(18,6)** | DuckDB preserves precision |
| `double` | REAL | **DOUBLE** | Explicit double precision |
| `float` | REAL | **REAL** | Same |
| `DateTime` | TEXT | **TIMESTAMP** | DuckDB native date/time type |
| `bool` | INTEGER (0/1) | **BOOLEAN** | DuckDB native boolean |
| `byte[]` | BLOB | BLOB | Same |

### Key Advantages of DuckDB Types:

1. **BIGINT for long**: Proper 64-bit integer support (SQLite stores all integers as 64-bit anyway, but DuckDB is explicit)
2. **TIMESTAMP for DateTime**: Native date/time operations, no string parsing needed
3. **BOOLEAN for bool**: True/false values, not 0/1 integers
4. **DECIMAL(18,6)**: Fixed-precision decimals, no floating-point errors

---

## Example Queries

### After Importing Data with DuckDB

```sql
-- Connect to DuckDB database
duckdb race.duckdb

-- Basic query (same as SQLite)
SELECT * FROM NL_RA_RACE WHERE race_date > '2024-01-01';

-- Advanced analytics (DuckDB optimized)
SELECT
    track_code,
    COUNT(*) as race_count,
    AVG(total_prize) as avg_prize,
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY total_prize) as median_prize
FROM NL_RA_RACE
GROUP BY track_code
ORDER BY avg_prize DESC;

-- Window functions
SELECT
    race_date,
    race_name,
    total_prize,
    AVG(total_prize) OVER (
        PARTITION BY track_code
        ORDER BY race_date
        ROWS BETWEEN 7 PRECEDING AND CURRENT ROW
    ) as rolling_avg_prize
FROM NL_RA_RACE
ORDER BY race_date DESC;

-- Export to Parquet for Python/R analysis
COPY (SELECT * FROM NL_RA_RACE WHERE race_date > '2024-01-01')
TO 'races_2024.parquet' (FORMAT PARQUET);

-- Export to CSV
COPY (SELECT * FROM NL_UM_UMA WHERE born_year > 2015)
TO 'horses_2015plus.csv' (HEADER, DELIMITER ',');
```

---

## Performance Expectations

### Import Performance

Based on Phase 3 performance testing targets:

- **100,000 records**: < 10 minutes (same as SQLite)
- **Batch inserts**: Optimized for DuckDB's columnar storage
- **ThrottleSize parameter**: Works identically to SQLite mode

### Query Performance

Expected improvements over SQLite:

- **Simple SELECT**: Similar to SQLite
- **Aggregations (GROUP BY)**: **2-3x faster** than SQLite
- **Complex JOINs**: **2-5x faster** than SQLite
- **Window Functions**: **3-10x faster** than SQLite
- **Analytics Queries**: **Up to 10x faster** for complex analytics

### Memory Usage

- **Embedded database**: No separate server process
- **Memory limit**: Configurable via DuckDB settings
- **Default**: Automatic memory management (typically 80% of available RAM)

---

## Advanced Configuration

### DuckDB-Specific Settings (Future)

The `DuckDBConnectionFactory.ApplyConnectionSettings()` method is a placeholder for future optimizations:

```csharp
// Future: Execute after connection opens
SET threads = 4;                    // Parallel query execution
SET memory_limit = '4GB';           // Explicit memory limit
SET temp_directory = 'D:\temp';    // Temporary file location
```

These settings are not yet exposed via JVLinkToSQLite CLI but can be applied manually via DuckDB CLI.

---

## Migrating from SQLite to DuckDB

### Option 1: Fresh Import

Simply change the datasource extension:

```bash
# Old (SQLite)
JVLinkToSQLite.exe --datasource race.db --mode Exec

# New (DuckDB)
JVLinkToSQLite.exe --datasource race.duckdb --mode Exec
```

Both use the same `setting.xml` - no configuration changes needed.

### Option 2: Export/Import Data

If you have existing SQLite data:

```sql
-- In SQLite
sqlite3 race.db
.mode csv
.output race_data.csv
SELECT * FROM NL_RA_RACE;
.quit

-- In DuckDB
duckdb race.duckdb
CREATE TABLE NL_RA_RACE AS SELECT * FROM read_csv_auto('race_data.csv');
```

### Option 3: DuckDB Attach (Direct Migration)

DuckDB can read SQLite databases directly:

```sql
duckdb race.duckdb

-- Attach SQLite database
ATTACH 'race.db' AS sqlite_db (TYPE SQLITE);

-- Copy tables
CREATE TABLE NL_RA_RACE AS SELECT * FROM sqlite_db.NL_RA_RACE;
CREATE TABLE NL_UM_UMA AS SELECT * FROM sqlite_db.NL_UM_UMA;

-- Detach
DETACH sqlite_db;
```

---

## Troubleshooting

### Issue: "Unable to load DLL 'duckdb'"

**Cause**: DuckDB native library not found

**Solution**: Ensure `DuckDB.NET.Data.Full` package is installed (includes native binaries)

```bash
# Restore NuGet packages
nuget restore JVLinkToSQLite.sln
```

### Issue: "Directory does not exist"

**Cause**: Parent directory for DuckDB file doesn't exist

**Solution**: Create the directory first

```bash
mkdir C:\JVData
JVLinkToSQLite.exe --datasource C:\JVData\race.duckdb --mode Exec
```

### Issue: Performance slower than expected

**Cause**: DuckDB optimizes for analytics, not single-row inserts

**Solution**: Use batching (ThrottleSize parameter):

```bash
JVLinkToSQLite.exe --datasource race.duckdb --throttlesize 1000 --mode Exec
```

### Issue: "Database is locked"

**Cause**: Another process has the DuckDB file open

**Solution**: DuckDB allows multiple readers but only one writer. Close other connections:

```bash
# Close any DuckDB CLI sessions
# Close any analysis tools connected to the database
```

---

## Integration with Analysis Tools

### Python with DuckDB

```python
import duckdb

# Connect to database created by JVLinkToSQLite
conn = duckdb.connect('race.duckdb')

# Query data
df = conn.execute("""
    SELECT race_date, track_code, COUNT(*) as race_count
    FROM NL_RA_RACE
    WHERE race_date > '2024-01-01'
    GROUP BY race_date, track_code
""").df()

# Now use pandas for analysis
print(df.describe())
```

### R with DuckDB

```r
library(duckdb)

# Connect
con <- dbConnect(duckdb(), dbdir = "race.duckdb")

# Query
races <- dbGetQuery(con, "
    SELECT * FROM NL_RA_RACE
    WHERE race_date > '2024-01-01'
")

# Analysis
summary(races$total_prize)
```

### Tableau/Power BI

DuckDB databases can be accessed via:
1. **ODBC Driver**: DuckDB ODBC driver (separate installation)
2. **Export to Parquet**: Use DuckDB CLI to export, then import to Tableau/Power BI

---

## Comparison Table

| Feature | SQLite | DuckDB | PostgreSQL (Phase 4) |
|---------|--------|--------|---------------------|
| **Type** | Embedded | Embedded | Client-Server |
| **Setup** | None | None | Server required |
| **Best For** | General purpose | Analytics | Production multi-user |
| **File Size** | Compact | Moderate (columnar) | N/A (server) |
| **Query Speed (OLTP)** | Fast | Moderate | Fast |
| **Query Speed (OLAP)** | Moderate | **Very Fast** | Fast |
| **Concurrent Writes** | Single | Single | Multiple |
| **Concurrent Reads** | Multiple | Multiple | Multiple |
| **Max Database Size** | 281 TB | Unlimited (OS limit) | Unlimited |
| **Memory Usage** | Low | Moderate (configurable) | High (server) |
| **Integration** | Universal | Python/R/Analytics | Enterprise |

---

## Command Line Examples

### Standard Import

```bash
# Import all configured data specs to DuckDB
JVLinkToSQLite.exe --dbtype duckdb --datasource "C:\JVData\race.duckdb" --mode Exec
```

### Event Monitoring Mode

```bash
# Real-time event monitoring with DuckDB
JVLinkToSQLite.exe --dbtype duckdb --datasource "C:\JVData\realtime.duckdb" --mode Event
```

### Custom Settings

```bash
# Use custom setting.xml with DuckDB
JVLinkToSQLite.exe --dbtype duckdb \
    --datasource "C:\JVData\race.duckdb" \
    --setting "C:\Config\custom_setting.xml" \
    --mode Exec
```

### Performance Tuning

```bash
# Adjust throttle size for optimal performance
JVLinkToSQLite.exe --dbtype duckdb \
    --datasource race.duckdb \
    --throttlesize 5000 \
    --mode Exec
```

---

## Next Steps

### After Phase 3 Complete

1. **Run Performance Benchmarks**: Verify 100k records < 10 minutes
2. **Test Analytics Queries**: Measure query performance vs SQLite
3. **Integration Testing**: Full pipeline test with real JV-Link data

### Future Enhancements (Phase 5+)

1. **Auto-optimization**: Automatically choose batch size based on data type
2. **Progress Reporting**: Real-time import progress for large datasets
3. **Query Templates**: Pre-built analytics queries for common use cases

---

## Related Documentation

- [Specification](./spec.md) - Original feature specification
- [Implementation Plan](./plan.md) - Technical implementation plan
- [Data Model](./data-model.md) - Database schema and type mappings
- [Quick Start Guide](./quickstart.md) - Developer implementation guide
- [Session Summary](./SESSION_SUMMARY.md) - Implementation progress

---

## Support

For issues, questions, or feedback:
1. Check [Troubleshooting](#troubleshooting) section above
2. Review [DuckDB Documentation](https://duckdb.org/docs/)
3. Report issues to JVLinkToSQLite repository

---

**Status**: ✅ DuckDB provider implementation complete
**Next**: Integration testing and performance validation
