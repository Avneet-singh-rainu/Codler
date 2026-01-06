using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace Codler
{
    internal sealed class UserMethodAdornmentLayer
    {
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("UserMethodAdornmentLayer")]
        [Order(After = PredefinedAdornmentLayers.Selection)]
        public AdornmentLayerDefinition Definition;
    }
}
