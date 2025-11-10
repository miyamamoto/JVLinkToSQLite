# ビルドガイド / Build Guide

## 概要

JVLinkToSQLiteは.NET Framework 4.8プロジェクトです。このドキュメントはビルドに必要な環境とビルド手順を説明します。

---

## 必要な環境

### 1. .NET Framework 4.8 Developer Pack

**ダウンロード先**: https://dotnet.microsoft.com/download/dotnet-framework/net48

または

### 2. Visual Studio 2017以降（推奨）

**推奨バージョン**: Visual Studio 2019 または 2022

**必要なワークロード**:
- .NET デスクトップ開発
- .NET Framework 4.8 ターゲティングパック

**ダウンロード先**: https://visualstudio.microsoft.com/ja/downloads/

または

### 3. Build Tools for Visual Studio（Visual Studio不要の場合）

**ダウンロード先**: https://visualstudio.microsoft.com/ja/downloads/#build-tools-for-visual-studio-2022

**必要なコンポーネント**:
- .NET Framework 4.8 SDK
- MSBuild
- NuGet パッケージマネージャー

---

## ビルド手順

### 方法1: Visual Studio を使用（最も簡単）

1. **Visual Studioを起動**

2. **ソリューションを開く**
   ```
   JVLinkToSQLite.sln を開く
   ```

3. **NuGetパッケージの復元**
   - Visual Studioが自動的に復元します
   - または、ソリューションエクスプローラーで右クリック → "NuGetパッケージの復元"

4. **ビルド**
   - メニュー: ビルド → ソリューションのビルド
   - またはキーボード: `Ctrl+Shift+B`

5. **テスト実行**
   - メニュー: テスト → すべてのテストを実行
   - またはキーボード: `Ctrl+R, A`

### 方法2: コマンドライン（MSBuild）

#### 前提条件
- MSBuildがPATHに含まれていること
- NuGet.exeがインストールされていること

#### 手順

1. **開発者コマンドプロンプトを開く**
   ```
   スタートメニュー → Visual Studio 2019 → Developer Command Prompt for VS 2019
   ```

2. **プロジェクトディレクトリに移動**
   ```cmd
   cd C:\Users\mitsu\JVLinkToSQLite
   ```

3. **NuGetパッケージの復元**
   ```cmd
   nuget restore JVLinkToSQLite.sln
   ```

4. **ビルド（Debug）**
   ```cmd
   msbuild JVLinkToSQLite.sln /p:Configuration=Debug
   ```

5. **ビルド（Release）**
   ```cmd
   msbuild JVLinkToSQLite.sln /p:Configuration=Release
   ```

6. **テスト実行**（NUnit Console Runnerが必要）
   ```cmd
   packages\NUnit.ConsoleRunner.3.16.3\tools\nunit3-console.exe ^
     Test.Urasandesu.JVLinkToSQLite\bin\Debug\Test.Urasandesu.JVLinkToSQLite.dll
   ```

### 方法3: PowerShell スクリプト

以下のスクリプトを `build.ps1` として保存して実行：

```powershell
# build.ps1 - JVLinkToSQLite ビルドスクリプト

param(
    [string]$Configuration = "Debug",
    [switch]$RunTests,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$SolutionPath = "JVLinkToSQLite.sln"

Write-Host "JVLinkToSQLite Build Script" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan

# MSBuildを探す
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -latest -requires Microsoft.Component.MSBuild `
    -find MSBuild\**\Bin\MSBuild.exe -prerelease | Select-Object -First 1

if (-not $msbuild) {
    Write-Error "MSBuild not found. Please install Visual Studio or Build Tools."
    exit 1
}

Write-Host "Using MSBuild: $msbuild" -ForegroundColor Cyan

# NuGet パッケージの復元
Write-Host "`nRestoring NuGet packages..." -ForegroundColor Yellow
& $msbuild /t:Restore $SolutionPath /v:minimal

if ($LASTEXITCODE -ne 0) {
    Write-Error "NuGet restore failed."
    exit 1
}

# クリーンビルド
if ($Clean) {
    Write-Host "`nCleaning solution..." -ForegroundColor Yellow
    & $msbuild /t:Clean $SolutionPath /p:Configuration=$Configuration /v:minimal
}

# ビルド
Write-Host "`nBuilding solution..." -ForegroundColor Yellow
& $msbuild $SolutionPath /p:Configuration=$Configuration /v:minimal

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit 1
}

Write-Host "`nBuild succeeded!" -ForegroundColor Green

# テスト実行
if ($RunTests) {
    Write-Host "`nRunning tests..." -ForegroundColor Yellow

    $nunitRunner = Get-ChildItem -Path "packages" -Filter "nunit3-console.exe" -Recurse |
                   Select-Object -First 1 -ExpandProperty FullName

    if (-not $nunitRunner) {
        Write-Warning "NUnit Console Runner not found. Skipping tests."
    } else {
        $testDll = "Test.Urasandesu.JVLinkToSQLite\bin\$Configuration\Test.Urasandesu.JVLinkToSQLite.dll"
        & $nunitRunner $testDll

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Tests failed."
            exit 1
        }

        Write-Host "`nAll tests passed!" -ForegroundColor Green
    }
}

Write-Host "`nBuild completed successfully!" -ForegroundColor Green
```

**実行方法**:
```powershell
# Debug ビルド
.\build.ps1

# Release ビルド
.\build.ps1 -Configuration Release

# ビルド + テスト実行
.\build.ps1 -RunTests

# クリーンビルド + テスト実行
.\build.ps1 -Configuration Release -Clean -RunTests
```

---

## 出力ファイル

### ビルド成果物

**Debugビルド**:
```
JVLinkToSQLite\bin\Debug\JVLinkToSQLite.exe
Urasandesu.JVLinkToSQLite\bin\Debug\Urasandesu.JVLinkToSQLite.dll
Test.Urasandesu.JVLinkToSQLite\bin\Debug\Test.Urasandesu.JVLinkToSQLite.dll
```

**Releaseビルド**:
```
JVLinkToSQLite\bin\Release\JVLinkToSQLite.exe
Urasandesu.JVLinkToSQLite\bin\Release\Urasandesu.JVLinkToSQLite.dll
Test.Urasandesu.JVLinkToSQLite\bin\Release\Test.Urasandesu.JVLinkToSQLite.dll
```

---

## NuGetパッケージ依存関係

プロジェクトは以下のNuGetパッケージに依存しています：

### マルチデータベース対応（新規追加）
- **DuckDB.NET.Data.Full** v1.4.1 - DuckDB サポート
- **Npgsql** v4.1.13 - PostgreSQL サポート
- **EntityFramework6.Npgsql** v6.4.3 - PostgreSQL EF統合

### 既存の依存関係
- **System.Data.SQLite.Core** - SQLite サポート
- **CommandLineParser** - CLI パラメータ解析
- **log4net** - ロギング
- **EntityFramework** v6.4.4 - ORM

### テスト用
- **NUnit** v3.13.3 - テストフレームワーク
- **NSubstitute** v5.0.0 - モックフレームワーク

これらは `nuget restore` または Visual Studio で自動的にダウンロードされます。

---

## ビルド確認手順

### 1. ビルドエラーがないことを確認

```
ビルド: 4 正常終了、0 失敗、0 スキップ
```

### 2. 出力ファイルが生成されていることを確認

```cmd
dir JVLinkToSQLite\bin\Release\JVLinkToSQLite.exe
dir Urasandesu.JVLinkToSQLite\bin\Release\Urasandesu.JVLinkToSQLite.dll
```

### 3. 依存DLLが配置されていることを確認

```cmd
dir JVLinkToSQLite\bin\Release\*.dll
```

以下のDLLが含まれているはず：
- System.Data.SQLite.dll
- DuckDB.NET.Data.dll
- Npgsql.dll
- その他の依存DLL

---

## テスト実行

### Visual Studioでテスト実行

1. **テストエクスプローラーを開く**
   - メニュー: テスト → テストエクスプローラー

2. **すべてのテストを実行**
   - テストエクスプローラーで "すべて実行"

3. **結果確認**
   - 148個のテストすべてがパスすることを確認

### コマンドラインでテスト実行

```cmd
cd C:\Users\mitsu\JVLinkToSQLite

REM NUnit Console Runnerを使用
packages\NUnit.ConsoleRunner.3.16.3\tools\nunit3-console.exe ^
  Test.Urasandesu.JVLinkToSQLite\bin\Debug\Test.Urasandesu.JVLinkToSQLite.dll ^
  --labels=All
```

**期待される結果**:
```
Test Run Summary
  Overall result: Passed
  Test Count: 148, Passed: 148, Failed: 0, Inconclusive: 0, Skipped: 0
```

---

## トラブルシューティング

### エラー: "nuget コマンドが見つかりません"

**解決策1**: NuGet.exeをダウンロード
```cmd
mkdir tools
cd tools
curl -o nuget.exe https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
..\tools\nuget.exe restore ..\JVLinkToSQLite.sln
```

**解決策2**: Visual Studio Developer Command Promptを使用
```
スタートメニュー → Visual Studio → Developer Command Prompt
```

### エラー: "MSBuild が見つかりません"

**解決策**: Developer Command Prompt for Visual Studioを使用するか、MSBuildをPATHに追加：

```cmd
set PATH=%PATH%;C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin
```

### エラー: "ターゲット フレームワーク v4.8 が見つかりません"

**解決策**: .NET Framework 4.8 Developer Packをインストール
- https://dotnet.microsoft.com/download/dotnet-framework/net48

### エラー: パッケージの復元に失敗

**解決策1**: NuGetキャッシュをクリア
```cmd
nuget locals all -clear
nuget restore JVLinkToSQLite.sln
```

**解決策2**: packages.configを確認
```cmd
type Urasandesu.JVLinkToSQLite\packages.config
```

### テストが実行されない

**解決策**: NUnit Console Runnerがインストールされているか確認
```cmd
dir packages\NUnit.ConsoleRunner.*
```

インストールされていない場合：
```cmd
nuget install NUnit.ConsoleRunner -Version 3.16.3 -OutputDirectory packages
```

---

## CI/CD統合

### GitHub Actions例

`.github/workflows/build.yml`:

```yaml
name: Build and Test

on:
  push:
    branches: [ main, 1-multi-db-support ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET Framework
      uses: microsoft/setup-msbuild@v1.1

    - name: Setup NuGet
      uses: NuGet/setup-nuget@v1

    - name: Restore NuGet packages
      run: nuget restore JVLinkToSQLite.sln

    - name: Build solution
      run: msbuild JVLinkToSQLite.sln /p:Configuration=Release /v:minimal

    - name: Run tests
      run: |
        $nunit = Get-ChildItem -Path packages -Filter nunit3-console.exe -Recurse | Select-Object -First 1
        & $nunit.FullName Test.Urasandesu.JVLinkToSQLite\bin\Release\Test.Urasandesu.JVLinkToSQLite.dll

    - name: Upload artifacts
      uses: actions/upload-artifact@v3
      with:
        name: JVLinkToSQLite-Release
        path: JVLinkToSQLite\bin\Release\
```

---

## 開発環境のセットアップ

### 推奨：Visual Studio 2022

1. **Visual Studio 2022 Community をダウンロード**
   - https://visualstudio.microsoft.com/ja/downloads/

2. **インストール時に以下を選択**:
   - ワークロード: ".NET デスクトップ開発"
   - 個別コンポーネント: ".NET Framework 4.8 SDK"

3. **Visual Studio を起動して JVLinkToSQLite.sln を開く**

4. **ビルド → ソリューションのビルド** (Ctrl+Shift+B)

5. **テスト → すべてのテストを実行** (Ctrl+R, A)

---

## 次のステップ

ビルドが成功したら：

1. **統合テストの実行**
   - PostgreSQLサーバーをセットアップ（Dockerを推奨）
   - 統合テストを実行
   - パフォーマンステストを実行

2. **リリースの作成**
   - バージョン番号の更新
   - リリースノートの作成
   - バイナリのパッケージング

3. **配布**
   - GitHub Releasesページに公開
   - ドキュメントの更新

---

## サポート

ビルドに関する問題が発生した場合：

1. このドキュメントのトラブルシューティングセクションを確認
2. GitHubのIssuesページで報告
3. ビルドログを添付してください

---

**最終更新**: 2025-11-10
**対象バージョン**: JVLinkToSQLite v2.x (マルチデータベース対応版)
