[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ConsumerRoot,
    [Parameter(Mandatory)] [string] $ApplicationId,
    [Parameter(Mandatory)] [string] $ModuleName,
    [Parameter(Mandatory)] [string] $EntityName,
    # Test-only fault injection. Normal callers must omit this parameter.
    [ValidateRange(1, 6)] [int] $TestFailAfterWrites = 0,
    [switch] $Apply
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Fail([string] $Message) { throw "H4-04 integrator: $Message" }
function Assert-NotReparsePoint([string] $Path, [string] $Description) {
    if ((Get-Item -LiteralPath $Path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { Fail "$Description must not be a symbolic link or reparse point: $Path" }
}
function Test-ChildPath([string] $Parent, [string] $Child) {
    $p = [IO.Path]::GetFullPath($Parent).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $c = [IO.Path]::GetFullPath($Child)
    return $c.StartsWith($p + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}
function Assert-File([string] $Path, [string] $Description) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { Fail "$Description is missing: $Path" }
    Assert-NotReparsePoint $Path $Description
}
function Add-UniqueXmlReference([xml] $Xml, [string] $Include) {
    if ($Xml.SelectSingleNode("//*[local-name()='ProjectReference' and @Include='$Include']")) { return $false }
    $group = $Xml.SelectSingleNode("//*[local-name()='ItemGroup'][ProjectReference]")
    if ($null -eq $group) { $group = $Xml.CreateElement('ItemGroup'); [void]$Xml.DocumentElement.AppendChild($group) }
    $node = $Xml.CreateElement('ProjectReference'); $node.SetAttribute('Include', $Include); [void]$group.AppendChild($node); return $true
}
function Add-UniqueDependsOn([string] $Content, [string] $Type) {
    if ($Content.Contains("typeof($Type)")) { return $Content }
    $match = [regex]::Match($Content, '(?s)\[DependsOn\((.*)\)\]')
    if (-not $match.Success) { Fail 'Expected one ABP DependsOn declaration was not found.' }
    $body = $match.Groups[1].Value
    if ($body -notmatch 'typeof\(') { Fail 'DependsOn declaration has an unsupported shape.' }
    $trimmed = $body.TrimEnd()
    if (-not $trimmed.EndsWith(',')) { $trimmed += ',' }
    $replacement = "`n    $Type`n" + $trimmed.Substring($trimmed.Length - 1)
    return $Content.Substring(0, $match.Groups[1].Index) + $trimmed.Substring(0, $trimmed.Length - 1) + ",`n    typeof($Type)" + $Content.Substring($match.Groups[1].Index + $match.Groups[1].Length)
}
function Add-Using([string] $Content, [string] $Namespace) {
    if ($Content.Contains("using $Namespace;")) { return $Content }
    $matches = @([regex]::Matches($Content, '(?m)^using .+?;\r?$'))
    if ($matches.Count -eq 0) { Fail "Expected using directives while adding '$Namespace'." }
    $last = $matches[$matches.Count - 1]
    return $Content.Insert($last.Index + $last.Length, "`nusing $Namespace;")
}
function Add-DbSet([string] $Content, [string] $Entity, [string] $Plural) {
    if ($Content.Contains("DbSet<$Entity> $Plural")) { return $Content }
    $marker = '#endregion'
    $index = $Content.IndexOf($marker, [StringComparison]::Ordinal)
    if ($index -lt 0) { Fail 'Expected DbContext module-entity region was not found.' }
    return $Content.Insert($index, "    public DbSet<$Entity> $Plural { get; set; }`n")
}
function Add-ModelCall([string] $Content, [string] $Method) {
    if ($Content.Contains("builder.$Method();")) { return $Content }
    $needle = 'builder.ConfigureTenantManagement();'
    $index = $Content.IndexOf($needle, [StringComparison]::Ordinal)
    if ($index -lt 0) { Fail 'Expected supported DbContext model configuration pattern was not found.' }
    return $Content.Insert($index + $needle.Length, "`n        builder.$Method();")
}
function Get-UpdatedXml([string] $Path, [string[]] $References) {
    $xml = [xml][IO.File]::ReadAllText($Path)
    foreach ($reference in $References) { [void](Add-UniqueXmlReference $xml $reference) }
    $settings = [Xml.XmlWriterSettings]::new(); $settings.Indent = $true; $settings.OmitXmlDeclaration = $true; $settings.NewLineChars = "`n"; $settings.Encoding = [Text.UTF8Encoding]::new($false)
    $stringWriter = [IO.StringWriter]::new()
    $writer = [Xml.XmlWriter]::Create($stringWriter, $settings)
    try { $xml.Save($writer); $writer.Flush(); return $stringWriter.ToString().TrimEnd() + "`n" } finally { $writer.Dispose(); $stringWriter.Dispose() }
}

if ($ApplicationId -notmatch '^[A-Z][A-Za-z0-9]*$' -or $ModuleName -notmatch '^[A-Z][A-Za-z0-9]*$' -or $EntityName -notmatch '^[A-Z][A-Za-z0-9]*$') { Fail 'ApplicationId, ModuleName, and EntityName must be PascalCase identifiers.' }
if (-not (Test-Path -LiteralPath $ConsumerRoot -PathType Container)) { Fail "ConsumerRoot does not exist or is not a directory: $ConsumerRoot" }
$root = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $ConsumerRoot).Path); Assert-NotReparsePoint $root 'ConsumerRoot'
$solution = Join-Path $root "$ApplicationId.slnx"; Assert-File $solution 'Consumer solution'
$moduleBase = "$ApplicationId.$ModuleName"; $projects = @('Domain','Application.Contracts','Application','EntityFrameworkCore','HttpApi','Web')
foreach ($project in $projects) { Assert-File (Join-Path $root "modules/$moduleBase.$project/$moduleBase.$project.csproj") "Generated $project project" }
$webModule = "$ModuleName`WebModule"; $apiModule = "$ModuleName`HttpApiModule"; $efModule = "$ModuleName`EntityFrameworkCoreModule"
$moduleFiles = @(
    @{ Path = "modules/$moduleBase.Web/$ModuleName`WebModule.cs"; Type = $webModule },
    @{ Path = "modules/$moduleBase.HttpApi/$ModuleName`HttpApiModule.cs"; Type = $apiModule },
    @{ Path = "modules/$moduleBase.EntityFrameworkCore/$ModuleName`EntityFrameworkCoreModule.cs"; Type = $efModule },
    @{ Path = "modules/$moduleBase.EntityFrameworkCore/EntityFrameworkCore/$ModuleName`DbContextModelCreatingExtensions.cs"; Type = "Configure$ModuleName" }
)
foreach ($item in $moduleFiles) { $file = Join-Path $root $item.Path; Assert-File $file "Generated module entry point"; if ((Get-Content -Raw -LiteralPath $file) -notmatch [regex]::Escape($item.Type)) { Fail "Generated module entry point has unsupported content: $file" } }
$targets = [ordered]@{
    $solution = $null
    (Join-Path $root "src/$ApplicationId.Web/$ApplicationId.Web.csproj") = $null
    (Join-Path $root "src/$ApplicationId.Web/$ApplicationId`WebModule.cs") = $null
    (Join-Path $root "src/$ApplicationId.EntityFrameworkCore/$ApplicationId.EntityFrameworkCore.csproj") = $null
    (Join-Path $root "src/$ApplicationId.EntityFrameworkCore/EntityFrameworkCore/$ApplicationId`EntityFrameworkCoreModule.cs") = $null
    (Join-Path $root "src/$ApplicationId.EntityFrameworkCore/EntityFrameworkCore/$ApplicationId`DbContext.cs") = $null
}
foreach ($path in @($targets.Keys)) { Assert-File $path 'Consumer composition target'; $targets[$path] = [IO.File]::ReadAllBytes($path) }

$updates = [ordered]@{}
$slnXml = [xml][Text.Encoding]::UTF8.GetString($targets[$solution])
foreach ($project in $projects) {
    $modulePath = "modules/$moduleBase.$project/$moduleBase.$project.csproj"
    if (-not $slnXml.SelectSingleNode("//*[local-name()='Project' and @Path='$modulePath']")) {
        $folder = $slnXml.SelectSingleNode("//*[local-name()='Folder' and @Name='/modules/']")
        if ($null -eq $folder) { $folder = $slnXml.CreateElement('Folder'); $folder.SetAttribute('Name','/modules/'); [void]$slnXml.DocumentElement.AppendChild($folder) }
        $projectNode = $slnXml.CreateElement('Project'); $projectNode.SetAttribute('Path',$modulePath); [void]$folder.AppendChild($projectNode)
    }
}
$settings = [Xml.XmlWriterSettings]::new(); $settings.Indent = $true; $settings.OmitXmlDeclaration = $true; $settings.NewLineChars = "`n"; $settings.Encoding = [Text.UTF8Encoding]::new($false); $sw = [IO.StringWriter]::new(); $xw = [Xml.XmlWriter]::Create($sw,$settings); $slnXml.Save($xw); $xw.Flush(); $xw.Dispose(); $updates[$solution] = $sw.ToString().TrimEnd()+"`n"; $sw.Dispose()
$updates[(Join-Path $root "src/$ApplicationId.Web/$ApplicationId.Web.csproj")] = Get-UpdatedXml (Join-Path $root "src/$ApplicationId.Web/$ApplicationId.Web.csproj") @("../../modules/$moduleBase.Web/$moduleBase.Web.csproj", "../../modules/$moduleBase.HttpApi/$moduleBase.HttpApi.csproj")
$web = [Text.Encoding]::UTF8.GetString($targets[(Join-Path $root "src/$ApplicationId.Web/$ApplicationId`WebModule.cs")]); $web = Add-Using $web "$moduleBase.Web"; $web = Add-Using $web "$moduleBase.HttpApi"; $web = Add-UniqueDependsOn $web $webModule; $web = Add-UniqueDependsOn $web $apiModule; $updates[(Join-Path $root "src/$ApplicationId.Web/$ApplicationId`WebModule.cs")] = $web
$updates[(Join-Path $root "src/$ApplicationId.EntityFrameworkCore/$ApplicationId.EntityFrameworkCore.csproj")] = Get-UpdatedXml (Join-Path $root "src/$ApplicationId.EntityFrameworkCore/$ApplicationId.EntityFrameworkCore.csproj") @("../../modules/$moduleBase.EntityFrameworkCore/$moduleBase.EntityFrameworkCore.csproj")
$ef = [Text.Encoding]::UTF8.GetString($targets[(Join-Path $root "src/$ApplicationId.EntityFrameworkCore/EntityFrameworkCore/$ApplicationId`EntityFrameworkCoreModule.cs")]); $ef = Add-Using $ef "$moduleBase.EntityFrameworkCore"; $ef = Add-UniqueDependsOn $ef $efModule; $updates[(Join-Path $root "src/$ApplicationId.EntityFrameworkCore/EntityFrameworkCore/$ApplicationId`EntityFrameworkCoreModule.cs")] = $ef
$db = [Text.Encoding]::UTF8.GetString($targets[(Join-Path $root "src/$ApplicationId.EntityFrameworkCore/EntityFrameworkCore/$ApplicationId`DbContext.cs")]); $db = Add-Using $db "$moduleBase.$ModuleName"; $db = Add-Using $db "$moduleBase.EntityFrameworkCore"; $db = Add-DbSet $db $EntityName $ModuleName; $db = Add-ModelCall $db "Configure$ModuleName"; $updates[(Join-Path $root "src/$ApplicationId.EntityFrameworkCore/EntityFrameworkCore/$ApplicationId`DbContext.cs")] = $db

$changed = @($updates.Keys | Where-Object { [Text.Encoding]::UTF8.GetString($targets[$_]) -cne $updates[$_] })
Write-Host "H4-04 integration plan ($([bool]$Apply ? 'apply' : 'dry-run'))"; $changed | ForEach-Object { Write-Host "  update: $_" }
if ($changed.Count -eq 0) { Write-Host 'Consumer is fully integrated; no changes are required.'; exit 0 }
if (-not $Apply) { Write-Host 'Dry-run complete; no consumer files were changed.'; exit 0 }
$written = @()
try {
    foreach ($path in $changed) { [IO.File]::WriteAllText($path, $updates[$path], [Text.UTF8Encoding]::new($false)); $written += $path; if ($TestFailAfterWrites -gt 0 -and $written.Count -ge $TestFailAfterWrites) { Fail 'Test fault injected during multi-file application.' } }
    Write-Host 'Consumer composition completed.'
} catch {
    foreach ($path in $written) { [IO.File]::WriteAllBytes($path, $targets[$path]) }
    throw
}
