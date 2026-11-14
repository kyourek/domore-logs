using NUnit.Framework;
using System.Windows;
using System.Windows.Media;

namespace Domore.Windows.Controls;

[TestFixture]
public sealed class LogGlyphTableTest {
    private static LogGlyphTable Table(double emSize) {
        var typeface = new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        bool hasGlyphTypeface = typeface.TryGetGlyphTypeface(out var glyphTypeface);
        if (hasGlyphTypeface == false) {
            Assert.Ignore("Consolas is not installed.");
        }
        return new LogGlyphTable(glyphTypeface, emSize);
    }

    [Test]
    public void MapsCharactersToFontGlyphs() {
        var table = Table(10);
        bool mapped = table.TryGet('A', out var index, out var advance);
        Assert.That(mapped, Is.True);
        Assert.That(index, Is.EqualTo(table.Typeface.CharacterToGlyphMap['A']));
        Assert.That(advance, Is.EqualTo(table.Typeface.AdvanceWidths[index] * 10));
    }

    [Test]
    public void RepeatedLookupsReturnSameGlyph() {
        var table = Table(12);
        table.TryGet('z', out var first, out var firstAdvance);
        table.TryGet('z', out var second, out var secondAdvance);
        Assert.That(second, Is.EqualTo(first));
        Assert.That(secondAdvance, Is.EqualTo(firstAdvance));
    }

    [Test]
    public void SurrogatesAreUnmapped() {
        var table = Table(12);
        Assert.That(table.TryGet('\uD83D', out _, out _), Is.False);
    }

    [Test]
    public void CharactersMissingFromFontAreUnmapped() {
        var table = Table(12);
        Assert.That(table.TryGet('', out _, out _), Is.False);
    }
}
