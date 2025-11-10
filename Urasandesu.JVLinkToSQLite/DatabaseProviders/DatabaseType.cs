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

namespace Urasandesu.JVLinkToSQLite.DatabaseProviders
{
    /// <summary>
    /// Specifies the type of database supported by JVLinkToSQLite.
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// SQLite database (file-based, embedded).
        /// Default database type for backward compatibility.
        /// </summary>
        SQLite = 0,

        /// <summary>
        /// DuckDB database (file-based, embedded, OLAP-optimized).
        /// Best for analytics and aggregation queries.
        /// </summary>
        DuckDB = 1,

        /// <summary>
        /// PostgreSQL database (client-server, enterprise-grade).
        /// Best for production deployments and multi-user access.
        /// </summary>
        PostgreSQL = 2
    }
}
