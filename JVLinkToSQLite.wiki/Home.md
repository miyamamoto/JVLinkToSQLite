![JVLinkToSQLite](Images/Home/JVLinkToSQLitePreviewImage.png)

JVLinkToSQLite は、JRA-VAN データラボが提供する競馬データ（JV-Data）を SQLite データベースに変換するツールです。

昨今の機械学習技術の流行により、データ サイエンスを本業とする人間でなくても、簡単にデータ分析ができる環境が整ってきました。ただ現状、業界固有のデータとなると、その業界で昔から使われてきた仕組みや独自性が強いフォーマットで運用されていることが少なくなく、スタンダードな環境でそのまま分析ができないことが問題になりがちでした。

JV-Data も、そのような独自性が強いデータの 1 つですが、JVLinkToSQLite を使用することで、データ分析においてより一般的なフォーマットである SQLite、DuckDB、PostgreSQL データベースに変換することが可能になります。さらに、JVLinkToSQLite は大部分がオープンソース（OSS）です。新しく競馬ソフトを開発する際、JV-Data を本ツールによって変換させれば、開発者は本当にやりたい予想理論の実装や支援機能の開発に注力できるようになるでしょう。

なお、より汎用的なデータ形式に変換可能なソフト<sup>※1</sup>は、既にいくつか作られています。以下は、そのような機能を持ったソフトと JVLinkToSQLite との比較になりますので、ご自分の用途に合うソフトを選んでいただければと思います：

|名称                                                            |変換元<br />JV-Data                                                                      |変換先<br />汎用データ形式|変換<br />方針<sup>※2</sup>|利用料金|OSS？                    |備考                      |
|--------------------------------------------------------------|--------------------------------------------------------------------------------------|----------------|-----------------------|----|------------------------|------------------------|
|[JVLinkToSQLite](https://github.com/urasandesu/JVLinkToSQLite)|V4.9.0                                                                                |SQLite, DuckDB, <br />PostgreSQL|JV-Data<br />相当        |フリー<br />ウェア|大部分が<br />○             |　                       |
|[EveryDB2](https://everydb.iwinz.net/)                        |V4.9.0                                                                                |SQLserver, Access, <br />Oracle, PostgreSQL, <br />SQLite, MySQL, <br />EXCEL, MariaDB|JV-Data<br />相当        |フリー<br />ウェア|×                       |　                       |
|[JV2Mongo](https://bitbucket.org/Satachito/jv2mongo/wiki/Home)|V4.9.0                                                                                |MongoDB         |JV-Data<br />相当        |フリー<br />ウェア|×                       |　                       |
|[KeiBa DataBase Server](http://www.team-nave.com/)            |V4.9.0                                                                                |Firebird        |JV-Data<br />相当        |シェア<br />ウェア<br />2,200 円<br />（税込）|×                       |2024/12/31<br />サポート終了予定|
|[mykeibadb](https://keough.watson.jp/)                        |V4.9.0                                                                                |MySQL           |JV-Data<br />相当        |フリー<br />ウェア|×                       |　                       |
|[PC-KEIBA Database](https://pc-keiba.com/)                    |V4.9.0                                                                                |PostgreSQL      |JV-Data<br />相当        |フリー<br />ウェア|×                       |　                       |
|[SaraD](https://sarad4keiba.blogspot.com/)                    |V4.9.0                                                                                |SQLite          |JV-Data<br />相当        |フリー<br />ウェア|×                       |　                       |
|[TARGET frontierJV](https://jra-van.jp/target/)               |V4.9.0                                                                                |CSV             |独自                     |フリー<br />ウェア|×                       |　                       |
|[this_move](https://www.throwthedice.online/)                 |V4.9.0                                                                                |CSV, Excel      |独自                     |フリー<br />ウェア|×                       |　                       |
|[つくれます Access版<br /> for JVDL](https://www5f.biglobe.ne.jp/~f-lap/index.htm)|V4.9.0                                                                  |Access          |JV-Data<br />相当        |フリー<br />ウェア|×                       |　                       |

幸いにも JVLinkToSQLite を選んでいただけた方は、こちらの[簡単な使い方](https://github.com/urasandesu/JVLinkToSQLite/wiki/Getting-Started)をご覧ください。

=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=*=

※1）2024/05/08～2024/08/18 の期間で、JRA-VAN データラボが紹介していたソフトの内、[掲載希望コーナーが「データベース」](https://jra-van.jp/dlb/#db)であり、マニュアルに変換後のデータ仕様が掲載されていたもの。
※2）「JV-Data相当」＝JV-Data の情報をなるべくそのままデータベース化しようとしているもの、「独自」＝ソフトで使う独自形式がまずあり、それを汎用データ形式でエクスポート可能にしているもの
