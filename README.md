# LambdaAotDynamoDbTemplate

AWS Lambda（.NET Native AOT）+ API Gateway（REST API）+ DynamoDB 構成のバックエンドAPIを
新規に始めるときの雛形。[Pokemon（クイズキャンペーンAPI）](../Pokemon)プロジェクトから、
案件固有のビジネスロジックを除いた汎用インフラ部分のみを抜き出したもの。

## 収録しているもの（汎用インフラ・そのまま使える）

| 領域 | 内容 |
|---|---|
| `Configures/` | Lambda(AOT)ホスティング、DynamoDBクライアント/コンテキストのDI登録、JWT署名鍵取得、reCAPTCHA検証設定、CloudFront直叩き防止ミドルウェア、セキュリティヘッダー付与、Swagger設定 |
| `Services/` | Cookie読み書き、JWT発行/検証、reCAPTCHA検証、Secrets Manager/Parameter Storeからのシークレット取得（キャッシュ付き）、Cookie+JWTによる匿名ユーザ識別 |
| `Logging/` | `AppLog.Debug(...)`等でDI無しに呼べる構造化ログ。呼び出し元ファイル:行番号を自動付与し、引数はAOT対応のJSONシリアライズ＋機微情報マスク付き |
| `Exceptions/` | ログ出力を伴うアプリ内例外基底クラス |
| `ApiSerializationContext.cs` | AOT向けJSONソース生成コンテキスト（新しいDTO/内部モデルを追加したら`[JsonSerializable]`もここに追加する） |
| `MasterData/` | [MasterMemory](https://github.com/Cysharp/MasterMemory)によるマスタデータ基盤。テーブル定義（`Entities/`）とYAMLソース（`Sources/`）を持ち、`MasterDataConfig.ConfigureMasterData()`でDIに`MemoryDatabase`を登録する |
| `MasterData.Generator/` | `MasterData/Sources/*.yml`を`MasterData/Generated/master.bytes`にビルドする開発時専用のコンソールツール（Lambda本体には含めない） |
| `docker-compose.yml` / `docker/` | DynamoDB Local + アプリのローカル開発環境一式 |
| `deploy/lambda/Dockerfile` | Lambda本番用（Native AOT、マネージド`dotnet:10`ランタイムイメージ）のビルド |

## 案件ごとに追加するもの（サンプル実装が入っている。置き換え・削除してよい）

`sample_items`テーブル・`Dto.SampleItemRes` / `SamplePostReq` / `SamplePostRes` / `SampleAuthRes`・
`Endpoints/Sample.cs`が一連の動くサンプルになっている（3〜4章参照）。案件のテーブル・APIが決まったら、
これらを削除・置き換えて、同じパターンで追加していく。

- **`DBs/SampleItem.cs`**: DynamoDBテーブル定義（`[DynamoDBTable]` + `[DynamoDBHashKey]`）のサンプル。
- **`Configures/DynamoDbTableNames.cs`**: 環境変数からテーブルの実名を受け取る仕組みのサンプル
  （`SampleItems`プロパティ / `TABLE_SAMPLE_ITEMS`環境変数）。テーブルを追加したらここにもプロパティを足す。
- **`Dto.cs`**: DB内部モデルとAPIレスポンス型を分離するサンプル（`SampleItemRes`）と、
  GET/POSTサンプルエンドポイントのリクエスト・レスポンス型。
- **`docker/dynamodb/tables/sample_items.yaml`・`docker/dynamodb/seed/sample_items.yaml`**:
  ローカルDynamoDB用のテーブル定義とシードデータ。新規テーブルもこのディレクトリに同様の形式で追加する
  （`docker/dynamodb/init-tables.sh`はディレクトリ内の`*.yaml`を自動で拾う汎用スクリプトなので変更不要）。
- **`Endpoints/Sample.cs`**: GET・POST・認証必須（`UnauthorizedHttpResult`を返しうる）の3パターンの
  最小サンプル。`Program.cs`で`api.MapSampleEndpoints();`として登録済み。案件のAPIが決まったら
  `Endpoints/XxxEndpoints.cs`を機能単位で作り、`MapXxxEndpoints()`拡張メソッドとして実装、
  `Program.cs`の`var api = app.MapGroup("/api");`以降で`api.MapXxxEndpoints();`を呼ぶ。
- **`MasterData/Entities/MasterSample.cs`・`MasterData/Sources/MasterSample.yml`**: MasterMemoryによる
  マスタデータのサンプル。新規テーブルの追加手順・`MasterData.Generator`の使い方はdocs/usage.md参照。

汎用インフラ（`Configures/`・`Services/`・`Logging/`等）の詳しい使い方は[docs/usage.md](docs/usage.md)参照。

## 認証パターンについて

匿名ユーザをサーバ発行GUID + 署名付きJWTで識別し、Cookie（`HttpOnly`）で保持する構成を想定している
（`Services/CookieService.cs` / `JwtTokenService.cs` / `UserIdentityService.cs`）。

`Authorization: Bearer`ヘッダで直接JWTを検証する`JwtTokenService.TryGetClaimsAsync(string?)`も
あえて残してある。Cookie方式ではなくBearerヘッダ方式を使いたい案件では、これをそのまま呼び出し、
`Configures/SwaggerConfig.cs`内のコメントアウトされたBearer認証UI定義を有効化する。

## ローカルでの動作確認

初回・`MasterData/Sources/`配下のYAMLを変更した際は、先にマスタデータのバイナリを生成しておく
（未生成のまま起動すると`FileNotFoundException`になる）。

```
dotnet run --project MasterData.Generator
```

```
docker compose up
```

DynamoDB Local + アプリ本体（`http://localhost:5001`）が起動する。テーブル定義・シードは
`docker/dynamodb/tables/` `docker/dynamodb/seed/`に追加した`*.yaml`を`db-init`サービスが自動投入する
（`-inMemory`のため、コンテナ再起動でデータは消える）。

.NET SDKがローカルにある場合、DBだけdocker-composeで起動し、アプリは`dotnet run`で動かすと
コード変更の反映が速い。

```
docker compose up db db-init
dotnet run --project LambdaAotDynamoDbTemplate
```

### DynamoDB Localの中身をPowerShellから確認する

`scripts/show-tables.ps1`・`scripts/get-all.ps1`は、`http://localhost:8001`（`docker-compose.yml`が
ポート公開しているDynamoDB Local）に対して直接AWS CLIを叩く確認用スクリプト。ポート公開さえされていれば
DynamoDB Localをdocker-compose上・ホスト上どちらで動かしていても使える。

```
pwsh scripts/show-tables.ps1
pwsh scripts/get-all.ps1 sample_items
```

### フロントとの同一オリジン（Cookie認証）

Cookie認証はブラウザの同一オリジン（または同一サイト）判定に依存するため、フロントとAPIは同一オリジンにそろえる。

- **本番**: CloudFrontなどで同一ドメインにし、パスでフロントとAPIを振り分ける（CORS不要）。
- **ローカル**: クライアント側の開発サーバーのプロキシで`/api`などをこのサーバー（`http://localhost:5001`）へ転送し、
  同一オリジンにそろえるのを推奨する。例としてViteの`server.proxy`:

  ```ts
  // vite.config.ts
  export default defineConfig({
    server: {
      proxy: {
        '/api': 'http://localhost:5001',
      },
    },
  });
  ```

  Next.jsの`rewrites`、Angularの`proxy.conf.json`、webpackの`devServer.proxy`など、他のツールにも同種の機能がある。
  サーバー側のテンプレートは特定のフロント構成を前提にしない。

推奨するローカルの起動手順:

1. `docker-compose up -d db db-init`でDBだけ起動する。
2. サーバーを`dotnet run`（`launchSettings.json`のプロファイル）で起動する。
3. クライアントは自前の開発サーバーを起動し、その開発サーバーのポートでアクセスする。

`docker-compose.yml`の`app`サービスは、コンテナで丸ごと動かしたい場合向けの任意のもの。通常は`dotnet run`を推奨する。

どうしても別オリジンで呼びたい場合だけ、`Program.cs`のコメントアウトされたCORS設定を有効にする。

- 固定オリジン + `AllowCredentials`にする（`AllowAnyOrigin`とは併用不可）。
- `localhost`同士はポートが違ってもsame-siteなので、`SameSite=None`は不要。
- `AddCors`と`UseCors`は必ず同時に有効にする（`AddCors`なしで`UseCors`だけだと例外になる）。

## 本番デプロイ用イメージ

`MasterData/Sources/`のYAMLに変更があれば、先にマスタデータのバイナリを再生成しておく
（`MasterData/Generated/master.bytes`はgit管理外のため、未生成のままイメージを作ると
起動時に`FileNotFoundException`になる）。

```
dotnet run --project MasterData.Generator
docker build -f deploy/lambda/Dockerfile -t <image-name> .
```

Native AOTでビルドした`bootstrap`を含む、Lambdaのマネージド`.NET 10`ランタイムイメージ
（`public.ecr.aws/lambda/dotnet:10`）ベースの本番用イメージを作る。ビルドコンテキストは
リポジトリルート。
