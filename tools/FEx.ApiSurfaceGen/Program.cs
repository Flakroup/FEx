using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FEx.ApiSurfaceGen;

// Roslyn-based API surface extractor. Replaces the regex extraction in Generate-ApiSurface.ps1.
// Parses each .cs file into a syntax tree and renders exact member signatures (default values,
// generic constraints, multi-line declarations, overloads) that regex cannot reliably capture.
internal static class Program
{
    private static int Main(string[] args)
    {
        var repoPath = GetArg(args, "-RepoPath");
        var outputDir = GetArg(args, "-OutputDir");

        if (string.IsNullOrEmpty(repoPath))
        {
            Console.Error.WriteLine("Usage: FEx.ApiSurfaceGen -RepoPath <repo> [-OutputDir <dir>]");
            return 1;
        }

        repoPath = Path.GetFullPath(repoPath);
        if (!Directory.Exists(repoPath))
        {
            Console.Error.WriteLine($"Repository not found: {repoPath}");
            return 1;
        }

        var repoName = new DirectoryInfo(repoPath).Name;
        if (string.IsNullOrEmpty(outputDir))
            outputDir = Path.Combine(AppContext.BaseDirectory, ".api-surface", repoName);

        outputDir = Path.GetFullPath(outputDir);
        Directory.CreateDirectory(outputDir);
        foreach (var stale in Directory.GetFiles(outputDir, "*.toml"))
            File.Delete(stale);

        Console.WriteLine($"Scanning {repoName}...");

        var projectFiles = Directory
            .EnumerateFiles(repoPath, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !IsExcluded(p))
            .OrderBy(p => Path.GetFileName(p), StringComparer.Ordinal)
            .ToList();

        Console.WriteLine($"Found {projectFiles.Count} projects");

        var totals = new Totals();

        foreach (var projectFile in projectFiles)
        {
            var projectDir = Path.GetDirectoryName(projectFile)!;
            var projectName = Path.GetFileNameWithoutExtension(projectFile);
            var tomlName = projectName.StartsWith("FEx.", StringComparison.Ordinal)
                ? projectName.Substring(4)
                : projectName;
            var tomlFile = Path.Combine(outputDir, tomlName + ".toml");

            var csFiles = Directory
                .EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Where(p => !IsExcluded(p))
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("[project]");
            sb.AppendLine($"name = {TomlString(projectName)}");
            var relProjectPath = RelativePath(projectDir, repoPath);
            sb.AppendLine($"path = {TomlString(relProjectPath)}");
            sb.AppendLine();

            var hasContent = false;

            foreach (var csFile in csFiles)
                hasContent |= ProcessFile(csFile, projectDir, sb, totals);

            if (hasContent)
                File.WriteAllText(tomlFile, sb.ToString(), new UTF8Encoding(false));
        }

        var gitHash = ReadGitHeadShort(repoPath);
        var meta = new StringBuilder();
        meta.AppendLine("[meta]");
        meta.AppendLine($"repo = {TomlString(repoName)}");
        meta.AppendLine($"generated = {TomlString(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}");
        meta.AppendLine($"git_commit = {TomlString(gitHash)}");
        meta.AppendLine($"classes = {totals.Classes}");
        meta.AppendLine($"interfaces = {totals.Interfaces}");
        meta.AppendLine($"enums = {totals.Enums}");
        meta.AppendLine($"extensions = {totals.Extensions}");
        meta.AppendLine($"ctors = {totals.Ctors}");
        meta.AppendLine($"methods = {totals.Methods}");
        meta.AppendLine($"properties = {totals.Properties}");
        File.WriteAllText(Path.Combine(outputDir, "_meta.toml"), meta.ToString(), new UTF8Encoding(false));

        Console.WriteLine();
        Console.WriteLine($"Summary ({repoName}):");
        Console.WriteLine($"  Classes:     {totals.Classes}");
        Console.WriteLine($"  Interfaces:  {totals.Interfaces}");
        Console.WriteLine($"  Enums:       {totals.Enums}");
        Console.WriteLine($"  Extensions:  {totals.Extensions}");
        Console.WriteLine($"  Ctors:       {totals.Ctors}");
        Console.WriteLine($"  Methods:     {totals.Methods}");
        Console.WriteLine($"  Properties:  {totals.Properties}");
        Console.WriteLine($"  Output:      {outputDir}");
        return 0;
    }

    private static bool ProcessFile(string filePath, string projectDir, StringBuilder toml, Totals totals)
    {
        string text;
        try { text = File.ReadAllText(filePath); }
        catch { return false; }

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var tree = CSharpSyntaxTree.ParseText(text);
        var root = tree.GetRoot();
        var relativePath = RelativePath(filePath, projectDir);
        var hasContent = false;

        foreach (var type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
        {
            // Match the legacy filter: only types explicitly public/internal reach the surface.
            if (!IsTypeVisible(type.Modifiers))
                continue;

            var ns = GetNamespace(type);

            switch (type)
            {
                case ClassDeclarationSyntax cls:
                    hasContent |= WriteType(toml, "classes", cls.Identifier.Text, ns,
                        BaseList(cls.BaseList), IsStatic(cls.Modifiers), relativePath, Summary(cls), totals);
                    hasContent |= WriteMembers(toml, cls, cls.Identifier.Text, isInterface: false, relativePath, totals);
                    break;

                case StructDeclarationSyntax st:
                    hasContent |= WriteType(toml, "classes", st.Identifier.Text, ns,
                        BaseList(st.BaseList), IsStatic(st.Modifiers), relativePath, Summary(st), totals);
                    hasContent |= WriteMembers(toml, st, st.Identifier.Text, isInterface: false, relativePath, totals);
                    break;

                case RecordDeclarationSyntax rec:
                    hasContent |= WriteType(toml, "classes", rec.Identifier.Text, ns,
                        BaseList(rec.BaseList), IsStatic(rec.Modifiers), relativePath, Summary(rec), totals);
                    hasContent |= WriteMembers(toml, rec, rec.Identifier.Text, isInterface: false, relativePath, totals);
                    break;

                case InterfaceDeclarationSyntax iface:
                    hasContent |= WriteType(toml, "interfaces", iface.Identifier.Text, ns,
                        BaseList(iface.BaseList), isStatic: false, relativePath, Summary(iface), totals);
                    hasContent |= WriteMembers(toml, iface, iface.Identifier.Text, isInterface: true, relativePath, totals);
                    break;

                case EnumDeclarationSyntax en:
                    hasContent |= WriteType(toml, "enums", en.Identifier.Text, ns,
                        baseList: "", isStatic: false, relativePath, Summary(en), totals);
                    break;
            }
        }

        return hasContent;
    }

    private static bool WriteType(StringBuilder toml, string section, string name, string ns,
        string baseList, bool isStatic, string file, string summary, Totals totals)
    {
        // Match the legacy filter: only public/internal types reach the surface.
        // (Nested types keep their declared accessibility; private nested types are skipped by the caller's modifier check below.)
        toml.AppendLine($"[[{section}]]");
        toml.AppendLine($"name = {TomlString(name)}");
        if (!string.IsNullOrEmpty(ns)) toml.AppendLine($"ns = {TomlString(ns)}");
        if (!string.IsNullOrEmpty(baseList)) toml.AppendLine($"base = {TomlString(baseList)}");
        if (!string.IsNullOrEmpty(summary)) toml.AppendLine($"summary = {TomlString(summary)}");
        if (!string.IsNullOrEmpty(file)) toml.AppendLine($"file = {TomlString(file)}");
        if (isStatic) toml.AppendLine("static = true");
        toml.AppendLine();

        switch (section)
        {
            case "classes": totals.Classes++; break;
            case "interfaces": totals.Interfaces++; break;
            case "enums": totals.Enums++; break;
        }

        return true;
    }

    private static bool WriteMembers(StringBuilder toml, TypeDeclarationSyntax type, string parent,
        bool isInterface, string file, Totals totals)
    {
        var any = false;

        foreach (var member in type.Members)
        {
            switch (member)
            {
                case ConstructorDeclarationSyntax ctor when IsApiVisible(ctor.Modifiers, isInterface):
                {
                    var sig = ctor.Identifier.Text + NormalizeWs(ctor.ParameterList.ToString());
                    toml.AppendLine("[[ctors]]");
                    toml.AppendLine($"parent = {TomlString(parent)}");
                    toml.AppendLine($"sig = {TomlString(sig)}");
                    AppendSummaryFile(toml, Summary(ctor), file);
                    if (IsStatic(ctor.Modifiers)) toml.AppendLine("static = true");
                    toml.AppendLine();
                    totals.Ctors++;
                    any = true;
                    break;
                }

                case MethodDeclarationSyntax method when IsApiVisible(method.Modifiers, isInterface):
                {
                    if (IsExtensionMethod(type, method))
                    {
                        var ret = method.ReturnType.ToString();
                        var extSig = $"{ret} {method.Identifier.Text}{method.TypeParameterList}" +
                                     NormalizeWs(method.ParameterList.ToString());
                        toml.AppendLine("[[extensions]]");
                        toml.AppendLine($"name = {TomlString(method.Identifier.Text)}");
                        toml.AppendLine($"returns = {TomlString(ret)}");
                        toml.AppendLine($"sig = {TomlString(extSig)}");
                        AppendSummaryFile(toml, Summary(method), file);
                        toml.AppendLine();
                        totals.Extensions++;
                        any = true;
                        break;
                    }

                    var sig = RenderMethod(method);
                    toml.AppendLine("[[methods]]");
                    toml.AppendLine($"parent = {TomlString(parent)}");
                    toml.AppendLine($"name = {TomlString(method.Identifier.Text)}");
                    toml.AppendLine($"sig = {TomlString(sig)}");
                    toml.AppendLine($"returns = {TomlString(method.ReturnType.ToString())}");
                    AppendSummaryFile(toml, Summary(method), file);
                    if (IsStatic(method.Modifiers)) toml.AppendLine("static = true");
                    toml.AppendLine();
                    totals.Methods++;
                    any = true;
                    break;
                }

                case PropertyDeclarationSyntax prop when IsApiVisible(prop.Modifiers, isInterface):
                {
                    var sig = RenderProperty(prop);
                    toml.AppendLine("[[properties]]");
                    toml.AppendLine($"parent = {TomlString(parent)}");
                    toml.AppendLine($"name = {TomlString(prop.Identifier.Text)}");
                    toml.AppendLine($"sig = {TomlString(sig)}");
                    toml.AppendLine($"returns = {TomlString(prop.Type.ToString())}");
                    AppendSummaryFile(toml, Summary(prop), file);
                    if (IsStatic(prop.Modifiers)) toml.AppendLine("static = true");
                    toml.AppendLine();
                    totals.Properties++;
                    any = true;
                    break;
                }
            }
        }

        return any;
    }

    private static string RenderMethod(MethodDeclarationSyntax method)
    {
        var sb = new StringBuilder();
        if (IsStatic(method.Modifiers)) sb.Append("static ");
        sb.Append(method.ReturnType.ToString());
        sb.Append(' ');
        sb.Append(method.Identifier.Text);
        if (method.TypeParameterList != null) sb.Append(method.TypeParameterList.ToString());
        sb.Append(NormalizeWs(method.ParameterList.ToString()));
        foreach (var c in method.ConstraintClauses)
        {
            sb.Append(' ');
            sb.Append(NormalizeWs(c.ToString()));
        }
        return NormalizeWs(sb.ToString());
    }

    private static string RenderProperty(PropertyDeclarationSyntax prop)
    {
        var sb = new StringBuilder();
        if (IsStatic(prop.Modifiers)) sb.Append("static ");
        sb.Append(prop.Type.ToString());
        sb.Append(' ');
        sb.Append(prop.Identifier.Text);
        sb.Append(' ');

        if (prop.AccessorList != null)
        {
            var accessors = prop.AccessorList.Accessors
                .Where(a => !a.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
                .Select(a => a.Keyword.Text + ";");
            sb.Append("{ ").Append(string.Join(" ", accessors)).Append(" }");
        }
        else
        {
            // Expression-bodied property (=> ...): read-only.
            sb.Append("{ get; }");
        }

        return NormalizeWs(sb.ToString());
    }

    // Members default to private; surface only those explicitly visible (public/internal/protected),
    // and interface members (implicitly public unless marked private).
    private static bool IsApiVisible(SyntaxTokenList modifiers, bool isInterface)
    {
        if (modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
            return false;
        if (modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)
                            || m.IsKind(SyntaxKind.InternalKeyword)
                            || m.IsKind(SyntaxKind.ProtectedKeyword)))
            return true;
        return isInterface;
    }

    private static bool IsExtensionMethod(TypeDeclarationSyntax type, MethodDeclarationSyntax method)
    {
        return type is ClassDeclarationSyntax
            && IsStatic(type.Modifiers)
            && method.ParameterList.Parameters.Count > 0
            && method.ParameterList.Parameters[0].Modifiers.Any(m => m.IsKind(SyntaxKind.ThisKeyword));
    }

    private static void AppendSummaryFile(StringBuilder toml, string summary, string file)
    {
        if (!string.IsNullOrEmpty(summary)) toml.AppendLine($"summary = {TomlString(summary)}");
        if (!string.IsNullOrEmpty(file)) toml.AppendLine($"file = {TomlString(file)}");
    }

    private static bool IsTypeVisible(SyntaxTokenList modifiers) =>
        modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.InternalKeyword));

    private static bool IsStatic(SyntaxTokenList modifiers) =>
        modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));

    private static string BaseList(BaseListSyntax baseList) =>
        baseList == null ? "" : NormalizeWs(string.Join(", ", baseList.Types.Select(t => t.ToString())));

    private static string GetNamespace(SyntaxNode node)
    {
        for (var current = node.Parent; current != null; current = current.Parent)
        {
            switch (current)
            {
                case FileScopedNamespaceDeclarationSyntax fs: return fs.Name.ToString();
                case NamespaceDeclarationSyntax ns: return ns.Name.ToString();
            }
        }
        return "";
    }

    private static string Summary(SyntaxNode node)
    {
        foreach (var trivia in node.GetLeadingTrivia())
        {
            if (!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                && !trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                continue;

            var structure = trivia.GetStructure();
            if (structure == null) continue;

            var summaryNode = structure.DescendantNodes()
                .OfType<XmlElementSyntax>()
                .FirstOrDefault(e => e.StartTag.Name.LocalName.Text == "summary");
            if (summaryNode == null) continue;

            var raw = string.Concat(summaryNode.Content.Select(c => c.ToString()));
            // Strip doc-comment leaders and inline tags, collapse whitespace.
            raw = System.Text.RegularExpressions.Regex.Replace(raw, @"^\s*///?", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            raw = System.Text.RegularExpressions.Regex.Replace(raw, @"<[^>]+>", " ");
            return NormalizeWs(raw);
        }
        return "";
    }

    private static string NormalizeWs(string value) =>
        string.IsNullOrEmpty(value)
            ? ""
            : System.Text.RegularExpressions.Regex.Replace(value, @"\s+", " ").Trim();

    private static string TomlString(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        var escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return "\"" + escaped + "\"";
    }

    private static string RelativePath(string fullPath, string root)
    {
        var rel = fullPath.Length > root.Length && fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            ? fullPath.Substring(root.Length)
            : fullPath;
        return rel.TrimStart('\\', '/');
    }

    private static readonly string[] ExcludeDirs = { "obj", "bin", "submodules", "samples", "TestResults" };

    private static bool IsExcluded(string path)
    {
        // Normalize to '/' so the check is separator-agnostic, then match whole path segments.
        var segments = path.Replace('\\', '/').Split('/');
        return segments.Any(s => ExcludeDirs.Contains(s, StringComparer.OrdinalIgnoreCase));
    }

    private static string GetArg(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return "";
    }

    private static string ReadGitHeadShort(string repoPath)
    {
        try
        {
            var gitDir = Path.Combine(repoPath, ".git");
            var head = File.ReadAllText(Path.Combine(gitDir, "HEAD")).Trim();
            string fullSha;
            if (head.StartsWith("ref: ", StringComparison.Ordinal))
            {
                var refPath = head.Substring(5);
                var refFile = Path.Combine(gitDir, refPath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(refFile))
                {
                    fullSha = File.ReadAllText(refFile).Trim();
                }
                else
                {
                    var packed = Path.Combine(gitDir, "packed-refs");
                    fullSha = "";
                    if (File.Exists(packed))
                    {
                        foreach (var line in File.ReadLines(packed))
                        {
                            if (line.Length == 0 || line[0] == '#' || line[0] == '^') continue;
                            var space = line.IndexOf(' ');
                            if (space > 0 && line.Substring(space + 1) == refPath)
                            {
                                fullSha = line.Substring(0, space);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                fullSha = head;
            }
            return fullSha.Length >= 7 ? fullSha.Substring(0, 7) : fullSha;
        }
        catch
        {
            return "unknown";
        }
    }

    private sealed class Totals
    {
        public int Classes;
        public int Interfaces;
        public int Enums;
        public int Extensions;
        public int Ctors;
        public int Methods;
        public int Properties;
    }
}
