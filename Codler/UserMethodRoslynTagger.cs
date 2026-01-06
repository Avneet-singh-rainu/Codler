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
        private readonly List<(SnapshotSpan Span, bool IsDefinition)> _spans = new();

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public UserMethodTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _buffer.Changed += (_, __) => Recompute();
            Recompute();
        }

        private void Recompute()
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
                        true
                    ));
                }
            }

            // Method invocations
            foreach (var call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                SimpleNameSyntax name = call.Expression switch
                {
                    IdentifierNameSyntax i => i,
                    MemberAccessExpressionSyntax m => m.Name,
                    _ => null
                };

                if (name == null) continue;

                var sym = model.GetSymbolInfo(name).Symbol as IMethodSymbol;
                if (sym?.Locations.Any(l => l.IsInSource) == true)
                {
                    _spans.Add((
                        new SnapshotSpan(snapshot, name.Identifier.Span.Start, name.Identifier.Span.Length),
                        false
                    ));
                }
            }

            TagsChanged?.Invoke(
                this,
                new SnapshotSpanEventArgs(
                    new SnapshotSpan(snapshot, 0, snapshot.Length)));
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
