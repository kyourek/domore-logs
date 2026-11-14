using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Media.TextFormatting;

namespace Domore.Windows.Controls;

internal static class LogBidi {
    private const int InsufficientCapacity = unchecked((int)0x8007000E);

    [DllImport("usp10.dll", ExactSpelling = true, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)]
    private static extern int ScriptItemize(
        string text, int length, int capacity,
        [In] ref uint control, [In] ref ushort state,
        [Out] LogScriptItem[] items, out int count);

    public static int[] GetLevels(string text, IReadOnlyList<IndexedGlyphRun> runs) {
        /*
         * GlyphRun exposes drawing direction, but its level can be zero for
         * digits nested inside RTL text. Analyze the complete paragraph with
         * both control/state supplied to obtain the actual embedding levels.
         * All buffers belong to the caller; the native call retains none.
         */
        var capacity = Math.Min(32, text.Length + 1);
        LogScriptItem[] items;
        int count;
        var retry = true;
        do {
            items = new LogScriptItem[capacity + 1];
            uint control = 0;
            ushort state = 0;
            var result = ScriptItemize(text, text.Length, capacity, ref control, ref state, items, out count);
            retry = result == InsufficientCapacity && capacity < text.Length + 1;
            if (retry) {
                capacity = (int)Math.Min(text.Length + 1L, 2L * capacity);
            }
            else {
                Marshal.ThrowExceptionForHR(result);
            }
        } while (retry);
        var levels = new int[runs.Count];
        var item = 0;
        for (var i = 0; i < runs.Count; i++) {
            var position = runs[i].TextSourceCharacterIndex;
            bool hasNext = item + 1 < count && items[item + 1].Position <= position;
            while (hasNext) {
                item++;
                hasNext = item + 1 < count && items[item + 1].Position <= position;
            }
            /*
             * WPF can coalesce adjacent even-level items into a single
             * already ordered glyph run (for example timestamp and header).
             * Its enclosing level is the minimum across that entire span.
             */
            var level = items[item].State & 31;
            var stop = position + runs[i].TextSourceLength;
            for (var next = item + 1; next < count && items[next].Position < stop; next++) {
                level = Math.Min(level, items[next].State & 31);
            }
            levels[i] = level;
        }
        return levels;
    }
}
