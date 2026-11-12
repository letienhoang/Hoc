[CmdletBinding()]
param(
    [switch] $CompileGeneratedProjects
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$generator = Join-Path $PSScriptRoot 'new-admin-module.ps1'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ("hoc-h4-02-" + [guid]::NewGuid().ToString('N'))

function Assert-True([bool] $Condition, [string] $Message) { if (-not $Condition) { throw "Assertion failed: $Message" } }
function Invoke-Generator([hashtable] $Parameters, [bool] $ExpectSuccess) {
    $failed = $false
    try { & $generator @Parameters } catch { $failed = $true }
    if ($ExpectSuccess -and $failed) { throw "Generator unexpectedly failed: $($Parameters | Out-String)" }
    if (-not $ExpectSuccess -and -not $failed) { throw "Generator unexpectedly succeeded: $($Parameters | Out-String)" }
}
function New-Fixture([string] $Name = 'ContosoAdmin') {
    $root = Join-Path $testRoot $Name
    New-Item -ItemType Directory -Path $root -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $root "$Name.slnx") -Value '<Solution />' -Encoding utf8NoBOM
    Set-Content -LiteralPath (Join-Path $root 'common.props') -Value '<Project />' -Encoding utf8NoBOM
    return $root
}

try {
    New-Item -ItemType Directory -Path $testRoot | Out-Null
    $root = New-Fixture
    $generatorParameters = @{ ConsumerRoot = $root; ApplicationId = 'ContosoAdmin'; ModuleName = 'Products'; EntityName = 'Product'; RouteSegment = 'products' }
    Invoke-Generator -Parameters $generatorParameters -ExpectSuccess $true
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $root 'modules'))) 'Dry-run created modules.'

    Invoke-Generator -Parameters ($generatorParameters + @{ Apply = $true }) -ExpectSuccess $true
    $projects = @('Domain', 'Application.Contracts', 'Application', 'EntityFrameworkCore', 'HttpApi', 'Web')
    foreach ($project in $projects) { Assert-True (Test-Path -LiteralPath (Join-Path $root "modules\ContosoAdmin.Products.$project")) "Missing generated $project project." }
    $allText = (Get-ChildItem -LiteralPath (Join-Path $root 'modules') -Recurse -File | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"
    Assert-True ($allText -match 'ContosoAdmin.Products') 'Generated namespace/assembly identity is missing.'
    Assert-True ($allText -match 'Product') 'Generated entity identity is missing.'
    Assert-True ($allText -match 'RootPath = "products"') 'Generated route identity is missing.'
    Assert-True ($allText -match 'Products.Read') 'Generated permission identity is missing.'
    Assert-True ($allText -notmatch 'Hoc\.DemoRecords|DemoRecords|demo records|Demo records') 'Stale DemoRecords executable reference or UI label remained.'
    Assert-True ((Get-Content -LiteralPath (Join-Path $root 'modules\ContosoAdmin.Products.Application\ContosoAdmin.Products.Application.csproj') -Raw) -match 'ContosoAdmin.Products.Application.Contracts') 'Application project reference was not transformed.'
    if ($CompileGeneratedProjects) {
        $toolingRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
        $dotnet = Join-Path $toolingRoot '.tools\dotnet\dotnet.exe'
        Assert-True (Test-Path -LiteralPath $dotnet -PathType Leaf) 'Pinned local dotnet SDK was not found.'
        foreach ($project in @('ContosoAdmin.Products.Web', 'ContosoAdmin.Products.HttpApi', 'ContosoAdmin.Products.EntityFrameworkCore')) {
            & $dotnet build (Join-Path $root "modules\$project\$project.csproj") --configuration Release -m:1 /nr:false
            if ($LASTEXITCODE -ne 0) { throw "Generated project did not Release-build: $project" }
        }
    }
    Invoke-Generator -Parameters ($generatorParameters + @{ Apply = $true }) -ExpectSuccess $false

    foreach ($bad in @(@('-ApplicationId', 'contoso'), @('-ModuleName', '../Products'), @('-EntityName', 'product'), @('-RouteSegment', '../products'))) {
        $candidate = @{} + $generatorParameters
        $candidate[$bad[0].TrimStart('-')] = $bad[1]
        Invoke-Generator -Parameters $candidate -ExpectSuccess $false
    }
    $missing = @{} + $generatorParameters; $missing.ConsumerRoot = Join-Path $testRoot 'missing'
    Invoke-Generator -Parameters $missing -ExpectSuccess $false
    $incomplete = Join-Path $testRoot 'incomplete'; New-Item -ItemType Directory -Path $incomplete | Out-Null
    $missingFiles = @{} + $generatorParameters; $missingFiles.ConsumerRoot = $incomplete
    Invoke-Generator -Parameters $missingFiles -ExpectSuccess $false
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $incomplete 'modules'))) 'Failed preflight left generated output.'
    Write-Host 'H4-02 generator self-tests passed.'
}
finally {
    if (Test-Path -LiteralPath $testRoot) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
}
