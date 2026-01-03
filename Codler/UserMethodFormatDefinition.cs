using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace Codler
{
    [Export(typeof(EditorFormatDefinition))]
    [Name("MarkerFormatDefinition/UserMethodDefinition")]
    [UserVisible(true)]
    internal sealed class UserMethodDefinitionFormat : MarkerFormatDefinition
    {
        public UserMethodDefinitionFormat()
        {
            BackgroundColor = Color.FromRgb(255, 0, 102); // pink
            ForegroundColor = Colors.White;
            DisplayName = "User Method Definition";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name("MarkerFormatDefinition/UserMethodInvocation")]
    [UserVisible(true)]
    internal sealed class UserMethodInvocationFormat : MarkerFormatDefinition
    {
        public UserMethodInvocationFormat()
        {
            BackgroundColor = Colors.Yellow;
            ForegroundColor = Colors.Black;
            DisplayName = "User Method Invocation";
        }
    }
}
