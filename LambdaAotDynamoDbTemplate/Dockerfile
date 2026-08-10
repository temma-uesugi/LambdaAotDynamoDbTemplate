FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src
COPY . .

EXPOSE 5000

ENTRYPOINT ["dotnet", "run", "--project", "LambdaAotDynamoDbTemplate", "--no-launch-profile"]
