using Microsoft.VisualStudio.Text.Tagging;

namespace Codler
{
    internal sealed class UserMethodHighlightTag : TextMarkerTag
    {
        public bool IsDefinition { get; }

        public UserMethodHighlightTag(bool isDefinition)
            : base(isDefinition ? "CodlerUserMethodDefinition" : "CodlerUserMethodInvocation")
        {
            IsDefinition = isDefinition;
        }
    }
}
