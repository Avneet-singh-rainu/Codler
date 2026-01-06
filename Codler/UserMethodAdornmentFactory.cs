using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Media;

namespace Codler
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("CSharp")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class UserMethodAdornmentFactory : IWpfTextViewCreationListener
    {
        [Import]
        internal IViewTagAggregatorFactoryService TagAggregatorFactory;

        public void TextViewCreated(IWpfTextView view)
        {
            var layer = view.GetAdornmentLayer("UserMethodAdornmentLayer");
            var aggregator = TagAggregatorFactory.CreateTagAggregator<UserMethodHighlightTag>(view);
            void Refresh()
            {
                layer.RemoveAllAdornments();

                var options = CodlerOptionsPage.Get();

                var snapshot = view.TextSnapshot;
                var full = new SnapshotSpan(snapshot, 0, snapshot.Length);

                foreach (var tag in aggregator.GetTags(full))
                {
                    foreach (var span in tag.Span.GetSpans(snapshot))
                    {
                        var geo = view.TextViewLines.GetMarkerGeometry(span);
                        if (geo == null) continue;

                        var brush = new SolidColorBrush(options.ForegroundColor)
                        {
                            Opacity = Clamp(options.OpacityPercent / 100.0, 0.1, 1.0)
                        };
                        brush.Freeze();

                        var rect = new System.Windows.Shapes.Rectangle
                        {
                            Width = geo.Bounds.Width,
                            Height = geo.Bounds.Height,
                            Fill = brush,
                            RadiusX = 2,
                            RadiusY = 2,
                            IsHitTestVisible = false
                        };

                        Canvas.SetLeft(rect, geo.Bounds.Left);
                        Canvas.SetTop(rect, geo.Bounds.Top);

                        layer.AddAdornment(
                            AdornmentPositioningBehavior.TextRelative,
                            span,
                            null,
                            rect,
                            null);
                    }
                }
            }

            aggregator.TagsChanged += (_, __) => Refresh();
            view.LayoutChanged += (_, __) => Refresh();
            view.VisualElement.Loaded += (_, __) => Refresh();
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
