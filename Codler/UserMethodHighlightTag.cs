using Microsoft.VisualStudio.Text.Tagging;

namespace Codler
{
    // Single tag class, chooses MarkerFormatDefinition based on isDefinition
    internal sealed class UserMethodHighlightTag : TextMarkerTag
    {
        public bool IsDefinition { get; }

        public UserMethodHighlightTag(bool isDefinition)
            : base(isDefinition
                  ? "MarkerFormatDefinition/UserMethodDefinition"
                  : "MarkerFormatDefinition/UserMethodInvocation")
        {
            IsDefinition = isDefinition;
        }
    }
}
