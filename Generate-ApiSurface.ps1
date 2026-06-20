<#
.SYNOPSIS
    Generates per-project TOML API surface maps for AI agent consumption.
.DESCRIPTION
    Thin wrapper over the Roslyn-based extractor (tools/FEx.ApiSurfaceGen). Scans C# source in a
    given repo and generates one TOML file per project under .api-surface/<repo>/, including types,
    constructors, methods (with default values and generic constraints), properties, and extension
    methods. After generation, auto-syncs the surface to the Obsidian vault.
.PARAMETER RepoPath
    Path to the repository root (e.g., "FEx"). Relative to this script's location or absolute.
.PARAMETER OutputDir
    Output directory. Defaults to ".api-surface/<RepoName>".
.EXAMPLE
    .\Generate-ApiSurface.ps1 -RepoPath FEx
    .\Generate-ApiSurface.ps1 -RepoPath .
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

$genProject = Join-Path $ScriptRoot "tools" "FEx.ApiSurfaceGen" "FEx.ApiSurfaceGen.csproj"
if (-not (Test-Path $genProject)) {
    Write-Error "Extractor project not found: $genProject"
    return
}

# Run the Roslyn extractor (builds on first run)
dotnet run --project $genProject -c Release -- -RepoPath $resolvedRepo -OutputDir $OutputDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "API surface generation failed (exit code $LASTEXITCODE)"
    return
}

# Auto-sync to Obsidian vault
$syncScript = Join-Path $ScriptRoot "Sync-ApiSurfaceToObsidian.ps1"
if (Test-Path $syncScript) {
    Write-Host ""
    Write-Host "Syncing to Obsidian vault..." -ForegroundColor Cyan
    & $syncScript -ApiSurfacePath $OutputDir
}
