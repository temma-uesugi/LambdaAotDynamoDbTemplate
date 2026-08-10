#Requires -Version 7.0

$ErrorActionPreference = "Stop"

$endpointUrl = "http://localhost:8001"

$env:AWS_ACCESS_KEY_ID = "local"
$env:AWS_SECRET_ACCESS_KEY = "local"

$raw = aws dynamodb list-tables --endpoint-url $endpointUrl --region us-east-1 --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) {
    throw "Failed to list tables."
}

$raw.TableNames
