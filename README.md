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
