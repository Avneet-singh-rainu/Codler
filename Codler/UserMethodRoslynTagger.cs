using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Codler
{
    internal sealed class UserMethodTagger : ITagger<UserMethodHighlightTag>
    {
        private readonly ITextBuffer _buffer;
        private readonly List<(SnapshotSpan Span, bool IsDefinition)> _spans = new();
        private static List<SyntaxTree> _solutionTrees = new();
        private static readonly object _syncLock = new object();
        private static bool _treesInitialized = false;
        private static HashSet<string> _userMethodNames = new();
        private static readonly HashSet<string> _frameworkMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Object base methods
            "ToString", "GetHashCode", "Equals", "GetType", "ReferenceEquals", "MemberwiseClone",
            
            // Disposable pattern
            "Dispose", "DisposeAsync",
            
            // Collection interface methods
            "Add", "Remove", "Clear", "Contains", "Count", "IndexOf", "Insert", "CopyTo",
            "GetEnumerator", "MoveNext", "Reset", "Current",
            
            // Dictionary methods
            "TryGetValue", "Get", "Set", "Keys", "Values",
            
            // LINQ methods
            "Select", "Where", "First", "FirstOrDefault", "Last", "LastOrDefault",
            "Single", "SingleOrDefault", "Any", "All", "Count", "Sum", "Min", "Max", "Average",
            "Take", "Skip", "TakeWhile", "SkipWhile", "Distinct", "Union", "Intersect", "Except",
            "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending", "GroupBy", "Join",
            "GroupJoin", "Concat", "Zip", "Aggregate", "ToList", "ToArray", "ToDictionary",
            "ToLookup", "AsEnumerable", "AsQueryable", "Cast", "OfType",
            
            // String methods
            "Parse", "TryParse", "Format", "Join", "Split", "Trim", "TrimStart", "TrimEnd",
            "ToUpper", "ToLower", "Substring", "Replace", "Remove", "Insert", "PadLeft", "PadRight",
            "StartsWith", "EndsWith", "Contains", "IndexOf", "LastIndexOf", "Compare", "CompareTo",
            
            // I/O methods
            "WriteLine", "Write", "ReadLine", "Read", "ReadAllText", "WriteAllText",
            "ReadAllLines", "WriteAllLines", "Open", "Close", "Flush", "Seek", "Position",
            
            // Task/async methods
            "Wait", "WaitAll", "WaitAny", "ContinueWith", "GetAwaiter", "GetResult",
            "FromResult", "Run", "Delay", "WhenAll", "WhenAny", "ConfigureAwait",
            
            // Event methods
            "Invoke", "BeginInvoke", "EndInvoke", "AddHandler", "RemoveHandler",
            
            // Comparison and equality
            "CompareTo", "Clone", "Copy", "DeepClone",
            
            // Math methods
            "Abs", "Acos", "Asin", "Atan", "Atan2", "Ceiling", "Cos", "Cosh", "Exp", "Floor",
            "Log", "Log10", "Max", "Min", "Pow", "Round", "Sign", "Sin", "Sinh", "Sqrt", "Tan", "Tanh",
            
            // DateTime methods
            "Now", "UtcNow", "Today", "Parse", "TryParse", "ToString", "Add", "AddDays", "AddHours",
            "AddMinutes", "AddMonths", "AddSeconds", "AddYears", "Subtract", "Compare", "Equals",
            
            // Reflection methods
            "GetMethod", "GetMethods", "GetProperty", "GetProperties", "GetField", "GetFields",
            "GetMember", "GetMembers", "Invoke", "CreateInstance", "GetType", "IsAssignableFrom",
            
            // Attribute methods
            "GetCustomAttribute", "GetCustomAttributes", "IsDefined",
            
            // Common framework patterns
            "Create", "Build", "Configure", "Setup", "Initialize", "Load", "Save", "Delete",
            "Find", "Search", "Filter", "Sort", "Validate", "Convert", "Transform", "Map"
        };

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public UserMethodTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _buffer.Changed += (_, __) => Recompute();

            // Initialize solution trees once for all instances
            if (!_treesInitialized)
            {
                LoadSolutionFiles();
                _treesInitialized = true;
            }

            Recompute();
        }

        private void LoadSolutionFiles()
        {
            try
            {
                // Get the file path from the buffer's properties
                string bufferFilePath = null;

                if (_buffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDoc))
                {
                    bufferFilePath = textDoc.FilePath;
                }

                if (string.IsNullOrEmpty(bufferFilePath))
                    return;

                // Find the current project directory and .csproj file
                var fileInfo = new FileInfo(bufferFilePath);
                var currentDir = fileInfo.Directory;
                string csprojPath = null;
                DirectoryInfo projectDir = null;

                // Walk up to find .csproj file
                for (int i = 0; i < 10 && currentDir != null; i++)
                {
                    var csprojFiles = currentDir.GetFiles("*.csproj");
                    if (csprojFiles.Any())
                    {
                        csprojPath = csprojFiles.First().FullName;
                        projectDir = currentDir;
                        break;
                    }
                    currentDir = currentDir.Parent;
                }

                if (projectDir == null)
                    return;

                var projectsToLoad = new HashSet<string> { projectDir.FullName };

                // Parse the .csproj file to find project references
                if (!string.IsNullOrEmpty(csprojPath))
                {
                    try
                    {
                        var csprojContent = File.ReadAllText(csprojPath);

                        // Simple regex to find project references
                        var projectRefPattern = @"<ProjectReference\s+Include=""([^""]*\.csproj)""";
                        var matches = Regex.Matches(csprojContent, projectRefPattern);

                        foreach (System.Text.RegularExpressions.Match match in matches)
                        {
                            var relativePath = match.Groups[1].Value;
                            var referencedProjectPath = Path.GetFullPath(Path.Combine(projectDir.FullName, relativePath));
                            var referencedProjectDir = Path.GetDirectoryName(referencedProjectPath);

                            if (Directory.Exists(referencedProjectDir))
                            {
                                projectsToLoad.Add(referencedProjectDir);
                            }
                        }
                    }
                    catch { }
                }

                // Load .cs files from current project and referenced projects
                lock (_syncLock)
                {
                    foreach (var projDir in projectsToLoad)
                    {
                        try
                        {
                            var csFiles = Directory.GetFiles(projDir, "*.cs", SearchOption.AllDirectories)
                                .Where(f => !f.Contains("\\bin\\") &&
                                           !f.Contains("\\obj\\") &&
                                           !f.Contains("\\.vs\\") &&
                                           !f.Contains("\\Debug\\") &&
                                           !f.Contains("\\Release\\") &&
                                           !f.Contains("\\packages\\") &&
                                           !f.Contains("\\node_modules\\"));

                            foreach (var csFile in csFiles)
                            {
                                if (!_solutionTrees.Any(t => t.FilePath == csFile))
                                {
                                    var code = File.ReadAllText(csFile);
                                    var tree = CSharpSyntaxTree.ParseText(code, path: csFile);
                                    _solutionTrees.Add(tree);

                                    // Cache user method names from this file
                                    var root = tree.GetRoot();
                                    var methodNames = root.DescendantNodes()
                                        .OfType<MethodDeclarationSyntax>()
                                        .Select(m => m.Identifier.Text)
                                        .Concat(root.DescendantNodes()
                                            .OfType<InterfaceDeclarationSyntax>()
                                            .SelectMany(i => i.Members.OfType<MethodDeclarationSyntax>())
                                            .Select(m => m.Identifier.Text));

                                    foreach (var name in methodNames)
                                    {
                                        _userMethodNames.Add(name);
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void Recompute()
        {
            lock (_syncLock)
            {
                _spans.Clear();

                try
                {
                    var snapshot = _buffer.CurrentSnapshot;
                    var code = snapshot.GetText();

                    // Parse the current file
                    var tree = CSharpSyntaxTree.ParseText(code);

                    // Build a compilation with all solution files
                    var compilation = BuildCompilationWithAllProjects(tree);

                    var model = compilation.GetSemanticModel(tree);
                    if (model == null) return;

                    var root = tree.GetRoot();

                    // Method + constructor definitions
                    foreach (var m in root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
                    {
                        var id = m switch
                        {
                            MethodDeclarationSyntax md => md.Identifier,
                            ConstructorDeclarationSyntax cd => cd.Identifier,
                            _ => default
                        };

                        if (id != default)
                        {
                            _spans.Add((
                                new SnapshotSpan(snapshot, id.Span.Start, id.Span.Length),
                                true // IsDefinition
                            ));
                        }
                    }

                    // Property definitions (with getters/setters)
                    foreach (var prop in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                    {
                        _spans.Add((
                            new SnapshotSpan(snapshot, prop.Identifier.Span.Start, prop.Identifier.Span.Length),
                            true // IsDefinition
                        ));
                    }

                    // Interface method definitions
                    foreach (var interfaceDecl in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
                    {
                        foreach (var method in interfaceDecl.Members.OfType<MethodDeclarationSyntax>())
                        {
                            _spans.Add((
                                new SnapshotSpan(snapshot, method.Identifier.Span.Start, method.Identifier.Span.Length),
                                true // IsDefinition
                            ));
                        }
                    }

                    // Method invocations - handle all types: static, instance, interface, abstract
                    foreach (var call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        SimpleNameSyntax name = null;

                        // Handle different invocation patterns
                        switch (call.Expression)
                        {
                            case IdentifierNameSyntax identifierName:
                                // Simple method call: MethodName()
                                name = identifierName;
                                break;

                            case MemberAccessExpressionSyntax memberAccess:
                                // Member access: object.Method() or Type.StaticMethod()
                                name = memberAccess.Name;
                                break;

                            case GenericNameSyntax genericName:
                                // Generic method: Method<T>()
                                name = genericName;
                                break;
                        }

                        if (name == null) continue;

                        // Try to get symbol info
                        var symbolInfo = model.GetSymbolInfo(name);
                        var sym = symbolInfo.Symbol as IMethodSymbol;

                        // If we have a resolved symbol, check if it's a user method
                        if (sym != null)
                        {
                            if (IsUserMethod(sym))
                            {
                                _spans.Add((
                                    new SnapshotSpan(snapshot, name.Identifier.Span.Start, name.Identifier.Span.Length),
                                    false // IsInvocation
                                ));
                            }
                        }
                        else
                        {
                            // If symbol can't be resolved, check if it's a known user method from our cache
                            var methodName = name.Identifier.Text;

                            // Only highlight if it's in our user method cache AND not a framework method
                            if (!_frameworkMethods.Contains(methodName))
                            {
                                lock (_syncLock)
                                {
                                    if (_userMethodNames.Contains(methodName))
                                    {
                                        _spans.Add((
                                            new SnapshotSpan(snapshot, name.Identifier.Span.Start, name.Identifier.Span.Length),
                                            false // IsInvocation
                                        ));
                                    }
                                }
                            }
                        }
                    }

                    // Object creation expressions (constructors)
                    foreach (var objCreation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
                    {
                        var typeName = objCreation.Type;
                        if (typeName != null)
                        {
                            var sym = model.GetSymbolInfo(objCreation).Symbol as IMethodSymbol;
                            if (sym != null && IsUserMethod(sym))
                            {
                                // Highlight the type name in 'new MyClass()'
                                var span = typeName.Span;
                                _spans.Add((
                                    new SnapshotSpan(snapshot, span.Start, span.Length),
                                    false // IsInvocation
                                ));
                            }
                        }
                    }
                }
                catch
                {
                    // Silently ignore errors during analysis
                }

                TagsChanged?.Invoke(
                    this,
                    new SnapshotSpanEventArgs(
                        new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length)));
            }
        }

        private CSharpCompilation BuildCompilationWithAllProjects(SyntaxTree currentTree)
        {
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            };

            try
            {
                var systemRuntime = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
                if (systemRuntime != null && !systemRuntime.IsDynamic && !string.IsNullOrEmpty(systemRuntime.Location))
                {
                    references.Add(MetadataReference.CreateFromFile(systemRuntime.Location));
                }
            }
            catch { }

            // Add currently loaded assemblies from the app domain (which may include user's project assemblies)
            try
            {
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                    .Where(a =>
                    {
                        var name = a.GetName().Name;
                        // Exclude framework assemblies but include user assemblies
                        return !name.StartsWith("System.") &&
                               !name.StartsWith("Microsoft.") &&
                               !name.Equals("mscorlib") &&
                               !name.Equals("netstandard");
                    });

                foreach (var assembly in loadedAssemblies)
                {
                    try
                    {
                        references.Add(MetadataReference.CreateFromFile(assembly.Location));
                    }
                    catch { }
                }
            }
            catch { }

            var syntaxTrees = new List<SyntaxTree> { currentTree };

            // Add all loaded solution trees (now only current project)
            lock (_syncLock)
            {
                syntaxTrees.AddRange(_solutionTrees);
            }

            // Remove duplicates based on file path
            syntaxTrees = syntaxTrees
                .GroupBy(t => t.FilePath ?? "")
                .Select(g => g.First())
                .ToList();

            return CSharpCompilation.Create("ProjectAnalysis")
                .AddReferences(references)
                .AddSyntaxTrees(syntaxTrees);
        }


        private bool IsUserMethod(IMethodSymbol method)
        {
            if (method == null) return false;

            // Exclude compiler-generated methods (e.g., backing fields, anonymous methods)
            if (method.IsImplicitlyDeclared) return false;

            // Check the method and all its related definitions
            var methodsToCheck = new List<IMethodSymbol> { method };

            // Add original definition
            if (method.OriginalDefinition != null && !SymbolEqualityComparer.Default.Equals(method.OriginalDefinition, method))
            {
                methodsToCheck.Add(method.OriginalDefinition);
            }

            // Add ALL interface methods this method implements or is part of
            if (method.ContainingType != null)
            {
                // If this is an interface method, use it directly
                if (method.ContainingType.TypeKind == TypeKind.Interface)
                {
                    // Already an interface method
                }
                else
                {
                    // Find all interfaces and their methods that this implements
                    foreach (var iface in method.ContainingType.AllInterfaces)
                    {
                        foreach (var ifaceMethod in iface.GetMembers().OfType<IMethodSymbol>())
                        {
                            var implementation = method.ContainingType.FindImplementationForInterfaceMember(ifaceMethod);
                            if (implementation != null && SymbolEqualityComparer.Default.Equals(method, implementation))
                            {
                                // This method implements the interface method
                                methodsToCheck.Add(ifaceMethod);
                            }
                        }
                    }
                }
            }

            // Add overridden methods (for abstract/virtual methods)
            var overriddenMethod = method.OverriddenMethod;
            while (overriddenMethod != null)
            {
                methodsToCheck.Add(overriddenMethod);
                overriddenMethod = overriddenMethod.OverriddenMethod;
            }

            // Check if ANY of these methods has source code in the solution
            foreach (var m in methodsToCheck)
            {
                if (m.Locations.Any(l => l.IsInSource))
                {
                    // Found source code, now check if it's user code (not framework)
                    var methodNamespace = m.ContainingNamespace?.ToDisplayString() ?? "";

                    // Exclude if namespace starts with System
                    if (methodNamespace.StartsWith("System", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Exclude if namespace starts with Microsoft framework namespaces
                    var excludedNamespacePrefixes = new[]
                    {
                        "Microsoft.CodeAnalysis",
                        "Microsoft.CSharp",
                        "Microsoft.VisualBasic",
                        "Microsoft.VisualStudio",
                        "Microsoft.Win32",
                        "Microsoft.Build",
                        "Microsoft.Extensions.DependencyInjection",
                        "Microsoft.Extensions.Logging",
                        "Microsoft.Extensions.Configuration",
                        "Microsoft.Extensions.Options",
                        "Microsoft.AspNetCore",
                        "Microsoft.EntityFrameworkCore"
                    };

                    bool isFramework = false;
                    foreach (var prefix in excludedNamespacePrefixes)
                    {
                        if (methodNamespace.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            isFramework = true;
                            break;
                        }
                    }

                    if (!isFramework)
                    {
                        // It's user code with source!
                        return true;
                    }
                }
            }

            // No source found or all were framework
            return false;
        }

        public IEnumerable<ITagSpan<UserMethodHighlightTag>> GetTags(
            NormalizedSnapshotSpanCollection spans)
        {
            foreach (var item in _spans)
            {
                yield return new TagSpan<UserMethodHighlightTag>(
                    item.Span,
                    new UserMethodHighlightTag(item.IsDefinition));
            }
        }
    }
}
