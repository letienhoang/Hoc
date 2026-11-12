[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$validator = Join-Path $PSScriptRoot 'verify-consumer.ps1'
if (-not (Test-Path -LiteralPath $validator -PathType Leaf)) {
    throw "Validator was not found: $validator"
}

$testRoot = Join-Path ([IO.Path]::GetTempPath()) ("hoc-h4-01-validator-" + [guid]::NewGuid().ToString('N'))
$before = @{}

function Write-Utf8File([string]$Path, [string]$Content) {
    $directory = Split-Path -Parent $Path
    [IO.Directory]::CreateDirectory($directory) | Out-Null
    [IO.File]::WriteAllText($Path, $Content, [Text.UTF8Encoding]::new($false))
}

function New-ValidConsumer([string]$Path) {
    Write-Utf8File (Join-Path $Path 'global.json') '{ "sdk": { "version": "10.0.403" } }'
    Write-Utf8File (Join-Path $Path 'ContosoAdmin.slnx') '<Solution><Folder Name="/src/"><Project Path="src/ContosoAdmin.Web/ContosoAdmin.Web.csproj" /></Folder></Solution>'
    Write-Utf8File (Join-Path $Path 'src/ContosoAdmin.Web/ContosoAdmin.Web.csproj') @'
<Project Sdk="Microsoft.NET.Sdk.Web"><PropertyGroup><RootNamespace>ContosoAdmin.Web</RootNamespace></PropertyGroup><ItemGroup><PackageReference Include="Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite" Version="5.7.0" /><PackageReference Include="Volo.Abp.Account.Web.OpenIddict" Version="10.7.0" /></ItemGroup></Project>
'@
    Write-Utf8File (Join-Path $Path 'src/ContosoAdmin.Web/appsettings.json') @'
{ "App": { "Name": "ContosoAdmin" }, "OpenIddict": { "Applications": { "ContosoAdmin_Web": { "ClientId": "ContosoAdmin_Web", "RootUrl": "https://localhost:44303" }, "ContosoAdmin_Swagger": { "ClientId": "ContosoAdmin_Swagger", "RootUrl": "https://localhost:44303" } } } }
'@
    Write-Utf8File (Join-Path $Path 'src/ContosoAdmin.Web/ConsumerIdentity.cs') @'
namespace ContosoAdmin.Web;
public static class ConsumerIdentity { public const string Audience = "ContosoAdmin"; public const string WebClient = "ContosoAdmin_Web"; }
'@
}

function Invoke-Case([string]$Name, [string]$Root, [bool]$ShouldPass) {
    $fixtureExists = Test-Path -LiteralPath $Root -PathType Container
    $beforeState = if ($fixtureExists) { @(Get-ChildItem -LiteralPath $Root -Recurse -File | ForEach-Object { "$(($_.FullName).Substring($Root.Length)):$($_.Length):$($_.LastWriteTimeUtc.Ticks)" }) -join "`n" } else { '' }
    $output = & $validator -ConsumerRoot $Root -ApplicationId 'ContosoAdmin' 2>&1
    $exitCode = $LASTEXITCODE
    $afterState = if ($fixtureExists) { @(Get-ChildItem -LiteralPath $Root -Recurse -File | ForEach-Object { "$(($_.FullName).Substring($Root.Length)):$($_.Length):$($_.LastWriteTimeUtc.Ticks)" }) -join "`n" } else { '' }
    if ($beforeState -ne $afterState) { throw "$Name modified an inspected consumer fixture." }
    if (($exitCode -eq 0) -ne $ShouldPass) { throw "$Name returned exit code $exitCode. Output: $($output -join ' | ')" }
    if ($ShouldPass -and -not ($output -match 'RESULT: PASS')) { throw "$Name did not report PASS." }
    if (-not $ShouldPass -and -not ($output -match 'RESULT: FAIL')) { throw "$Name did not report FAIL." }
    Write-Output "PASS: $Name"
}

try {
    [IO.Directory]::CreateDirectory($testRoot) | Out-Null

    $valid = Join-Path $testRoot 'valid'; New-ValidConsumer $valid; Invoke-Case 'valid consumer structure' $valid $true
    $comment = Join-Path $testRoot 'comment-only-demo-records'; New-ValidConsumer $comment; Write-Utf8File (Join-Path $comment 'src/ContosoAdmin.Web/Comment.cs') @'
namespace ContosoAdmin.Web;
public static class CommentOnly
{
    private const string EscapedQuote = "historical \"quoted\" context";
    private const char EscapedCharacter = '\'';
    // DemoRecords is retained here only as historical review context.
    /* Hoc.DemoRecords must not be treated as an executable reference in comments. */
}
'@; Invoke-Case 'comment-only DemoRecords mention' $comment $true
    $escapedQuoteStale = Join-Path $testRoot 'escaped-quote-stale-reference'; New-ValidConsumer $escapedQuoteStale; Write-Utf8File (Join-Path $escapedQuoteStale 'src/ContosoAdmin.Web/Stale.cs') @'
namespace ContosoAdmin.Web;
public static class StaleReference
{
    private const string EscapedQuote = "historical \"quoted\" context";
    private const char EscapedCharacter = '\'';
    // legitimate historical comment follows an escaped quote
    // DemoRecords is retained only as provenance.
}
using Hoc.DemoRecords;
'@; Invoke-Case 'escaped quote preserves stale executable detection' $escapedQuoteStale $false
    $staleDemo = Join-Path $testRoot 'stale-demo'; New-ValidConsumer $staleDemo; Write-Utf8File (Join-Path $staleDemo 'src/ContosoAdmin.Web/Stale.cs') 'using Hoc.DemoRecords;'; Invoke-Case 'stale DemoRecords runtime reference' $staleDemo $false
    $missingSolution = Join-Path $testRoot 'missing-solution-reference'; New-ValidConsumer $missingSolution; Write-Utf8File (Join-Path $missingSolution 'src/ContosoAdmin.Web/Unlisted.csproj') '<Project Sdk="Microsoft.NET.Sdk" />'; Invoke-Case 'project missing from solution' $missingSolution $false
    $missingProject = Join-Path $testRoot 'missing-project-reference'; New-ValidConsumer $missingProject; Write-Utf8File (Join-Path $missingProject 'src/ContosoAdmin.Web/ContosoAdmin.Web.csproj') '<Project><PropertyGroup><RootNamespace>ContosoAdmin.Web</RootNamespace></PropertyGroup><ItemGroup><ProjectReference Include="../Missing/DoesNotExist.csproj" /><PackageReference Include="Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite" Version="5.7.0" /><PackageReference Include="Volo.Abp.Account.Web.OpenIddict" Version="10.7.0" /></ItemGroup></Project>'; Invoke-Case 'missing project reference' $missingProject $false
    $staleProject = Join-Path $testRoot 'stale-hoc-project-dependency'; New-ValidConsumer $staleProject; Write-Utf8File (Join-Path $staleProject 'src/ContosoAdmin.Web/ContosoAdmin.Web.csproj') '<Project><PropertyGroup><RootNamespace>ContosoAdmin.Web</RootNamespace></PropertyGroup><ItemGroup><ProjectReference Include="../Hoc.Legacy/Hoc.Legacy.csproj" /><PackageReference Include="Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite" Version="5.7.0" /><PackageReference Include="Volo.Abp.Account.Web.OpenIddict" Version="10.7.0" /></ItemGroup></Project>'; Invoke-Case 'stale Hoc project dependency' $staleProject $false
    $mismatch = Join-Path $testRoot 'mismatched-identity'; New-ValidConsumer $mismatch; Write-Utf8File (Join-Path $mismatch 'src/ContosoAdmin.Web/ContosoAdmin.Web.csproj') '<Project><PropertyGroup><RootNamespace>OtherAdmin.Web</RootNamespace></PropertyGroup><ItemGroup><PackageReference Include="Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite" Version="5.7.0" /><PackageReference Include="Volo.Abp.Account.Web.OpenIddict" Version="10.7.0" /></ItemGroup></Project>'; Invoke-Case 'mismatched application identity' $mismatch $false
    $migration = Join-Path $testRoot 'inherited-migration'; New-ValidConsumer $migration; Write-Utf8File (Join-Path $migration 'src/ContosoAdmin.EntityFrameworkCore/Migrations/HocDbContextModelSnapshot.cs') 'namespace ContosoAdmin; public class HocDbContextModelSnapshot { }'; Invoke-Case 'inherited Hoc migration artifact' $migration $false
    $openid = Join-Path $testRoot 'openid-identity'; New-ValidConsumer $openid; Write-Utf8File (Join-Path $openid 'src/ContosoAdmin.Web/appsettings.json') '{ "OpenIddict": { "Applications": { "Hoc_Web": { "ClientId": "Hoc_Web" } } } }'; Invoke-Case 'incorrect OpenIddict identity' $openid $false
    $missingSwagger = Join-Path $testRoot 'missing-swagger-identity'; New-ValidConsumer $missingSwagger; Write-Utf8File (Join-Path $missingSwagger 'src/ContosoAdmin.Web/appsettings.json') '{ "OpenIddict": { "Applications": { "ContosoAdmin_Web": { "ClientId": "ContosoAdmin_Web" } } } }'; Invoke-Case 'missing OpenIddict Swagger identity' $missingSwagger $false
    $sdk = Join-Path $testRoot 'sdk-mismatch'; New-ValidConsumer $sdk; Write-Utf8File (Join-Path $sdk 'global.json') '{ "sdk": { "version": "9.0.100" } }'; Invoke-Case 'SDK baseline mismatch' $sdk $false
    $package = Join-Path $testRoot 'package-mismatch'; New-ValidConsumer $package; Write-Utf8File (Join-Path $package 'src/ContosoAdmin.Web/ContosoAdmin.Web.csproj') '<Project><PropertyGroup><RootNamespace>ContosoAdmin.Web</RootNamespace></PropertyGroup><ItemGroup><PackageReference Include="Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite" Version="5.6.0" /><PackageReference Include="Volo.Abp.Account.Web.OpenIddict" Version="10.6.0" /></ItemGroup></Project>'; Invoke-Case 'package baseline mismatch' $package $false
    $secret = Join-Path $testRoot 'secret-artifact'; New-ValidConsumer $secret; Write-Utf8File (Join-Path $secret 'src/ContosoAdmin.Web/openiddict.pfx') 'synthetic-only'; Invoke-Case 'secret or certificate artifact' $secret $false
    $malformed = Join-Path $testRoot 'malformed'; New-ValidConsumer $malformed; Remove-Item -LiteralPath (Join-Path $malformed 'global.json') -Force; Invoke-Case 'missing required structural file' $malformed $false
    Invoke-Case 'invalid consumer root' (Join-Path $testRoot 'missing-root') $false
    Write-Output 'RESULT: PASS (validator self-tests completed with synthetic fixtures only).'
}
finally {
    if (Test-Path -LiteralPath $testRoot) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
}

exit 0
