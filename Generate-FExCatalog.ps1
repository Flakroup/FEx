<#
.SYNOPSIS
    Generates FEx API Catalog for AI agent consumption
.DESCRIPTION
    Scans FEx source code and generates comprehensive API catalog in JSON and Markdown formats
    Extracts: classes, interfaces, extension methods, enums, and their XML documentation
.EXAMPLE
    .\Generate-FExCatalog.ps1
    .\Generate-FExCatalog.ps1 -Verbose
#>

[CmdletBinding()]
param(
    [string]$SourcePath = "src",
    [string]$OutputJson = "FEx-API-Catalog.json",
    [string]$OutputMarkdown = "FEx-API-Catalog.md"
)

$ErrorActionPreference = "Stop"

#region Helper Functions

function Get-XmlDocSummary {
    param([string[]]$Lines, [int]$StartIndex)
    
    $summary = [System.Collections.ArrayList]@()
    $inSummary = $false
    $foundClosing = $false
    
    # Look backwards from StartIndex to find XML doc comments
    for ($i = $StartIndex; $i -ge [Math]::Max(0, $StartIndex - 30); $i--) {
        $line = $Lines[$i]
        
        # Check if this is a doc comment line
        if ($line -match '^\s*///(.*)$') {
            $content = $matches[1].Trim()
            
            # Check for closing summary tag
            if ($content -match '</summary>') {
                $inSummary = $true
                $foundClosing = $true
                # Extract any text before closing tag
                $textBefore = $content -replace '</summary>.*', ''
                $textBefore = $textBefore -replace '<summary>', ''
                if ($textBefore.Trim()) {
                    [void]$summary.Insert(0, $textBefore.Trim())
                }
            }
            # We're inside summary block
            elseif ($inSummary) {
                # Check for opening summary tag
                if ($content -match '<summary>(.*)') {
                    $textAfter = $matches[1].Trim()
                    if ($textAfter) {
                        [void]$summary.Insert(0, $textAfter)
                    }
                    break  # Found opening tag, we're done
                }
                else {
                    # Regular content line
                    $cleanContent = $content -replace '<[^>]+>', ''  # Remove any XML tags
                    if ($cleanContent.Trim()) {
                        [void]$summary.Insert(0, $cleanContent.Trim())
                    }
                }
            }
        }
        # If we found closing tag but then hit non-comment line, stop
        elseif ($inSummary) {
            break
        }
    }
    
    $result = ($summary -join ' ').Trim()
    # Clean up multiple spaces
    $result = $result -replace '\s+', ' '
    return $result
}

function Parse-CSharpFile {
    param([string]$FilePath, [string]$ProjectName, [string]$ProjectNamespace)
    
    $content = Get-Content $FilePath -Raw
    $lines = Get-Content $FilePath
    
    # Extract namespace
    $namespaceMatch = [regex]::Match($content, 'namespace\s+([\w\.]+)')
    $namespace = if ($namespaceMatch.Success) { $namespaceMatch.Groups[1].Value } else { $ProjectNamespace }
    
    $result = @{
        Classes = @()
        Interfaces = @()
        Enums = @()
        ExtensionMethods = @()
        Methods = @()
        Properties = @()
    }
    
    # Find public classes
    $classMatches = [regex]::Matches($content, '(?m)^\s*public\s+(?:static\s+|abstract\s+|sealed\s+)*class\s+(\w+)(?:<[^>]+>)?\s*(?::\s*([^{]+))?')
    foreach ($match in $classMatches) {
        $className = $match.Groups[1].Value
        $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
        $summary = Get-XmlDocSummary -Lines $lines -StartIndex ($lineNumber - 2)
        
        # Check if it's a static class (potential extension class)
        $isStatic = $content.Substring([Math]::Max(0, $match.Index - 100), [Math]::Min(100, $content.Length - $match.Index)) -match '\bstatic\s+class\b'
        
        $result.Classes += @{
            Name = $className
            FullName = "$namespace.$className"
            IsStatic = $isStatic
            BaseTypes = $match.Groups[2].Value.Trim()
            Summary = $summary
            Location = (Split-Path $FilePath -Leaf)
        }
    }
    
    # Find public interfaces
    $interfaceMatches = [regex]::Matches($content, '(?m)^\s*public\s+interface\s+(I\w+)(?:<[^>]+>)?')
    foreach ($match in $interfaceMatches) {
        $interfaceName = $match.Groups[1].Value
        $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
        $summary = Get-XmlDocSummary -Lines $lines -StartIndex ($lineNumber - 2)
        
        $result.Interfaces += @{
            Name = $interfaceName
            FullName = "$namespace.$interfaceName"
            Summary = $summary
            Location = (Split-Path $FilePath -Leaf)
        }
    }
    
    # Find public enums
    $enumMatches = [regex]::Matches($content, '(?m)^\s*public\s+enum\s+(\w+)')
    foreach ($match in $enumMatches) {
        $enumName = $match.Groups[1].Value
        $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
        $summary = Get-XmlDocSummary -Lines $lines -StartIndex ($lineNumber - 2)
        
        $result.Enums += @{
            Name = $enumName
            FullName = "$namespace.$enumName"
            Summary = $summary
            Location = (Split-Path $FilePath -Leaf)
        }
    }
    
    # Find extension methods (public static methods with 'this' parameter)
    $methodMatches = [regex]::Matches($content, '(?m)^\s*public\s+static\s+(\w+(?:<[^>]+>)?(?:\[\])?)\s+(\w+)\s*\(([^)]*\bthis\b[^)]*)\)')
    foreach ($match in $methodMatches) {
        $returnType = $match.Groups[1].Value
        $methodName = $match.Groups[2].Value
        $parameters = $match.Groups[3].Value
        $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
        $summary = Get-XmlDocSummary -Lines $lines -StartIndex ($lineNumber - 1)
        
        # Extract 'this Type' parameter
        if ($parameters -match '\bthis\s+([\w<>\[\]\?\.]+)\s+(\w+)') {
            $targetType = $matches[1].Trim()
            
            $result.ExtensionMethods += @{
                TargetType = $targetType
                MethodName = $methodName
                ReturnType = $returnType
                Signature = "$returnType $methodName($parameters)"
                Summary = $summary
                Location = (Split-Path $FilePath -Leaf)
                Namespace = $namespace
            }
        }
    }
    
    # Find regular public methods (non-extension, non-constructor)
    $regularMethodMatches = [regex]::Matches($content, '(?m)^\s*public\s+(?:static\s+|virtual\s+|override\s+|abstract\s+|async\s+)*(\w+(?:<[^>]+>)?(?:\[\])?(?:\?)?)\s+(\w+)\s*\(([^)]*(?!\bthis\b)[^)]*)\)')
    foreach ($match in $regularMethodMatches) {
        $returnType = $match.Groups[1].Value
        $methodName = $match.Groups[2].Value
        $parameters = $match.Groups[3].Value
        
        # Skip constructors (method name same as potential class name)
        # Skip if parameters contain 'this' (extension methods already handled)
        if ($parameters -notmatch '\bthis\b') {
            $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
            $summary = Get-XmlDocSummary -Lines $lines -StartIndex ($lineNumber - 1)
            
            # Only add if we got a summary or if it's a common utility pattern
            if ($summary -or $methodName -match '^(Get|Set|Create|Build|Parse|Convert|Transform|Execute|Process|Handle|Validate|Check|Is|Has|Can)') {
                $result.Methods += @{
                    MethodName = $methodName
                    ReturnType = $returnType
                    Signature = "$returnType $methodName($parameters)"
                    Summary = $summary
                    Location = (Split-Path $FilePath -Leaf)
                    Namespace = $namespace
                }
            }
        }
    }
    
    # Find public properties
    $propertyMatches = [regex]::Matches($content, '(?m)^\s*public\s+(?:static\s+|virtual\s+|override\s+|abstract\s+)?(\w+(?:<[^>]+>)?(?:\[\])?(?:\?)?)\s+(\w+)\s*\{\s*get')
    foreach ($match in $propertyMatches) {
        $propertyType = $match.Groups[1].Value
        $propertyName = $match.Groups[2].Value
        $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
        $summary = Get-XmlDocSummary -Lines $lines -StartIndex ($lineNumber - 1)
        
        # Only add properties with documentation or common patterns
        if ($summary) {
            $result.Properties += @{
                PropertyName = $propertyName
                PropertyType = $propertyType
                Summary = $summary
                Location = (Split-Path $FilePath -Leaf)
                Namespace = $namespace
            }
        }
    }
    
    return $result
}

function Get-ProjectInfo {
    param([string]$ProjectPath)
    
    [xml]$csproj = Get-Content $ProjectPath
    $projectName = (Split-Path $ProjectPath -Leaf) -replace '\.csproj$', ''
    
    # Determine namespace
    $namespace = "Flakroup.FEx"
    if ($projectName -match '^FEx\.(.+)') {
        $suffix = $matches[1] -replace '\.', ''
        $namespace = "Flakroup.FEx.$suffix"
    }
    
    # Get description if available
    $description = $csproj.Project.PropertyGroup.Description
    
    return @{
        Name = $projectName
        Namespace = $namespace
        Description = $description
        Path = $ProjectPath
    }
}

#endregion

#region Main Script

Write-Host "🔍 Scanning FEx projects..." -ForegroundColor Cyan

$catalog = @{
    Version = "1.0"
    Generated = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
    Description = "FEx Framework API Catalog - Auto-generated for AI agent consumption"
    Projects = @()
}

# Find all .csproj files
$projectFiles = Get-ChildItem -Path $SourcePath -Filter "*.csproj" -Recurse | 
    Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' } |
    Sort-Object Name

Write-Host "Found $($projectFiles.Count) projects" -ForegroundColor Green

foreach ($projectFile in $projectFiles) {
    $projectInfo = Get-ProjectInfo -ProjectPath $projectFile.FullName
    Write-Verbose "Processing: $($projectInfo.Name)"
    
    $projectData = @{
        Name = $projectInfo.Name
        Namespace = $projectInfo.Namespace
        Description = $projectInfo.Description
        Classes = @()
        Interfaces = @()
        Enums = @()
        ExtensionMethods = @()
        Methods = @()
        Properties = @()
    }
    
    # Find all .cs files in project directory
    $projectDir = Split-Path $projectFile.FullName
    $csFiles = Get-ChildItem -Path $projectDir -Filter "*.cs" -Recurse |
        Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' }
    
    foreach ($csFile in $csFiles) {
        $parsed = Parse-CSharpFile -FilePath $csFile.FullName -ProjectName $projectInfo.Name -ProjectNamespace $projectInfo.Namespace
        
        $projectData.Classes += $parsed.Classes
        $projectData.Interfaces += $parsed.Interfaces
        $projectData.Enums += $parsed.Enums
        $projectData.ExtensionMethods += $parsed.ExtensionMethods
        $projectData.Methods += $parsed.Methods
        $projectData.Properties += $parsed.Properties
    }
    
    # Only add projects that have public API
    if ($projectData.Classes.Count -gt 0 -or 
        $projectData.Interfaces.Count -gt 0 -or 
        $projectData.Enums.Count -gt 0 -or 
        $projectData.ExtensionMethods.Count -gt 0 -or
        $projectData.Methods.Count -gt 0 -or
        $projectData.Properties.Count -gt 0) {
        $catalog.Projects += $projectData
    }
}

#endregion

#region Generate JSON

Write-Host "📄 Generating JSON catalog..." -ForegroundColor Cyan
$jsonOutput = $catalog | ConvertTo-Json -Depth 10
$jsonOutput | Out-File -FilePath $OutputJson -Encoding UTF8
Write-Host "✅ JSON catalog saved: $OutputJson" -ForegroundColor Green

#endregion

#region Generate Markdown

Write-Host "📝 Generating Markdown catalog..." -ForegroundColor Cyan

$md = @"
# FEx Framework API Catalog

**Generated:** $($catalog.Generated)  
**Version:** $($catalog.Version)

> 🤖 **AI Agent Usage:** Load this file into context when working with projects that reference FEx framework.

---

## 📋 Table of Contents

"@

foreach ($project in $catalog.Projects | Sort-Object Name) {
    $md += "- [$($project.Name)](#$($project.Name.ToLower() -replace '\.', ''))`n"
}

$md += "`n---`n`n"

# Generate detailed sections
foreach ($project in $catalog.Projects | Sort-Object Name) {
    $anchor = $project.Name.ToLower() -replace '\.', ''
    $md += "## $($project.Name)`n`n"
    
    if ($project.Description) {
        $md += "**Description:** $($project.Description)`n`n"
    }
    
    $md += "**Namespace:** ``$($project.Namespace)``  `n"
    $md += "**Classes:** $($project.Classes.Count) | **Interfaces:** $($project.Interfaces.Count) | **Enums:** $($project.Enums.Count)`n"
    $md += "**Extension Methods:** $($project.ExtensionMethods.Count) | **Methods:** $($project.Methods.Count) | **Properties:** $($project.Properties.Count)`n`n"
    
    # Extension Methods (most useful for AI)
    if ($project.ExtensionMethods.Count -gt 0) {
        $md += "### 🔌 Extension Methods`n`n"
        
        $extensionsByTarget = $project.ExtensionMethods | Group-Object TargetType | Sort-Object Name
        
        foreach ($group in $extensionsByTarget) {
            $md += "#### Extensions for ``$($group.Name)```n`n"
            
            foreach ($ext in $group.Group | Sort-Object MethodName) {
                $md += "- **``$($ext.MethodName)``** → ``$($ext.ReturnType)```n"
                if ($ext.Summary) {
                    $md += "  - $($ext.Summary)`n"
                }
                $md += "  - ``$($ext.Signature)```n"
                $md += "  - 📁 $($ext.Location)`n"
                $md += "`n"
            }
        }
    }
    
    # Interfaces
    if ($project.Interfaces.Count -gt 0) {
        $md += "### 🔷 Interfaces`n`n"
        
        foreach ($interface in $project.Interfaces | Sort-Object Name) {
            $md += "- **``$($interface.Name)``**"
            if ($interface.Summary) {
                $md += " - $($interface.Summary)"
            }
            $md += "`n"
        }
        $md += "`n"
    }
    
    # Classes
    if ($project.Classes.Count -gt 0) {
        $md += "### 📦 Classes`n`n"
        
        foreach ($class in $project.Classes | Sort-Object Name) {
            $md += "- **``$($class.Name)``**"
            if ($class.IsStatic) {
                $md += " *(static)*"
            }
            if ($class.Summary) {
                $md += " - $($class.Summary)"
            }
            $md += "`n"
        }
        $md += "`n"
    }
    
    # Enums
    if ($project.Enums.Count -gt 0) {
        $md += "### 🔢 Enums`n`n"
        
        foreach ($enum in $project.Enums | Sort-Object Name) {
            $md += "- **``$($enum.Name)``**"
            if ($enum.Summary) {
                $md += " - $($enum.Summary)"
            }
            $md += "`n"
        }
        $md += "`n"
    }
    
    # Public Methods (non-extension)
    if ($project.Methods.Count -gt 0) {
        $md += "### ⚙️ Public Methods`n`n"
        
        # Group by file for better organization
        $methodsByFile = $project.Methods | Group-Object Location | Sort-Object Name
        
        foreach ($group in $methodsByFile) {
            if ($group.Group.Count -gt 5) {
                $md += "#### 📁 $($group.Name) ($($group.Group.Count) methods)`n`n"
            } else {
                $md += "#### 📁 $($group.Name)`n`n"
            }
            
            foreach ($method in $group.Group | Sort-Object MethodName) {
                $md += "- **``$($method.MethodName)``** → ``$($method.ReturnType)```n"
                if ($method.Summary) {
                    $md += "  - $($method.Summary)`n"
                }
                $md += "  - ``$($method.Signature)```n"
                $md += "`n"
            }
        }
    }
    
    # Properties
    if ($project.Properties.Count -gt 0) {
        $md += "### 📊 Properties`n`n"
        
        foreach ($property in $project.Properties | Sort-Object PropertyName) {
            $md += "- **``$($property.PropertyName)``** : ``$($property.PropertyType)```n"
            if ($property.Summary) {
                $md += "  - $($property.Summary)`n"
            }
            $md += "  - 📁 $($property.Location)`n"
            $md += "`n"
        }
    }
    
    $md += "---`n`n"
}

$md += @"

## 🚀 Usage Instructions

### For AI Agents

1. **Load this catalog** at the start of working with FEx-consuming projects
2. **Reference APIs** by their full signatures when suggesting code
3. **Prefer FEx utilities** over reimplementing common functionality
4. **Check extension methods** - FEx provides rich extensions for strings, collections, enums, etc.

### For Developers

```powershell
# Regenerate catalog after code changes
.\Generate-FExCatalog.ps1

# View with detailed logging
.\Generate-FExCatalog.ps1 -Verbose
```

### Example Usage Prompt

```
Load API catalog: X:\GitLab\Flakroup\FEx\FEx-API-Catalog.md

I'm working on a project that references FEx framework. 
Please review available utilities before implementing new code.
```

---

*Catalog generated by ``Generate-FExCatalog.ps1``*
"@

$md | Out-File -FilePath $OutputMarkdown -Encoding UTF8
Write-Host "✅ Markdown catalog saved: $OutputMarkdown" -ForegroundColor Green

#endregion

#region Summary

Write-Host "`n📊 Summary:" -ForegroundColor Cyan
Write-Host "  Projects scanned: $($catalog.Projects.Count)" -ForegroundColor White

$totalClasses = ($catalog.Projects | ForEach-Object { $_.Classes.Count } | Measure-Object -Sum).Sum
$totalInterfaces = ($catalog.Projects | ForEach-Object { $_.Interfaces.Count } | Measure-Object -Sum).Sum
$totalEnums = ($catalog.Projects | ForEach-Object { $_.Enums.Count } | Measure-Object -Sum).Sum
$totalExtMethods = ($catalog.Projects | ForEach-Object { $_.ExtensionMethods.Count } | Measure-Object -Sum).Sum
$totalMethods = ($catalog.Projects | ForEach-Object { $_.Methods.Count } | Measure-Object -Sum).Sum
$totalProperties = ($catalog.Projects | ForEach-Object { $_.Properties.Count } | Measure-Object -Sum).Sum

Write-Host "  Total classes: $totalClasses" -ForegroundColor White
Write-Host "  Total interfaces: $totalInterfaces" -ForegroundColor White
Write-Host "  Total enums: $totalEnums" -ForegroundColor White
Write-Host "  Total extension methods: $totalExtMethods" -ForegroundColor White
Write-Host "  Total public methods: $totalMethods" -ForegroundColor White
Write-Host "  Total properties: $totalProperties" -ForegroundColor White

# Calculate XmlDoc coverage
$allItems = @()
$catalog.Projects | ForEach-Object {
    $allItems += $_.Classes
    $allItems += $_.Interfaces
    $allItems += $_.Enums
    $allItems += $_.ExtensionMethods
    $allItems += $_.Methods
    $allItems += $_.Properties
}

$withDocs = ($allItems | Where-Object { $_.Summary -and $_.Summary.Length -gt 0 }).Count
$coverage = if ($allItems.Count -gt 0) { [Math]::Round(($withDocs / $allItems.Count) * 100, 1) } else { 0 }

Write-Host "`n📝 XML Documentation Coverage:" -ForegroundColor Cyan
Write-Host "  Items with docs: $withDocs / $($allItems.Count) ($coverage%)" -ForegroundColor White

Write-Host "`n✨ Catalog generation complete!" -ForegroundColor Green

#endregion

