# Implementation Plan: Multi-Database Support (DuckDB and PostgreSQL)

**Branch**: `1-multi-db-support` | **Date**: 2025-11-09 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/1-multi-db-support/spec.md`

## Summary

JVLinkToSQLite に DuckDB と PostgreSQL のサポートを追加し、ユーザーが用途に応じてデータベースを選択できるようにする。既存の SQLite サポートを維持しつつ、データベース抽象化レイヤーを導入して、各データベース固有の SQL 生成とデータ型マッピングを実現する。

**技術アプローチ**:
- データベースプロバイダーパターンによる抽象化
- DuckDB.NET と Npgsql を NuGet 経由で統合
- 既存の DataBridge 実装を拡張して SQL 生成を抽象化
- ファイル拡張子または接続文字列形式からデータベースタイプを自動検出

## Technical Context

**Language/Version**: C# / .NET Framework 4.x（既存プロジェクトとの互換性維持）
**Primary Dependencies**:
- 既存: Entity Framework 6, DryIoc, CommandLineParser, NUnit, NSubstitute
- 新規: DuckDB.NET (NuGet), Npgsql (NuGet)

**Storage**: SQLite (既存), DuckDB (新規), PostgreSQL (新規)
**Testing**: NUnit + NSubstitute（既存テストフレームワーク維持）
**Target Platform**: Windows Desktop Application (.NET Framework)
**Project Type**: Multi-project solution (CLI + Core Libraries + Tests)

**Performance Goals**:
- 10万レコード/10分以内（全データベースタイプ）
- DuckDB で集計クエリが SQLite 比 3倍高速化
- PostgreSQL で 5クライアント以上の同時接続対応

**Constraints**:
- 既存の SQLite 実装との後方互換性維持
- setting.xml の構造変更なし
- 既存のプロジェクト構造（4プロジェクト）を維持
- GPLv3 ライセンス互換性確保

**Scale/Scope**:
- 40種類以上の JV-Data 型に対応する DataBridge
- 3つのデータベースタイプ（SQLite, DuckDB, PostgreSQL）
- 既存の JVLinkToSQLite コードベース（約 10万行）への非破壊的な拡張

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I: Modularity and Separation of Concerns
**Status**: ✅ PASS

**Validation**:
- データベース抽象化は `Urasandesu.JVLinkToSQLite` プロジェクトに配置
- 新しい名前空間 `Urasandesu.JVLinkToSQLite.DatabaseProviders` を追加
- CLI (`JVLinkToSQLite`) は引数解析のみ、ビジネスロジックは分離
- プロジェクト間の依存関係は一方向を維持

### Principle II: Data Integrity and Reliability
**Status**: ✅ PASS

**Validation**:
- 各データベースでトランザクション管理を実装
- データ型マッピングは明示的に定義（SQL方言対応）
- エラー時のロールバック処理を各プロバイダーで実装
- NULL 値ハンドリングをドキュメント化

### Principle III: Test-First Development
**Status**: ✅ PASS (with plan)

**Validation**:
- Phase 2 でテストを先に作成する計画
- 各データベースプロバイダーに対するユニットテスト
- 統合テスト（10万レコード）を実施
- テストカバレッジ目標: 新規コード 80%以上

**Test Structure**:
- `Test.Urasandesu.JVLinkToSQLite` にデータベースプロバイダーのテスト追加
- モック: NSubstitute で各データベース接続をモック
- 統合テスト: 実際の DuckDB/PostgreSQL インスタンスで検証

### Principle IV: Performance and Scalability
**Status**: ✅ PASS

**Validation**:
- 既存の ThrottleSize パラメータを全データベースで使用
- 非同期処理パターンを維持
- DuckDB: バッチ挿入の最適化
- PostgreSQL: 接続プール、準備済みステートメント使用

**Performance Benchmarks**:
- 各データベースで 10万レコード挿入テスト
- DuckDB の集計クエリパフォーマンス測定

### Principle V: Security and Validation
**Status**: ✅ PASS

**Validation**:
- データベースタイプの自動検出に入力検証を追加
- PostgreSQL 接続文字列のパストラバーサル攻撃対策
- 環境変数 `JVLINK_DB_PASSWORD` からパスワード読み取り
- エラーメッセージに接続詳細を含めない

### Principle VI: API Compatibility
**Status**: ✅ PASS

**Validation**:
- 既存の JVLink API ラッパーは変更なし
- setting.xml との互換性維持
- コマンドライン引数の追加のみ（`--dbtype`）
- 既存の SQLite 動作は完全に保持

### Principle VII: Observability and Logging
**Status**: ✅ PASS

**Validation**:
- データベース操作を既存の `IJVServiceOperationListener` で通知
- 接続エラー時に詳細なログ記録
- パフォーマンスメトリクス（処理時間、レコード数）を出力

### Constitution Compliance Summary

**Overall**: ✅ **ALL GATES PASS**

全ての原則に準拠しており、Constitution 違反はありません。既存のプロジェクト構造を維持し、非破壊的な拡張として実装します。

## Project Structure

### Documentation (this feature)

```
specs/1-multi-db-support/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0: Technology research and decisions
├── data-model.md        # Phase 1: Data model and abstractions
├── quickstart.md        # Phase 1: Quick start guide
├── contracts/           # Phase 1: Interface contracts
│   ├── IDatabaseProvider.cs    # Database provider interface
│   ├── ISqlGenerator.cs        # SQL generator interface
│   └── IConnectionFactory.cs   # Connection factory interface
├── checklists/
│   └── requirements.md  # Requirements checklist (completed)
└── tasks.md             # Phase 2: Implementation tasks (to be created)
```

### Source Code (repository root)

```
JVLinkToSQLite/                                    # CLI Application
├── MainOptions.cs                                  # [MODIFY] Add --dbtype parameter
├── SettingOptions.cs                               # No changes
├── Program.cs                                      # No changes
└── Properties/

Urasandesu.JVLinkToSQLite/                          # Core Library
├── JVLinkWrappers/                                 # No changes (JVLink API wrapper)
│   ├── JVLinkService.cs
│   ├── DataBridges/
│   │   ├── DataBridgeFactory.cs                    # [MODIFY] Use database provider
│   │   ├── DataBridge.cs                           # [MODIFY] Abstract SQL generation
│   │   └── ... (40+ DataBridge implementations)
│   └── ...
├── DatabaseProviders/                              # [NEW] Database abstraction layer
│   ├── IDatabaseProvider.cs                        # Database provider interface
│   ├── ISqlGenerator.cs                            # SQL generator interface
│   ├── IConnectionFactory.cs                       # Connection factory interface
│   ├── DatabaseProviderFactory.cs                  # Factory for providers
│   ├── SQLite/
│   │   ├── SQLiteDatabaseProvider.cs               # SQLite implementation
│   │   ├── SQLiteSqlGenerator.cs
│   │   └── SQLiteConnectionFactory.cs
│   ├── DuckDB/
│   │   ├── DuckDBDatabaseProvider.cs               # DuckDB implementation
│   │   ├── DuckDBSqlGenerator.cs
│   │   └── DuckDBConnectionFactory.cs
│   └── PostgreSQL/
│       ├── PostgreSQLDatabaseProvider.cs           # PostgreSQL implementation
│       ├── PostgreSQLSqlGenerator.cs
│       └── PostgreSQLConnectionFactory.cs
├── Settings/                                        # No changes
│   ├── JVLinkToSQLiteSetting.cs
│   └── SQLiteConnectionInfo.cs                     # [MODIFY] Rename to DatabaseConnectionInfo
├── JVLinkToSQLiteBootstrap.cs                      # [MODIFY] Initialize database provider
└── ...

Urasandesu.JVLinkToSQLite.Basis/                    # Foundation Library
└── ... (No changes expected)

Urasandesu.JVLinkToSQLite.JVData/                   # JV-Data Structures
└── ... (No changes)

Test.Urasandesu.JVLinkToSQLite/                     # Tests
├── DatabaseProviders/                              # [NEW] Provider tests
│   ├── SQLiteDatabaseProviderTests.cs
│   ├── DuckDBDatabaseProviderTests.cs
│   ├── PostgreSQLDatabaseProviderTests.cs
│   └── DatabaseProviderFactoryTests.cs
└── ...

Test.Urasandesu.JVLinkToSQLite.Basis/               # Basis Tests
└── ... (No changes expected)

Test.Urasandesu.JVLinkToSQLite.JVData/              # JVData Tests
└── ... (No changes expected)

ObfuscatedResources/                                # Obfuscation
└── ... (No changes)
```

**Structure Decision**:

既存の 4 プロジェクト構造を維持し、新機能は `Urasandesu.JVLinkToSQLite` プロジェクト内の新しい名前空間 `DatabaseProviders` に配置します。これにより：

1. **Principle I (Modularity)** に準拠: データベース抽象化は独立した名前空間
2. **後方互換性**: 既存のプロジェクト依存関係は変更なし
3. **テスト容易性**: `Test.Urasandesu.JVLinkToSQLite` に対応するテストを追加

## Complexity Tracking

**No violations detected.** All Constitutional principles are satisfied without requiring additional justification.

## Implementation Phases

### Phase 0: Research and Technology Decisions ✅

**Status**: ✅ **COMPLETE**
**Output**: `research.md`
**Completed**: 2025-11-09

**Completed Research**:
1. ✅ DuckDB.NET API and best practices (DuckDB.NET.Data.Full 1.4.1)
2. ✅ Npgsql API and Entity Framework 6 integration (Npgsql 4.1.x + EF6.Npgsql 6.4.x)
3. ✅ SQL dialect differences (DDL, data types, case sensitivity)
4. ✅ Connection pooling strategies (PostgreSQL built-in pooling)
5. ✅ Transaction isolation levels (ReadCommitted default for all)

### Phase 1: Design and Contracts ✅

**Status**: ✅ **COMPLETE**
**Output**: `data-model.md`, `contracts/`, `quickstart.md`
**Completed**: 2025-11-09

**Completed Design Artifacts**:
1. ✅ Database provider interface contracts (IDatabaseProvider, ISqlGenerator, IConnectionFactory)
2. ✅ SQL generator abstraction with type mapping tables
3. ✅ Data type mapping tables (SQLite, DuckDB, PostgreSQL)
4. ✅ Error handling strategy (exception hierarchy, retry logic)
5. ✅ Quick start guide for developers (step-by-step implementation)

**Created Files**:
- `contracts/IDatabaseProvider.cs` - Database provider interface
- `contracts/ISqlGenerator.cs` - SQL generator interface
- `contracts/IConnectionFactory.cs` - Connection factory interface
- `contracts/DatabaseType.cs` - Database type enumeration
- `data-model.md` - Complete data model documentation
- `quickstart.md` - Developer implementation guide

### Phase 2: Implementation Tasks ⏳

**Status**: **READY** - Awaiting `/speckit.tasks` command
**Output**: `tasks.md`

Tasks will be generated based on completed Phase 1 design artifacts.

## Next Steps

1. ✅ Complete Phase 0: Execute research and create `research.md`
2. ✅ Complete Phase 1: Create design artifacts
3. ⏩ Run `/speckit.tasks` to generate implementation tasks
4. ⏳ Run `/speckit.implement` to execute tasks

---

**Plan Status**: ✅ **Phase 0 & Phase 1 Complete** - Ready for `/speckit.tasks`
