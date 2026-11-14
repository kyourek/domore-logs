using System.Windows;
using System.Windows.Media.TextFormatting;

namespace Domore.Windows.Controls;

internal sealed class LogParagraphProperties : TextParagraphProperties {
    public override FlowDirection FlowDirection => FlowDirection.LeftToRight;
    public override TextAlignment TextAlignment => TextAlignment.Left;
    public override double LineHeight => 0;
    public override bool FirstLineInParagraph => true;
    public override TextRunProperties DefaultTextRunProperties { get; }
    public override TextWrapping TextWrapping => TextWrapping.NoWrap;
    public override TextMarkerProperties TextMarkerProperties => null;
    public override double Indent => 0;

    public LogParagraphProperties(TextRunProperties properties) {
        DefaultTextRunProperties = properties;
    }
}
