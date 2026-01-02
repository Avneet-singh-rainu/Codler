using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace Codler
{
    /// <summary>
    /// Classification type definition export for Codler
    /// </summary>
    internal static class CodlerClassificationDefinition
    {
        // This disables "The field is never used" compiler's warning. Justification: the field is used by MEF.
#pragma warning disable 169

        /// <summary>
        /// Defines the "Codler" classification type.
        /// </summary>
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("Codler")]
        private static ClassificationTypeDefinition typeDefinition;

#pragma warning restore 169
    }
}
