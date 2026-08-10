#Requires -Version 7.0

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$TableName
)

$ErrorActionPreference = "Stop"

$endpointUrl = "http://localhost:8001"

$env:AWS_ACCESS_KEY_ID = "local"
$env:AWS_SECRET_ACCESS_KEY = "local"

function ConvertFrom-DynamoDBAttributeValue {
    param($AttributeValue)

    if ($null -eq $AttributeValue) { return $null }
    if ($AttributeValue.PSObject.Properties.Match("N").Count -gt 0) {
        $numberText = $AttributeValue.N
        $longValue = 0L
        if ([long]::TryParse($numberText, [ref]$longValue)) { return $longValue }
        return [double]$numberText
    }
    if ($AttributeValue.PSObject.Properties.Match("S").Count -gt 0) { return $AttributeValue.S }
    if ($AttributeValue.PSObject.Properties.Match("BOOL").Count -gt 0) { return $AttributeValue.BOOL }
    return $AttributeValue
}

$raw = aws dynamodb scan --table-name $TableName --endpoint-url $endpointUrl --region us-east-1 --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) {
    throw "Failed to scan table '$TableName'."
}

$rows = foreach ($item in $raw.Items) {
    $row = [ordered]@{}
    foreach ($attributeName in $item.PSObject.Properties.Name) {
        $row[$attributeName] = ConvertFrom-DynamoDBAttributeValue $item.$attributeName
    }
    [pscustomobject]$row
}

@($rows) | ConvertTo-Json
