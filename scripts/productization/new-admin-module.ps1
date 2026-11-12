[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ConsumerRoot,
    [Parameter(Mandatory)] [string] $ApplicationId,
    [Parameter(Mandatory)] [string] $ModuleName,
    [Parameter(Mandatory)] [string] $EntityName,
    [Parameter(Mandatory)] [string] $RouteSegment,
    # Test-only fault injection used by the adjacent self-test; normal callers omit it.
    [ValidateRange(1, 10000)] [int] $TestFailAfterFileCopies = 0,
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

function Get-TemplateFiles([string] $Source) {
    # The starter is source-only.  Do not let a local build, secret overlay, or
    # arbitrary binary placed below samples become generated consumer content.
    $allowedExtensions = @('.cs', '.csproj', '.cshtml', '.json', '.md', '.txt')
    $retainedNames = @('LICENSE', 'LICENSE.txt', 'NOTICE', 'NOTICE.txt')
    return @(Get-ChildItem -LiteralPath $Source -Recurse -File -Force | Where-Object {
        $relative = $_.FullName.Substring($Source.Length).TrimStart([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
        $segments = $relative -split '[\\/]'
        $segments -notcontains 'bin' -and
        $segments -notcontains 'obj' -and
        $segments -notcontains '.tools' -and
        $segments -notcontains '.git' -and
        $_.Name -notmatch '(?i)^appsettings(\..+)?\.secrets\.json$' -and
        ($allowedExtensions -contains $_.Extension.ToLowerInvariant() -or $retainedNames -contains $_.Name)
    })
}

function Set-VietnameseLocalizationLabels([string] $Path, [string] $EntityLabel, [string] $GroupLabel) {
    $labels = if ($EntityLabel -eq 'Product' -and $GroupLabel -eq 'Products') {
        @{ 'Bản ghi minh họa' = 'Sản phẩm'; 'Xem bản ghi minh họa' = 'Xem sản phẩm'; 'Tạo bản ghi minh họa' = 'Tạo sản phẩm'; 'Cập nhật bản ghi minh họa' = 'Cập nhật sản phẩm'; 'Xóa bản ghi minh họa' = 'Xóa sản phẩm'; 'Chi tiết bản ghi minh họa' = 'Chi tiết sản phẩm'; 'Chỉnh sửa bản ghi minh họa' = 'Chỉnh sửa sản phẩm'; 'Không có bản ghi minh họa nào.' = 'Không có sản phẩm nào.'; 'Bạn có chắc chắn muốn xóa bản ghi minh họa này không?' = 'Bạn có chắc chắn muốn xóa sản phẩm này không?' }
    }
    else {
        @{ 'Bản ghi minh họa' = $GroupLabel; 'Xem bản ghi minh họa' = "Xem $EntityLabel"; 'Tạo bản ghi minh họa' = "Tạo $EntityLabel"; 'Cập nhật bản ghi minh họa' = "Cập nhật $EntityLabel"; 'Xóa bản ghi minh họa' = "Xóa $EntityLabel"; 'Chi tiết bản ghi minh họa' = "Chi tiết $EntityLabel"; 'Chỉnh sửa bản ghi minh họa' = "Chỉnh sửa $EntityLabel"; 'Không có bản ghi minh họa nào.' = "Không có $GroupLabel nào."; 'Bạn có chắc chắn muốn xóa bản ghi minh họa này không?' = "Bạn có chắc chắn muốn xóa $EntityLabel này không?" }
    }
    $content = [IO.File]::ReadAllText($Path)
    foreach ($sourceLabel in $labels.Keys) { $content = $content.Replace($sourceLabel, $labels[$sourceLabel]) }
    [IO.File]::WriteAllText($Path, $content, [Text.UTF8Encoding]::new($false))
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

$moduleNamespace = "$ApplicationId.$ModuleName"
$pluralEntity = $ModuleName
$stagingRoot = Join-Path $modulesRoot ('.h4-02-staging-' + [guid]::NewGuid().ToString('N'))
$modulesRootCreated = $false
$published = @()
try {
    if (-not (Test-Path -LiteralPath $modulesRoot)) {
        New-Item -ItemType Directory -Path $modulesRoot | Out-Null
        $modulesRootCreated = $true
    }
    Assert-NotReparsePoint $modulesRoot 'Consumer modules directory'
    New-Item -ItemType Directory -Path $stagingRoot | Out-Null
    $copiedFileCount = 0
    for ($index = 0; $index -lt $sourceProjectNames.Count; $index++) {
        $source = Join-Path $templateRoot $sourceProjectNames[$index]
        $destination = Join-Path $stagingRoot (Split-Path -Leaf $outputs[$index])
        foreach ($file in Get-TemplateFiles $source) {
            $relative = $file.FullName.Substring($source.Length).TrimStart([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
            $destinationFile = Join-Path $destination $relative
            $destinationDirectory = Split-Path -Parent $destinationFile
            if (-not (Test-Path -LiteralPath $destinationDirectory)) { New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null }
            Copy-Item -LiteralPath $file.FullName -Destination $destinationFile -Force
            $copiedFileCount++
            if ($TestFailAfterFileCopies -gt 0 -and $copiedFileCount -ge $TestFailAfterFileCopies) { Fail 'Test fault injected after copying a template file.' }
        }
    }

    $createdFiles = @(Get-ChildItem -LiteralPath $stagingRoot -Recurse -File)
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
    Get-ChildItem -LiteralPath $stagingRoot -Recurse -File -Filter 'vi.json' | ForEach-Object {
        Set-VietnameseLocalizationLabels -Path $_.FullName -EntityLabel $EntityName -GroupLabel $pluralEntity
    }
    Get-ChildItem -LiteralPath $stagingRoot -Recurse -Directory | Sort-Object FullName -Descending | ForEach-Object {
        $newName = $_.Name.Replace('Hoc.DemoRecords', $moduleNamespace).Replace('DemoRecords', $ModuleName).Replace('DemoRecord', $EntityName)
        if ($newName -ne $_.Name) { Rename-Item -LiteralPath $_.FullName -NewName $newName }
    }
    for ($index = 0; $index -lt $outputs.Count; $index++) {
        $stagedOutput = Join-Path $stagingRoot (Split-Path -Leaf $outputs[$index])
        if (-not (Test-Path -LiteralPath $stagedOutput -PathType Container)) { Fail "Staged project is missing: $stagedOutput" }
        if (Test-Path -LiteralPath $outputs[$index]) { Fail "Output appeared during generation; refusing to overwrite: $($outputs[$index])" }
        Move-Item -LiteralPath $stagedOutput -Destination $outputs[$index]
        $published += $outputs[$index]
    }
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    Write-Host 'Module generation completed.'
}
catch {
    foreach ($path in $published) {
        if ((Test-Path -LiteralPath $path) -and (Test-ChildPath $modulesRoot $path)) { Remove-Item -LiteralPath $path -Recurse -Force }
    }
    if (Test-Path -LiteralPath $stagingRoot) { Remove-Item -LiteralPath $stagingRoot -Recurse -Force }
    if ($modulesRootCreated -and (Test-Path -LiteralPath $modulesRoot) -and -not (Get-ChildItem -LiteralPath $modulesRoot -Force | Select-Object -First 1)) { Remove-Item -LiteralPath $modulesRoot -Force }
    throw
}
