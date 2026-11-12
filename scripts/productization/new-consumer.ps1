[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $SourceRoot,
    [Parameter(Mandatory)] [string] $SourceSha,
    [Parameter(Mandatory)] [string] $OutputRoot,
    [Parameter(Mandatory)] [string] $ApplicationId,
    [Parameter(Mandatory)] [string] $RootUrl,
    # Test-only fault injection. Normal callers must omit this parameter.
    [ValidateRange(1, 100000)] [int] $TestFailAfterTransforms = 0,
    [switch] $Apply
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$supportedSha = '6a8605a63d18d35767f520d57d33193f6d15aa29'

function Fail([string] $Message) { throw "H4-03 generator: $Message" }
function Assert-NotReparsePoint([string] $Path, [string] $Description) {
    if ((Get-Item -LiteralPath $Path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { Fail "$Description must not be a symbolic link or reparse point: $Path" }
}
function Test-ChildPath([string] $Parent, [string] $Child) {
    $parentFull = [IO.Path]::GetFullPath($Parent).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $childFull = [IO.Path]::GetFullPath($Child)
    return $childFull.StartsWith($parentFull + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}
function Assert-SafeAncestorChain([string] $Path, [string] $StopAt, [string] $Description) {
    $current = [IO.DirectoryInfo]::new($Path)
    $limit = [IO.Path]::GetFullPath($StopAt).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    while ($null -ne $current) {
        Assert-NotReparsePoint $current.FullName $Description
        if ($current.FullName.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) -eq $limit) { return }
        $current = $current.Parent
    }
    Fail "$Description did not resolve beneath its expected boundary."
}
function Invoke-Git([string] $Root, [string[]] $Arguments) {
    $result = & git -C $Root @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { Fail "Git command failed: git -C '$Root' $($Arguments -join ' '): $result" }
    return ($result -join "`n").Trim()
}
function Remove-PathIfPresent([string] $Path) {
    if (Test-Path -LiteralPath $Path) { Remove-Item -LiteralPath $Path -Recurse -Force }
}
function Remove-ProjectReferenceLines([string] $Path) {
    $lines = [IO.File]::ReadAllLines($Path)
    [IO.File]::WriteAllLines($Path, @($lines | Where-Object { $_ -notmatch '(?i)Hoc\.DemoRecords|DemoRecords' }), [Text.UTF8Encoding]::new($false))
}
function Rename-TreeIdentity([string] $Root, [string] $Id) {
    $textExtensions = @('.cs', '.csproj', '.cshtml', '.json', '.props', '.targets', '.slnx', '.config', '.md', '.txt', '.xml', '.js', '.css', '.yml', '.yaml', '.lock')
    $transformCount = 0
    foreach ($file in @(Get-ChildItem -LiteralPath $Root -Recurse -File -Force)) {
        if ($textExtensions -contains $file.Extension.ToLowerInvariant() -or $file.Name -in @('LICENSE', 'NOTICE', 'NuGet.Config', '.gitignore', '.gitattributes', 'global.json')) {
            $content = [IO.File]::ReadAllText($file.FullName)
            # Học-specific names are application-owned; third-party identities start with Volo., Microsoft., and OpenIddict.
            $content = $content.Replace('Hoc_Web', "${Id}_Web").Replace('Hoc_Swagger', "${Id}_Swagger")
            $content = $content.Replace('"Hoc"', "`"$Id`"").Replace("'Hoc'", "'$Id'")
            $content = [regex]::Replace($content, '(?<![A-Za-z0-9_])Hoc(?=\.|\b)', $Id)
            $content = $content.Replace('Hoc', $Id)
            [IO.File]::WriteAllText($file.FullName, $content, [Text.UTF8Encoding]::new($false))
            $transformCount++
            if ($TestFailAfterTransforms -gt 0 -and $transformCount -ge $TestFailAfterTransforms) { Fail 'Test fault injected during transformation.' }
        }
    }
    foreach ($item in @(Get-ChildItem -LiteralPath $Root -Recurse -Force | Sort-Object FullName -Descending)) {
        $newName = $item.Name.Replace('Hoc', $Id)
        if ($newName -ne $item.Name) { Rename-Item -LiteralPath $item.FullName -NewName $newName }
    }
    $oldSolution = Join-Path $Root 'Hoc.slnx'
    if (Test-Path -LiteralPath $oldSolution) { Rename-Item -LiteralPath $oldSolution -NewName "$Id.slnx" }
}

if ($SourceSha -ne $supportedSha) { Fail "Unsupported source SHA '$SourceSha'; v1 supports only $supportedSha." }
if ($ApplicationId -notmatch '^[A-Z][A-Za-z0-9]*$') { Fail 'ApplicationId must be a PascalCase identifier.' }
try { $uri = [Uri]$RootUrl } catch { Fail 'RootUrl must be an absolute HTTPS URL.' }
if (-not $uri.IsAbsoluteUri -or $uri.Scheme -ne 'https' -or -not $uri.IsWellFormedOriginalString() -or -not [string]::IsNullOrEmpty($uri.Query) -or -not [string]::IsNullOrEmpty($uri.Fragment) -or $uri.AbsolutePath -ne '/') { Fail 'RootUrl must be an absolute HTTPS origin without a path, query, or fragment.' }
if (-not (Test-Path -LiteralPath $SourceRoot -PathType Container)) { Fail "SourceRoot does not exist or is not a directory: $SourceRoot" }
$sourceFull = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $SourceRoot).Path)
Assert-NotReparsePoint $sourceFull 'SourceRoot'
if (-not (Test-Path -LiteralPath (Join-Path $sourceFull '.git'))) { Fail 'SourceRoot must be a local Git checkout.' }
$sourceTop = Invoke-Git $sourceFull @('rev-parse', '--show-toplevel')
if (-not [IO.Path]::GetFullPath($sourceTop).TrimEnd('\','/') -eq $sourceFull.TrimEnd('\','/')) { Fail 'SourceRoot must be the Git checkout root.' }
$origin = Invoke-Git $sourceFull @('config', '--get', 'remote.origin.url')
if ($origin -notmatch '(?i)(^|[:/])letienhoang/Hoc(?:\.git)?$') { Fail "SourceRoot origin is not the approved Hoc repository: $origin" }
[void](Invoke-Git $sourceFull @('cat-file', '-e', "$SourceSha^{commit}"))
$outputFull = [IO.Path]::GetFullPath($OutputRoot)
if (Test-Path -LiteralPath $outputFull) { Fail "OutputRoot already exists; refusing to overwrite: $outputFull" }
if (Test-ChildPath $sourceFull $outputFull) { Fail 'OutputRoot must not be inside SourceRoot.' }
$outputParent = Split-Path -Parent $outputFull
if (-not (Test-Path -LiteralPath $outputParent -PathType Container)) { Fail "OutputRoot parent does not exist: $outputParent" }
Assert-SafeAncestorChain $outputParent ([IO.Path]::GetPathRoot($outputParent)) 'OutputRoot parent'
$parentWorktree = & git -C $outputParent rev-parse --is-inside-work-tree 2>$null
if ($LASTEXITCODE -eq 0 -and $parentWorktree -eq 'true') { Fail 'OutputRoot must not be created inside an existing Git worktree.' }
foreach ($required in @('Hoc.slnx', 'global.json', 'common.props', 'NuGet.Config', 'src/Hoc.Web/Hoc.Web.csproj', 'src/Hoc.EntityFrameworkCore/EntityFrameworkCore/HocDevelopmentConfiguration.cs')) {
    $exists = & git -C $sourceFull cat-file -e "$SourceSha`:$required" 2>$null
    if ($LASTEXITCODE -ne 0) { Fail "Pinned source is missing required input '$required'." }
}
$sdk = (Invoke-Git $sourceFull @('show', "$SourceSha`:global.json") | ConvertFrom-Json).sdk.version
if ($sdk -ne '10.0.403') { Fail "Pinned source SDK baseline '$sdk' is unsupported." }

Write-Host "H4-03 consumer bootstrap plan ($([bool]$Apply ? 'apply' : 'dry-run'))"
Write-Host "  source:      $sourceFull @ $SourceSha"
Write-Host "  output:      $outputFull"
Write-Host "  identity:    $ApplicationId"
Write-Host "  root URL:    $($uri.GetLeftPart([UriPartial]::Authority))"
Write-Host '  remove:      DemoRecords projects, composition, tests, and inherited migrations'
if (-not $Apply) { Write-Host 'Dry-run complete; no consumer files were changed.'; exit 0 }

$staging = Join-Path $outputParent ('.h4-03-staging-' + [guid]::NewGuid().ToString('N'))
$published = $false
try {
    New-Item -ItemType Directory -Path $staging | Out-Null
    # git archive reads only the exact tracked tree: no upstream .git, ignored overlay, local artifacts, or untracked files.
    & git -C $sourceFull archive --format=tar $SourceSha | tar -xf - -C $staging
    if ($LASTEXITCODE -ne 0) { Fail 'Unable to extract the pinned tracked source tree into staging.' }
    foreach ($path in @(Get-ChildItem -LiteralPath (Join-Path $staging 'samples') -Directory -Filter 'Hoc.DemoRecords.*' -ErrorAction SilentlyContinue)) { Remove-PathIfPresent $path.FullName }
    foreach ($path in @(
        (Join-Path $staging 'test/Hoc.Domain.Tests/DemoRecords'),
        (Join-Path $staging 'test/Hoc.EntityFrameworkCore.Tests/EntityFrameworkCore/DemoRecords'),
        (Join-Path $staging 'test/Hoc.Web.Tests/DemoRecords'),
        (Join-Path $staging 'src/Hoc.EntityFrameworkCore/Migrations')
    )) { Remove-PathIfPresent $path }
    # Locks record the removed projects. Consumers regenerate their own locks on the first restore.
    Get-ChildItem -LiteralPath $staging -Recurse -Filter 'packages.lock.json' | Remove-Item -Force
    $samplesRoot = Join-Path $staging 'samples'
    if ((Test-Path -LiteralPath $samplesRoot) -and -not (Get-ChildItem -LiteralPath $samplesRoot -Force | Select-Object -First 1)) { Remove-Item -LiteralPath $samplesRoot -Force }
    foreach ($project in @(Get-ChildItem -LiteralPath $staging -Recurse -Filter '*.csproj')) { Remove-ProjectReferenceLines $project.FullName }
    foreach ($path in @('src/Hoc.EntityFrameworkCore/EntityFrameworkCore/HocDbContext.cs', 'src/Hoc.EntityFrameworkCore/EntityFrameworkCore/HocEntityFrameworkCoreModule.cs', 'src/Hoc.Web/HocWebModule.cs', 'test/Hoc.EntityFrameworkCore.Tests/EntityFrameworkCore/HocEntityFrameworkCoreTestModule.cs')) {
        $fullPath = Join-Path $staging $path
        $lines = [IO.File]::ReadAllLines($fullPath)
        [IO.File]::WriteAllLines($fullPath, @($lines | Where-Object { $_ -notmatch 'DemoRecords|DemoRecord' }), [Text.UTF8Encoding]::new($false))
    }
    $solution = Join-Path $staging 'Hoc.slnx'
    [IO.File]::WriteAllLines($solution, @([IO.File]::ReadAllLines($solution) | Where-Object { $_ -notmatch 'DemoRecords' }), [Text.UTF8Encoding]::new($false))
    Rename-TreeIdentity $staging $ApplicationId
    $consumerWebSettings = Join-Path $staging "src/$ApplicationId.Web/appsettings.json"
    $settings = [IO.File]::ReadAllText($consumerWebSettings).Replace('https://localhost:44303', $uri.GetLeftPart([UriPartial]::Authority))
    [IO.File]::WriteAllText($consumerWebSettings, $settings, [Text.UTF8Encoding]::new($false))
    $leftovers = @(Get-ChildItem -LiteralPath $staging -Recurse -File | Where-Object {
        $relative = $_.FullName.Substring($staging.Length).TrimStart([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
        $relative -match '(?i)(^|[\\/])Migrations([\\/]|$)' -or $relative -match '(?i)(^|[\\/])samples[\\/]ContosoAdmin\.DemoRecords'
    })
    if ($leftovers.Count -gt 0) { Fail "Staged consumer still contains removed sample or migration artifacts: $($leftovers.FullName -join ', ')" }
    if (Test-Path -LiteralPath $outputFull) { Fail 'OutputRoot appeared during generation; refusing to overwrite.' }
    Move-Item -LiteralPath $staging -Destination $outputFull
    $published = $true
    Write-Host "Consumer generation completed: $outputFull"
}
catch {
    if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
    if ($published -and (Test-Path -LiteralPath $outputFull)) { Remove-Item -LiteralPath $outputFull -Recurse -Force }
    throw
}
