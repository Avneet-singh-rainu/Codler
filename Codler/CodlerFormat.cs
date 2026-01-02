using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Win32;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace Codler
{
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Codler")]
    [Name("Codler")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class CodlerFormat : ClassificationFormatDefinition
    {
        public CodlerFormat()
        {
            this.DisplayName = "User-Defined Method";
            this.ForegroundColor = Colors.DarkBlue;
            this.BackgroundColor = Colors.Yellow;
            this.IsBold = true;
            this.BackgroundOpacity = 0.3;
        }
    }
}
