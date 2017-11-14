$jsonPath = "$env:APPLICATION_PATH\appsettings.json"

Write-Output "Loading json file from $jsonPath"

$json = Get-Content $jsonPath -raw | ConvertFrom-Json

$json.connectionstring=[Environment]::GetEnvironmentVariable("connectionstring")
$json.tablereference=[Environment]::GetEnvironmentVariable("tablereference")
$json.NhsPathwaysConfigurations.ApplicationUrl=[Environment]::GetEnvironmentVariable("NhsPathwaysConfigurations.ApplicationUrl")

$json | ConvertTo-Json  | Set-Content $jsonPath

dotnet ./NHS111.Business.CCG.Api.dll