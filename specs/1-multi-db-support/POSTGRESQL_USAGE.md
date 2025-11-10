# PostgreSQL Support - Usage Guide

**Feature**: Multi-Database Support - PostgreSQL Implementation
**Status**: ✅ Provider Implementation Complete
**Phase**: 4 of 6
**Last Updated**: 2025-11-10

---

## Overview

JVLinkToSQLite now supports **PostgreSQL** as a production-grade database backend. PostgreSQL is a powerful, open-source relational database management system known for:

- **Production-Ready**: Enterprise-grade RDBMS with ACID compliance
- **Multi-User Access**: Concurrent read/write operations from multiple applications
- **Advanced Features**: Full-text search, JSON support, advanced indexing
- **Scalability**: Handle millions of records with ease
- **Data Integrity**: Strong typing, constraints, and transactional guarantees
- **Client-Server Architecture**: Centralized data accessible from multiple machines

---

## Quick Start

### Prerequisites

1. **PostgreSQL Server**: Install PostgreSQL 9.6+ on your server or local machine
   - Download from: https://www.postgresql.org/download/
   - Or use Docker: `docker run --name jvlink-postgres -e POSTGRES_PASSWORD=yourpassword -p 5432:5432 -d postgres`

2. **Create Database**:
   ```sql
   -- Connect to PostgreSQL as superuser
   psql -U postgres

   -- Create database for JVLink data
   CREATE DATABASE jvlink;

   -- Create user (optional, for security)
   CREATE USER jvlink_user WITH PASSWORD 'your_secure_password';
   GRANT ALL PRIVILEGES ON DATABASE jvlink TO jvlink_user;
   ```

### Basic Usage

```bash
# Using connection string
JVLinkToSQLite.exe --dbtype postgresql \
  --datasource "Host=localhost;Database=jvlink;Username=jvlink_user;Password=yourpass" \
  --mode Exec

# Auto-detect from connection string pattern (Host= detected)
JVLinkToSQLite.exe \
  --datasource "Host=localhost;Database=jvlink;Username=jvlink_user;Password=yourpass" \
  --mode Exec

# Using environment variable for password (RECOMMENDED for security)
set JVLINK_DB_PASSWORD=yourpass
JVLinkToSQLite.exe --dbtype postgresql \
  --datasource "Host=localhost;Database=jvlink;Username=jvlink_user" \
  --mode Exec
```

### Remote Database

```bash
# Connect to remote PostgreSQL server
JVLinkToSQLite.exe --dbtype postgresql \
  --datasource "Host=192.168.1.100;Port=5432;Database=jvlink;Username=jvlink_user" \
  --mode Exec

# Connect via SSL (recommended for production)
JVLinkToSQLite.exe --dbtype postgresql \
  --datasource "Host=db.example.com;Database=jvlink;Username=jvlink_user;SSL Mode=Require" \
  --mode Exec
```

---

## Installation Requirements

### NuGet Packages (Already Installed)

The following packages are required and have been added to the project:

```xml
<package id="Npgsql" version="4.1.13" targetFramework="net48" />
<package id="EntityFramework6.Npgsql" version="6.4.3" targetFramework="net48" />
```

### PostgreSQL Server Installation

**Windows**:
1. Download installer from https://www.postgresql.org/download/windows/
2. Run installer and follow wizard
3. Set password for `postgres` superuser
4. Default port: 5432

**Docker** (recommended for development):
```bash
docker run --name jvlink-postgres \
  -e POSTGRES_DB=jvlink \
  -e POSTGRES_USER=jvlink_user \
  -e POSTGRES_PASSWORD=yourpass \
  -p 5432:5432 \
  -v jvlink_data:/var/lib/postgresql/data \
  -d postgres:14

# Verify connection
docker exec -it jvlink-postgres psql -U jvlink_user -d jvlink
```

**Linux** (Ubuntu/Debian):
```bash
sudo apt update
sudo apt install postgresql postgresql-contrib
sudo systemctl start postgresql
sudo -u postgres createdb jvlink
```

---

## PostgreSQL vs SQLite vs DuckDB: When to Use Each

### Use PostgreSQL When:

✅ **Production Multi-User Applications**
- Multiple users need simultaneous access
- Centralized data repository for teams
- Client-server architecture required

✅ **Data Integrity is Critical**
- Need ACID transactions
- Referential integrity (foreign keys)
- Complex constraints and validations

✅ **Advanced Queries and Features**
- Full-text search
- JSON/JSONB data types
- Advanced indexing (B-tree, GiST, GIN, BRIN)
- Window functions and CTEs

✅ **Scalability and Performance**
- Large datasets (100M+ records)
- High concurrent write loads
- Connection pooling required

### Use DuckDB When:

✅ **Analytics and Data Science**
- OLAP workloads
- Complex aggregations
- Integration with Python/R

### Use SQLite When:

✅ **Simplicity and Portability**
- Single-user applications
- Embedded scenarios
- No server installation wanted

---

## Type Mapping: PostgreSQL vs SQLite vs DuckDB

PostgreSQL provides the strongest type system with extensive validation:

| C# Type | SQLite Type | DuckDB Type | PostgreSQL Type | Notes |
|---------|-------------|-------------|-----------------|-------|
| `string` | TEXT | VARCHAR | TEXT | PostgreSQL supports VARCHAR(n) for length constraints |
| `int` | INTEGER | INTEGER | INTEGER | Same across all |
| `long` | INTEGER | BIGINT | **BIGINT** | PostgreSQL uses true 64-bit integer |
| `short` | INTEGER | SMALLINT | **SMALLINT** | PostgreSQL optimized 16-bit |
| `byte` | INTEGER | TINYINT | **SMALLINT** | PostgreSQL minimum is SMALLINT |
| `decimal` | REAL | DECIMAL(18,6) | **NUMERIC(18,6)** | PostgreSQL NUMERIC for precision |
| `double` | REAL | DOUBLE | **DOUBLE PRECISION** | Explicit double precision |
| `float` | REAL | REAL | **REAL** | Same |
| `DateTime` | TEXT | TIMESTAMP | **TIMESTAMP** | Native date/time with timezone support |
| `bool` | INTEGER (0/1) | BOOLEAN | **BOOLEAN** | Native true/false |
| `byte[]` | BLOB | BLOB | **BYTEA** | PostgreSQL binary type |

### Key Advantages of PostgreSQL Types:

1. **NUMERIC(18,6) for decimal**: Exact precision, no rounding errors
2. **TIMESTAMP for DateTime**: Native date/time operations, timezone support
3. **BOOLEAN for bool**: True SQL boolean type
4. **Constraints**: CHECK, UNIQUE, NOT NULL, FOREIGN KEY support
5. **Custom Types**: Ability to define custom composite types

---

## Connection String Format

### Basic Format

```
Host=hostname;Port=5432;Database=database_name;Username=user;Password=pass
```

### Common Parameters

| Parameter | Description | Default | Required |
|-----------|-------------|---------|----------|
| `Host` | Server hostname or IP | localhost | ✅ Yes |
| `Port` | Server port | 5432 | No |
| `Database` | Database name | - | ✅ Yes |
| `Username` | Database user | - | ✅ Yes |
| `Password` | User password | - | Usually Yes |
| `SSL Mode` | SSL/TLS mode | Disable | No |
| `Pooling` | Enable connection pooling | true | No |
| `Timeout` | Connection timeout (seconds) | 15 | No |

### Security: Environment Variable Password

**Recommended approach** (avoids storing password in command line):

```bash
# Set environment variable (Windows)
set JVLINK_DB_PASSWORD=my_secure_password

# Set environment variable (Linux/Mac)
export JVLINK_DB_PASSWORD=my_secure_password

# Run without password in connection string
JVLinkToSQLite.exe --dbtype postgresql \
  --datasource "Host=localhost;Database=jvlink;Username=jvlink_user" \
  --mode Exec
```

The PostgreSQL provider automatically reads `JVLINK_DB_PASSWORD` if password is not in connection string.

### SSL/TLS Connections

For production deployments, use SSL:

```bash
# Require SSL
--datasource "Host=db.example.com;Database=jvlink;Username=user;SSL Mode=Require"

# Verify SSL certificate
--datasource "Host=db.example.com;Database=jvlink;Username=user;SSL Mode=VerifyFull;Root Certificate=path/to/ca.crt"
```

---

## Example Queries

### After Importing Data with PostgreSQL

```sql
-- Connect to PostgreSQL
psql -U jvlink_user -d jvlink

-- Basic query (same as SQLite/DuckDB)
SELECT * FROM "NL_RA_RACE" WHERE race_date > '2024-01-01';

-- Advanced analytics with window functions
SELECT
    track_code,
    race_name,
    total_prize,
    AVG(total_prize) OVER (
        PARTITION BY track_code
        ORDER BY race_date
        ROWS BETWEEN 7 PRECEDING AND CURRENT ROW
    ) as rolling_avg_prize,
    RANK() OVER (PARTITION BY track_code ORDER BY total_prize DESC) as prize_rank
FROM "NL_RA_RACE"
WHERE race_date > '2024-01-01'
ORDER BY track_code, race_date;

-- Full-text search (PostgreSQL exclusive)
CREATE INDEX idx_race_name_fulltext ON "NL_RA_RACE" USING GIN(to_tsvector('japanese', race_name));

SELECT race_name, track_code
FROM "NL_RA_RACE"
WHERE to_tsvector('japanese', race_name) @@ to_tsquery('japanese', '天皇賞');

-- JSON aggregation (PostgreSQL 9.4+)
SELECT
    track_code,
    json_agg(json_build_object(
        'race_name', race_name,
        'total_prize', total_prize,
        'race_date', race_date
    )) as races
FROM "NL_RA_RACE"
WHERE race_date > '2024-01-01'
GROUP BY track_code;

-- Common Table Expressions (CTEs)
WITH top_tracks AS (
    SELECT track_code, AVG(total_prize) as avg_prize
    FROM "NL_RA_RACE"
    GROUP BY track_code
    HAVING AVG(total_prize) > 100000000
)
SELECT r.*
FROM "NL_RA_RACE" r
INNER JOIN top_tracks t ON r.track_code = t.track_code
ORDER BY r.race_date DESC;
```

---

## Performance Expectations

### Import Performance

Based on Phase 4 performance testing targets:

- **100,000 records**: < 10 minutes (similar to SQLite/DuckDB)
- **Batch inserts**: Optimized with batching
- **COPY command**: Future optimization for bulk inserts (10x faster)
- **ThrottleSize parameter**: Works identically to SQLite/DuckDB mode

### Query Performance

Expected performance compared to SQLite:

- **Simple SELECT**: Similar to SQLite
- **Indexed queries**: **2-5x faster** than SQLite
- **Complex JOINs**: **2-3x faster** than SQLite
- **Aggregations**: **1.5-2x faster** than SQLite
- **Concurrent queries**: **Much faster** (multiple users simultaneously)

### Concurrent Access

PostgreSQL advantage over SQLite/DuckDB:

- **Multiple concurrent writes**: Full MVCC support
- **No database locking**: Readers don't block writers
- **Connection pooling**: Reuse connections for better performance

### Memory Usage

- **Server process**: Dedicated PostgreSQL server (typically 100MB-1GB RAM)
- **Client minimal**: JVLinkToSQLite client uses minimal memory
- **Scalability**: Handles large datasets without loading into client memory

---

## Advanced Configuration

### Connection Pooling

Npgsql supports connection pooling by default:

```bash
# Configure pooling (in connection string)
--datasource "Host=localhost;Database=jvlink;Username=user;Pooling=true;MinPoolSize=1;MaxPoolSize=20"
```

### Server Performance Tuning

Edit `postgresql.conf` for optimal performance:

```conf
# Memory settings (adjust based on server RAM)
shared_buffers = 256MB              # 25% of system RAM
effective_cache_size = 1GB          # 50-75% of system RAM
work_mem = 16MB                     # Per-query memory

# Write-ahead log
wal_buffers = 16MB
checkpoint_completion_target = 0.9

# Query planning
random_page_cost = 1.1              # For SSD storage
effective_io_concurrency = 200      # For SSD storage
```

Restart PostgreSQL after configuration changes:
```bash
# Windows
net stop postgresql-x64-14
net start postgresql-x64-14

# Linux
sudo systemctl restart postgresql
```

### Indexing for Performance

Create indexes on frequently queried columns:

```sql
-- Index on race_date for date range queries
CREATE INDEX idx_race_date ON "NL_RA_RACE"(race_date);

-- Composite index for multi-column queries
CREATE INDEX idx_track_date ON "NL_RA_RACE"(track_code, race_date);

-- Unique constraint
CREATE UNIQUE INDEX idx_race_id ON "NL_RA_RACE"(race_id);

-- Analyze tables for query optimizer
ANALYZE "NL_RA_RACE";
```

---

## Migrating from SQLite/DuckDB to PostgreSQL

### Option 1: Fresh Import

Simply change the datasource to PostgreSQL connection string:

```bash
# Old (SQLite)
JVLinkToSQLite.exe --datasource race.db --mode Exec

# New (PostgreSQL)
JVLinkToSQLite.exe --dbtype postgresql \
  --datasource "Host=localhost;Database=jvlink;Username=jvlink_user" \
  --mode Exec
```

Both use the same `setting.xml` - no configuration changes needed.

### Option 2: DuckDB to PostgreSQL Export

DuckDB can export directly to PostgreSQL:

```sql
-- In DuckDB
ATTACH 'host=localhost dbname=jvlink user=jvlink_user' AS postgres_db (TYPE POSTGRES);

-- Copy tables
CREATE TABLE postgres_db."NL_RA_RACE" AS SELECT * FROM "NL_RA_RACE";
CREATE TABLE postgres_db."NL_UM_UMA" AS SELECT * FROM "NL_UM_UMA";
```

### Option 3: CSV Export/Import

```bash
# From SQLite
sqlite3 race.db
.mode csv
.output race_data.csv
SELECT * FROM NL_RA_RACE;
.quit

# To PostgreSQL
psql -U jvlink_user -d jvlink
\COPY "NL_RA_RACE" FROM 'race_data.csv' DELIMITER ',' CSV HEADER;
```

### Option 4: pg_dump and pg_restore (PostgreSQL to PostgreSQL)

```bash
# Backup
pg_dump -U jvlink_user -d jvlink -F c -f jvlink_backup.dump

# Restore
pg_restore -U jvlink_user -d jvlink_new jvlink_backup.dump
```

---

## Troubleshooting

### Issue: "Unable to connect to the server"

**Cause**: PostgreSQL server not running or connection refused

**Solution**:
```bash
# Check PostgreSQL status (Windows)
sc query postgresql-x64-14

# Check PostgreSQL status (Linux)
sudo systemctl status postgresql

# Check PostgreSQL is listening
netstat -an | findstr 5432

# Check pg_hba.conf allows your IP
sudo nano /etc/postgresql/14/main/pg_hba.conf
# Add: host    all    all    0.0.0.0/0    md5
```

### Issue: "Password authentication failed"

**Cause**: Wrong username or password

**Solution**:
```bash
# Reset password
psql -U postgres
ALTER USER jvlink_user WITH PASSWORD 'new_password';

# Or use environment variable
set JVLINK_DB_PASSWORD=correct_password
```

### Issue: "Database does not exist"

**Cause**: Target database not created

**Solution**:
```sql
-- Create database
psql -U postgres
CREATE DATABASE jvlink;
GRANT ALL PRIVILEGES ON DATABASE jvlink TO jvlink_user;
```

### Issue: "Permission denied for table NL_RA_RACE"

**Cause**: User lacks permissions

**Solution**:
```sql
-- Grant permissions
psql -U postgres -d jvlink
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO jvlink_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO jvlink_user;
```

### Issue: Performance slower than expected

**Cause**: Missing indexes or poor server configuration

**Solution**:
```sql
-- Create indexes on frequently queried columns
CREATE INDEX idx_race_date ON "NL_RA_RACE"(race_date);

-- Analyze tables
ANALYZE;

-- Check query plan
EXPLAIN ANALYZE SELECT * FROM "NL_RA_RACE" WHERE race_date > '2024-01-01';
```

### Issue: "Too many connections"

**Cause**: Connection pool exhausted

**Solution**:
```conf
# Edit postgresql.conf
max_connections = 100

# Or limit application connections
--datasource "Host=localhost;Database=jvlink;Username=user;MaxPoolSize=10"
```

---

## Integration with Analysis Tools

### Python with psycopg2

```python
import psycopg2
import pandas as pd

# Connect to PostgreSQL database
conn = psycopg2.connect(
    host="localhost",
    database="jvlink",
    user="jvlink_user",
    password="yourpass"
)

# Query data into pandas DataFrame
df = pd.read_sql_query("""
    SELECT race_date, track_code, race_name, total_prize
    FROM "NL_RA_RACE"
    WHERE race_date > '2024-01-01'
    ORDER BY race_date DESC
""", conn)

# Analysis
print(df.describe())
conn.close()
```

### R with RPostgreSQL

```r
library(RPostgreSQL)

# Connect
drv <- dbDriver("PostgreSQL")
con <- dbConnect(drv,
                 dbname = "jvlink",
                 host = "localhost",
                 port = 5432,
                 user = "jvlink_user",
                 password = "yourpass")

# Query
races <- dbGetQuery(con, "
    SELECT * FROM \"NL_RA_RACE\"
    WHERE race_date > '2024-01-01'
")

# Analysis
summary(races$total_prize)

dbDisconnect(con)
```

### Tableau / Power BI

PostgreSQL has first-class support in BI tools:

1. **Tableau**: Use built-in PostgreSQL connector
   - Server > PostgreSQL
   - Enter Host, Database, Username, Password
   - Select tables and create visualizations

2. **Power BI Desktop**:
   - Get Data > Database > PostgreSQL database
   - Enter server and database name
   - Select tables to import or use DirectQuery

3. **Excel**: Use ODBC driver
   - Install PostgreSQL ODBC driver
   - Data > From Other Sources > From ODBC
   - Configure DSN with connection details

---

## Comparison Table

| Feature | SQLite | DuckDB | **PostgreSQL** |
|---------|--------|--------|----------------|
| **Type** | Embedded | Embedded | **Client-Server** |
| **Setup** | None | None | **Server required** |
| **Best For** | General purpose | Analytics | **Production multi-user** |
| **File Size** | Compact | Moderate | **N/A (server storage)** |
| **Query Speed (OLTP)** | Fast | Moderate | **Fast** |
| **Query Speed (OLAP)** | Moderate | Very Fast | **Fast** |
| **Concurrent Writes** | Single | Single | **Multiple (MVCC)** |
| **Concurrent Reads** | Multiple | Multiple | **Multiple** |
| **Max Database Size** | 281 TB | Unlimited | **Unlimited** |
| **Memory Usage** | Low | Moderate | **Server: Moderate-High** |
| **Integration** | Universal | Python/R | **Enterprise/BI tools** |
| **Transactions** | Limited | Limited | **Full ACID** |
| **Constraints** | Basic | Basic | **Advanced (FK, CHECK, etc)** |
| **Full-Text Search** | FTS5 extension | Limited | **Native tsvector** |
| **JSON Support** | JSON1 extension | Native | **JSONB (indexed)** |
| **Replication** | Manual | No | **Built-in streaming** |
| **Backup** | File copy | File copy | **pg_dump, PITR** |

---

## Command Line Examples

### Standard Import

```bash
# Import all configured data specs to PostgreSQL
JVLinkToSQLite.exe --dbtype postgresql \
  --datasource "Host=localhost;Database=jvlink;Username=jvlink_user" \
  --mode Exec
```

### Event Monitoring Mode

```bash
# Real-time event monitoring with PostgreSQL
JVLinkToSQLite.exe --dbtype postgresql \
  --datasource "Host=localhost;Database=jvlink_realtime;Username=jvlink_user" \
  --mode Event
```

### Remote Server

```bash
# Connect to remote PostgreSQL server
JVLinkToSQLite.exe --dbtype postgresql \
  --datasource "Host=192.168.1.100;Port=5432;Database=jvlink;Username=jvlink_user;Password=pass" \
  --mode Exec
```

### Custom Settings with SSL

```bash
# Use custom setting.xml with SSL connection
JVLinkToSQLite.exe --dbtype postgresql \
  --datasource "Host=db.example.com;Database=jvlink;Username=jvlink_user;SSL Mode=Require" \
  --setting "C:\\Config\\custom_setting.xml" \
  --mode Exec
```

### Performance Tuning

```bash
# Adjust throttle size for bulk inserts
JVLinkToSQLite.exe --dbtype postgresql \
  --datasource "Host=localhost;Database=jvlink;Username=jvlink_user" \
  --throttlesize 5000 \
  --mode Exec
```

---

## Production Deployment Checklist

### Security

- [ ] Use strong passwords (minimum 12 characters, mixed case, numbers, symbols)
- [ ] Store password in `JVLINK_DB_PASSWORD` environment variable (not in scripts)
- [ ] Enable SSL/TLS connections (`SSL Mode=Require`)
- [ ] Configure `pg_hba.conf` to restrict IP addresses
- [ ] Create dedicated database user (not postgres superuser)
- [ ] Grant minimum necessary privileges
- [ ] Enable PostgreSQL audit logging
- [ ] Keep PostgreSQL server updated with security patches

### Performance

- [ ] Create indexes on frequently queried columns
- [ ] Tune `shared_buffers` and `work_mem` in postgresql.conf
- [ ] Enable connection pooling
- [ ] Run `ANALYZE` after data imports
- [ ] Monitor query performance with `pg_stat_statements`
- [ ] Set up regular `VACUUM` maintenance

### Reliability

- [ ] Configure automatic backups (`pg_dump` scheduled task)
- [ ] Enable WAL archiving for point-in-time recovery
- [ ] Monitor disk space (data directory)
- [ ] Set up monitoring/alerting (pg_monitor, Nagios, Prometheus)
- [ ] Test restore procedures regularly
- [ ] Document recovery procedures

### Monitoring

```sql
-- Active connections
SELECT count(*) FROM pg_stat_activity WHERE state = 'active';

-- Slow queries
SELECT pid, now() - query_start as duration, query
FROM pg_stat_activity
WHERE state = 'active' AND now() - query_start > interval '5 minutes';

-- Database size
SELECT pg_size_pretty(pg_database_size('jvlink'));

-- Table sizes
SELECT schemaname, tablename, pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename))
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

---

## Next Steps

### After Phase 4 Complete

1. **Run Integration Tests**: Verify PostgreSQL provider with real JV-Link data
2. **Performance Benchmarks**: Compare query performance vs SQLite/DuckDB
3. **Security Audit**: Verify password handling and SSL connections
4. **Load Testing**: Test concurrent access scenarios

### Future Enhancements (Phase 5+)

1. **COPY Optimization**: Use PostgreSQL COPY command for 10x faster bulk inserts
2. **Connection Retry Logic**: Automatic retry for transient connection failures
3. **Read Replicas**: Support for PostgreSQL streaming replication
4. **Partitioning**: Table partitioning for very large datasets

---

## Related Documentation

- [Specification](./spec.md) - Original feature specification
- [Implementation Plan](./plan.md) - Technical implementation plan
- [Data Model](./data-model.md) - Database schema and type mappings
- [DuckDB Usage Guide](./DUCKDB_USAGE.md) - DuckDB-specific features
- [Implementation Notes](./IMPLEMENTATION_NOTES.md) - Developer documentation
- [Session Summary](./SESSION_SUMMARY.md) - Implementation progress

---

## Support

For issues, questions, or feedback:

1. Check [Troubleshooting](#troubleshooting) section above
2. Review [PostgreSQL Documentation](https://www.postgresql.org/docs/)
3. Check Npgsql documentation: https://www.npgsql.org/doc/
4. Report issues to JVLinkToSQLite repository

---

**Status**: ✅ PostgreSQL provider implementation complete
**Next**: Integration testing and performance validation
**Production Ready**: Pending integration tests and security audit
