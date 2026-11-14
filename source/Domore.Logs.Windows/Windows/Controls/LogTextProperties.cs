using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Domore.Windows.Controls;

internal sealed class LogTextProperties : TextRunProperties {
    public override Typeface Typeface { get; }
    public override double FontRenderingEmSize { get; }
    public override double FontHintingEmSize => FontRenderingEmSize;
    public override TextDecorationCollection TextDecorations => null;
    public override Brush ForegroundBrush { get; }
    public override Brush BackgroundBrush => null;
    public override CultureInfo CultureInfo => CultureInfo.CurrentUICulture;
    public override TextEffectCollection TextEffects => null;

    public LogTextProperties(Typeface typeface, double size, Brush brush, double pixelsPerDip) {
        PixelsPerDip = pixelsPerDip;
        Typeface = typeface;
        FontRenderingEmSize = size;
        ForegroundBrush = brush;
    }
}
