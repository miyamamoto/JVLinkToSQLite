<!--
Sync Impact Report:
Version change: initial → 1.0.0
Added principles:
  - I. Modularity and Separation of Concerns
  - II. Data Integrity and Reliability
  - III. Test-First Development
  - IV. Performance and Scalability
  - V. Security and Validation
  - VI. API Compatibility
  - VII. Observability and Logging
Templates status:
  ✅ spec-template.md (aligned)
  ✅ plan-template.md (aligned)
  ✅ tasks-template.md (aligned)
Follow-up TODOs: None
-->

# JVLinkToSQLite Constitution

## Core Principles

### I. Modularity and Separation of Concerns

**原則**: 各プロジェクトは明確に定義された単一の責務を持つ

- **JVLinkToSQLite**: CLI インターフェイスとユーザー操作のみを担当
- **Urasandesu.JVLinkToSQLite**: JVLink API ラッパーとビジネスロジックを提供
- **Urasandesu.JVLinkToSQLite.JVData**: JV-Data 構造体定義のみを含む
- **Urasandesu.JVLinkToSQLite.Basis**: 汎用的な基礎機能のみを提供
- 新機能は適切なプロジェクト/名前空間に配置する
- プロジェクト間の依存関係は一方向のみ（循環参照禁止）

**根拠**: 既存のマルチプロジェクト構造を維持し、コードの再利用性とテスト容易性を確保

### II. Data Integrity and Reliability

**原則**: JRA-VAN データの完全性を保証する

- JVLink API から取得したデータは、変換前に検証する
- SQLite への書き込みはトランザクション内で実行する
- データ損失を防ぐため、エラー時は適切にロールバックする
- DataBridge 実装は、JV-Data 構造体の全フィールドを正確にマッピングする
- データ型変換は明示的に行い、暗黙的な変換は避ける
- NULL 値の扱いを明確に定義し、ドキュメント化する

**根拠**: 競馬データは金銭的価値を持つ情報であり、データの正確性が最重要

### III. Test-First Development (NON-NEGOTIABLE)

**原則**: テスト駆動開発を厳格に実施する

- **新機能**: テストを先に作成 → ユーザー承認 → テストが失敗することを確認 → 実装
- **バグ修正**: 失敗するテストケースを作成 → 修正 → テストが成功することを確認
- **リファクタリング**: 既存のテストが全て成功することを確認してから実施
- テストプロジェクト構造は本体プロジェクト構造を反映する
  - `Test.Urasandesu.JVLinkToSQLite` → `Urasandesu.JVLinkToSQLite`
  - `Test.Urasandesu.JVLinkToSQLite.Basis` → `Urasandesu.JVLinkToSQLite.Basis`
  - `Test.Urasandesu.JVLinkToSQLite.JVData` → `Urasandesu.JVLinkToSQLite.JVData`
- NUnit を使用し、NSubstitute でモック/スタブを作成する
- テストカバレッジ目標: 新規コードは 80% 以上、既存コードは段階的に向上

**根拠**: データ変換ツールとして、品質とバグのない動作が不可欠

### IV. Performance and Scalability

**原則**: 大量データ処理に対応する効率的な実装

- **スロットル制御**: ThrottleSize パラメータを使用して、JVLink API への負荷を制御する
- **非同期処理**: データ読み取りと SQLite 書き込みを非同期で実行する
- **メモリ効率**: ストリーミング処理を優先し、全データをメモリに展開しない
- **バルク操作**: 可能な限り、SQLite へのバッチ挿入を使用する
- **インデックス設計**: 頻繁に検索されるカラムには適切なインデックスを作成する
- **パフォーマンステスト**: 大量データ（10万レコード以上）でのパフォーマンステストを実施する

**根拠**: JRA-VAN は膨大な競馬データを提供し、効率的な処理が必須

### V. Security and Validation

**原則**: セキュリティ脆弱性を防止する

- **入力検証**: ユーザー入力（コマンドライン引数、XML 設定）は必ず検証する
- **SQL インジェクション防止**: Entity Framework のパラメータ化クエリを使用する
- **パス検証**: ファイルパス（datasource, setting）はパストラバーサル攻撃を防ぐ
- **XML セキュリティ**: XXE (XML External Entity) 攻撃を防ぐ設定を使用する
- **認証情報**: JVLink API の認証情報は平文で保存しない
- **エラーメッセージ**: システム内部情報を含むエラーメッセージを外部に露出しない

**根拠**: データ変換ツールとして、ユーザーのシステムとデータを保護する責任がある

### VI. API Compatibility

**原則**: JVLink API との互換性を維持する

- JVLink API のバージョン変更に対応する
- 新しい JV-Data 型が追加された場合、対応する DataBridge を実装する
- JV-Data 構造体の変更は、後方互換性を考慮する
- JVLink API のエラーコードを適切にハンドリングし、ユーザーに分かりやすいメッセージを提供する
- リトライロジックを実装し、一時的なネットワークエラーに対応する

**根拠**: JRA-VAN のサービス仕様変更に柔軟に対応し、ユーザー体験を維持

### VII. Observability and Logging

**原則**: デバッグと運用監視を容易にする

- **構造化ログ**: レベル（Debug, Info, Warning, Error）を適切に使い分ける
- **操作通知**: `IJVServiceOperationListener` を通じて、重要な操作を通知する
- **進捗表示**: 大量データ処理時は、進捗状況をユーザーに表示する
- **エラートレース**: エラー発生時は、スタックトレースと文脈情報を記録する
- **設定可能なログレベル**: `LogLevels` を使用して、ログの詳細度を制御できるようにする
- **パフォーマンスメトリクス**: 処理時間、レコード数などの統計情報を出力する

**根拠**: データ変換の長時間処理において、問題の早期発見と解決が重要

## Architecture Requirements

### Technology Stack

- **.NET Framework**: アプリケーション基盤（既存バージョンを維持）
- **Entity Framework 6**: O/RM（SQLite とのやり取りに使用）
- **SQLite**: データベースエンジン（軽量で配布が容易）
- **DryIoc**: 依存性注入コンテナ（既存実装を維持）
- **CommandLineParser**: CLI 引数解析（既存実装を維持）
- **NUnit + NSubstitute**: テストフレームワーク（既存実装を維持）

**変更時の要件**:
- 新しいライブラリの導入は、ライセンス互換性（GPLv3）を確認する
- 既存ライブラリの置き換えは、移行計画と後方互換性を文書化する

### Data Flow Architecture

- **初期化**: `JVLinkService.JVInit()` → SQLite データベース作成
- **データ取得**: `JVOpen()` → `JVRead()` ループ → `DataBridge` 変換 → SQLite 書き込み
- **設定管理**: XML ファイル → `XmlSerializationService` → 設定オブジェクト

**不変要件**:
- データフローは単方向（JVLink → SQLite）
- 中間変換は DataBridge でカプセル化
- エラーハンドリングは各層で適切に実施

## Development Workflow

### Code Review Requirements

- **全ての PR**: 少なくとも 1 名のレビュー承認が必要
- **レビュー観点**:
  - Constitution 原則への準拠
  - テストカバレッジの確認
  - パフォーマンスへの影響評価
  - セキュリティ脆弱性の有無
  - コード品質（可読性、保守性）

### Quality Gates

1. **コンパイル**: ビルドエラーがないこと
2. **テスト**: 全てのユニットテストが成功すること
3. **スタイル**: コーディング規約に準拠すること
4. **セキュリティ**: 既知の脆弱性がないこと
5. **パフォーマンス**: ベースラインから大幅な劣化がないこと

### Branching Strategy

- **main**: 本番リリース可能なコード
- **feature/\***: 新機能開発ブランチ
- **fix/\***: バグ修正ブランチ
- **refactor/\***: リファクタリングブランチ

**マージ要件**: PR レビュー承認 + 全 Quality Gates クリア

## Governance

### Constitutional Authority

この Constitution は、JVLinkToSQLite プロジェクトにおける全ての開発活動の基盤となる。
他のドキュメントやプラクティスと矛盾する場合、Constitution が優先される。

### Amendment Process

1. **提案**: GitHub Issue で改善提案を作成
2. **議論**: コミュニティで議論し、合意形成
3. **承認**: メンテナが承認
4. **実装**: Constitution を更新し、Version を increment
5. **移行**: 既存コードを新原則に適合させる移行計画を実行

### Compliance Review

- **PR レビュー時**: Constitution への準拠を確認
- **四半期レビュー**: 既存コードベースの Constitution 準拠状況を評価
- **違反時**: 優先度を付けて是正計画を立案・実行

### Complexity Justification

- 複雑な実装を導入する場合、以下を文書化する:
  - なぜシンプルな実装では不十分か
  - 複雑さによって得られる具体的なメリット
  - 保守コストの増加をどう軽減するか

**Version**: 1.0.0 | **Ratified**: 2025-11-09 | **Last Amended**: 2025-11-09
