# LambdaAotDynamoDbTemplate

AWS Lambda（.NET Native AOT）+ API Gateway + DynamoDB 構成のバックエンドAPIの雛形。
`dotnet new`のテンプレートとして使う。雛形自体の説明（収録内容・ローカル開発・デプロイ）は[docs/README.project.md](docs/README.project.md)（生成先プロジェクトでは`README.md`になる）を参照。

## 新規案件への導入（`dotnet new` テンプレート）

このリポジトリは`dotnet new`のカスタムテンプレート（`.template.config/template.json`）として使える。
ソリューション名・プロジェクト名（`LambdaAotDynamoDbTemplate`）は`-n`の値に一括で置換される
（フォルダ名・ファイル名・namespace・`.sln`・csproj参照・Dockerfile・compose内の文字列など）。
`MasterData` / `MasterData.Generator`は名前を変えない。

```
# 初回のみ（以後はこのリポジトリを更新すれば次回の dotnet new に反映される）
dotnet new install <このリポジトリのパス>

# 新規案件の作成
dotnet new lambda-aot-ddb -n MyApp -o <出力先>
```

`dotnet new lambda-aot-ddb`はどのディレクトリから実行してもよい（インストール済みのため、元リポジトリにいる必要はない）。

- `-o`を指定すると、そこに生成される。相対パスは実行時のカレントディレクトリが基準。絶対パスなら実行場所に依存しない。
- `-o`を省略すると、カレントディレクトリ配下に`-n`の名前のフォルダ（例: `MyApp/`）が作られ、その中に生成される。
- 出力先に同名ファイルがあると上書きせずエラーになる（`--force`で上書き可）。

`template.json`の`shortName` / `identity`を変えたときだけ、`dotnet new install <パス>`をやり直す。
外すときは`dotnet new uninstall <パス>`。`bin/` / `obj/` / `.git/` / `.idea/` / `.claude/` / `master.bytes`は含まれない。

## DynamoDBのテーブル名の指定方式を選ぶ

案件を作ったら、次のどちらかを選ぶ。テンプレートのコードはパターン1の状態で生成される。設定で切り替える仕組みはなく、パターン2の案件ではコードを書き換える。

| | パターン1: prefix方式（既定） | パターン2: テーブルごとの環境変数方式 |
|---|---|---|
| 向いている案件 | テーブル名を自分で決められる（自社CDKで構築する等） | テーブル名を決められない（別会社がインフラを構築する等） |
| 環境変数 | `DYNAMODB_TABLE_NAME_PREFIX`の1つ（例: `myapp-stg-`） | テーブルごとの`TABLE_XXX` |
| `IDynamoDBContext` | prefixが自動で付く。`OverrideTableName`は使わない | 操作ごとに`OverrideTableName`を指定する |
| `IAmazonDynamoDB`（低レベルAPI） | `DynamoDbTableNames`の値（`prefix + テーブル名`）を使う | `DynamoDbTableNames`の値（環境変数の値）を使う |

2つの方式は混ぜない。SDKは`OverrideTableName`の名前にも`TableNamePrefix`を付けるので、併用するとprefixが二重に付く。

**パターン1の手順**: コードの変更は不要。デプロイ先のLambdaに`DYNAMODB_TABLE_NAME_PREFIX`を設定し、テーブルを`prefix + [DynamoDBTable]の名前`（例: `myapp-stg-sample_items`）で作る。ローカルでは設定しない。

**パターン2の手順**（コードの全文は[docs/usage.md](docs/usage.md)の3章を参照）:

1. `Configures/DynamoDbConfig.cs`: `TableNamePrefix`の設定をやめ、`DynamoDbTableNames.FromEnvironment()`を登録する。
2. `Configures/DynamoDbTableNames.cs`: 全テーブルの分のプロパティを持ち、`TABLE_XXX`から読む形に置き換える。
3. `IDynamoDBContext`の全ての操作（`Endpoints/Sample.cs`の`/sample/item/{id}`を含む）に`OverrideTableName`を指定する。
4. デプロイ先のLambdaに`TABLE_XXX`を設定し、`DYNAMODB_TABLE_NAME_PREFIX`は設定しない。

テーブルの追加手順も方式によって異なる（[docs/usage.md](docs/usage.md)の3章を参照）。
