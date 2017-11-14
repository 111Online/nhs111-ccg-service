$jsonPath = "$env:APPLICATION_PATH\appsettings.json"

Write-Output "Loading json file from $jsonPath"

$json = Get-Content $jsonPath -raw | ConvertFrom-Json

$json.connection=[Environment]::GetEnvironmentVariable("connectionstring")
$json.table=[Environment]::GetEnvironmentVariable("tablereference")

$json | ConvertTo-Json  | Set-Content $jsonPath

dotnet ./NHS111.Business.CCG.Api.dll