using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

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
                ThreadHelper.ThrowIfNotOnUIThread();

                layer.RemoveAllAdornments();

                CodlerOptionsPage options = null;
                try
                {
                    options = CodlerOptionsPage.Get();
                }
                catch
                {
                    // If we can't get options, just don't render
                    return;
                }

                if (options == null || !options.EnableHighlighting)
                    return;

                var snapshot = view.TextSnapshot;
                var full = new SnapshotSpan(snapshot, 0, snapshot.Length);

                try
                {
                    foreach (var tag in aggregator.GetTags(full))
                    {
                        foreach (var span in tag.Span.GetSpans(snapshot))
                        {
                            var geo = view.TextViewLines.GetMarkerGeometry(span);
                            if (geo == null) continue;

                            var isDefinition = tag.Tag.IsDefinition;
                            var foregroundColor = isDefinition
                                ? options.DefinitionForegroundColor
                                : options.InvocationForegroundColor;
                            var opacityPercent = isDefinition
                                ? options.DefinitionOpacityPercent
                                : options.InvocationOpacityPercent;

                            var brush = new SolidColorBrush(foregroundColor)
                            {
                                Opacity = Clamp(opacityPercent / 100.0, 0.1, 1.0)
                            };
                            brush.Freeze();

                            var rect = new Rectangle
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
                catch (Exception)
                {
                    // Ignore errors during rendering
                }
            }

            aggregator.TagsChanged += (_, __) =>
            {
                try
                {
                    // Use Invoke instead of BeginInvoke to avoid threading warnings
                    if (view.VisualElement.Dispatcher.CheckAccess())
                    {
                        Refresh();
                    }
                    else
                    {
                        _ = view.VisualElement.Dispatcher.InvokeAsync(Refresh,
                            System.Windows.Threading.DispatcherPriority.Background);
                    }
                }
                catch { }
            };

            view.LayoutChanged += (_, __) =>
            {
                try
                {
                    Refresh();
                }
                catch { }
            };

            view.VisualElement.Loaded += (_, __) =>
            {
                try
                {
                    Refresh();
                }
                catch { }
            };
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
