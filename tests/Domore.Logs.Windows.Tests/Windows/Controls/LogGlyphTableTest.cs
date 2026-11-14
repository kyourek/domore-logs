using NUnit.Framework;
using System.Windows;
using System.Windows.Media;

namespace Domore.Windows.Controls;

[TestFixture]
public sealed class LogGlyphTableTest {
    private static LogGlyphTable Table(double emSize, string fontFamily = "Consolas") {
        var typeface = new Typeface(new FontFamily(fontFamily), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        bool hasGlyphTypeface = typeface.TryGetGlyphTypeface(out var glyphTypeface);
        if (hasGlyphTypeface == false) {
            Assert.Ignore(fontFamily + " is not installed.");
        }
        return new LogGlyphTable(glyphTypeface, emSize);
    }

    [TestCase(0x0633, TestName = "ArabicLetterRequiresShaping")]
    [TestCase(0x05D0, TestName = "HebrewLetterRequiresBidiLayout")]
    [TestCase(0x0301, TestName = "CombiningMarkRequiresPositioning")]
    [TestCase(0x200F, TestName = "BidiControlRequiresLayout")]
    public void CharactersRequiringLayoutAreUnmappedEvenWhenFontHasThem(int codePoint) {
        var table = Table(12, "Arial");
        Assert.That(table.TryGet((char)codePoint, out _, out _), Is.False);
    }

    [TestCase(0x0061)]
    [TestCase(0x00E9)]
    [TestCase(0x2014)]
    public void SimpleCharactersRemainMapped(int codePoint) {
        var table = Table(12, "Arial");
        Assert.That(table.TryGet((char)codePoint, out _, out _), Is.True);
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
