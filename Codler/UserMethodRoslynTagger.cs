using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Codler
{
    internal sealed class UserMethodTagger : ITagger<UserMethodHighlightTag>
    {
        private readonly ITextBuffer _buffer;
        private List<(SnapshotSpan span, bool isDefinition)> _spans = new();

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public UserMethodTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _buffer.Changed += BufferChanged;
            RecomputeHighlights();
        }

        private void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            RecomputeHighlights();

            TagsChanged?.Invoke(this,
                new SnapshotSpanEventArgs(new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length)));
        }

        public IEnumerable<ITagSpan<UserMethodHighlightTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var t in _spans)
            {
                yield return new TagSpan<UserMethodHighlightTag>(t.span, new UserMethodHighlightTag(t.isDefinition));
            }
        }

        private void RecomputeHighlights()
        {
            _spans.Clear();

            var snapshot = _buffer.CurrentSnapshot;
            var code = snapshot.GetText();

            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("Temp")
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(tree);

            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();

            // 1️⃣ Method definitions
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                _spans.Add((new SnapshotSpan(snapshot, method.Identifier.Span.Start, method.Identifier.Span.Length), true));
            }

            // 2️⃣ Constructor definitions
            foreach (var ctor in root.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
            {
                _spans.Add((new SnapshotSpan(snapshot, ctor.Identifier.Span.Start, ctor.Identifier.Span.Length), true));
            }

            // 3️⃣ User-defined method calls
            foreach (var call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                SimpleNameSyntax id = call.Expression switch
                {
                    IdentifierNameSyntax i => i,
                    MemberAccessExpressionSyntax m => m.Name,
                    _ => null
                };
                if (id == null) continue;

                var sym = model.GetSymbolInfo(id).Symbol as IMethodSymbol;
                if (sym == null) continue;

                if (sym.Locations.Any(l => l.IsInSource))
                {
                    _spans.Add((new SnapshotSpan(snapshot, id.Identifier.Span.Start, id.Identifier.Span.Length), false));
                }
            }
        }
    }
}
