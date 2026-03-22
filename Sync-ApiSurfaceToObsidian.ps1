<#
.SYNOPSIS
    Syncs .api-surface TOML files to Obsidian vault and generates a searchable _index.md.
.DESCRIPTION
    Copies all TOML files from .api-surface/FEx/ to the Obsidian vault under Projekty/FEx/API/.
    Generates _index.md with all classes, interfaces, enums, and extension methods for Obsidian search.
.PARAMETER ApiSurfacePath
    Path to the .api-surface directory. Defaults to .api-surface/FEx/ relative to this script.
.PARAMETER ObsidianVault
    Path to the Obsidian vault. Defaults to X:\Obsidian\DevVault.
.EXAMPLE
    .\Sync-ApiSurfaceToObsidian.ps1
    .\Sync-ApiSurfaceToObsidian.ps1 -ObsidianVault "D:\MyVault"
#>

[CmdletBinding()]
param(
    [string]$ApiSurfacePath,
    [string]$ObsidianVault = "X:\Obsidian\DevVault"
)

$ErrorActionPreference = "Stop"

if (-not $ApiSurfacePath) {
    $ApiSurfacePath = Join-Path $PSScriptRoot ".api-surface" "FEx"
}

if (-not (Test-Path $ApiSurfacePath)) {
    Write-Error "API surface not found: $ApiSurfacePath. Run Generate-ApiSurface.ps1 first."
    return
}

$targetDir = Join-Path $ObsidianVault "Projekty" "FEx" "API"
New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

# Copy all TOML files
$tomlFiles = Get-ChildItem -Path $ApiSurfacePath -Filter "*.toml"
foreach ($file in $tomlFiles) {
    Copy-Item $file.FullName -Destination $targetDir -Force
}
Write-Host "Copied $($tomlFiles.Count) TOML files to $targetDir" -ForegroundColor Green

# Parse _meta.toml
$metaFile = Join-Path $ApiSurfacePath "_meta.toml"
$metaContent = Get-Content $metaFile -Raw
$gitCommit = if ($metaContent -match 'git_commit\s*=\s*"([^"]+)"') { $matches[1] } else { "unknown" }
$generated = if ($metaContent -match 'generated\s*=\s*"([^"]+)"') { $matches[1] } else { "unknown" }
$totalClasses = if ($metaContent -match 'classes\s*=\s*(\d+)') { $matches[1] } else { "?" }
$totalInterfaces = if ($metaContent -match 'interfaces\s*=\s*(\d+)') { $matches[1] } else { "?" }
$totalEnums = if ($metaContent -match 'enums\s*=\s*(\d+)') { $matches[1] } else { "?" }
$totalExtensions = if ($metaContent -match 'extensions\s*=\s*(\d+)') { $matches[1] } else { "?" }

# Build searchable index
$index = [System.Text.StringBuilder]::new()

[void]$index.AppendLine("---")
[void]$index.AppendLine("projekt: FEx")
[void]$index.AppendLine("typ: api-surface")
[void]$index.AppendLine("generated: $generated")
[void]$index.AppendLine("git_commit: $gitCommit")
[void]$index.AppendLine("tags:")
[void]$index.AppendLine("  - api-surface")
[void]$index.AppendLine("  - fex")
[void]$index.AppendLine("---")
[void]$index.AppendLine("")
[void]$index.AppendLine("## Kluczowe fakty")
[void]$index.AppendLine("")
[void]$index.AppendLine("- $totalClasses classes | $totalInterfaces interfaces | $totalEnums enums | $totalExtensions extensions")
[void]$index.AppendLine("- Generated: $generated (commit: $gitCommit)")
[void]$index.AppendLine("- Zrodlo: ``Generate-ApiSurface.ps1`` w repo FEx")
[void]$index.AppendLine("- Szczegoly per projekt: pliki TOML w tym folderze")
[void]$index.AppendLine("")

# Parse each TOML file (except _meta.toml)
$dataFiles = $tomlFiles | Where-Object { $_.Name -ne "_meta.toml" } | Sort-Object Name

foreach ($file in $dataFiles) {
    $content = Get-Content $file.FullName -Raw
    $projectName = if ($content -match 'name\s*=\s*"([^"]+)"') { $matches[1] } else { $file.BaseName }

    [void]$index.AppendLine("## $projectName")
    [void]$index.AppendLine("")

    # Classes
    $classMatches = [regex]::Matches($content, '(?s)\[\[classes\]\]\s*\n(.*?)(?=\[\[|\z)')
    if ($classMatches.Count -gt 0) {
        foreach ($m in $classMatches) {
            $block = $m.Groups[1].Value
            $name = if ($block -match 'name\s*=\s*"([^"]+)"') { $matches[1] } else { continue }
            $ns = if ($block -match 'ns\s*=\s*"([^"]+)"') { $matches[1] } else { "" }
            $base = if ($block -match 'base\s*=\s*"([^"]+)"') { " : $($matches[1])" } else { "" }
            $summary = if ($block -match 'summary\s*=\s*"([^"]+)"') { " - $($matches[1])" } else { "" }
            $isStatic = if ($block -match 'static\s*=\s*true') { " (static)" } else { "" }
            [void]$index.AppendLine("- class ``$name``$isStatic$base ($ns)$summary")
        }
    }

    # Interfaces
    $ifaceMatches = [regex]::Matches($content, '(?s)\[\[interfaces\]\]\s*\n(.*?)(?=\[\[|\z)')
    if ($ifaceMatches.Count -gt 0) {
        foreach ($m in $ifaceMatches) {
            $block = $m.Groups[1].Value
            $name = if ($block -match 'name\s*=\s*"([^"]+)"') { $matches[1] } else { continue }
            $ns = if ($block -match 'ns\s*=\s*"([^"]+)"') { $matches[1] } else { "" }
            $summary = if ($block -match 'summary\s*=\s*"([^"]+)"') { " - $($matches[1])" } else { "" }
            [void]$index.AppendLine("- interface ``$name`` ($ns)$summary")
        }
    }

    # Enums
    $enumMatches = [regex]::Matches($content, '(?s)\[\[enums\]\]\s*\n(.*?)(?=\[\[|\z)')
    if ($enumMatches.Count -gt 0) {
        foreach ($m in $enumMatches) {
            $block = $m.Groups[1].Value
            $name = if ($block -match 'name\s*=\s*"([^"]+)"') { $matches[1] } else { continue }
            $ns = if ($block -match 'ns\s*=\s*"([^"]+)"') { $matches[1] } else { "" }
            $summary = if ($block -match 'summary\s*=\s*"([^"]+)"') { " - $($matches[1])" } else { "" }
            [void]$index.AppendLine("- enum ``$name`` ($ns)$summary")
        }
    }

    # Extensions
    $extMatches = [regex]::Matches($content, '(?s)\[\[extensions\]\]\s*\n(.*?)(?=\[\[|\z)')
    if ($extMatches.Count -gt 0) {
        foreach ($m in $extMatches) {
            $block = $m.Groups[1].Value
            $name = if ($block -match 'name\s*=\s*"([^"]+)"') { $matches[1] } else { continue }
            $sig = if ($block -match 'sig\s*=\s*"([^"]+)"') { $matches[1] } else { $name }
            $summary = if ($block -match 'summary\s*=\s*"([^"]+)"') { " - $($matches[1])" } else { "" }
            [void]$index.AppendLine("- ext ``$sig``$summary")
        }
    }

    [void]$index.AppendLine("")
}

$indexFile = Join-Path $targetDir "_index.md"
$index.ToString() | Out-File -FilePath $indexFile -Encoding UTF8 -NoNewline

Write-Host "Generated _index.md ($((Get-Item $indexFile).Length / 1KB -as [int]) KB)" -ForegroundColor Green
Write-Host "Done. Obsidian vault updated." -ForegroundColor Cyan
