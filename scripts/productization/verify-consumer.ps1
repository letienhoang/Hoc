[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ConsumerRoot,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_.-]*$')]
    [string]$ApplicationId,

    [string]$ExpectedSdkVersion = '10.0.403',
    [string]$ExpectedAbpVersion = '10.7.0',
    [string]$ExpectedLeptonXVersion = '5.7.0'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$failures = [System.Collections.Generic.List[string]]::new()

function Add-Pass([string]$Message) {
    Write-Output "PASS: $Message"
}

function Add-Fail([string]$Message) {
    $failures.Add($Message)
    Write-Output "FAIL: $Message"
}

function Get-RelativePath([string]$Path, [string]$Root) {
    return $Path.Substring($Root.Length).TrimStart([char]'\', [char]'/' )
}

function Get-InspectableFiles([string]$Root, [string[]]$Extensions) {
    return Get-ChildItem -LiteralPath $Root -Recurse -File | Where-Object {
        $relative = Get-RelativePath $_.FullName $Root
        $relative -notmatch '(^|[\\/])(\.git|bin|obj|node_modules|docs|wwwroot[\\/]libs)([\\/]|$)' -and
        $Extensions -contains $_.Extension.ToLowerInvariant()
    }
}

try {
    $resolvedRoot = (Resolve-Path -LiteralPath $ConsumerRoot -ErrorAction Stop).Path
}
catch {
    Add-Fail "Consumer root does not exist or cannot be resolved."
    Write-Output 'RESULT: FAIL (static validation only; no runtime, database, authentication, or production proof).'
    exit 1
}

if (-not (Test-Path -LiteralPath $resolvedRoot -PathType Container)) {
    Add-Fail "Consumer root is not a directory."
    Write-Output 'RESULT: FAIL (static validation only; no runtime, database, authentication, or production proof).'
    exit 1
}

$solutionFiles = @(Get-ChildItem -LiteralPath $resolvedRoot -Filter '*.slnx' -File)
$expectedSolution = "$ApplicationId.slnx"
if ($solutionFiles.Name -contains $expectedSolution) {
    Add-Pass "Found expected solution '$expectedSolution'."
}
else {
    Add-Fail "Expected solution '$expectedSolution' was not found at the consumer root."
}

$projectFiles = @(Get-InspectableFiles $resolvedRoot @('.csproj'))
$expectedWebProject = Join-Path (Join-Path 'src' "$ApplicationId.Web") "$ApplicationId.Web.csproj"
if ($projectFiles.Count -eq 0) {
    Add-Fail 'No inspectable project files were found.'
}
elseif (($projectFiles | ForEach-Object { Get-RelativePath $_.FullName $resolvedRoot }) -contains $expectedWebProject) {
    Add-Pass "Found expected Web project '$expectedWebProject'."
}
else {
    Add-Fail "Expected Web project '$expectedWebProject' was not found."
}

$solutionText = if ($solutionFiles.Name -contains $expectedSolution) { Get-Content -LiteralPath (Join-Path $resolvedRoot $expectedSolution) -Raw } else { '' }
if ($solutionText -match [regex]::Escape($ApplicationId)) {
    Add-Pass 'Solution references the expected application identity.'
}
else {
    Add-Fail 'Solution does not reference the expected application identity.'
}

$projectText = ($projectFiles | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"
if ($projectText -match "(?s)<(RootNamespace|AssemblyName)>\s*$([regex]::Escape($ApplicationId))(\.|<)") {
    Add-Pass 'Project metadata contains the expected application identity.'
}
else {
    Add-Fail 'No project RootNamespace or AssemblyName starts with the expected application identity.'
}

$sourceFiles = Get-InspectableFiles $resolvedRoot @('.cs', '.csproj', '.json', '.props', '.targets', '.slnx')
$staleReferences = @(foreach ($file in $sourceFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    if ($content -match '(?im)^\s*(using|namespace)\s+Hoc(\.|;)' -or
        $content -match '(?is)<(RootNamespace|AssemblyName)>\s*Hoc([.<])' -or
        $content -match '(?is)<ProjectReference[^>]*(Hoc\.DemoRecords|DemoRecords)' -or
        $content -match '(?i)Hoc\.DemoRecords|\bDemoRecords\b') {
        Get-RelativePath $file.FullName $resolvedRoot
    }
})
if ($staleReferences.Count -eq 0) {
    Add-Pass 'No stale Hoc or DemoRecords executable, project, or configuration references were found.'
}
else {
    Add-Fail ("Stale Hoc/DemoRecords references found in: " + ($staleReferences -join ', '))
}

$migrationFiles = @(Get-ChildItem -LiteralPath $resolvedRoot -Recurse -File | Where-Object {
    (Get-RelativePath $_.FullName $resolvedRoot) -match '(^|[\\/])Migrations([\\/]|$)' -and
    (Get-RelativePath $_.FullName $resolvedRoot) -notmatch '(^|[\\/])docs([\\/]|$)' -and
    $_.FullName -notmatch '([\\/])(bin|obj)([\\/])'
})
$inheritedMigrations = @(foreach ($file in $migrationFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    if ($file.Name -match '(?i)Hoc|DemoRecords' -or $content -match '(?i)HocDbContext|Hoc\.DemoRecords|DemoRecords') {
        Get-RelativePath $file.FullName $resolvedRoot
    }
})
if ($inheritedMigrations.Count -eq 0) {
    Add-Pass 'No inherited Hoc/DemoRecords migration artifact was found.'
}
else {
    Add-Fail ("Inherited migration artifact found in: " + ($inheritedMigrations -join ', '))
}

$identityFiles = Get-InspectableFiles $resolvedRoot @('.cs', '.json', '.csproj')
$identityText = ($identityFiles | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"
if ($identityText -match [regex]::Escape($ApplicationId)) {
    Add-Pass 'Application identity appears in inspectable source or configuration.'
}
else {
    Add-Fail 'Expected application identity is absent from inspectable source and configuration.'
}
if ($identityText -match '(?i)OpenIddict' -and $identityText -match [regex]::Escape("${ApplicationId}_Web")) {
    Add-Pass 'OpenIddict configuration/source includes the consumer Web client identity.'
}
else {
    Add-Fail "OpenIddict consumer Web identity '${ApplicationId}_Web' was not found."
}
if ($identityText -match '(?i)Hoc_Web|Hoc_Swagger|AddAudiences\(\s*[\"'']Hoc[\"'']') {
    Add-Fail 'OpenIddict source/configuration still contains a Hoc application identity.'
}
else {
    Add-Pass 'OpenIddict source/configuration contains no known Hoc application identity.'
}

$globalJson = Join-Path $resolvedRoot 'global.json'
if (Test-Path -LiteralPath $globalJson -PathType Leaf) {
    try {
        $sdkVersion = (Get-Content -LiteralPath $globalJson -Raw | ConvertFrom-Json).sdk.version
        if ($sdkVersion -eq $ExpectedSdkVersion) { Add-Pass "SDK baseline is $ExpectedSdkVersion." }
        else { Add-Fail "SDK baseline is '$sdkVersion'; expected '$ExpectedSdkVersion'." }
    }
    catch { Add-Fail 'global.json is malformed or lacks sdk.version.' }
}
else { Add-Fail 'global.json is missing.' }

$abpVersions = @([regex]::Matches($projectText, '<PackageReference\s+Include="(Volo\.Abp[^" ]*)"\s+Version="([^"]+)"', 'IgnoreCase') | Where-Object { $_.Groups[1].Value -ne 'Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite' } | ForEach-Object { $_.Groups[2].Value } | Select-Object -Unique)
if ($abpVersions.Count -eq 0) { Add-Fail 'No explicit Volo.Abp package version was found.' }
elseif ($abpVersions -contains $ExpectedAbpVersion -and $abpVersions.Count -eq 1) { Add-Pass "ABP package baseline is $ExpectedAbpVersion." }
else { Add-Fail ("ABP package versions are '" + ($abpVersions -join ', ') + "'; expected only '$ExpectedAbpVersion'.") }

$leptonVersions = @([regex]::Matches($projectText, '<PackageReference\s+Include="Volo\.Abp\.AspNetCore\.Mvc\.UI\.Theme\.LeptonXLite"\s+Version="([^"]+)"', 'IgnoreCase') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
if ($leptonVersions.Count -eq 0) { Add-Fail 'LeptonX Lite package reference was not found.' }
elseif ($leptonVersions -contains $ExpectedLeptonXVersion -and $leptonVersions.Count -eq 1) { Add-Pass "LeptonX Lite package baseline is $ExpectedLeptonXVersion." }
else { Add-Fail ("LeptonX Lite versions are '" + ($leptonVersions -join ', ') + "'; expected only '$ExpectedLeptonXVersion'.") }

$secretArtifacts = @(Get-ChildItem -LiteralPath $resolvedRoot -Recurse -File | Where-Object {
    $relative = Get-RelativePath $_.FullName $resolvedRoot
    $relative -notmatch '(^|[\\/])(\.git|bin|obj|node_modules|docs)([\\/]|$)' -and
    ($_.Name -ieq 'appsettings.secrets.json' -or $_.Extension -in @('.pfx', '.p12', '.pem', '.key'))
} | ForEach-Object { Get-RelativePath $_.FullName $resolvedRoot })
if ($secretArtifacts.Count -eq 0) { Add-Pass 'No tracked-style secret or certificate artifact was found.' }
else { Add-Fail ("Secret/certificate artifact must not be retained in consumer source: " + ($secretArtifacts -join ', ')) }

if ($failures.Count -eq 0) {
    Write-Output 'RESULT: PASS (static validation only; no runtime, database, authentication, migration, or production proof).'
    exit 0
}

Write-Output "RESULT: FAIL ($($failures.Count) check(s) failed; static validation only; no runtime, database, authentication, migration, or production proof)."
exit 1
