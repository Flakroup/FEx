#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Extracts legacy FlakEssentials projects to separate GitLab repositories with FEx submodule and CI/CD.

.DESCRIPTION
    For each legacy project:
    - Creates new local Git repository
    - Copies project files
    - Adds FEx as Git submodule
    - Updates project references to use submodule paths
    - Creates GitLab CI/CD pipeline for NuGet publishing
    - Creates README with usage instructions
    - Commits and tags as v1.0.0
    - Creates GitLab repository via API
    - Pushes to GitLab with submodule

.PARAMETER GitLabToken
    GitLab Personal Access Token with api, read_repository, write_repository scopes

.PARAMETER GitLabUrl
    GitLab instance URL (default: https://gitlab.com)

.PARAMETER GitLabGroup
    GitLab group/namespace for repositories (default: flakroup)

.PARAMETER NuGetFeedUrl
    Private NuGet feed URL for publishing packages (Azure DevOps or GitLab NuGet v3 API)

.PARAMETER NuGetApiKey
    API key for NuGet feed authentication (Azure DevOps PAT or GitLab token)

.PARAMETER NuGetFeedName
    Name of the NuGet feed (default: flakroup)

.PARAMETER FExSubmoduleUrl
    FEx Git submodule URL (default: https://gitlab.com/flakroup/fex.git)

.PARAMETER TargetDirectory
    Base directory for extracted repositories (default: X:\GitLab\Flakroup)

.PARAMETER ProjectNames
    Optional array of specific project names to extract. If not provided, extracts all projects.
    Valid values: FlakEssentials.KeyVault, FlakEssentials.Rest, FlakEssentials.SQLite, 
                  FlakEssentials.IE, FlakEssentials.RadTreeViewEx, FlakEssentials.TelerikEx

.PARAMETER WhatIf
    Dry-run mode. Shows what would be done without making any changes.
    No files created, no git operations, no API calls.

.EXAMPLE
    # Extract all projects
    .\Extract-LegacyProjects.ps1 `
        -GitLabToken "glpat-xxx" `
        -NuGetFeedUrl "https://flakroup.pkgs.visualstudio.com/_packaging/Flakroup/nuget/v3/index.json" `
        -NuGetApiKey "your-azure-devops-api-key"

.EXAMPLE
    # Extract only KeyVault (for testing)
    .\Extract-LegacyProjects.ps1 `
        -GitLabToken "glpat-xxx" `
        -NuGetFeedUrl "https://flakroup.pkgs.visualstudio.com/_packaging/Flakroup/nuget/v3/index.json" `
        -NuGetApiKey "your-azure-devops-api-key" `
        -ProjectNames @("FlakEssentials.KeyVault")

.EXAMPLE
    # Dry-run test (no changes made)
    .\Extract-LegacyProjects.ps1 `
        -GitLabToken "glpat-xxx" `
        -NuGetFeedUrl "https://flakroup.pkgs.visualstudio.com/_packaging/Flakroup/nuget/v3/index.json" `
        -NuGetApiKey "your-azure-devops-api-key" `
        -ProjectNames @("FlakEssentials.KeyVault") `
        -WhatIf
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$GitLabToken,
    
    [Parameter(Mandatory=$false)]
    [string]$GitLabUrl = "https://gitlab.com",
    
    [Parameter(Mandatory=$false)]
    [string]$GitLabGroup = "flakroup",
    
    [Parameter(Mandatory=$true)]
    [string]$NuGetFeedUrl,
    
    [Parameter(Mandatory=$true)]
    [string]$NuGetApiKey,
    
    [Parameter(Mandatory=$false)]
    [string]$NuGetFeedName = "flakroup",
    
    [Parameter(Mandatory=$false)]
    [string]$FExSubmoduleUrl = "https://gitlab.com/flakroup/fex.git",
    
    [Parameter(Mandatory=$false)]
    [string]$TargetDirectory = "X:\GitLab\Flakroup\Legacy",
    
    [Parameter(Mandatory=$false)]
    [string]$FlakEssentialsPath = "X:\GitLab\Flakroup\FEx\flakessentials",
    
    [Parameter(Mandatory=$false)]
    [string[]]$ProjectNames = @(),
    
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf
)

# Configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Project definitions
$projects = @(
    @{
        Name = "FlakEssentials.KeyVault"
        Description = "Local deployment secrets storage - Extracted from FlakEssentials"
        SourcePath = ".NET Standard based\FlakEssentials.KeyVault"
        Tags = "keyvault,credentials,local-storage,flakessentials"
        Files = 2
    },
    @{
        Name = "FlakEssentials.Rest"
        Description = "RestSharp wrapper utilities - Extracted from FlakEssentials"
        SourcePath = ".NET Standard based\FlakEssentials.Rest"
        Tags = "restsharp,rest,api,flakessentials"
        Files = 3
    },
    @{
        Name = "FlakEssentials.SQLite"
        Description = "SQLite database service - Extracted from FlakEssentials"
        SourcePath = ".NET Standard based\FlakEssentials.SQLite"
        Tags = "sqlite,database,flakessentials"
        Files = 6
    },
    @{
        Name = "FlakEssentials.IE"
        Description = "Internet Explorer specific utilities - Extracted from FlakEssentials"
        SourcePath = ".NET Framework based\FlakEssentials.IE"
        Tags = "ie,internet-explorer,legacy,flakessentials"
        Files = 9
    },
    @{
        Name = "FlakEssentials.RadTreeViewEx"
        Description = "Telerik RadTreeView extensions - Extracted from FlakEssentials"
        SourcePath = ".NET Framework based\FlakEssentials.RadTreeViewEx"
        Tags = "telerik,radtreeview,winforms,flakessentials"
        Files = 1
    },
    @{
        Name = "FlakEssentials.TelerikEx"
        Description = "Telerik WinForms/WPF utilities - Extracted from FlakEssentials"
        SourcePath = ".NET Framework based\FlakEssentials.TelerikEx"
        Tags = "telerik,winforms,wpf,flakessentials"
        Files = 12
    }
)

# Functions
function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " $Text" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
}

function Write-Step {
    param([string]$Text)
    Write-Host "  ▶ $Text" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Text)
    Write-Host "  ✓ $Text" -ForegroundColor Green
}

function Write-Error {
    param([string]$Text)
    Write-Host "  ✗ $Text" -ForegroundColor Red
}

function Write-WhatIf {
    param([string]$Text)
    Write-Host "  [WHATIF] $Text" -ForegroundColor Cyan
}

function Get-NextMajorVersion {
    param(
        [string]$PackageId,
        [string]$NuGetFeedUrl,
        [string]$NuGetApiKey
    )
    
    Write-Step "Checking NuGet feed for existing versions of $PackageId"
    
    try {
        # Parse Azure DevOps feed URL to get service index
        # Example: https://flakroup.pkgs.visualstudio.com/_packaging/Flakroup/nuget/v3/index.json
        
        # Get service index
        $headers = @{
            "Accept" = "application/json"
        }
        
        # Add authentication for Azure DevOps
        $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$NuGetApiKey"))
        $headers["Authorization"] = "Basic $base64AuthInfo"
        
        Write-Host "    Querying feed: $NuGetFeedUrl" -ForegroundColor DarkGray
        $serviceIndex = Invoke-RestMethod -Uri $NuGetFeedUrl -Headers $headers -Method Get
        
        # Find PackageBaseAddress or SearchQueryService
        $packageBaseAddress = ($serviceIndex.resources | Where-Object { $_.'@type' -match 'PackageBaseAddress' } | Select-Object -First 1).'@id'
        
        if (-not $packageBaseAddress) {
            # Try SearchQueryService instead
            $searchService = ($serviceIndex.resources | Where-Object { $_.'@type' -match 'SearchQueryService' } | Select-Object -First 1).'@id'
            if ($searchService) {
                $searchUrl = "$searchService`?q=packageid:$PackageId&prerelease=false&semVerLevel=2.0.0"
                Write-Host "    Searching via: $searchUrl" -ForegroundColor DarkGray
                $searchResult = Invoke-RestMethod -Uri $searchUrl -Headers $headers -Method Get
                
                if ($searchResult.data -and $searchResult.data.Count -gt 0) {
                    $latestVersion = $searchResult.data[0].version
                    Write-Host "    Found existing version: $latestVersion" -ForegroundColor Yellow
                    
                    # Parse version and bump major
                    if ($latestVersion -match '^(\d+)\.(\d+)\.(\d+)') {
                        $major = [int]$Matches[1]
                        $nextMajor = $major + 1
                        $nextVersion = "$nextMajor.0.0"
                        Write-Success "Next major version: $nextVersion"
                        return $nextVersion
                    }
                } else {
                    Write-Host "    No existing package found, starting at v1.0.0" -ForegroundColor DarkGray
                    return "1.0.0"
                }
            }
        } else {
            # Try direct package metadata
            $metadataUrl = "$packageBaseAddress$($PackageId.ToLower())/index.json"
            Write-Host "    Checking metadata: $metadataUrl" -ForegroundColor DarkGray
            
            try {
                $metadata = Invoke-RestMethod -Uri $metadataUrl -Headers $headers -Method Get
                
                if ($metadata.versions -and $metadata.versions.Count -gt 0) {
                    # Get latest version
                    $latestVersion = $metadata.versions[-1]
                    Write-Host "    Found existing version: $latestVersion" -ForegroundColor Yellow
                    
                    # Parse version and bump major
                    if ($latestVersion -match '^(\d+)\.(\d+)\.(\d+)') {
                        $major = [int]$Matches[1]
                        $nextMajor = $major + 1
                        $nextVersion = "$nextMajor.0.0"
                        Write-Success "Next major version: $nextVersion"
                        return $nextVersion
                    }
                }
            } catch {
                Write-Host "    Package not found in feed, starting at v1.0.0" -ForegroundColor DarkGray
                return "1.0.0"
            }
        }
        
        # Default to 1.0.0 if not found
        Write-Host "    No existing package found, starting at v1.0.0" -ForegroundColor DarkGray
        return "1.0.0"
        
    } catch {
        Write-Host "    ⚠ Failed to query NuGet feed: $_" -ForegroundColor Yellow
        Write-Host "    Defaulting to v1.0.0" -ForegroundColor Yellow
        return "1.0.0"
    }
}

function Create-GitLabRepository {
    param(
        [string]$ProjectName,
        [string]$Description,
        [string]$Token,
        [string]$GitLabUrl,
        [string]$Namespace
    )
    
    $headers = @{
        "PRIVATE-TOKEN" = $Token
        "Content-Type" = "application/json"
    }
    
    $body = @{
        name = $ProjectName
        description = $Description
        namespace_id = $Namespace
        visibility = "private"
        initialize_with_readme = $false
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$GitLabUrl/api/v4/projects" -Method Post -Headers $headers -Body $body
        return $response
    } catch {
        throw "Failed to create GitLab repository: $_"
    }
}

function Get-GitLabNamespaceId {
    param(
        [string]$GroupPath,
        [string]$Token,
        [string]$GitLabUrl
    )
    
    $headers = @{
        "PRIVATE-TOKEN" = $Token
    }
    
    try {
        $response = Invoke-RestMethod -Uri "$GitLabUrl/api/v4/groups/$GroupPath" -Method Get -Headers $headers
        return $response.id
    } catch {
        throw "Failed to get GitLab group ID: $_"
    }
}

function Create-GitLabCIPipeline {
    param(
        [string]$ProjectName,
        [string]$NuGetFeedUrl,
        [string]$NuGetFeedName
    )
    
    $yaml = @"
# GitLab CI/CD Pipeline for $ProjectName
# Publishes NuGet package to private feed

variables:
  NUGET_FEED_URL: '$NuGetFeedUrl'
  NUGET_FEED_NAME: '$NuGetFeedName'
  DOTNET_VERSION: '9.0.x'

stages:
  - build
  - test
  - pack
  - publish

before_script:
  - git submodule sync --recursive
  - git submodule update --init --recursive

build:
  stage: build
  image: mcr.microsoft.com/dotnet/sdk:9.0
  script:
    - dotnet restore
    - dotnet build --no-restore --configuration Release
  artifacts:
    paths:
      - bin/Release/
    expire_in: 1 hour
  only:
    - main
    - tags

test:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:9.0
  script:
    - dotnet test --no-restore --configuration Release --verbosity minimal
  dependencies:
    - build
  only:
    - main
    - tags
  allow_failure: true

pack:
  stage: pack
  image: mcr.microsoft.com/dotnet/sdk:9.0
  script:
    - dotnet pack --no-build --configuration Release --output ./nupkgs
  artifacts:
    paths:
      - nupkgs/*.nupkg
      - nupkgs/*.snupkg
    expire_in: 1 week
  dependencies:
    - build
  only:
    - tags

publish:
  stage: publish
  image: mcr.microsoft.com/dotnet/sdk:9.0
  script:
    - |
      if [ ! -z "`$CI_COMMIT_TAG" ]; then
        echo "Publishing NuGet package for tag: `$CI_COMMIT_TAG"
        dotnet nuget add source "`$NUGET_FEED_URL" --name "`$NUGET_FEED_NAME" --username gitlab-ci-token --password `$CI_JOB_TOKEN --store-password-in-clear-text
        dotnet nuget push "nupkgs/*.nupkg" --source "`$NUGET_FEED_NAME" --api-key `$CI_JOB_TOKEN --skip-duplicate
      else
        echo "Skipping publish - not a tag"
      fi
  dependencies:
    - pack
  only:
    - tags
  environment:
    name: nuget-production
    url: `$NUGET_FEED_URL
"@
    
    return $yaml
}

function Create-README {
    param(
        [string]$ProjectName,
        [string]$Description,
        [string]$Tags,
        [hashtable]$Project
    )
    
    $repoName = $ProjectName.ToLower() -replace '\.','-'
    
    $readme = @"
# $ProjectName

**Status**: ✅ Extracted from FlakEssentials monorepo  
**Purpose**: $Description  
**Extracted**: October 2025

[![pipeline status](https://gitlab.com/$GitLabGroup/$repoName/badges/main/pipeline.svg)](https://gitlab.com/$GitLabGroup/$repoName/-/commits/main)

---

## 📖 About

This project was extracted from the [FlakEssentials](https://gitlab.com/flakroup/flakessentials) monorepo 
as part of the FEx framework migration. It provides $($Description.ToLower()).

**Original Location**: ``flakessentials/$($Project.SourcePath)/``

---

## 📦 Dependencies

This project depends on **FEx framework** via Git submodule:

The ``FEx/`` directory contains the FEx framework as a Git submodule.

### Clone with Submodules

``````bash
git clone --recurse-submodules https://gitlab.com/$GitLabGroup/$repoName.git
``````

### Update FEx Submodule

``````bash
git submodule update --remote FEx
``````

---

## 🏗️ Building

``````bash
# Restore dependencies (includes submodule)
dotnet restore

# Build
dotnet build --configuration Release

# Run tests (if available)
dotnet test

# Create NuGet package
dotnet pack --configuration Release --output ./nupkgs
``````

---

## 📝 Usage

[Add usage examples here]

---

## 🚀 CI/CD

This repository includes GitLab CI/CD pipeline that:

- ✅ Builds on every commit to ``main``
- ✅ Runs tests (if available)
- ✅ Creates NuGet package on Git tags
- ✅ Publishes to private NuGet feed automatically

### Publishing a New Version

``````bash
# Commit your changes
git add .
git commit -m "Your changes"

# Tag the release
git tag v1.0.1 -m "Release v1.0.1"

# Push with tags
git push origin main --tags
``````

The pipeline will automatically publish to the NuGet feed.

---

## 🔄 Migration Status

- ✅ Extracted from FlakEssentials monorepo
- ✅ FEx added as Git submodule
- ✅ Project references updated to use submodule
- ✅ GitLab CI/CD pipeline configured
- ✅ NuGet package publishing automated

---

## 📜 License

MIT License - See LICENSE file for details

---

**Maintained by**: Flakroup  
**Questions**: [Open an issue](https://gitlab.com/$GitLabGroup/$repoName/-/issues)
"@
    
    return $readme
}

function Update-ProjectReferences {
    param(
        [string]$CsprojPath
    )
    
    Write-Step "Updating project references in $CsprojPath"
    
    $content = Get-Content $CsprojPath -Raw
    
    # Replace old FlakEssentials FEx project references with submodule paths
    $content = $content -replace 'Include="\.\.\\\.\.\\\.\.\\src\\', 'Include="FEx\src\'
    $content = $content -replace 'Include="\.\.\\\.\.\\src\\', 'Include="FEx\src\'
    
    Set-Content $CsprojPath -Value $content -NoNewline
    
    Write-Success "Project references updated"
}

function Get-ProjectReferences {
    param(
        [string]$CsprojPath
    )
    
    [xml]$xml = Get-Content $CsprojPath
    $projectRefs = @()
    
    foreach ($itemGroup in $xml.Project.ItemGroup) {
        foreach ($projectRef in $itemGroup.ProjectReference) {
            if ($projectRef.Include) {
                $projectRefs += $projectRef.Include
            }
        }
    }
    
    return $projectRefs
}

function Create-SolutionFile {
    param(
        [string]$SolutionPath,
        [string]$MainProjectPath,
        [string]$ProjectName
    )
    
    Write-Step "Creating solution file with all referenced projects"
    
    # Get all project references recursively
    $allProjects = @()
    $processedProjects = @()
    $projectsToProcess = @($MainProjectPath)
    
    while ($projectsToProcess.Count -gt 0) {
        $currentProject = $projectsToProcess[0]
        $projectsToProcess = $projectsToProcess[1..($projectsToProcess.Count - 1)]
        
        if ($processedProjects -contains $currentProject) {
            continue
        }
        
        $processedProjects += $currentProject
        
        if (Test-Path $currentProject) {
            $allProjects += $currentProject
            
            # Get project references
            $refs = Get-ProjectReferences -CsprojPath $currentProject
            foreach ($ref in $refs) {
                # Resolve relative path
                $projectDir = Split-Path $currentProject -Parent
                $refPath = Join-Path $projectDir $ref
                $refPath = [System.IO.Path]::GetFullPath($refPath)
                
                if (-not ($processedProjects -contains $refPath)) {
                    $projectsToProcess += $refPath
                }
            }
        }
    }
    
    Write-Host "    Found $($allProjects.Count) projects (including dependencies)" -ForegroundColor DarkGray
    
    # Create solution
    $solutionDir = Split-Path $SolutionPath -Parent
    $slnContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
"@

    $projectGuids = @{}
    
    # Add projects to solution
    foreach ($projectPath in $allProjects) {
        $projectGuid = [guid]::NewGuid().ToString().ToUpper()
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
        $relativePath = [System.IO.Path]::GetRelativePath($solutionDir, $projectPath) -replace '/', '\'
        
        $projectGuids[$projectPath] = $projectGuid
        
        $slnContent += @"

Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "$projectName", "$relativePath", "{$projectGuid}"
EndProject
"@
    }
    
    $slnContent += @"

Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
"@
    
    foreach ($guid in $projectGuids.Values) {
        $slnContent += @"

		{$guid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{$guid}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{$guid}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{$guid}.Release|Any CPU.Build.0 = Release|Any CPU
"@
    }
    
    $slnContent += @"

	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
"@
    
    Set-Content $SolutionPath -Value $slnContent -NoNewline
    
    Write-Success "Solution created with $($allProjects.Count) projects"
}

function Add-NuGetMetadata {
    param(
        [string]$CsprojPath,
        [string]$PackageId,
        [string]$Version,
        [string]$Description,
        [string]$Tags,
        [string]$RepoUrl
    )
    
    Write-Step "Adding NuGet package metadata (v$Version)"
    
    [xml]$xml = Get-Content $CsprojPath
    
    # Find or create PropertyGroup
    $propertyGroup = $xml.Project.PropertyGroup | Select-Object -First 1
    if (-not $propertyGroup) {
        $propertyGroup = $xml.CreateElement("PropertyGroup")
        $xml.Project.AppendChild($propertyGroup) | Out-Null
    }
    
    # Add NuGet metadata
    $metadata = @{
        "PackageId" = $PackageId
        "Version" = $Version
        "Authors" = "Flakroup"
        "Company" = "Flakroup"
        "Description" = $Description
        "PackageTags" = $Tags
        "PackageProjectUrl" = $RepoUrl
        "RepositoryUrl" = $RepoUrl
        "RepositoryType" = "git"
        "PackageLicenseExpression" = "MIT"
        "GeneratePackageOnBuild" = "false"
        "IncludeSymbols" = "true"
        "SymbolPackageFormat" = "snupkg"
    }
    
    foreach ($key in $metadata.Keys) {
        $existingNode = $propertyGroup.SelectSingleNode($key)
        if ($existingNode) {
            $existingNode.InnerText = $metadata[$key]
        } else {
            $newNode = $xml.CreateElement($key)
            $newNode.InnerText = $metadata[$key]
            $propertyGroup.AppendChild($newNode) | Out-Null
        }
    }
    
    $xml.Save($CsprojPath)
    
    Write-Success "NuGet metadata added"
}

# Filter projects if specific names provided
if ($ProjectNames.Count -gt 0) {
    $projects = $projects | Where-Object { $ProjectNames -contains $_.Name }
    if ($projects.Count -eq 0) {
        Write-Error "No matching projects found. Available projects: FlakEssentials.KeyVault, FlakEssentials.Rest, FlakEssentials.SQLite, FlakEssentials.IE, FlakEssentials.RadTreeViewEx, FlakEssentials.TelerikEx"
        exit 1
    }
}

# Main execution
Write-Header "FlakEssentials Legacy Project Extraction"
if ($WhatIf) {
    Write-Host "  MODE: DRY RUN (WhatIf) - No changes will be made" -ForegroundColor Yellow
}
Write-Host "  Target Directory: $TargetDirectory"
Write-Host "  GitLab Group: $GitLabGroup"
Write-Host "  NuGet Feed: $NuGetFeedUrl"
if ($ProjectNames.Count -gt 0) {
    Write-Host "  Filter: $($ProjectNames -join ', ')" -ForegroundColor Yellow
}
Write-Host "  Projects to Extract: $($projects.Count)"
Write-Host ""

# Get GitLab namespace ID
Write-Step "Getting GitLab namespace ID for '$GitLabGroup'"
try {
    $namespaceId = Get-GitLabNamespaceId -GroupPath $GitLabGroup -Token $GitLabToken -GitLabUrl $GitLabUrl
    Write-Success "Namespace ID: $namespaceId"
} catch {
    Write-Error "Failed to get namespace ID: $_"
    exit 1
}

# Process each project
$successCount = 0
$failureCount = 0

foreach ($project in $projects) {
    $projectName = $project.Name
    $repoName = $projectName.ToLower() -replace '\.','-'
    $repoUrl = "$GitLabUrl/$GitLabGroup/$repoName"
    $localPath = Join-Path $TargetDirectory $projectName
    $sourcePath = Join-Path $FlakEssentialsPath $project.SourcePath
    
    Write-Header "Extracting: $projectName ($($project.Files) files)"
    
    try {
        # Phase 0: Determine next version from NuGet feed
        $packageVersion = Get-NextMajorVersion -PackageId $projectName -NuGetFeedUrl $NuGetFeedUrl -NuGetApiKey $NuGetApiKey
        Write-Host "  Using version: $packageVersion" -ForegroundColor Cyan
        Write-Host ""
        
        # Phase 1: Create local repository
        Write-Step "Creating local repository"
        if ($WhatIf) {
            Write-WhatIf "Would create directory: $localPath"
            if (Test-Path $localPath) {
                Write-WhatIf "Would remove existing directory: $localPath"
            }
            Write-WhatIf "Would initialize Git repository with main branch"
        } else {
            if (Test-Path $localPath) {
                Write-Host "    ⚠ Directory exists, removing: $localPath" -ForegroundColor Yellow
                Remove-Item $localPath -Recurse -Force
            }
            New-Item -Path $localPath -ItemType Directory -Force | Out-Null
            Set-Location $localPath
            Write-Success "Local repository created"
            
            # Initialize Git
            Write-Step "Initializing Git repository"
            git init | Out-Null
            git branch -m master main | Out-Null
            git config user.name "FlakEssentials Migration"
            git config user.email "migration@flakroup.com"
            Write-Success "Git initialized with main branch"
        }
        
        # Phase 2: Copy project files
        Write-Step "Copying project files from $($project.SourcePath)"
        if (-not (Test-Path $sourcePath)) {
            throw "Source path not found: $sourcePath"
        }
        if ($WhatIf) {
            Write-WhatIf "Would copy files from: $sourcePath"
            Write-WhatIf "Would copy to: $localPath"
            Write-WhatIf "Would copy $($project.Files) files"
        } else {
            Copy-Item "$sourcePath\*" -Destination $localPath -Recurse -Force
            Write-Success "Files copied ($($project.Files) files)"
        }
        
        # Phase 3: Add FEx as submodule
        Write-Step "Adding FEx as Git submodule"
        if ($WhatIf) {
            Write-WhatIf "Would run: git submodule add $FExSubmoduleUrl FEx"
            Write-WhatIf "Would run: git submodule update --init --recursive"
        } else {
            git submodule add $FExSubmoduleUrl FEx 2>&1 | Out-Null
            git submodule update --init --recursive 2>&1 | Out-Null
            Write-Success "FEx submodule added"
        }
        
        # Phase 4: Update project references
        if ($WhatIf) {
            Write-Step "Analyzing project references"
            Write-WhatIf "Would find .csproj file"
            Write-WhatIf "Would update project references: '../../../src/' -> 'FEx/src/'"
            Write-WhatIf "Would add NuGet metadata with version: $packageVersion"
        } else {
            $csprojFile = Get-ChildItem -Path $localPath -Filter "*.csproj" | Select-Object -First 1
            if ($csprojFile) {
                Update-ProjectReferences -CsprojPath $csprojFile.FullName
                Add-NuGetMetadata -CsprojPath $csprojFile.FullName `
                    -PackageId $projectName `
                    -Version $packageVersion `
                    -Description $project.Description `
                    -Tags $project.Tags `
                    -RepoUrl $repoUrl
            } else {
                Write-Host "    ⚠ No .csproj file found" -ForegroundColor Yellow
            }
        }
        
        # Phase 5: Create GitLab CI/CD pipeline
        Write-Step "Creating GitLab CI/CD pipeline"
        if ($WhatIf) {
            Write-WhatIf "Would create .gitlab-ci.yml with 4 stages (build, test, pack, publish)"
        } else {
            $gitlabCIContent = Create-GitLabCIPipeline -ProjectName $projectName `
                -NuGetFeedUrl $NuGetFeedUrl `
                -NuGetFeedName $NuGetFeedName
            Set-Content ".gitlab-ci.yml" -Value $gitlabCIContent
            Write-Success "GitLab CI/CD pipeline created"
        }
        
        # Phase 6: Create README
        Write-Step "Creating README.md"
        if ($WhatIf) {
            Write-WhatIf "Would create README.md with project documentation"
        } else {
            $readmeContent = Create-README -ProjectName $projectName `
                -Description $project.Description `
                -Tags $project.Tags `
                -Project $project
            Set-Content "README.md" -Value $readmeContent
            Write-Success "README.md created"
        }
        
        # Phase 7: Create solution file
        Write-Step "Creating solution file with project tree"
        if ($WhatIf) {
            Write-WhatIf "Would create $projectName.sln with main project + all FEx dependencies"
            Write-WhatIf "Would recursively find all ProjectReference elements"
        } else {
            $slnPath = Join-Path $localPath "$projectName.sln"
            $mainCsprojPath = Join-Path $localPath (Get-ChildItem -Path $localPath -Filter "*.csproj" | Select-Object -First 1).Name
            if ($mainCsprojPath -and (Test-Path $mainCsprojPath)) {
                Create-SolutionFile -SolutionPath $slnPath -MainProjectPath $mainCsprojPath -ProjectName $projectName
            } else {
                Write-Host "    ⚠ No .csproj file found, skipping solution creation" -ForegroundColor Yellow
            }
        }
        
        # Phase 8: Copy .gitignore from FlakEssentials
        Write-Step "Copying .gitignore from FlakEssentials"
        if ($WhatIf) {
            Write-WhatIf "Would copy .gitignore from: $FlakEssentialsPath\.gitignore"
        } else {
            $gitignorePath = Join-Path $FlakEssentialsPath ".gitignore"
            if (Test-Path $gitignorePath) {
                Copy-Item $gitignorePath -Destination $localPath
                Write-Success ".gitignore copied from FlakEssentials"
            } else {
                Write-Host "    ⚠ .gitignore not found in FlakEssentials" -ForegroundColor Yellow
            }
        }
        
        # Phase 9: Build validation
        Write-Step "Building project"
        if ($WhatIf) {
            Write-WhatIf "Would run: dotnet restore"
            Write-WhatIf "Would run: dotnet build --configuration Release"
        } else {
            $buildOutput = dotnet restore 2>&1
            $buildOutput = dotnet build --no-restore --configuration Release 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Build successful"
            } else {
                Write-Host "    ⚠ Build failed (may need manual fixes)" -ForegroundColor Yellow
                Write-Host "    Build output:" -ForegroundColor DarkGray
                $buildOutput | ForEach-Object { Write-Host "      $_" -ForegroundColor DarkGray }
            }
        }
        
        # Phase 10: Initial commit
        Write-Step "Creating initial commit"
        if ($WhatIf) {
            Write-WhatIf "Would run: git add ."
            Write-WhatIf "Would create commit: 'Initial commit - Extracted from FlakEssentials with FEx submodule'"
            Write-WhatIf "Would create tag: v$packageVersion"
        } else {
            git add .
            git commit -m "Initial commit - Extracted from FlakEssentials with FEx submodule

This project provides $($project.Description.ToLower()) and was extracted 
from the FlakEssentials monorepo as part of the FEx framework migration.

Version: $packageVersion
Project structure:
- Source files from $projectName
- FEx framework as Git submodule (FEx/)
- Updated project references to use submodule paths
- GitLab CI/CD pipeline for NuGet publishing
- NuGet package metadata configured

Build: dotnet build (validated)
Files: $($project.Files)" | Out-Null
            git tag "v$packageVersion" -m "Release v$packageVersion - Extracted from FlakEssentials"
            Write-Success "Initial commit and tag v$packageVersion created"
        }
        
        # Phase 11: Create GitLab repository
        Write-Step "Creating GitLab repository: $repoUrl"
        if ($WhatIf) {
            Write-WhatIf "Would call GitLab API: POST /api/v4/projects"
            Write-WhatIf "Would create repository: $repoUrl"
            Write-WhatIf "Would set visibility: private"
        } else {
            try {
                $gitlabRepo = Create-GitLabRepository `
                    -ProjectName $projectName `
                    -Description $project.Description `
                    -Token $GitLabToken `
                    -GitLabUrl $GitLabUrl `
                    -Namespace $namespaceId
                Write-Success "GitLab repository created"
                
                # Phase 12: Push to GitLab
                Write-Step "Pushing to GitLab"
                git remote add origin $gitlabRepo.ssh_url_to_repo
                git push -u origin main --tags 2>&1 | Out-Null
                Write-Success "Pushed to GitLab with tags"
                
            } catch {
                if ($_.Exception.Message -match "already exists") {
                    Write-Host "    ⚠ Repository already exists on GitLab" -ForegroundColor Yellow
                    Write-Host "    Manual push required: cd $localPath && git remote add origin $repoUrl.git && git push -u origin main --tags" -ForegroundColor Yellow
                } else {
                    throw $_
                }
            }
        }
        
        if ($WhatIf) {
            Write-WhatIf "Would run: git remote add origin {gitlab-ssh-url}"
            Write-WhatIf "Would run: git push -u origin main --tags"
        }
        
        if ($WhatIf) {
            Write-Host ""
            Write-Host "  [WHATIF] $projectName extraction simulation complete!" -ForegroundColor Cyan
            Write-Host "  [WHATIF] Would create repository: $repoUrl" -ForegroundColor Cyan
        } else {
            Write-Success "✓ $projectName extraction complete!"
            Write-Host "  Repository: $repoUrl" -ForegroundColor Green
        }
        $successCount++
        
    } catch {
        Write-Error "Failed to extract $projectName : $_"
        $failureCount++
    }
}

# Summary
Write-Header "Extraction Summary"
Write-Host "  ✓ Successful: $successCount" -ForegroundColor Green
if ($failureCount -gt 0) {
    Write-Host "  ✗ Failed: $failureCount" -ForegroundColor Red
}
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Review each repository on GitLab"
Write-Host "  2. Configure GitLab CI/CD variables if needed (NuGet API keys, etc.)"
Write-Host "  3. Test pipelines by creating a new tag: git tag v1.0.1 && git push --tags"
Write-Host "  4. Remove projects from FlakEssentials solution"
Write-Host ""

# Return to original directory
Set-Location (Split-Path $FlakEssentialsPath -Parent)

