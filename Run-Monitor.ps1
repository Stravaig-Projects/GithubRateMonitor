param
(
    [string]
    [Parameter(Mandatory=$true)]
    $GitHubAuthToken,

    [string]
    [Parameter(Mandatory=$true)]
    $GitHubUserName
)

$SolutionFile = "$PSScriptRoot/src/Stravaig.GithubRateMonitor.sln"
$ProjectFile = "$PSScriptRoot/src/GithubRateMonitor/GithubRateMonitor.csproj";
$AppPublishFolder = "$PSScriptRoot/app";

if (-not (Test-Path $AppPublishFolder))
{
    New-Item -Type Directory $AppPublishFolder
}

Write-Host "Cleaning destination..." -ForegroundColor Green
Get-ChildItem -Path $AppPublishFolder -Recurse -File | Remove-Item

Write-Host "Restoring..." -ForegroundColor Green
& dotnet restore "$SolutionFile"
if ($LASTEXITCODE -ne 0)
{
    Write-Error "Restore failed!"
    EXIT $LASTEXITCODE;
}

Write-Host "Building..." -ForegroundColor Green
& dotnet build "$SolutionFile" --configuration Release
if ($LASTEXITCODE -ne 0)
{
    Write-Error "Build failed!"
    EXIT $LASTEXITCODE;
}

Write-Host "Publishing..." -ForegroundColor Green
& dotnet publish "$ProjectFile" --configuration Release  --output "$AppPublishFolder" --force
if ($LASTEXITCODE -ne 0)
{
    Write-Error "Publish failed!"
    EXIT $LASTEXITCODE;
}

Write-Host "Running..." -ForegroundColor Green
& dotnet "$AppPublishFolder/GithubRateMonitor.dll" --GitHubApi:Token=$GitHubAuthToken --GitHubApi:UserName=$GitHubUserName
if ($LASTEXITCODE -ne 0)
{
    Write-Error "Run failed!"
    EXIT $LASTEXITCODE;
}