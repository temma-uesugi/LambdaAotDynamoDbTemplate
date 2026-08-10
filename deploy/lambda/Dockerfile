# Lambda（マネージド.NET 10ランタイムイメージ / Native AOT）向け本番用イメージ。
# ローカル開発用の LambdaAotDynamoDbTemplate/Dockerfile（dotnet run）とは別に用意している。
# ビルドコンテキストはリポジトリルート想定（docker build -f deploy/lambda/Dockerfile .）。
# MasterData/Generated/master.bytesはgit管理外のため、ビルド前に`dotnet run --project MasterData.Generator`で
# 生成しておく必要がある（未生成のままイメージを作ると起動時にFileNotFoundExceptionになる）。
# Note: ベースイメージをprovided:al2023（OSのみ）+自前bootstrapにすると、Lambda関数のliveエイリアスが
# 古いイメージを指したままの場合に「entrypoint requires the handler name to be the first argument」で
# 起動時クラッシュすることがある（イメージ側の問題とは限らず、エイリアス設定の確認も要）。
# ここではマネージドdotnet:10イメージを採用している。

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Native AOT のネイティブコンパイルに必要なツールチェーン
RUN apt-get update \
    && apt-get install -y --no-install-recommends clang zlib1g-dev zip \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY MasterData/MasterData.csproj MasterData/
COPY LambdaAotDynamoDbTemplate/LambdaAotDynamoDbTemplate.csproj LambdaAotDynamoDbTemplate/
RUN dotnet restore LambdaAotDynamoDbTemplate/LambdaAotDynamoDbTemplate.csproj

COPY MasterData/ MasterData/
COPY LambdaAotDynamoDbTemplate/ LambdaAotDynamoDbTemplate/
RUN dotnet publish LambdaAotDynamoDbTemplate/LambdaAotDynamoDbTemplate.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained true \
    -p:PublishAot=true \
    -o /app/publish \
    && mv /app/publish/LambdaAotDynamoDbTemplate /app/publish/bootstrap

FROM public.ecr.aws/lambda/dotnet:10 AS final
WORKDIR /var/task
COPY --from=build /app/publish/ ./
ENTRYPOINT ["./bootstrap"]
