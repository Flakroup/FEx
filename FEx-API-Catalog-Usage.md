# FEx API Catalog - Instrukcja Użytkowania

## 🎯 Cel

Ten katalog zawiera **kompletną dokumentację API frameworka FEx** w formatach przyjaznych dla AI i ludzi, umożliwiając agentom AI szybkie odnalezienie dostępnych funkcjonalności bez konieczności przeszukiwania całego kodu źródłowego.

## 📁 Pliki

| Plik | Format | Przeznaczenie |
|------|--------|---------------|
| `FEx-API-Catalog.md` | Markdown | Czytelny dla ludzi, łatwy do załadowania przez AI |
| `FEx-API-Catalog.json` | JSON | Strukturalny, łatwy do parsowania programistycznego |
| `Generate-FExCatalog.ps1` | PowerShell | Skrypt do regeneracji katalogu |

## 🤖 Użycie z AI Agentami

### Metoda 1: Direct File Load (Cursor/Claude)

W projekcie, który referencjonuje FEx, podaj agentowi:

```
@FEx-API-Catalog.md

Pracuję nad projektem, który używa frameworka FEx. 
Przed implementacją nowej funkcjonalności, sprawdź czy FEx już to udostępnia.
```

### Metoda 2: Explicit Context (GitHub Copilot)

```
// Reference: X:\GitLab\Flakroup\FEx\FEx-API-Catalog.md
// Before implementing, check if FEx provides this functionality
```

### Metoda 3: Project-Specific Instructions

Stwórz `.cursorrules` lub `.github/copilot-instructions.md`:

```markdown
# Project Rules

This project uses **FEx Framework**.

**FEx API Catalog:** X:\GitLab\Flakroup\FEx\FEx-API-Catalog.md

**Instructions:**
1. Before implementing utilities, check FEx catalog
2. Prefer FEx extension methods over custom implementations
3. Reference FEx namespaces appropriately
```

## 🔄 Regeneracja Katalogu

Po dodaniu nowej funkcjonalności do FEx:

```powershell
# W głównym katalogu FEx
.\Generate-FExCatalog.ps1

# Z verbose logging
.\Generate-FExCatalog.ps1 -Verbose

# Custom output paths
.\Generate-FExCatalog.ps1 -OutputJson "api.json" -OutputMarkdown "api.md"
```

## 📊 Statystyki Aktualnego Katalogu

```
Projekty: 43
Klasy: 2,880+
Interfejsy: 632+
Enumeracje: 136+
Metody Extension: 2,373+
```

## 💡 Przykłady Użycia

### Przykład 1: String Manipulation

**Przed:**
```csharp
// Agent AI może zaproponować własną implementację
public bool IsValidEmail(string email) 
{
    return Regex.IsMatch(email, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");
}
```

**Po (z katalogiem):**
```csharp
using Flakroup.FEx.AgnosticsAbstractions.Extensions;

// Agent AI wie, że FEx ma StringExtensions.IsValidEmail()
public bool IsValidEmail(string email) 
{
    return email.IsValidEmail();
}
```

### Przykład 2: Collection Operations

**Przed:**
```csharp
// Agent AI implementuje własną logikę
var distinctItems = myList
    .GroupBy(x => x.Id)
    .Select(g => g.First())
    .ToList();
```

**Po (z katalogiem):**
```csharp
using Flakroup.FEx.AgnosticsAbstractions.Extensions;

// Agent AI używa FEx CollectionExtensions
var distinctItems = myList.DistinctBy(x => x.Id).ToList();
```

### Przykład 3: Async Operations

**Przed:**
```csharp
// Agent AI może nie wiedzieć o FEx async helpers
await Task.WhenAll(items.Select(async item => await ProcessAsync(item)));
```

**Po (z katalogiem):**
```csharp
using Flakroup.FEx.Asyncx;

// Agent AI używa FEx AsyncHelper
await items.ForEachAsync(ProcessAsync);
```

## 🎓 Best Practices

### Dla Developerów

1. **Regeneruj katalog regularnie** - po każdym merge do main branch
2. **Commituj katalog do repo** - zespół ma aktualne info
3. **Dodawaj XML docs** - skrypt wyciąga dokumentację z `/// <summary>`
4. **Review before implement** - sprawdź katalog przed pisaniem utility

### Dla AI Agentów

1. **Load catalog first** - załaduj plik na początku sesji
2. **Search before code** - przeszukaj katalog przed implementacją
3. **Prefer FEx APIs** - używaj FEx zamiast reinvent the wheel
4. **Suggest improvements** - jeśli brakuje funkcjonalności, zasugeruj dodanie

## 🔧 Integracja z CI/CD

### GitHub Actions / GitLab CI

```yaml
# .github/workflows/update-catalog.yml
name: Update FEx API Catalog

on:
  push:
    branches: [main, master]
    paths:
      - 'src/**/*.cs'

jobs:
  update-catalog:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Generate Catalog
        shell: pwsh
        run: .\Generate-FExCatalog.ps1
      
      - name: Commit Changes
        run: |
          git config --local user.name "GitHub Action"
          git config --local user.email "action@github.com"
          git add FEx-API-Catalog.*
          git diff --staged --quiet || git commit -m "🤖 Auto-update API catalog"
          git push
```

### Pre-commit Hook

```powershell
# .git/hooks/pre-commit (PowerShell version)
#!/usr/bin/env pwsh

$changedFiles = git diff --cached --name-only --diff-filter=ACM | Where-Object { $_ -match '\.cs$' }

if ($changedFiles) {
    Write-Host "🔄 C# files changed, regenerating API catalog..."
    .\Generate-FExCatalog.ps1
    git add FEx-API-Catalog.*
}
```

## 🚀 Advanced: Semantic Search

Dla zaawansowanych użytkowników, możesz dodać semantic search:

```powershell
# Search-FExAPI.ps1
param([string]$Query)

$catalog = Get-Content "FEx-API-Catalog.json" | ConvertFrom-Json

$catalog.Projects.ExtensionMethods | 
    Where-Object { $_.MethodName -like "*$Query*" -or $_.Summary -like "*$Query*" } |
    Format-Table MethodName, TargetType, Summary -AutoSize
```

Użycie:
```powershell
.\Search-FExAPI.ps1 -Query "email"
.\Search-FExAPI.ps1 -Query "async"
```

## 📞 Support

W przypadku problemów:
1. Sprawdź czy skrypt działa: `.\Generate-FExCatalog.ps1 -Verbose`
2. Upewnij się, że PowerShell >= 7.0
3. Zweryfikuj strukturę projektów w `src/`

---

**Ostatnia aktualizacja:** 2025-10-26  
**Wersja:** 1.0

