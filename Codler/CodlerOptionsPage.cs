using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace Codler
{
    [ComVisible(true)]
    public class CodlerOptionsPage : DialogPage
    {

        public static CodlerOptionsPage Get()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (CodlerPackage.Instance == null)
                return new CodlerOptionsPage(); // DEFAULTS

            return (CodlerOptionsPage)
                CodlerPackage.Instance.GetDialogPage(typeof(CodlerOptionsPage));
        }


        // -------------------------
        // General
        // -------------------------

        private bool _enableHighlighting = true;

        [Category("General")]
        [DisplayName("Enable User Method Highlighting")]
        [Description("Enable or disable Codler user method highlighting.")]
        public bool EnableHighlighting
        {
            get => _enableHighlighting;
            set => _enableHighlighting = value;
        }

        // -------------------------
        // Font
        // -------------------------

        private int _fontSize = 16;

        [Category("Font")]
        [DisplayName("Font Size")]
        [Description("Font size for user-defined methods.")]
        public int FontSize
        {
            get => _fontSize;
            set => _fontSize = Math.Max(8, value);
        }

        private string _fontFamily = "Consolas";

        [Category("Font")]
        [DisplayName("Font Family")]
        [Description("Font family for user-defined methods.")]
        [TypeConverter(typeof(FontFamilyConverter))]
        public string FontFamily
        {
            get => _fontFamily;
            set => _fontFamily = value;
        }

        private bool _bold = true;

        [Category("Font")]
        [DisplayName("Bold")]
        [Description("Render user methods in bold.")]
        public bool Bold
        {
            get => _bold;
            set => _bold = value;
        }

        private bool _italic = false;

        [Category("Font")]
        [DisplayName("Italic")]
        [Description("Render user methods in italic.")]
        public bool Italic
        {
            get => _italic;
            set => _italic = value;
        }

        // -------------------------
        // Colors
        // -------------------------

        private Color _definitionForegroundColor = Colors.LightGreen;

        [Category("Colors - Method Definitions")]
        [DisplayName("Definition Foreground Color")]
        [Description("Text color for user-defined method definitions.")]
        [TypeConverter(typeof(ColorConverter))]
        public Color DefinitionForegroundColor
        {
            get => _definitionForegroundColor;
            set => _definitionForegroundColor = value;
        }

        private Color _invocationForegroundColor = Colors.Yellow;

        [Category("Colors - Method Invocations")]
        [DisplayName("Invocation Foreground Color")]
        [Description("Text color for user-defined method invocations.")]
        [TypeConverter(typeof(ColorConverter))]
        public Color InvocationForegroundColor
        {
            get => _invocationForegroundColor;
            set => _invocationForegroundColor = value;
        }

        private Color _foregroundColor = Colors.Yellow;

        [Category("Colors")]
        [DisplayName("Foreground Color (Legacy)")]
        [Description("Legacy text color for user-defined methods.")]
        [TypeConverter(typeof(ColorConverter))]
        public Color ForegroundColor
        {
            get => _foregroundColor;
            set => _foregroundColor = value;
        }

        private Color _backgroundColor = Colors.Transparent;

        [Category("Colors")]
        [DisplayName("Background Color")]
        [Description("Background color for user-defined methods.")]
        [TypeConverter(typeof(ColorConverter))]
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set => _backgroundColor = value;
        }

        private double _opacity = 1.0;

        [Category("Colors")]
        [DisplayName("Opacity")]
        [Description("Opacity of the text (0.1 – 1.0).")]
        public double Opacity
        {
            get => _opacity;
            set => _opacity = Clamp(value, 0.1, 1.0);
        }

        private int opacityPercent = 35;

        [Category("Colors - Method Definitions")]
        [DisplayName("Definition Opacity (%)")]
        [Description("Opacity of user method definition highlight (10 - 100)")]
        public int DefinitionOpacityPercent
        {
            get { return opacityPercent; }
            set { opacityPercent = value; }
        }

        private int invocationOpacityPercent = 35;

        [Category("Colors - Method Invocations")]
        [DisplayName("Invocation Opacity (%)")]
        [Description("Opacity of user method invocation highlight (10 - 100)")]
        public int InvocationOpacityPercent
        {
            get { return invocationOpacityPercent; }
            set { invocationOpacityPercent = value; }
        }

        private int legacyOpacityPercent = 35;

        [Category("User Method Highlight")]
        [DisplayName("Highlight Opacity (%) [Legacy]")]
        [Description("Legacy opacity of user method highlight (10 - 100)")]
        public int OpacityPercent
        {
            get { return legacyOpacityPercent; }
            set { legacyOpacityPercent = value; }
        }


        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

    }

    // Custom font family converter with dropdown values
    public class FontFamilyConverter : StringConverter
    {
        private static readonly string[] CommonFonts = new[]
        {
            "Arial", "Calibri", "Cambria", "Candara", "Consolas", "Constantia", "Corbel", "Courier New",
            "Ebrima", "Franklin Gothic Medium", "Gabriola", "Georgia", "Impact", "Lucida Console",
            "Lucida Sans Unicode", "Microsoft Sans Serif", "Palatino Linotype", "Segoe UI",
            "Segoe UI Light", "Segoe UI Semibold", "Segoe UI Symbol", "Tahoma", "Times New Roman",
            "Trebuchet MS", "Verdana", "Webdings", "Wingdings"
        };

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(CommonFonts);
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false; // Allow custom font names
        }
    }
}
