using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace Domore.Windows.Controls;

/*
 * Maps characters to glyph indices and scaled advance widths. Lookups are
 * array reads in pages of 256 characters that are filled on first use, which
 * avoids the per-character dictionary lookups of the font's own maps.
 */
internal sealed class LogGlyphTable {
    private const int PageSize = 256;
    private const byte Unknown = 0;
    private const byte Mapped = 1;
    private const byte Unmapped = 2;
    private const char SoftHyphen = (char)0x00AD;

    private readonly Page[] Pages = new Page[(char.MaxValue + 1) / PageSize];
    private readonly IDictionary<int, ushort> CharacterMap;
    private readonly IDictionary<ushort, double> AdvanceWidths;
    private readonly double EmSize;

    private sealed class Page {
        public byte[] States { get; } = new byte[PageSize];
        public ushort[] Indices { get; } = new ushort[PageSize];
        public double[] Advances { get; } = new double[PageSize];
    }

    /*
     * Positioning glyphs one per character is only correct for text that
     * needs no shaping, reordering, or mark positioning, even when the font
     * maps every character. Latin, IPA, spacing modifier letters, and common
     * punctuation qualify. Other characters, including surrogates, marks,
     * bidirectional controls, and the soft hyphen, whose display depends on
     * line breaking, are left to WPF text layout.
     */
    private static bool IsSimple(char c) {
        bool basic = c >= 0x0020 && c < 0x0300 && c != SoftHyphen;
        bool punctuation = (c >= 0x2010 && c <= 0x2027) || (c >= 0x2030 && c <= 0x205E);
        return basic || punctuation;
    }

    private byte Resolve(Page page, int slot, char c) {
        bool requiresLayout = IsSimple(c) == false;
        if (requiresLayout) {
            return Unmapped;
        }
        bool mapped = CharacterMap.TryGetValue(c, out var index);
        if (mapped) {
            page.Indices[slot] = index;
            page.Advances[slot] = AdvanceWidths[index] * EmSize;
            return Mapped;
        }
        return Unmapped;
    }

    public GlyphTypeface Typeface { get; }

    public LogGlyphTable(GlyphTypeface typeface, double emSize) {
        Typeface = typeface ?? throw new ArgumentNullException(nameof(typeface));
        CharacterMap = typeface.CharacterToGlyphMap;
        AdvanceWidths = typeface.AdvanceWidths;
        EmSize = emSize;
    }

    public bool TryGet(char c, out ushort index, out double advance) {
        var pageIndex = c / PageSize;
        var slot = c % PageSize;
        var page = Pages[pageIndex] ??= new Page();
        var state = page.States[slot];
        bool unknown = state == Unknown;
        if (unknown) {
            state = page.States[slot] = Resolve(page, slot, c);
        }
        index = page.Indices[slot];
        advance = page.Advances[slot];
        return state == Mapped;
    }
}
