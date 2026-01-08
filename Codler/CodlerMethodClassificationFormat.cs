using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace Codler
{
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "CodlerMethodDefinition")]
    [Name("CodlerMethodDefinition")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class CodlerMethodDefinitionFormat : ClassificationFormatDefinition
    {
        public CodlerMethodDefinitionFormat()
        {
            DisplayName = "Codler - Method Definition";
            var options = CodlerOptionsPage.Get();
            ForegroundColor = options.DefinitionForegroundColor;
            ForegroundOpacity = options.DefinitionOpacityPercent / 100.0;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "CodlerMethodInvocation")]
    [Name("CodlerMethodInvocation")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class CodlerMethodInvocationFormat : ClassificationFormatDefinition
    {
        public CodlerMethodInvocationFormat()
        {
            DisplayName = "Codler - Method Invocation";
            var options = CodlerOptionsPage.Get();
            ForegroundColor = options.InvocationForegroundColor;
            ForegroundOpacity = options.InvocationOpacityPercent / 100.0;
        }
    }

    internal static class ClassificationTypeDefinitions
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("CodlerMethodDefinition")]
        internal static ClassificationTypeDefinition CodlerMethodDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("CodlerMethodInvocation")]
        internal static ClassificationTypeDefinition CodlerMethodInvocation = null;
    }
}
