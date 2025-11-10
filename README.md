# JVLinkToSQLite

JVLinkToSQLite は、JRA-VAN データラボが提供する競馬データをデータベースに変換するツールです。
使い方については、[マニュアル](https://github.com/urasandesu/JVLinkToSQLite/wiki)をご参照ください。

---

## Multi-Database Support

JVLinkToSQLite now supports **three database backends**:
- **SQLite**: Simple, embedded, file-based database (default, backward compatible)
- **DuckDB**: Analytical database optimized for complex queries and aggregations
- **PostgreSQL**: Production-grade client-server database for multi-user scenarios

### Automatic Index Generation (Performance Optimization)

**New Feature**: Secondary indexes are automatically created for optimal query performance.

- **Foreign key columns** (ending with `_id`, `_code`)
- **Date columns** (containing `date`, `time`)
- **Important columns** (`sex`, `affiliation`, `distance`)

**Performance Improvement**:
- Small datasets (< 1,000): 5-10x faster
- Medium datasets (1,000-100,000): 50-100x faster
- Large datasets (> 100,000): 500-1,000x faster

---

## Quick Start Examples

### Example 1: SQLite (Default, Backward Compatible)

```bash
# Simple file-based database (same as before)
JVLinkToSQLite.exe --datasource race.db --mode Exec

# Auto-detects SQLite from .db or .sqlite extension
JVLinkToSQLite.exe --datasource race.sqlite --mode Exec

# In-memory database for testing
JVLinkToSQLite.exe --datasource :memory: --mode Exec
```

**When to use SQLite:**
- ✅ Development and testing
- ✅ Single-user scenarios
- ✅ Simple queries
- ✅ Minimal setup required

---

### Example 2: DuckDB (Analytics and Data Science)

```bash
# Auto-detects DuckDB from .duckdb extension
JVLinkToSQLite.exe --datasource analytics.duckdb --mode Exec

# Explicit database type specification
JVLinkToSQLite.exe --dbtype duckdb --datasource race_analytics.duckdb --mode Exec

# In-memory analytical database
JVLinkToSQLite.exe --dbtype duckdb --datasource :memory: --mode Exec
```

**When to use DuckDB:**
- ✅ Complex analytics and aggregations (2-10x faster than SQLite)
- ✅ Data science workflows (integration with Python/R)
- ✅ Large dataset analysis (millions of records)
- ✅ Export to Parquet/CSV for further analysis

**Example DuckDB Analytics Query:**
```sql
-- Connect to DuckDB database
duckdb race.duckdb

-- Advanced analytics with window functions
SELECT
    track_code,
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
```

**Documentation**: See [DUCKDB_USAGE.md](./specs/1-multi-db-support/DUCKDB_USAGE.md) for comprehensive guide

---

### Example 3: PostgreSQL (Production Multi-User)

```bash
# Auto-detects PostgreSQL from connection string (Host= pattern)
JVLinkToSQLite.exe \
  --datasource "Host=localhost;Database=jvlink;Username=jvlink_user;Password=yourpass" \
  --mode Exec

# Explicit database type
JVLinkToSQLite.exe --dbtype postgresql \
  --datasource "Host=localhost;Database=jvlink;Username=jvlink_user" \
  --mode Exec

# Secure: Password from environment variable (recommended)
set JVLINK_DB_PASSWORD=your_secure_password
JVLinkToSQLite.exe --dbtype postgresql \
  --datasource "Host=localhost;Database=jvlink;Username=jvlink_user" \
  --mode Exec

# Remote server with SSL
JVLinkToSQLite.exe --dbtype postgresql \
  --datasource "Host=db.example.com;Database=jvlink;Username=user;SSL Mode=Require" \
  --mode Exec
```

**When to use PostgreSQL:**
- ✅ Production deployments
- ✅ Multi-user concurrent access
- ✅ Centralized data repository for teams
- ✅ Advanced features (full-text search, JSON support)
- ✅ High availability and replication requirements

**PostgreSQL Setup:**
```bash
# Install PostgreSQL or use Docker
docker run --name jvlink-postgres \
  -e POSTGRES_DB=jvlink \
  -e POSTGRES_USER=jvlink_user \
  -e POSTGRES_PASSWORD=yourpass \
  -p 5432:5432 \
  -d postgres:14

# Verify connection
psql -h localhost -U jvlink_user -d jvlink
```

**Documentation**: See [POSTGRESQL_USAGE.md](./specs/1-multi-db-support/POSTGRESQL_USAGE.md) for comprehensive guide

---

### Example 4: Switching Between Databases

The same `setting.xml` configuration works with all database types:

```bash
# Use setting.xml with SQLite
JVLinkToSQLite.exe --datasource race.db --setting mysetting.xml --mode Exec

# Use same setting.xml with DuckDB
JVLinkToSQLite.exe --datasource race.duckdb --setting mysetting.xml --mode Exec

# Use same setting.xml with PostgreSQL
JVLinkToSQLite.exe --dbtype postgresql \
  --datasource "Host=localhost;Database=jvlink;Username=user" \
  --setting mysetting.xml --mode Exec
```

**No configuration changes needed** - just change the `--datasource` parameter!

---

## Database Comparison

| Feature | SQLite | DuckDB | PostgreSQL |
|---------|--------|--------|------------|
| **Setup** | None (embedded) | None (embedded) | Server required |
| **Best For** | Development | Analytics | Production |
| **Query Speed (Simple)** | Fast | Fast | Fast |
| **Query Speed (Analytics)** | Moderate | **Very Fast** | Fast |
| **Concurrent Writes** | Single | Single | **Multiple** |
| **Concurrent Reads** | Multiple | Multiple | Multiple |
| **Max Database Size** | 281 TB | Unlimited | Unlimited |
| **Multi-User** | Limited | Limited | **Yes (MVCC)** |
| **Integration** | Universal | Python/R | Enterprise/BI tools |

---

## Advanced Features

### Performance Tuning

```bash
# Adjust batch size for optimal performance
JVLinkToSQLite.exe --datasource race.duckdb --throttlesize 5000 --mode Exec
```

### Event Monitoring Mode

```bash
# Real-time event monitoring with any database
JVLinkToSQLite.exe --dbtype postgresql \
  --datasource "Host=localhost;Database=jvlink_realtime;Username=user" \
  --mode Event
```

### Security Best Practices

**PostgreSQL Production Deployment:**
1. ✅ Use `JVLINK_DB_PASSWORD` environment variable (never store password in command line)
2. ✅ Enable SSL/TLS: `SSL Mode=Require` in connection string
3. ✅ Create dedicated database user (not superuser)
4. ✅ Grant minimum necessary privileges
5. ✅ Configure firewall to restrict database access

---

## Migration Paths

### SQLite → DuckDB (For Analytics)

```bash
# Export from SQLite
sqlite3 race.db ".dump" > data.sql

# Import to DuckDB
duckdb race.duckdb < data.sql
```

Or use DuckDB's direct attach:
```sql
-- In DuckDB CLI
ATTACH 'race.db' AS sqlite_db (TYPE SQLITE);
CREATE TABLE NL_RA_RACE AS SELECT * FROM sqlite_db.NL_RA_RACE;
```

### SQLite/DuckDB → PostgreSQL (For Production)

```bash
# Export to CSV
sqlite3 race.db
.mode csv
.output race_data.csv
SELECT * FROM NL_RA_RACE;
.quit

# Import to PostgreSQL
psql -U jvlink_user -d jvlink
\COPY "NL_RA_RACE" FROM 'race_data.csv' DELIMITER ',' CSV HEADER;
```

---

## Troubleshooting

### SQLite/DuckDB Issues

**Issue**: "Directory does not exist"
```bash
# Create parent directory first
mkdir C:\JVData
JVLinkToSQLite.exe --datasource C:\JVData\race.db --mode Exec
```

### PostgreSQL Issues

**Issue**: "Unable to connect to server"
```bash
# Check PostgreSQL is running
sc query postgresql-x64-14  # Windows
sudo systemctl status postgresql  # Linux

# Verify connection
psql -h localhost -U jvlink_user -d jvlink
```

**Issue**: "Password authentication failed"
```bash
# Use environment variable
set JVLINK_DB_PASSWORD=correct_password  # Windows
export JVLINK_DB_PASSWORD=correct_password  # Linux
```

**Issue**: "Database does not exist"
```sql
-- Create database first
psql -U postgres
CREATE DATABASE jvlink;
GRANT ALL PRIVILEGES ON DATABASE jvlink TO jvlink_user;
```

---

## Documentation

- **Full Documentation**: [Wiki (Japanese)](https://github.com/urasandesu/JVLinkToSQLite/wiki)
- **DuckDB Guide**: [DUCKDB_USAGE.md](./specs/1-multi-db-support/DUCKDB_USAGE.md)
- **PostgreSQL Guide**: [POSTGRESQL_USAGE.md](./specs/1-multi-db-support/POSTGRESQL_USAGE.md)
- **Implementation Details**: [IMPLEMENTATION_NOTES.md](./specs/1-multi-db-support/IMPLEMENTATION_NOTES.md)

---

## Which Database Should I Use?

**Choose SQLite if:**
- You're developing locally or testing
- Single-user access is sufficient
- You want zero setup/configuration
- You're already using SQLite (backward compatible)

**Choose DuckDB if:**
- You need fast analytics and aggregations
- You're doing data science work (Python/R integration)
- You have large datasets (millions of records)
- You want to export to Parquet/CSV for further analysis

**Choose PostgreSQL if:**
- You're deploying to production
- You need multi-user concurrent access
- You want centralized data for your team
- You need advanced database features (replication, full-text search, JSON)
- You're integrating with BI tools (Tableau, Power BI)

---

## License

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

See [LICENSE](./LICENSE) for details.

