<#
.SYNOPSIS
    Generates per-project TOML API surface maps for AI agent consumption.
.DESCRIPTION
    Scans C# source code in a given repo/solution and generates one TOML file per project
    under .api-surface/<repo>/. Extracts public and internal classes, interfaces, enums,
    extension methods, and their XML doc summaries.
.PARAMETER RepoPath
    Path to the repository root (e.g., "FEx").
    Relative to this script's location or absolute.
.PARAMETER OutputDir
    Output directory. Defaults to ".api-surface/<RepoName>".
.EXAMPLE
    .\Generate-ApiSurface.ps1 -RepoPath FEx
    .\Generate-ApiSurface.ps1 -RepoPath FEx -Verbose
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$RepoPath,

    [string]$OutputDir
)

$ErrorActionPreference = "Stop"
$ScriptRoot = $PSScriptRoot

# Resolve repo
$resolvedRepo = if ([System.IO.Path]::IsPathRooted($RepoPath)) { $RepoPath } else { Join-Path $ScriptRoot $RepoPath }
if (-not (Test-Path $resolvedRepo)) {
    Write-Error "Repository not found: $resolvedRepo"
    return
}

$repoName = Split-Path $resolvedRepo -Leaf
if (-not $OutputDir) {
    $OutputDir = Join-Path $ScriptRoot ".api-surface" $repoName
}

# Ensure output dir exists (clean previous)
if (Test-Path $OutputDir) {
    Remove-Item "$OutputDir\*.toml" -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Excluded directory patterns
$excludePatterns = @('\\obj\\', '\\bin\\', '\\submodules\\', '\\samples\\', '\\TestResults\\')

#region Helper Functions

function Get-XmlDocSummary {
    param([string[]]$Lines, [int]$StartIndex)

    $summary = [System.Collections.ArrayList]@()
    $inSummary = $false

    for ($i = $StartIndex; $i -ge [Math]::Max(0, $StartIndex - 30); $i--) {
        $line = $Lines[$i]
        if ($line -match '^\s*///(.*)$') {
            $content = $matches[1].Trim()
            if ($content -match '</summary>') {
                $inSummary = $true
                $textBefore = $content -replace '</summary>.*', '' -replace '<summary>', ''
                if ($textBefore.Trim()) { [void]$summary.Insert(0, $textBefore.Trim()) }
            }
            elseif ($inSummary) {
                if ($content -match '<summary>(.*)') {
                    $textAfter = $matches[1].Trim()
                    if ($textAfter) { [void]$summary.Insert(0, $textAfter) }
                    break
                }
                else {
                    $clean = ($content -replace '<[^>]+>', '').Trim()
                    if ($clean) { [void]$summary.Insert(0, $clean) }
                }
            }
        }
        elseif ($inSummary) { break }
    }

    return (($summary -join ' ').Trim() -replace '\s+', ' ')
}

function Parse-CSharpFile {
    param([string]$FilePath, [string]$ProjectRoot)

    $content = Get-Content $FilePath -Raw -ErrorAction SilentlyContinue
    if (-not $content) { return @{ Classes = @(); Interfaces = @(); Enums = @(); ExtensionMethods = @() } }

    $lines = Get-Content $FilePath
    $relativePath = $FilePath.Substring($ProjectRoot.Length).TrimStart('\', '/')

    # Namespace
    $nsMatch = [regex]::Match($content, 'namespace\s+([\w\.]+)')
    $ns = if ($nsMatch.Success) { $nsMatch.Groups[1].Value } else { "" }

    $result = @{ Classes = @(); Interfaces = @(); Enums = @(); ExtensionMethods = @() }

    # Public/internal classes
    $classMatches = [regex]::Matches($content, '(?m)^\s*(?:public|internal)\s+(?:static\s+|abstract\s+|sealed\s+|partial\s+)*class\s+(\w+)(?:<[^>]+>)?\s*(?::\s*([^{]+))?')
    foreach ($m in $classMatches) {
        $lineNum = ($content.Substring(0, $m.Index) -split "`n").Count
        $result.Classes += @{
            Name     = $m.Groups[1].Value
            Ns       = $ns
            Base     = $m.Groups[2].Value.Trim()
            Summary  = Get-XmlDocSummary -Lines $lines -StartIndex ($lineNum - 2)
            File     = $relativePath
            IsStatic = $m.Value -match '\bstatic\b'
        }
    }

    # Public/internal interfaces
    $ifaceMatches = [regex]::Matches($content, '(?m)^\s*(?:public|internal)\s+(?:partial\s+)?interface\s+(I\w+)(?:<[^>]+>)?')
    foreach ($m in $ifaceMatches) {
        $lineNum = ($content.Substring(0, $m.Index) -split "`n").Count
        $result.Interfaces += @{
            Name    = $m.Groups[1].Value
            Ns      = $ns
            Summary = Get-XmlDocSummary -Lines $lines -StartIndex ($lineNum - 2)
            File    = $relativePath
        }
    }

    # Public enums
    $enumMatches = [regex]::Matches($content, '(?m)^\s*(?:public|internal)\s+enum\s+(\w+)')
    foreach ($m in $enumMatches) {
        $lineNum = ($content.Substring(0, $m.Index) -split "`n").Count
        $result.Enums += @{
            Name    = $m.Groups[1].Value
            Ns      = $ns
            Summary = Get-XmlDocSummary -Lines $lines -StartIndex ($lineNum - 2)
            File    = $relativePath
        }
    }

    # Extension methods
    $extMatches = [regex]::Matches($content, '(?m)^\s*public\s+static\s+(\w+(?:<[^>]+>)?(?:\[\])?)\s+(\w+)\s*\(([^)]*\bthis\b[^)]*)\)')
    foreach ($m in $extMatches) {
        $lineNum = ($content.Substring(0, $m.Index) -split "`n").Count
        $result.ExtensionMethods += @{
            Name    = $m.Groups[2].Value
            Returns = $m.Groups[1].Value
            Sig     = "$($m.Groups[1].Value) $($m.Groups[2].Value)($($m.Groups[3].Value))"
            Summary = Get-XmlDocSummary -Lines $lines -StartIndex ($lineNum - 1)
            File    = $relativePath
        }
    }

    return $result
}

function Escape-TomlString {
    param([string]$Value)
    if (-not $Value) { return '""' }
    $escaped = $Value -replace '\\', '\\' -replace '"', '\"'
    return "`"$escaped`""
}

function Write-TomlEntry {
    param([string]$Type, [hashtable]$Item)
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine("[[$Type]]")
    [void]$sb.AppendLine("name = $(Escape-TomlString $Item.Name)")
    if ($Item.Ns)      { [void]$sb.AppendLine("ns = $(Escape-TomlString $Item.Ns)") }
    if ($Item.Base)    { [void]$sb.AppendLine("base = $(Escape-TomlString $Item.Base)") }
    if ($Item.Sig)     { [void]$sb.AppendLine("sig = $(Escape-TomlString $Item.Sig)") }
    if ($Item.Returns) { [void]$sb.AppendLine("returns = $(Escape-TomlString $Item.Returns)") }
    if ($Item.Summary) { [void]$sb.AppendLine("summary = $(Escape-TomlString $Item.Summary)") }
    if ($Item.File)    { [void]$sb.AppendLine("file = $(Escape-TomlString $Item.File)") }
    if ($Item.IsStatic) { [void]$sb.AppendLine("static = true") }
    [void]$sb.AppendLine()
    return $sb.ToString()
}

#endregion

#region Main

Write-Host "Scanning $repoName..." -ForegroundColor Cyan

# Find all .csproj files
$projectFiles = Get-ChildItem -Path $resolvedRepo -Filter "*.csproj" -Recurse |
    Where-Object {
        $path = $_.FullName
        -not ($excludePatterns | Where-Object { $path -match $_ })
    } |
    Sort-Object Name

Write-Host "Found $($projectFiles.Count) projects" -ForegroundColor Green

$totalClasses = 0
$totalInterfaces = 0
$totalEnums = 0
$totalExtMethods = 0

foreach ($projectFile in $projectFiles) {
    $projectDir = Split-Path $projectFile.FullName
    $projectName = ($projectFile.BaseName)

    # Strip common prefixes for filename
    $tomlName = $projectName -replace '^FEx\.', ''
    $tomlFile = Join-Path $OutputDir "$tomlName.toml"

    Write-Verbose "Processing: $projectName -> $tomlName.toml"

    $csFiles = Get-ChildItem -Path $projectDir -Filter "*.cs" -Recurse |
        Where-Object {
            $path = $_.FullName
            -not ($excludePatterns | Where-Object { $path -match $_ })
        }

    $toml = [System.Text.StringBuilder]::new()

    # Project header
    [void]$toml.AppendLine("[project]")
    [void]$toml.AppendLine("name = $(Escape-TomlString $projectName)")
    $relProjectPath = $projectDir.Substring($resolvedRepo.Length).TrimStart('\', '/')
    [void]$toml.AppendLine("path = $(Escape-TomlString $relProjectPath)")
    [void]$toml.AppendLine()

    $hasContent = $false

    foreach ($csFile in $csFiles) {
        $parsed = Parse-CSharpFile -FilePath $csFile.FullName -ProjectRoot $projectDir

        foreach ($cls in $parsed.Classes) {
            [void]$toml.Append((Write-TomlEntry -Type "classes" -Item $cls))
            $totalClasses++
            $hasContent = $true
        }
        foreach ($iface in $parsed.Interfaces) {
            [void]$toml.Append((Write-TomlEntry -Type "interfaces" -Item $iface))
            $totalInterfaces++
            $hasContent = $true
        }
        foreach ($enum in $parsed.Enums) {
            [void]$toml.Append((Write-TomlEntry -Type "enums" -Item $enum))
            $totalEnums++
            $hasContent = $true
        }
        foreach ($ext in $parsed.ExtensionMethods) {
            [void]$toml.Append((Write-TomlEntry -Type "extensions" -Item $ext))
            $totalExtMethods++
            $hasContent = $true
        }
    }

    if ($hasContent) {
        $toml.ToString() | Out-File -FilePath $tomlFile -Encoding UTF8 -NoNewline
        Write-Verbose "  -> $tomlName.toml"
    }
}

# Write _meta.toml
$gitHash = git -C $resolvedRepo rev-parse --short HEAD 2>$null
if (-not $gitHash) { $gitHash = "unknown" }

@"
[meta]
repo = $(Escape-TomlString $repoName)
generated = $(Escape-TomlString (Get-Date -Format "yyyy-MM-dd HH:mm:ss"))
git_commit = $(Escape-TomlString $gitHash)
classes = $totalClasses
interfaces = $totalInterfaces
enums = $totalEnums
extensions = $totalExtMethods
"@ | Out-File -FilePath (Join-Path $OutputDir "_meta.toml") -Encoding UTF8

Write-Host ""
Write-Host "Summary ($repoName):" -ForegroundColor Cyan
Write-Host "  Classes:     $totalClasses"
Write-Host "  Interfaces:  $totalInterfaces"
Write-Host "  Enums:       $totalEnums"
Write-Host "  Extensions:  $totalExtMethods"
Write-Host "  Output:      $OutputDir" -ForegroundColor Green

# Auto-sync to Obsidian vault
$syncScript = Join-Path $ScriptRoot "Sync-ApiSurfaceToObsidian.ps1"
if (Test-Path $syncScript) {
    Write-Host ""
    Write-Host "Syncing to Obsidian vault..." -ForegroundColor Cyan
    & $syncScript -ApiSurfacePath $OutputDir
}

#endregion
