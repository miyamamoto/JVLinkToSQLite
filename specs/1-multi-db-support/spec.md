# Feature Specification: Multi-Database Support (DuckDB and PostgreSQL)

**Feature Branch**: `1-multi-db-support`
**Created**: 2025-11-09
**Status**: Ready for Planning
**Input**: User description: "duckdbとpostgresqlにも対応したいです。"
**Clarifications**: Resolved (Q1: A, Q2: A, Q3: B)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - DuckDB への JV-Data エクスポート (Priority: P1)

ユーザーは、JRA-VAN から取得した競馬データを DuckDB データベースに変換して保存したい。DuckDB は分析クエリに優れたパフォーマンスを発揮し、大量データの集計や分析に適している。

**Why this priority**: DuckDB は OLAP ワークロードに特化しており、競馬データの統計分析や機械学習の前処理に最適。SQLite と同様にファイルベースで配布が容易なため、最初の追加データベースとして最適。

**Independent Test**: コマンドラインで `--dbtype duckdb` を指定して実行し、DuckDB ファイルが正常に作成され、JV-Data が正しく挿入されることを確認できる。

**Acceptance Scenarios**:

1. **Given** JVLinkToSQLite が DuckDB サポートでビルドされている、**When** ユーザーが `JVLinkToSQLite.exe --dbtype duckdb --datasource race.duckdb` を実行する、**Then** race.duckdb ファイルが作成され、JV-Data が DuckDB テーブルに挿入される
2. **Given** DuckDB ファイルが作成済み、**When** ユーザーが DuckDB CLI で `SELECT COUNT(*) FROM JV_RA_RACE` を実行する、**Then** 正しいレコード数が返される
3. **Given** 既存の SQLite データベースを使用していたユーザー、**When** DuckDB に切り替える、**Then** 同じ設定ファイル（setting.xml）がそのまま使用でき、データ種別設定が適用される

---

### User Story 2 - PostgreSQL への JV-Data エクスポート (Priority: P2)

ユーザーは、JRA-VAN から取得した競馬データを PostgreSQL データベースに変換して保存したい。PostgreSQL は Web アプリケーションのバックエンドとして広く使われており、複数ユーザーからの同時アクセスや高可用性が必要な場合に適している。

**Why this priority**: PostgreSQL は本番環境での運用に適したリレーショナルデータベースで、チーム内でのデータ共有や Web API の提供に必要。ただし、セットアップが必要なため P2。

**Independent Test**: PostgreSQL サーバーを起動し、接続文字列を指定して実行し、テーブルが作成されデータが挿入されることを確認できる。

**Acceptance Scenarios**:

1. **Given** PostgreSQL サーバーが稼働中、**When** ユーザーが `JVLinkToSQLite.exe --dbtype postgresql --datasource "Host=localhost;Database=jvdata;Username=user;Password=pass"` を実行する、**Then** PostgreSQL にテーブルが作成され、JV-Data が挿入される
2. **Given** PostgreSQL データベースが作成済み、**When** 複数のクライアントが同時にデータを読み取る、**Then** デッドロックやエラーなく正常に読み取れる
3. **Given** PostgreSQL への接続が一時的に失敗、**When** リトライロジックが動作する、**Then** 接続が回復後、データ挿入が再開される

---

### User Story 3 - データベース選択の柔軟性 (Priority: P3)

ユーザーは、プロジェクトや用途に応じて、SQLite、DuckDB、PostgreSQL を自由に選択して使い分けたい。開発時は SQLite、分析時は DuckDB、本番運用は PostgreSQL といった使い分けが可能。

**Why this priority**: 各データベースの特性を活かした柔軟な運用を可能にする。ただし、基本的な機能は P1/P2 で実現されるため P3。

**Independent Test**: 同じ setting.xml を使用して、異なるデータベースタイプで実行し、全て正常に動作することを確認できる。

**Acceptance Scenarios**:

1. **Given** ユーザーが複数のデータベースタイプを試したい、**When** `--dbtype` パラメータを変更して実行する、**Then** 各データベースに同じスキーマでテーブルが作成される
2. **Given** 既存の SQLite データベースがある、**When** ユーザーが DuckDB または PostgreSQL に切り替える、**Then** データ移行ツールやガイドが提供される（将来機能として）
3. **Given** 設定ファイルにデフォルトのデータベースタイプを保存、**When** コマンドライン引数を省略して実行する、**Then** 設定ファイルで指定したデータベースタイプが使用される

---

### Edge Cases

- データベース接続が失敗した場合、どのようなエラーメッセージを表示するか？（接続文字列の検証、わかりやすいエラーメッセージ）
- DuckDB または PostgreSQL が利用できない環境で実行された場合、どう対応するか？（依存関係チェック、適切なエラーメッセージ）
- 大量データ挿入中にデータベース接続が切断された場合、どう復旧するか？（トランザクション管理、チェックポイント）
- 各データベースの SQL 方言の違いにどう対応するか？（DDL の差異、データ型マッピング）
- PostgreSQL の接続プールやタイムアウト設定はどう扱うか？（デフォルト値、設定可能なパラメータ）

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: システムは SQLite、DuckDB、PostgreSQL の 3 つのデータベースタイプをサポートしなければならない
- **FR-002**: ユーザーはコマンドライン引数 `--dbtype [sqlite|duckdb|postgresql]` でデータベースタイプを指定できなければならない
- **FR-003**: `--datasource` パラメータは、各データベースタイプに応じた形式を受け付けなければならない：
  - SQLite/DuckDB: ファイルパス（例: `race.db`, `race.duckdb`）
  - PostgreSQL: 接続文字列（例: `Host=localhost;Database=jvdata;Username=user`）。パスワードは環境変数 `JVLINK_DB_PASSWORD` から読み取る
- **FR-004**: データベースタイプが指定されない場合、ファイル拡張子から自動検出しなければならない（.db または .sqlite → SQLite、.duckdb → DuckDB、接続文字列形式 → PostgreSQL）。検出できない場合はエラーメッセージを表示する
- **FR-005**: 各データベースに対して、JV-Data 構造体に対応するテーブルを作成しなければならない（40種類以上の DataBridge）
- **FR-006**: データベース固有の SQL 方言に対応したテーブル作成 DDL を生成しなければならない
- **FR-007**: データ型マッピングは各データベースに最適化されなければならない（例: SQLite の TEXT、PostgreSQL の VARCHAR、DuckDB の STRING）
- **FR-008**: トランザクション管理は各データベースのベストプラクティスに従わなければならない
- **FR-009**: エラーハンドリングは、データベース固有のエラーコードを適切に解釈し、ユーザーにわかりやすいメッセージを提供しなければならない
- **FR-010**: PostgreSQL の場合、接続プール、タイムアウト、リトライロジックを実装しなければならない。パスワードは環境変数 `JVLINK_DB_PASSWORD` から読み取り、コマンドライン履歴に残さない
- **FR-011**: パフォーマンステストは、各データベースタイプで 10 万レコード以上のデータ挿入を検証しなければならない
- **FR-012**: 既存の SQLite 向けの設定ファイル（setting.xml）は、他のデータベースタイプでもそのまま使用できなければならない
- **FR-013**: 依存関係（DuckDB、Npgsql）は NuGet パッケージとして管理し、ビルド時に自動的に解決されなければならない

### Key Entities

- **Database Provider**: データベースタイプ（SQLite, DuckDB, PostgreSQL）を抽象化し、統一されたインターフェイスを提供
  - 属性: タイプ、接続文字列、DDL ジェネレーター、データ型マッパー
  - 関係: DataBridge と連携してデータベース固有の SQL を生成

- **Connection Configuration**: データベース接続設定
  - 属性: データベースタイプ、データソース（ファイルパスまたは接続文字列）、接続オプション
  - 関係: JVLinkToSQLiteBootstrap で初期化時に読み込まれる

- **SQL Generator**: データベース固有の SQL 文を生成
  - 属性: CREATE TABLE DDL、INSERT 文、データ型マッピングルール
  - 関係: JVDataStructCreateTableSources、JVDataStructInsertSources を拡張

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: ユーザーは、SQLite、DuckDB、PostgreSQL のいずれかを選択して JV-Data をエクスポートでき、コマンド実行から完了まで追加の設定なしで動作する
- **SC-002**: 10 万レコードのデータ挿入が、各データベースタイプで 10 分以内に完了する
- **SC-003**: DuckDB への切り替えにより、集計クエリ（例: 過去 10 年間のレース統計）のパフォーマンスが SQLite と比較して 3 倍以上向上する
- **SC-004**: PostgreSQL を使用した場合、複数のクライアント（5 クライアント以上）が同時にデータを読み取っても、エラーやパフォーマンス低下が発生しない
- **SC-005**: データベース接続エラーが発生した場合、90% 以上のケースで、ユーザーが問題を特定し解決できるエラーメッセージが表示される
- **SC-006**: 既存の SQLite ユーザーが DuckDB または PostgreSQL に切り替える際、設定ファイルの変更は不要で、コマンドライン引数の追加のみで移行できる
- **SC-007**: 全ての既存ユニットテストが、SQLite、DuckDB、PostgreSQL の 3 つ全てで成功する（データベース固有のテストを除く）

## Scope & Boundaries

### In Scope

- SQLite、DuckDB、PostgreSQL の 3 データベースへの対応
- コマンドライン引数によるデータベースタイプ選択
- データベース固有の SQL 生成とデータ型マッピング
- 各データベースに対するテーブル作成と データ挿入
- トランザクション管理とエラーハンドリング
- 既存の設定ファイル（setting.xml）との互換性維持

### Out of Scope

- **SQLite から DuckDB/PostgreSQL へのデータ移行ツール**: 今回のフェーズでは新規データのみ対応。既存データの移行は将来のフェーズで検討。ユーザーは外部ツール（DuckDB CLI、PostgreSQL COPY コマンドなど）を使用して手動で移行可能
- データベース間のデータ同期機能
- GUI によるデータベース選択
- MongoDB、MySQL などの他のデータベースへの対応
- パフォーマンスチューニングのための自動インデックス作成（手動での最適化は可能）
- データベースバックアップ・リストア機能

## Dependencies & Assumptions

### Dependencies

- **DuckDB.NET**: DuckDB サポートに必要な .NET ライブラリ（NuGet パッケージ）
- **Npgsql**: PostgreSQL サポートに必要な .NET ライブラリ（NuGet パッケージ）
- **Entity Framework 6**: 既存の O/RM フレームワーク（拡張して使用）
- **JVLink API**: データソースとして継続使用

### Assumptions

- ユーザーは、PostgreSQL を使用する場合、PostgreSQL サーバーを事前にセットアップしている
- PostgreSQL を使用する場合、ユーザーは環境変数 `JVLINK_DB_PASSWORD` にパスワードを設定している
- DuckDB と PostgreSQL のライセンスは GPLv3 と互換性がある（MIT ライセンス）
- 既存の DataBridge 実装は、データベース固有の SQL 生成部分を抽象化することで再利用可能
- パフォーマンス要件（10 万レコード / 10 分）は、一般的な PC 環境（CPU: Intel Core i5 相当、メモリ: 8GB 以上）で達成可能
- データベースタイプの自動検出は、ファイル拡張子または接続文字列の形式から判定できる

## Clarification Decisions

以下の設計決定がユーザーによって承認されました：

### 決定 1: データベースタイプのデフォルト動作
**選択**: ファイル拡張子から自動検出
- `.db` または `.sqlite` → SQLite
- `.duckdb` → DuckDB
- 接続文字列形式（`Host=...` など）→ PostgreSQL
- 検出できない場合はエラーメッセージを表示

**根拠**: ユーザーフレンドリーで、ファイル名から意図が明確。誤検出を防ぐため、検出失敗時は明確なエラーメッセージを表示。

### 決定 2: 既存データの移行サポート
**選択**: 移行機能は将来のフェーズとし、今回は新規データのみ対応

**根拠**: 開発スコープを抑え、早期リリースを優先。ユーザーは外部ツール（DuckDB CLI、PostgreSQL COPY コマンド）を使用して手動で移行可能。

### 決定 3: PostgreSQL 接続設定の方法
**選択**: 環境変数 `JVLINK_DB_PASSWORD` からパスワードを読み取り

**根拠**: セキュリティを向上させ、コマンド履歴にパスワードが残らない。環境変数の設定は一般的な運用プラクティスで、ユーザーにとって理解しやすい。

## Notes

この仕様書は、JVLinkToSQLite Constitution v1.0.0 に準拠しています：
- **Principle I (Modularity)**: データベースプロバイダーを抽象化し、既存のプロジェクト構造を維持
- **Principle II (Data Integrity)**: 各データベースでトランザクション管理を実装
- **Principle III (Test-First)**: 各データベースタイプに対するユニットテストを作成
- **Principle IV (Performance)**: 10 万レコードのパフォーマンステストを実施
- **Principle V (Security)**: 接続文字列の検証とエラーハンドリング
- **Principle VI (API Compatibility)**: 既存の JVLink API ラッパーとの互換性を維持
- **Principle VII (Observability)**: データベース操作のログ記録

**ステータス更新**: 全ての明確化質問が解決されました。仕様は完成し、`/speckit.plan` で技術実装計画を作成する準備が整いました。
