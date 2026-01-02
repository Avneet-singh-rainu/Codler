using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Codler
{
    internal class Codler : IClassifier
    {
        private readonly IClassificationType type;
        private readonly ITextBuffer buffer;

        private SyntaxTree tree;
        private SemanticModel model;
        private List<TextSpan> highlightSpans = new List<TextSpan>();

        public Codler(
            IClassificationTypeRegistryService registry,
            ITextBuffer textBuffer)
        {
            type = registry.GetClassificationType("Codler");
            buffer = textBuffer;

            buffer.Changed += BufferChanged;
            Recompute();
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        // --------------------------------------------------------------------
        // FAST: Only classify spans from cached results
        // --------------------------------------------------------------------
        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var snapshot = span.Snapshot;
            var list = new List<ClassificationSpan>();

            foreach (var hs in highlightSpans)
            {
                if (hs.End < span.Start || hs.Start > span.End)
                    continue;

                list.Add(new ClassificationSpan(
                    new SnapshotSpan(snapshot, hs.Start, hs.Length),
                    type
                ));
            }

            return list;
        }

        // --------------------------------------------------------------------
        // Recompute Roslyn tree only when text actually changes
        // --------------------------------------------------------------------
        private void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            Recompute();

            // tell VS to re-color everything
            var snapshot = e.After;
            ClassificationChanged?.Invoke(
                this,
                new ClassificationChangedEventArgs(
                    new SnapshotSpan(snapshot, 0, snapshot.Length))
            );
        }

        // --------------------------------------------------------------------
        // Roslyn parsing + semantic analysis (expensive but done rarely)
        // --------------------------------------------------------------------
        private void Recompute()
        {
            string code = buffer.CurrentSnapshot.GetText();

            tree = CSharpSyntaxTree.ParseText(code);

            var compilation = CSharpCompilation.Create("Temp")
                .AddReferences(
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(tree);

            model = compilation.GetSemanticModel(tree);

            highlightSpans = GetHighlightSpans(tree, model);
        }

        // --------------------------------------------------------------------
        // Find all user methods + all calls
        // --------------------------------------------------------------------
        private List<TextSpan> GetHighlightSpans(SyntaxTree tree, SemanticModel model)
        {
            var spans = new List<TextSpan>();
            var root = tree.GetRoot();

            // 1. Method definitions
            foreach (var m in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                spans.Add(m.Identifier.Span);

            // 2. Constructor definitions
            foreach (var c in root.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
                spans.Add(c.Identifier.Span);

            // 3. Method calls (user-defined only)
            var calls = root.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var call in calls)
            {
                var expr = call.Expression;
                IdentifierNameSyntax id = expr as IdentifierNameSyntax;

                if (expr is MemberAccessExpressionSyntax m)
                    id = m.Name as IdentifierNameSyntax;

                if (id == null) continue;

                var sym = model.GetSymbolInfo(id).Symbol as IMethodSymbol;
                if (sym == null) continue;

                // user-defined only
                if (sym.Locations.Any(x => x.IsInSource))
                    spans.Add(id.Identifier.Span);
            }

            return spans;
        }
    }
}
