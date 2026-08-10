# 汎用コードの使い方

このテンプレートに収録されている、案件を問わずそのまま使えるコードの一覧と使い方。
`Endpoints/Sample.cs`・`DBs/SampleItem.cs`・`Dto.cs`のサンプル部分は、
実際のAPIが決まったら削除・置き換えてよい「動くサンプル」であり、それ以外は基本的にそのまま使う想定。

## 1. 起動フロー（Program.cs）

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

builder.ConfigureLambda();      // Lambda(REST API)ホスティング + AOT向けJSONシリアライザ登録
builder.ConfigureDynamoDb();    // IAmazonDynamoDB / IDynamoDBContext / DynamoDbTableNames のDI登録
builder.ConfigureSwagger();     // Swashbuckle（本番では後段でUseSwagger自体を呼ばない）
builder.ConfigureJwt();         // JWT署名鍵取得 + JwtTokenServiceのDI登録
builder.ConfigureRecaptcha();   // reCAPTCHAシークレット取得 + RecaptchaServiceのDI登録
```

各`ConfigureXxx`は`Configures/`配下に1ファイル1機能で定義されている。案件で不要な機能（reCAPTCHA等）は
呼び出し自体をコメントアウトし、対応する`Configures/Xxx.cs`・`Services/Xxx.cs`を削除すればよい。

## 2. Configures/

| ファイル | 役割 |
|---|---|
| `LambdaConfig.cs` | `AddAWSLambdaHosting`でAPI Gateway REST API連携を有効化。`dotnet run`等の通常起動時は素通りする |
| `DynamoDbConfig.cs` | ローカル(`IsDevelopment`)では`DYNAMODB_LOCAL_ENDPOINT`（未設定時`http://localhost:8001`）を参照するDynamoDBクライアントを、本番では通常のクライアントを登録する |
| `DynamoDbTableNames.cs` | テーブルごとの実テーブル名を環境変数から読む。**新規テーブル追加時はここにプロパティを足す**（3章参照） |
| `JwtConfig.cs` | ローカルはダミー鍵、本番は`SSM_PARAM_JWT`が指すParameter StoreからJWT署名鍵を取得 |
| `RecaptchaConfig.cs` | ローカルはダミー、本番は`SSM_PARAM_RECAPTCHA`からreCAPTCHAシークレットを取得。使わない案件ではファイルごと削除してよい |
| `OriginVerificationConfig.cs` | CloudFrontが注入する秘匿ヘッダー（`X-Origin-Secret`、値は`ORIGIN_VERIFY_SECRET`環境変数）を検証し、直叩きを403で拒否する。ローカルでは無効 |
| `SecurityHeadersConfig.cs` | `X-Content-Type-Options: nosniff`等、レスポンスに付与するセキュリティヘッダーをまとめる場所 |
| `SwaggerConfig.cs` | Swagger UIの設定。Bearer認証UIの定義例がコメントアウトで残してある（4章参照） |

## 3. DynamoDBテーブルを追加する

`DBs/SampleItem.cs`が実装のサンプル。新規テーブルを追加する手順は以下の4点セット。

1. **`DBs/XxxTable.cs`**: `[DynamoDBTable("xxx_table")]`を付けたクラスを作り、`[DynamoDBHashKey]`等でキー・属性を定義する（`SampleItem.cs`参照）。
2. **`Configures/DynamoDbTableNames.cs`**: プロパティと`FromEnvironment()`内の読み込み行を追加する（環境変数名はインフラ側の命名規則に合わせる）。
3. **`docker/dynamodb/tables/xxx_table.yaml`**: `aws dynamodb create-table --cli-input-yaml`形式でローカル用のテーブル定義を追加する（`sample_items.yaml`参照）。`docker/dynamodb/init-tables.sh`はこのディレクトリの`*.yaml`を自動で拾うので、スクリプト自体の変更は不要。
4. **（必要なら）`docker/dynamodb/seed/xxx_table.yaml`**: `batch-write-item`形式で初期データを入れたい場合に追加する（`sample_items.yaml`参照）。

エンドポイント側からは`IDynamoDBContext`（`LoadAsync`/`SaveAsync`等のオブジェクトマッパー）または
`IAmazonDynamoDB`（`UpdateItemAsync`等、条件付き書き込みやアトミックインクリメントが必要な場合の低レベルAPI）を
DIで受け取って使う。テーブル名は必ず`DynamoDbTableNames`経由で取得し、コードに直接テーブル名文字列を書かない。

## 4. 認証（Cookie + JWT）

`Services/CookieService.cs`・`JwtTokenService.cs`・`UserIdentityService.cs`で、サーバ発行GUIDを
署名付きJWTとしてHttpOnly Cookieに格納する匿名ユーザ識別の基盤ができている。

```csharp
// 新規ユーザにCookieを発行する場合の書き方
var userId = Guid.NewGuid();
var token = jwtTokenService.IssueToken(userId, Const.CookieExpiration);
cookieService.SetWithExpiry(context.Response, Const.UserIdCookieKey, token, Const.CookieExpiration);

// Cookieからユーザを識別する場合（Endpoints/Sample.csの/sample/authが実例）
var userId = await userIdentityService.TryGetUserIdAsync(context);
if (userId is null)
{
    return TypedResults.Unauthorized();
}
```

Cookie方式ではなくBearerヘッダ方式で認証したい案件では、`JwtTokenService.TryGetUserIdAsync(string?)`
（`Authorization: Bearer <token>`を直接検証する）をそのまま使い、`Configures/SwaggerConfig.cs`内の
コメントアウトされたBearer認証UI定義を有効化する。

## 5. reCAPTCHA

`Services/RecaptchaService.VerifyAsync(token)`はローカル環境（`IsDevelopment`）では常に`true`を返し、
本番ではGoogleのsiteverify APIを呼んで検証する。タイムアウト・通信失敗時は`false`（検証失敗）を返す
（在庫消費や決済確定など、失敗を握りつぶすと問題になる処理の前段で呼ぶ想定）。使わない案件では
`Configures/RecaptchaConfig.cs`・`Services/RecaptchaService.cs`・`Program.cs`の
`ConfigureRecaptcha()`呼び出しを削除してよい。

## 6. ログ（AppLog）

DIを介さずどこからでも呼べる。呼び出し元のファイル:行番号が自動で付き、オブジェクト引数は
`ApiSerializationContext`経由でJSON化される（`token`/`secret`/`password`等を含むキーは自動マスク）。

```csharp
AppLog.Debug("ユーザ取得", userId);
AppLog.Information("処理完了", new { count = 3 });
AppLog.Warning("想定外の状態", state);
AppLog.Error(exception, "処理に失敗しました", context);
```

オブジェクト引数を渡す場合、その型が`ApiSerializationContext`に`[JsonSerializable]`で登録されていないと
`unserializable: 型名`にフォールバックする（AOTではリフレクションで拾えないため）。ログに渡したい型を
増やしたら、DTOと同様に`ApiSerializationContext.cs`に追記すること。

## 7. 例外（Exceptions）

`AppException`を継承した例外は、コンストラクタ内で`AppLog.Error`を自動的に呼ぶ
（throw漏れなくログに残る）。設定値・シークレット取得失敗用に`AppConfigurationException`が既にある。
案件固有の例外を増やす場合は同様に`AppException`を継承する。

## 8. DTOとApiSerializationContext

`Dto.cs`にAPIの入出力型をまとめ、DynamoDBの内部モデル（`DBs/*.cs`）とは分離する
（`Dto.SampleItemRes`がDBs/SampleItem.csとの対比例）。新規にDTO・内部モデルを追加したら、
**必ず`ApiSerializationContext.cs`に`[JsonSerializable(typeof(...))]`を追加する**こと。
Native AOTではリフレクションでの自動解決ができないため、登録漏れは実行時エラーになる
（`int`/`string`/`bool`等のプリミティブ型は登録不要）。

## 9. エンドポイントの追加

`Endpoints/Sample.cs`が実装のサンプル（`Program.cs`で`api.MapSampleEndpoints();`として登録済み）。

- **`GET /api/sample/get/{amount}`**: 最小構成。DTOを介さずプリミティブ型をそのまま返す例。
- **`POST /api/sample/post`**: リクエスト・レスポンスをそれぞれ`Dto.SamplePostReq`/`Dto.SamplePostRes`として
  定義し、`[FromBody]`で受け取って返す例。
- **`GET /api/sample/auth`**: 正常時のDTOレスポンスに加えて`UnauthorizedHttpResult`も返しうる
  エンドポイントの書き方。戻り値の型を`Results<Ok<Dto.SampleAuthRes>, UnauthorizedHttpResult>`にし、
  分岐ごとに`TypedResults.Ok(...)` / `TypedResults.Unauthorized()`を返す
  （3つ以上の戻り値パターンが必要な場合は`Results<T1, T2, T3>`のように型引数を増やす）。
- **`GET /api/sample/item/{id}`**: `IDynamoDBContext`と`DynamoDbTableNames`をDIで受け取り、
  `sample_items`テーブルを`LoadAsync`で1件取得する例。見つからなければ`NotFound`を返す
  （2章のテーブル追加手順・3章のDynamoDB操作の実例）。

新規案件では、`Endpoints/XxxEndpoints.cs`を機能単位で作り、`MapXxxEndpoints(this IEndpointRouteBuilder app)`
という拡張メソッドとして実装し、`Program.cs`の`var api = app.MapGroup("/api");`以降で`api.MapXxxEndpoints();`
を呼んで登録する。本番で無効化したいデバッグ用エンドポイント等は、`if (!app.Environment.IsProduction()) { ... }`
の中でマップする。

## 10. マスタデータ（MasterMemory）

DynamoDBに格納するデータとは別に、コード変更なしに調整したい定数的なデータ（アイテム定義・パラメータ
テーブル等）をYAMLで管理し、[MasterMemory](https://github.com/Cysharp/MasterMemory)でバイナリ化して
アプリに埋め込む仕組み。`MasterData`（テーブル定義）・`MasterData.Generator`（YAML→バイナリ変換ツール）
の2プロジェクトで構成される。

`MasterData.Generator`を別プロジェクトに分けているのは、Native AOT LambdaというAOT/パッケージサイズに
シビアな構成のため。YAML読み込みに使う`YamlDotNet`（リフレクション主体でAOT/トリミングと相性が悪い）を
`MasterData`本体に同居させると、Lambda本体がそれをプロジェクト参照した瞬間にAOTパブリッシュへ巻き込まれて
しまう。`MasterData`＝Lambdaが参照する軽量スキーマ、`MasterData.Generator`＝開発時だけ動かすビルドツール、
と役割を分けている。

- **`MasterData/Entities/XxxEntity.cs`**: `[MemoryTable("テーブル名")]` + `[MessagePackObject(true)]`を
  付けたテーブル定義。コンストラクタ引数がYAMLのキー（PascalCase/camelCaseどちらでも対応）に対応する
  （`MasterData/Entities/MasterSample.cs`参照）。
  **名前空間は必ずブロック形式(`namespace X { }`)で書く**こと。ファイルスコープ名前空間(`namespace X;`)だと
  MasterMemoryのソースジェネレータ(3.0.4時点)が型を解決できずビルドエラーになる。
- **`MasterData/Sources/テーブル名.yml`**: 実データ。ファイル名は`[MemoryTable]`で指定したテーブル名と
  一致させる（`MasterData/Sources/MasterSample.yml`参照）。対応するYAMLが無いテーブルは、生成時に警告を
  出してスキップされる（エラーにはならない）。
- **`MasterData/MasterDataConfig.cs`**: `builder.ConfigureMasterData();`で、生成済みバイナリを読み込んで
  `MemoryDatabase`をDIのシングルトンとして登録する（`Program.cs`に登録済み）。エンドポイント側は他のDI
  サービスと同様に`MemoryDatabase`を引数で受け取って使う。

  ```csharp
  app.MapGet("/sample/master/{id:int}", (int id, MemoryDatabase masterDb) =>
      masterDb.MasterSampleTable.FindById(id) is { } item
          ? TypedResults.Ok(item)
          : TypedResults.NotFound());
  ```

### 新規テーブルを追加する手順

1. **`MasterData/Entities/XxxEntity.cs`**: `[MemoryTable("Xxx")]`を付けたクラスを作る
   （`MasterSample.cs`参照）。
2. **`MasterData/Sources/Xxx.yml`**: テーブル名と同名のYAMLファイルを追加する。
3. 下記コマンドでバイナリを再生成する。

### マスタデータの生成

`MasterData/Sources/*.yml`を編集したら、`MasterData/Generated/master.bytes`を再生成する
（このファイルはgit管理外で、`LambdaAotDynamoDbTemplate.csproj`がビルド時に`master.bytes`として
出力ディレクトリへコピーする設定になっている。未生成のまま起動すると`FileNotFoundException`になる）。

```
dotnet run --project MasterData.Generator
```

## 11. 環境変数一覧

| 環境変数 | 用途 | ローカルでの扱い |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | 環境判定（`Development`/`Staging`/`Production`等） | `docker-compose.yml`で`Development`固定 |
| `DYNAMODB_LOCAL_ENDPOINT` | DynamoDB Localのエンドポイント | `docker-compose.yml`で`http://db:8000`。ホストで`dotnet run`する場合は未設定時のデフォルト`http://localhost:8001`が使われる |
| `TABLE_SAMPLE_ITEMS` | `sample_items`テーブルの実テーブル名 | 未設定時は`sample_items`（テーブル追加時は同様の環境変数を追加する） |
| `SSM_PARAM_JWT` | JWT署名鍵のParameter Storeパラメータ名 | ローカルでは未参照（ダミー鍵を使用） |
| `SSM_PARAM_RECAPTCHA` | reCAPTCHAシークレットのParameter Storeパラメータ名 | ローカルでは未参照（reCAPTCHA検証自体をスキップ） |
| `ORIGIN_VERIFY_SECRET` | CloudFront直叩き防止用の秘匿ヘッダーの期待値 | ローカルではミドルウェア自体を登録しないため未参照 |
| `FRONTEND_PORT` / `API_PATH` | ローカルの同一オリジン確認用プロキシ（Caddy）向け設定 | `.env`（`.env.example`参照） |
