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
// 
// Additional permission under GNU GPL version 3 section 7
// 
// If you modify this Program, or any covered work, by linking or combining it with 
// ObscUra (or a modified version of that library), containing parts covered 
// by the terms of ObscUra's license, the licensors of this Program grant you 
// additional permission to convey the resulting work.

using System;
using Urasandesu.JVLinkToSQLite.DatabaseProviders;
using Urasandesu.JVLinkToSQLite.Settings;

namespace Urasandesu.JVLinkToSQLite
{
    public class JVLinkToSQLiteBootstrap
    {
        private readonly IXmlSerializationService _xss;

        public JVLinkToSQLiteBootstrap(IXmlSerializationService xss)
        {
            _xss = xss;
        }

        public class LoadSettingParameter
        {
            public string SettingXmlPath { get; set; }
            public string DatabaseType { get; set; }
            public string SQLiteDataSource { get; set; }
            public int SQLiteThrottleSize { get; set; }
        }

        public JVLinkToSQLiteSetting LoadSettingOrDefault(LoadSettingParameter param)
        {
            if (param == null)
            {
                throw new ArgumentNullException(nameof(param));
            }

            var setting = default(JVLinkToSQLiteSetting);
            if (_xss.ExistsXmlFile(param.SettingXmlPath))
            {
                using (var sr = _xss.NewDeserializingTextReader(param.SettingXmlPath))
                {
                    setting = _xss.Deserialize<JVLinkToSQLiteSetting>(sr);
                }
            }
            else
            {
                setting = JVLinkToSQLiteSetting.Default;
                using (var sw = _xss.NewSerializingTextWriter(param.SettingXmlPath))
                {
                    _xss.Serialize(sw, setting);
                }
            }

            // Create database provider based on type or auto-detect
            IDatabaseProvider databaseProvider;
            if (!string.IsNullOrEmpty(param.DatabaseType))
            {
                // Explicit database type specified
                DatabaseType dbType;
                if (!Enum.TryParse(param.DatabaseType, true, out dbType))
                {
                    throw new ArgumentException($"Invalid database type: {param.DatabaseType}. Valid values: sqlite, duckdb, postgresql", nameof(param));
                }
                databaseProvider = DatabaseProviderFactory.Create(dbType, param.SQLiteDataSource);
            }
            else
            {
                // Auto-detect from connection string
                databaseProvider = DatabaseProviderFactory.CreateFromConnectionString(param.SQLiteDataSource);
            }

            // Maintain backward compatibility with SQLiteConnectionInfo
            var connInfo = new SQLiteConnectionInfo(param.SQLiteDataSource, param.SQLiteThrottleSize);
            setting.FillWithSQLiteConnectionInfo(connInfo);

            // Store database provider in setting
            setting.FillWithDatabaseProvider(databaseProvider);

            return setting;
        }

        public void SaveSetting(string settingXmlPath, JVLinkToSQLiteSetting setting)
        {
            using (var sw = _xss.NewSerializingTextWriter(settingXmlPath))
            {
                _xss.Serialize(sw, setting);
            }
        }
    }
}
