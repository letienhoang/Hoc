[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$generator = Join-Path $PSScriptRoot 'new-consumer.ps1'
$sha = '6a8605a63d18d35767f520d57d33193f6d15aa29'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('h4-03-test-' + [guid]::NewGuid().ToString('N'))
$sourceStatusBefore = (& git -C $root status --porcelain) -join "`n"
function Assert([bool] $Condition, [string] $Message) { if (-not $Condition) { throw "ASSERTION FAILED: $Message" } }
function Expect-Failure([scriptblock] $Action, [string] $Message) { try { & $Action; throw "Expected failure: $Message" } catch { if ($_.Exception.Message -like "Expected failure:*") { throw }; Write-Host "PASS: $Message" } }
try {
    New-Item -ItemType Directory -Path $temp | Out-Null
    $dry = Join-Path $temp 'DryRun'
    & $generator -SourceRoot $root -SourceSha $sha -OutputRoot $dry -ApplicationId ContosoAdmin -RootUrl https://localhost:44320
    Assert (-not (Test-Path -LiteralPath $dry)) 'Dry-run created output.'
    Expect-Failure { & $generator -SourceRoot $root -SourceSha ('0' * 40) -OutputRoot (Join-Path $temp 'badsha') -ApplicationId ContosoAdmin -RootUrl https://localhost:44320 } 'Unsupported SHA is rejected.'
    Expect-Failure { & $generator -SourceRoot $temp -SourceSha $sha -OutputRoot (Join-Path $temp 'badroot') -ApplicationId ContosoAdmin -RootUrl https://localhost:44320 } 'Invalid source checkout is rejected.'
    Expect-Failure { & $generator -SourceRoot $root -SourceSha $sha -OutputRoot (Join-Path $temp 'badid') -ApplicationId 'bad-id' -RootUrl https://localhost:44320 } 'Invalid ApplicationId is rejected.'
    Expect-Failure { & $generator -SourceRoot $root -SourceSha $sha -OutputRoot (Join-Path $temp 'badurl') -ApplicationId ContosoAdmin -RootUrl http://localhost:44320 } 'Non-HTTPS RootUrl is rejected.'
    $existing = Join-Path $temp 'exists'; New-Item -ItemType Directory -Path $existing | Out-Null
    Expect-Failure { & $generator -SourceRoot $root -SourceSha $sha -OutputRoot $existing -ApplicationId ContosoAdmin -RootUrl https://localhost:44320 } 'Existing OutputRoot is rejected.'
    $sourceChild = Join-Path $root '.h4-03-test-output'
    Expect-Failure { & $generator -SourceRoot $root -SourceSha $sha -OutputRoot $sourceChild -ApplicationId ContosoAdmin -RootUrl https://localhost:44320 } 'Output below SourceRoot is rejected.'
    $link = Join-Path $temp 'linked-parent'
    try {
        New-Item -ItemType Junction -Path $link -Target $temp | Out-Null
        Expect-Failure { & $generator -SourceRoot $root -SourceSha $sha -OutputRoot (Join-Path $link 'escaped') -ApplicationId ContosoAdmin -RootUrl https://localhost:44320 } 'Reparse-point output parent is rejected.'
    }
    finally { if (Test-Path -LiteralPath $link) { Remove-Item -LiteralPath $link -Force } }
    $faulted = Join-Path $temp 'faulted'
    Expect-Failure { & $generator -SourceRoot $root -SourceSha $sha -OutputRoot $faulted -ApplicationId ContosoAdmin -RootUrl https://localhost:44320 -Apply -TestFailAfterTransforms 1 } 'Injected failure is cleaned up.'
    Assert (-not (Test-Path -LiteralPath $faulted)) 'Fault injection published partial output.'
    $consumer = Join-Path $temp 'ContosoAdmin'
    & $generator -SourceRoot $root -SourceSha $sha -OutputRoot $consumer -ApplicationId ContosoAdmin -RootUrl https://localhost:44320 -Apply
    Assert (Test-Path -LiteralPath (Join-Path $consumer 'ContosoAdmin.slnx')) 'Expected solution is missing.'
    Assert (-not (Test-Path -LiteralPath (Join-Path $consumer '.git'))) 'Consumer inherited Git metadata.'
    Assert (-not (Test-Path -LiteralPath (Join-Path $consumer 'samples'))) 'Consumer retained sample directory.'
    Assert (-not (Test-Path -LiteralPath (Join-Path $consumer 'src/ContosoAdmin.EntityFrameworkCore/Migrations'))) 'Consumer retained migrations.'
    $webSettings = Get-Content -LiteralPath (Join-Path $consumer 'src/ContosoAdmin.Web/appsettings.json') -Raw | ConvertFrom-Json
    Assert ($webSettings.App.SelfUrl -eq 'https://localhost:44320') 'Generated Web App:SelfUrl does not match RootUrl.'
    $migratorSettings = Get-Content -LiteralPath (Join-Path $consumer 'src/ContosoAdmin.DbMigrator/appsettings.json') -Raw | ConvertFrom-Json
    Assert ($migratorSettings.OpenIddict.Applications.ContosoAdmin_Web.RootUrl -eq 'https://localhost:44320') 'Generated Web OpenIddict RootUrl does not match RootUrl.'
    Assert ($migratorSettings.OpenIddict.Applications.ContosoAdmin_Swagger.RootUrl -eq 'https://localhost:44320') 'Generated Swagger OpenIddict RootUrl does not match RootUrl.'
    $launch = Get-Content -LiteralPath (Join-Path $consumer 'src/ContosoAdmin.Web/Properties/launchSettings.json') -Raw | ConvertFrom-Json
    Assert ($launch.iisSettings.iisExpress.applicationUrl -eq 'https://localhost:44320/') 'Generated IIS Express URL does not match RootUrl.'
    Assert ($launch.iisSettings.iisExpress.sslPort -eq 44320) 'Generated IIS Express SSL port does not match RootUrl.'
    Assert ($launch.profiles.'IIS Express'.applicationUrl -eq 'https://localhost:44320/') 'Generated IIS Express profile URL does not match RootUrl.'
    Assert ($launch.profiles.'ContosoAdmin.Web'.applicationUrl -eq 'https://localhost:44320/') 'Generated project profile URL does not match RootUrl.'
    $consumerReadme = Get-Content -LiteralPath (Join-Path $consumer 'README.md') -Raw
    Assert ($consumerReadme -match 'separate, explicitly authorized database operation') 'Generated README does not describe the separate database authorization.'
    Assert ($consumerReadme -match 'can both migrate and seed persistent data') 'Generated README does not describe DbMigrator persistence behavior.'
    & (Join-Path $PSScriptRoot 'verify-consumer.ps1') -ConsumerRoot $consumer -ApplicationId ContosoAdmin
    Assert ($LASTEXITCODE -eq 0) 'H4-01 structural validator failed.'
    & (Join-Path $PSScriptRoot 'new-admin-module.ps1') -ConsumerRoot $consumer -ApplicationId ContosoAdmin -ModuleName Products -EntityName Product -RouteSegment products
    Assert (((& git -C $root status --porcelain) -join "`n") -eq $sourceStatusBefore) 'Generator modified SourceRoot.'
    Write-Host 'RESULT: PASS'
}
finally { if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force } }
