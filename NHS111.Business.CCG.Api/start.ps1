$jsonPath = "$env:APPLICATION_PATH\appsettings.json"

Write-Output "Loading json file from $jsonPath"

$json = Get-Content $jsonPath -raw | ConvertFrom-Json

$json.connection=[Environment]::GetEnvironmentVariable("connectionstring")
$json.ccgtable=[Environment]::GetEnvironmentVariable("ccgtablereference")
$json.stptable=[Environment]::GetEnvironmentVariable("stptablereference")
$json.stptable=[Environment]::GetEnvironmentVariable("nationalwhitelistblobname")
$json.ApplicationInsights.InstrumentationKey=[Environment]::GetEnvironmentVariable("InstrumentationKey")

$json | ConvertTo-Json  | Set-Content $jsonPath

dotnet ./NHS111.Business.CCG.Api.dll