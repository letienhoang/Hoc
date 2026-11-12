[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ConsumerRoot,
    [Parameter(Mandatory)] [string] $ApplicationId,
    [Parameter(Mandatory)] [string] $ModuleName,
    [Parameter(Mandatory)] [string] $EntityName,
    [Parameter(Mandatory)] [string] $RouteSegment,
    [switch] $Apply
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Fail([string] $Message) { throw "H4-02 generator: $Message" }

function Test-ChildPath([string] $Parent, [string] $Child) {
    $parentFull = [IO.Path]::GetFullPath($Parent).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $childFull = [IO.Path]::GetFullPath($Child)
    return $childFull.StartsWith($parentFull + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}

function Assert-NotReparsePoint([string] $Path, [string] $Description) {
    if ((Get-Item -LiteralPath $Path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
        Fail "$Description must not be a symbolic link or reparse point: $Path"
    }
}

function Get-ModuleProjectNames {
    return @('Domain', 'Application.Contracts', 'Application', 'EntityFrameworkCore', 'HttpApi', 'Web') |
        ForEach-Object { "$ApplicationId.$ModuleName.$_" }
}

if ($ApplicationId -notmatch '^[A-Z][A-Za-z0-9]*$') { Fail 'ApplicationId must be a PascalCase identifier.' }
if ($ModuleName -notmatch '^[A-Z][A-Za-z0-9]*$') { Fail 'ModuleName must be a PascalCase identifier.' }
if ($EntityName -notmatch '^[A-Z][A-Za-z0-9]*$') { Fail 'EntityName must be a PascalCase identifier.' }
if ($RouteSegment -notmatch '^[a-z][a-z0-9-]*$') { Fail 'RouteSegment must be a lowercase route segment using letters, digits, and hyphens.' }

if (-not (Test-Path -LiteralPath $ConsumerRoot -PathType Container)) { Fail "ConsumerRoot does not exist or is not a directory: $ConsumerRoot" }
$consumerRootFull = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $ConsumerRoot).Path)
Assert-NotReparsePoint $consumerRootFull 'ConsumerRoot'

$solutionCandidates = @(
    (Join-Path $consumerRootFull "$ApplicationId.slnx"),
    (Join-Path $consumerRootFull "$ApplicationId.sln")
)
if (-not ($solutionCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf })) {
    Fail "Expected consumer solution '$ApplicationId.slnx' or '$ApplicationId.sln' was not found at ConsumerRoot."
}
if (-not (Test-Path -LiteralPath (Join-Path $consumerRootFull 'common.props') -PathType Leaf)) {
    Fail 'ConsumerRoot must contain common.props so generated projects retain the established shared build conventions.'
}

$toolingRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$templateRoot = Join-Path $toolingRoot 'samples'
$sourceProjectNames = @('Hoc.DemoRecords.Domain', 'Hoc.DemoRecords.Application.Contracts', 'Hoc.DemoRecords.Application', 'Hoc.DemoRecords.EntityFrameworkCore', 'Hoc.DemoRecords.HttpApi', 'Hoc.DemoRecords.Web')
foreach ($sourceProjectName in $sourceProjectNames) {
    $sourceProject = Join-Path $templateRoot $sourceProjectName
    if (-not (Test-Path -LiteralPath $sourceProject -PathType Container)) { Fail "Template source is missing: $sourceProject" }
    Assert-NotReparsePoint $sourceProject "Template source '$sourceProjectName'"
}
if (-not (Test-Path -LiteralPath (Join-Path $toolingRoot 'global.json') -PathType Leaf)) { Fail 'Tooling global.json is missing.' }
$globalJson = Get-Content -LiteralPath (Join-Path $toolingRoot 'global.json') -Raw | ConvertFrom-Json
if ($globalJson.sdk.version -ne '10.0.403') { Fail "Unsupported tooling SDK baseline '$($globalJson.sdk.version)'; expected 10.0.403." }
$templateCsproj = Get-ChildItem -LiteralPath $templateRoot -Recurse -Filter '*.csproj' | Select-Object -First 1
if ((Get-Content -LiteralPath $templateCsproj.FullName -Raw) -notmatch 'net10.0') { Fail 'Template target framework baseline is not net10.0.' }

$modulesRoot = Join-Path $consumerRootFull 'modules'
if (Test-Path -LiteralPath $modulesRoot) { Assert-NotReparsePoint $modulesRoot 'Consumer modules directory' }
if (-not (Test-ChildPath $consumerRootFull $modulesRoot)) { Fail 'Computed modules path escapes ConsumerRoot.' }
$outputs = @(Get-ModuleProjectNames | ForEach-Object { Join-Path $modulesRoot $_ })
foreach ($output in $outputs) {
    if (-not (Test-ChildPath $consumerRootFull $output)) { Fail "Computed output path escapes ConsumerRoot: $output" }
    if (Test-Path -LiteralPath $output) { Fail "Output already exists; refusing to overwrite: $output" }
}

Write-Host "H4-02 module plan ($([bool]$Apply ? 'apply' : 'dry-run'))"
Write-Host "  tooling template: $templateRoot"
Write-Host "  consumer root:    $consumerRootFull"
Write-Host "  identity:         $ApplicationId.$ModuleName / entity $EntityName / route $RouteSegment"
$outputs | ForEach-Object { Write-Host "  create:           $_" }

if (-not $Apply) {
    Write-Host 'Dry-run complete; no consumer files were changed.'
    exit 0
}

$created = @()
try {
    if (-not (Test-Path -LiteralPath $modulesRoot)) { New-Item -ItemType Directory -Path $modulesRoot | Out-Null }
    Assert-NotReparsePoint $modulesRoot 'Consumer modules directory'
    for ($index = 0; $index -lt $sourceProjectNames.Count; $index++) {
        $source = Join-Path $templateRoot $sourceProjectNames[$index]
        $destination = $outputs[$index]
        Copy-Item -LiteralPath $source -Destination $destination -Recurse -Force
        $created += $destination
    }

    $pluralEntity = "$EntityName`$s"
    $moduleNamespace = "$ApplicationId.$ModuleName"
    $createdFiles = @(Get-ChildItem -LiteralPath $created -Recurse -File)
    foreach ($file in $createdFiles) {
        $content = [IO.File]::ReadAllText($file.FullName)
        $content = $content.Replace('Hoc.DemoRecords', $moduleNamespace)
        $content = $content.Replace('DemoRecords', $ModuleName)
        $content = $content.Replace('DemoRecord', $EntityName)
        $content = $content.Replace('demo records', $pluralEntity)
        $content = $content.Replace('Demo records', $pluralEntity)
        $content = $content.Replace('demo record', $EntityName)
        $content = $content.Replace('Demo record', $EntityName)
        $content = $content.Replace('demo-records', $RouteSegment)
        $content = $content.Replace('demorecords', $RouteSegment)
        $content = $content.Replace('/DemoRecords', "/$ModuleName")
        [IO.File]::WriteAllText($file.FullName, $content, [Text.UTF8Encoding]::new($false))
        $newName = $file.Name.Replace('Hoc.DemoRecords', $moduleNamespace).Replace('DemoRecords', $ModuleName).Replace('DemoRecord', $EntityName)
        if ($newName -ne $file.Name) { Rename-Item -LiteralPath $file.FullName -NewName $newName }
    }
    Get-ChildItem -LiteralPath $modulesRoot -Recurse -Directory | Sort-Object FullName -Descending | ForEach-Object {
        $newName = $_.Name.Replace('Hoc.DemoRecords', $moduleNamespace).Replace('DemoRecords', $ModuleName).Replace('DemoRecord', $EntityName)
        if ($newName -ne $_.Name) { Rename-Item -LiteralPath $_.FullName -NewName $newName }
    }
    Write-Host 'Module generation completed.'
}
catch {
    foreach ($path in $created) {
        if ((Test-Path -LiteralPath $path) -and (Test-ChildPath $modulesRoot $path)) { Remove-Item -LiteralPath $path -Recurse -Force }
    }
    throw
}
