ここでは簡単な使い方として、以下の項目を説明します：
* インストール
* 初期設定
* 実行
* アンインストール



## インストール
■前提条件
* 予め[JRA-VAN データラボ](https://jra-van.jp/dlb/) のページから、基本ソフトである JV-Link をダウンロードし、インストールしておいてください。

■手順
1. ダウンロードした自己解凍形式ファイルをダブルクリックします。
![JVLinkToSQLiteArtifact.exe](Images/GETTIN_tilde_1/JVLinkToSQLiteArtifact_exe.png)
2. 解凍が終わるのを待ちます。
![Extract JVLinkToSQLiteArtifact.exe](Images/GETTIN_tilde_1/Extract_JVLinkToSQLiteArtifact_exe.png)
3. 解凍が終わると、同フォルダに `JVLinkToSQLiteArtifact` というフォルダが展開されます。このフォルダの中に JVLinkToSQLite 本体とマニュアルが収録されています。
![JVLinkToSQLiteArtifact folder](Images/GETTIN_tilde_1/JVLinkToSQLiteArtifact_folder.png)
4. `JVLinkToSQLiteArtifact` フォルダは、わかりやすい名前にしたり、お好きな場所に配置していただいて構いません。以降の説明においては、このフォルダを `JVLinkToSQLite` にリネームし、C ドライブ直下に配置した上で、PowerShell から実行するものとしています。
![Prepare JVLinkToSQLite execution](Images/GETTIN_tilde_1/Prepare_JVLinkToSQLite_execution.png)
5. オプション無しでコマンドを投入し、以下のようなヘルプ メッセージが表示されれば実行可能となります：

```powershell
PS C:\JVLinkToSQLite> .\jvlinktosqlite
JVLinkToSQLite 0.1.0.0
Copyright c 2023 Akira Sugiura

  -m, --mode                       モードの設定。以下のパターンを指定可能です：
                                   * Exec
                                   * Event
                                   * Init
                                   * About
                                   * DefaultSetting

  -d, --datasource                 (Default: race.db) データ ソース。SQLite
                                   ファイルのパスを指定します。

  --dbtype                         データベースの種類。以下のパターンを指定可能です：
                                   * sqlite (デフォルト)
                                   * duckdb
                                   * postgresql
                                   ※指定しない場合、データソースから自動判定されます。

  -t, --throttlesize               (Default: 100)
                                   スロットルサイズ。JVLinkToSQLiteは、JV-Dataのレコード読み取りと
                                   SQLiteへの書き込みを非同期で行いますが、このパラメータはSQLite
                                   へ書き込むまでに、JV-Dataを何レコード分遅らせるかを指定します。
                                   書き込みを遅らせないほうがスループットもメモリ効率も良いのですが、
                                   開発中に何度か、サーバーへの単位時間当たりのアクセス頻度オーバーが
                                   原因と考えられる、JV-Link側の不規則なエラーが発生したため、処理を
                                   遅らせるようにしました。もし動作設定を変えていないのに不規則な
                                   エラーが発生するようでしたら、この値を増やしてみてください。

  -s, --setting                    (Default: setting.xml)
                                   動作設定。JVLinkToSQLiteSetting クラスのインスタンスの情報を記載した
                                   XML ファイルのパスを指定します。

  -l, --loglevel                   (Default: Info) ログ レベルの設定。以下のパターンを指定可能です：
                                   * Error
                                   * Warning
                                   * Info
                                   * Verbose
                                   * Debug

  -u, --skipslastmodifiedupdate    (Default: false)
                                   最新読み出し開始ポイント日時の更新をスキップするかどうかを指定します。

  --help                           Display this help screen.

  --version                        Display version information.


```



## 初期設定
■前提条件
* 既に他の JRA-VAN データラボ会員用ソフトを使っているなどして、JV-Link の利用キーを設定済みでしたら、この手順は不要です。

■手順
1. `Init` モードを指定したコマンドを投入します：

```powershell
PS C:\JVLinkToSQLite> .\jvlinktosqlite -m init
```

2. すると、以下の「初期化モード」画面が開くので、「設定」ボタンを押してください：
![SetupForm](Images/GETTIN_tilde_1/SetupForm.png)
3. 「JV-Link 設定」画面が開いたら、利用キーの設定を行ってください：
![Init JVLink](Images/GETTIN_tilde_1/Init_JVLink.png)
4. 「JV-Link 設定」画面を「OK」ボタンを押して閉じます。
5. 「初期化モード」画面を[x]ボタンを押して閉じます。



## 実行
■前提条件
* [初期設定](#初期設定)を確認し、必要であればその手順を一通り流しておいてください。
* 本ツールには、変換後の SQLite データベースの内容を確認する機能はありませんので、例えば、[A5:SQL Mk-2](https://a5m2.mmatsubara.com/) や [Command Line Shell For SQLite](https://www.sqlite.org/cli.html) 等、別のソフトをご用意ください。

■手順
1. `Exec` モードを指定したコマンドを投入します：

```powershell
# SQLite（デフォルト）
PS C:\JVLinkToSQLite> .\jvlinktosqlite -m exec

# DuckDB（分析・大規模データ向け）
PS C:\JVLinkToSQLite> .\jvlinktosqlite -m exec -d race.duckdb

# PostgreSQL（本番環境・マルチユーザー向け）
PS C:\JVLinkToSQLite> .\jvlinktosqlite -m exec --dbtype postgresql -d "Host=localhost;Database=jvlink;Username=user;Password=pass"
```

2. 処理が終わるまで待ちます。デフォルトでは、過去 1 年分の蓄積系データを取得します。実行環境によりますが、作者の環境<sup>※1</sup>では、データが全てダウンロードされている状態からでも 6～7分ほどかかりましたので、しばらくお待ちください。
3. 処理が終わると、`race.db` という名前で SQLite データベースが作成されます（DuckDB の場合は `race.duckdb`、PostgreSQL の場合は指定したデータベース）。

実行時に作られる `setting.xml` の内容を書き換えて再実行することで、対象データの取捨選択も可能です。詳細は、[動作設定の仕様](https://github.com/urasandesu/JVLinkToSQLite/wiki/SettingXml-Spec)をご参照ください。

作成されるデータベースの仕様については、[テーブルの仕様](https://github.com/urasandesu/JVLinkToSQLite/wiki/Table-Spec)をご参照ください。

競馬ソフトを開発しており、JVLinkToSQLite によって JV-Data を SQLite データベース変換させたいと考えている開発者の方は、[JVLinkToSQLite を使った競馬ソフトの開発](https://github.com/urasandesu/JVLinkToSQLite/wiki/DevHRSoftUsingJVLinkToSQLite)をご参照ください。

他のコマンドを確認したい場合は、以下のようなヘルプのオプションを指定したコマンドを投入してください：
```powershell
# 全体のヘルプを表示
.\jvlinktosqlite --help

# メイン処理のヘルプを表示
.\jvlinktosqlite main --help

# 動作設定処理のヘルプを表示
.\jvlinktosqlite setting --help
```



## アンインストール
■前提条件
* ‐

■手順
1. レジストリなどは使用していませんので、[インストール](#インストール)の手順で配置したフォルダを削除すれば完了します。

=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=

※1）CPU: Intel Core i7-6600U CPU（第 6 世代）@2.60GHz、メモリ: 16GB、ストレージ: SSD 500GB、OS: Windows 10 Pro 22H2
