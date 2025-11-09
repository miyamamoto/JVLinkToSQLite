# JVLinkToSQLite 仕様書

## プロジェクト概要

**JVLinkToSQLite** は、JRA-VAN データラボが提供する競馬データを SQLite データベースに変換するコマンドラインツールです。

### 基本情報
- **開発者**: Akira Sugiura
- **ライセンス**: GNU General Public License v3.0 (GPLv3)
- **プラットフォーム**: .NET Framework (C#)
- **リポジトリ**: https://github.com/urasandesu/JVLinkToSQLite

### 主な目的
JRA-VAN が提供する JVLink API を通じて競馬データを取得し、SQLite データベースに格納することで、以下を実現します：
- 競馬データの永続化とローカル管理
- SQL を使った柔軟なデータ分析
- オフライン環境でのデータアクセス

## アーキテクチャ

### プロジェクト構成

```
JVLinkToSQLite/
├── JVLinkToSQLite/                          # メインアプリケーション（CLI）
├── Urasandesu.JVLinkToSQLite/               # コアライブラリ
├── Urasandesu.JVLinkToSQLite.Basis/         # 基礎ライブラリ
├── Urasandesu.JVLinkToSQLite.JVData/        # JVData 構造体定義
├── Test.Urasandesu.JVLinkToSQLite/          # メインアプリケーションテスト
├── Test.Urasandesu.JVLinkToSQLite.Basis/    # 基礎ライブラリテスト
├── Test.Urasandesu.JVLinkToSQLite.JVData/   # JVData テスト
└── ObfuscatedResources/                     # 難読化リソース
```

### 技術スタック

#### 主要フレームワーク・ライブラリ
- **.NET Framework**: アプリケーション基盤
- **Entity Framework 6**: O/RM
- **SQLite**: データベースエンジン
- **CommandLineParser**: コマンドライン引数解析
- **DryIoc**: 依存性注入 (DI) コンテナ
- **Castle.DynamicProxy**: 動的プロキシ生成
- **NUnit**: ユニットテストフレームワーク
- **NSubstitute**: モッキングライブラリ

#### 開発ツール
- **Obfuscar**: コード難読化
- **Mono.Cecil**: .NET アセンブリ操作

## 主要コンポーネント

### 1. JVLinkToSQLite（メインアプリケーション）

**責務**: コマンドライン インターフェイス、ユーザー操作の処理

#### 主要クラス

##### Program.cs
- **役割**: エントリーポイント
- **処理**:
  - コマンドライン引数のパース
  - `MainOptions` または `SettingOptions` へのディスパッチ

##### MainOptions.cs
- **役割**: メイン処理のコマンドライン オプション定義
- **主要パラメータ**:
  - `-m, --mode`: 動作モード（Exec, Event, Init, About, DefaultSetting）
  - `-d, --datasource`: SQLite データベース ファイルパス（デフォルト: `race.db`）
  - `-t, --throttlesize`: スロットルサイズ（デフォルト: 100）
  - `-s, --setting`: 設定 XML ファイルパス（デフォルト: `setting.xml`）
  - `-u, --skipslastmodifiedupdate`: 最終更新日時の更新をスキップするか

##### SettingOptions.cs
- **役割**: 設定変更処理のコマンドライン オプション定義
- **主要パラメータ**:
  - `-s, --setting`: 変更対象の設定 XML ファイルパス
  - `-x, --xpath`: 変更対象を指定する XPath
  - `-v, --value`: 変更後の値
  - `-f, --force`: 確認メッセージをスキップして強制実行

##### Modes.cs
- **Exec**: 通常実行モード
- **Event**: イベント駆動モード
- **Init**: 初期化モード
- **About**: バージョン情報表示
- **DefaultSetting**: デフォルト設定出力

### 2. Urasandesu.JVLinkToSQLite（コアライブラリ）

**責務**: JVLink API ラッパー、データ変換ロジック、SQLite 操作

#### 主要サブシステム

##### JVLinkWrappers
**責務**: JVLink API の .NET ラッパー

###### 主要クラス
- **IJVLinkService / JVLinkService**: JVLink API の主要機能ラッパー
  - `JVInit()`: JVLink 初期化
  - `JVOpen()`: データ読み取りセッション開始
  - `JVRead()`: データ読み取り
  - `JVClose()`: セッションクローズ
  - `JVStatus()`: ステータス取得

- **DataBridge系クラス**: JV-Data 構造体と SQLite テーブルのブリッジ
  - `DataBridgeFactory`: DataBridge インスタンス生成
  - `DataBridge<T>`: 各 JV-Data 型に対応する具体的なブリッジ実装
  - 例:
    - `JV_RA_RACEDataBridge`: レース情報
    - `JV_UM_UMADataBridge`: 馬情報
    - `JV_KS_KISYUDataBridge`: 騎手情報
    - `JV_O1_ODDS_TANFUKUWAKUDataBridge`: オッズ情報（単勝・複勝・枠連）
    - など、40種類以上の JV-Data 型に対応

- **JVDataSpecKey**: データ種別キー
- **JVKaisaiDateKey / JVKaisaiDateTimeKey**: 開催日/日時キー
- **JVRaceKey**: レースキー

##### OperatorAggregates
**責務**: 各種データ取得操作の集約

- **JVPastDataOperatorAggregate**: 蓄積データ取得操作
- **JVEventDataOperatorAggregate**: イベントデータ取得操作
- **JVImmediateDataOperatorAggregate**: 即時データ取得操作
- **JVNullOperatorAggregate**: Null オブジェクトパターン実装

##### Settings
**責務**: 設定データモデル

- **JVLinkToSQLiteSetting**: 全体設定
- **JVDataSpecSetting**: データ種別設定
- **SQLiteConnectionInfo**: SQLite 接続情報
- **JVNormalUpdateSetting**: 通常更新設定
- **JVSetupDataUpdateSetting**: セットアップデータ更新設定

##### その他重要クラス
- **JVLinkToSQLiteBootstrap**: アプリケーション ブートストラップ
- **XmlSerializationService**: XML シリアライゼーション
- **JVServiceOperationNotifier**: 操作通知
- **JVOperationMessenger**: 操作メッセージング

### 3. Urasandesu.JVLinkToSQLite.JVData

**責務**: JV-Data 構造体の定義

JRA-VAN が提供する各種 JV-Data 型の構造体定義を含む。
- レース情報（RA）
- 馬情報（UM）
- 騎手情報（KS）
- オッズ情報（O1-O6）
- 払戻情報（HR）
- 成績情報（SE）
- など

### 4. Urasandesu.JVLinkToSQLite.Basis

**責務**: 汎用的な基礎機能

## データフロー

### 1. 初期化フロー（Init モード）

```
ユーザー
  ↓ (コマンド実行)
Program.Main()
  ↓
MainOptionsHandler.Main()
  ↓
JVLinkToSQLiteBootstrap
  ↓
JVLinkService.JVInit()
  ↓
SQLite データベース初期化
```

### 2. データ取得・変換フロー（Exec モード）

```
ユーザー
  ↓ (コマンド実行 + 設定ファイル)
Program.Main()
  ↓
MainOptionsHandler.Main()
  ↓
JVLinkToSQLiteBootstrap.LoadSetting()
  ↓ (setting.xml 読み込み)
JVPastDataOperatorAggregate / JVEventDataOperatorAggregate
  ↓
JVLinkService.JVOpen()
  ↓ (JVLink API 呼び出し)
JRA-VAN サーバー
  ↓ (データ受信)
JVLinkService.JVRead()
  ↓ (非同期読み取り)
DataBridge<T>
  ↓ (構造体 → SQLite 変換)
SQLite データベース
  ↓ (スロットル制御付き書き込み)
race.db ファイル
```

### 3. 設定変更フロー（setting コマンド）

```
ユーザー
  ↓ (setting コマンド + XPath + 値)
Program.Main()
  ↓
SettingOptionsHandler.Main()
  ↓ (XPath で設定ノード特定)
XmlDocument 操作
  ↓ (値の書き換え)
setting.xml 保存
```

## 主要機能

### 1. データ取得モード

#### 蓄積データ取得（Past Data）
- **用途**: 過去の競馬データを一括取得
- **データ種別キー**: JVDataSpecKey
- **期間指定**: JVKaisaiDateKey, JVKaisaiDateTimeKey, JVKaisaiDateTimeRangeKey

#### イベントデータ取得（Event Data）
- **用途**: リアルタイムイベントデータを監視・取得
- **監視**: JVWatchEventDispatcher によるイベント監視
- **通知**: IJVWatchEventListener による通知

#### 即時データ取得（Immediate Data）
- **用途**: 特定レースのデータを即座に取得
- **レース指定**: JVRaceKey

### 2. スロットル制御

**課題**: JVLink API への過剰なアクセスによるエラー防止

**解決策**:
- `ThrottleSize` パラメータによる読み取り・書き込みの遅延制御
- デフォルト 100 レコード遅延
- 非同期処理によるスループット維持

### 3. 設定管理

#### setting.xml
**構造**:
```xml
<JVLinkToSQLiteSetting>
  <JVDataSpecSettings>
    <JVDataSpecSetting>
      <!-- データ種別ごとの設定 -->
    </JVDataSpecSetting>
  </JVDataSpecSettings>
  <NormalUpdateSetting>
    <!-- 通常更新設定 -->
  </NormalUpdateSetting>
  <SetupDataUpdateSetting>
    <!-- セットアップデータ更新設定 -->
  </SetupDataUpdateSetting>
</JVLinkToSQLiteSetting>
```

**操作**:
- XPath による柔軟な設定変更
- XmlSerializationService による読み書き

### 4. データブリッジ機構

**役割**: JV-Data 構造体と SQLite テーブルのマッピング

**処理**:
1. **テーブル作成**: `JVDataStructCreateTableSources` による CREATE TABLE SQL 生成
2. **データ挿入**: `JVDataStructInsertSources` による INSERT SQL 生成
3. **列マッピング**: `JVDataStructColumns` による列定義
4. **データ取得**: `JVDataStructGetters` による構造体からのデータ抽出

## エラーハンドリング

### JVLink API エラー

- **JVLinkException**: JVLink API エラーのラッピング
- **JVResultInterpretation**: リザルトコードの解釈
- **ReturnCodes**: アプリケーション終了コード定義

### エラーコード範囲
- **ReturnCodeRanges**: JVLink API エラーコードの範囲定義

## ログ・通知

### ログレベル
- **LogLevels**: ログレベル定義（詳細度制御）

### 操作通知
- **IJVServiceOperationListener**: 操作リスナー インターフェイス
- **JVServiceOperationNotifier**: 操作通知管理
- **JVServiceOperationEventArgs**: 操作イベント引数
- **JVServiceMessageEventArgs**: メッセージイベント引数

### リスニングレベル
- **ListeningLevels**: 通知レベル制御

## ビルド・デプロイ

### ビルドスクリプト
- **Build.ps1**: PowerShell ビルドスクリプト

### 難読化
- **ObfuscatedResources**: 難読化リソース管理
- **Obfuscar**: コード難読化ツール

## テスト戦略

### テストプロジェクト構成
- **Test.Urasandesu.JVLinkToSQLite**: メインアプリケーション テスト
- **Test.Urasandesu.JVLinkToSQLite.Basis**: 基礎ライブラリ テスト
- **Test.Urasandesu.JVLinkToSQLite.JVData**: JVData テスト

### テストツール
- **NUnit**: テストフレームワーク
- **NSubstitute**: モック/スタブ

## 依存関係

### 外部依存
- **JVLink API**: JRA-VAN が提供する COM API
- **SQLite**: データベースエンジン

### 内部依存関係図

```
JVLinkToSQLite (CLI)
  ↓ 参照
Urasandesu.JVLinkToSQLite (Core)
  ↓ 参照
Urasandesu.JVLinkToSQLite.Basis (Foundation)
  ↓ 参照
Urasandesu.JVLinkToSQLite.JVData (Data Structures)
```

## 今後の拡張可能性

### 考慮事項
1. **データ種別の追加**: 新しい JV-Data 型への対応
2. **パフォーマンス最適化**: バルクインサート、トランザクション制御の改善
3. **エラーリカバリ**: 中断時の再開機能
4. **データ検証**: 取得データの整合性チェック
5. **並列処理**: 複数データ種別の並列取得

## ライセンス情報

### 本プロジェクト
- **GPLv3**: GNU General Public License v3.0
- **追加許諾**: ObscUra ライブラリとのリンクに関する GPLv3 Section 7 に基づく追加許諾

### 使用ライブラリ
- **Apache License 2.0**: Castle Core, Entity Framework 6
- **MIT License**: CommandLineParser, DryIoc, NUnit, Mono.Cecil, Obfuscar
- **BSD 2-Clause**: NSubstitute
- **Public Domain**: SQLite

## 参照リソース

- **公式ドキュメント**: https://github.com/urasandesu/JVLinkToSQLite/wiki
- **JRA-VAN データラボ**: https://jra-van.jp/
- **ライセンス全文**: LICENSE ファイル参照
- **サードパーティライセンス**: NOTICE.md 参照
