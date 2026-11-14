using System.Globalization;
using System.Windows.Media.TextFormatting;

namespace Domore.Windows.Controls;

internal sealed class LogTextSource : TextSource {
    private readonly string Text;
    private readonly TextRunProperties Properties;

    public LogTextSource(string text, TextRunProperties properties, double pixelsPerDip) {
        Text = text;
        Properties = properties;
        PixelsPerDip = pixelsPerDip;
    }

    public override TextRun GetTextRun(int index) {
        bool atEnd = index >= Text.Length;
        return atEnd ? new TextEndOfParagraph(1) : new TextCharacters(Text, index, Text.Length - index, Properties);
    }

    public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int index) {
        return new TextSpan<CultureSpecificCharacterBufferRange>(index,
            new CultureSpecificCharacterBufferRange(CultureInfo.CurrentUICulture,
                new CharacterBufferRange(Text, 0, index)));
    }

    public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int index) {
        return index;
    }
}
