using System;

namespace Domore.Windows.Controls;

/*
 * Retains the newest lines up to a limit. Storage grows on demand until the
 * limit is reached and then wraps, so each append is constant time and the
 * oldest line is discarded. Start is zero whenever the buffer is not full.
 * Each line also has a serial number, its position among all lines ever
 * added, which stays the same while older lines are discarded.
 */
internal sealed class LogLineBuffer {
    private LogLine[] Items = [];
    private int Start;

    public int Count { get; private set; }
    public int Limit { get; private set; }
    public long AddedCount { get; private set; }
    public long FirstSerial => AddedCount - Count;

    public LogLine this[int index] {
        get {
            bool indexIsOutOfRange = index < 0 || index >= Count;
            if (indexIsOutOfRange) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return Items[(Start + index) % Items.Length];
        }
    }

    public LogLineBuffer(int limit) {
        bool limitIsInvalid = limit < 1;
        if (limitIsInvalid) {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }
        Limit = limit;
    }

    public bool Add(LogLine line) {
        AddedCount++;
        bool full = Count == Limit;
        if (full) {
            Items[Start] = line;
            Start = (Start + 1) % Items.Length;
            return true;
        }
        bool storageIsFull = Count == Items.Length;
        if (storageIsFull) {
            var length = (int)Math.Min(Limit, Math.Max(256L, 2L * Items.Length));
            Array.Resize(ref Items, length);
        }
        Items[Count] = line;
        Count++;
        return false;
    }

    public int Resize(int limit) {
        bool limitIsInvalid = limit < 1;
        if (limitIsInvalid) {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }
        var removed = Math.Max(0, Count - limit);
        var count = Count - removed;
        var items = new LogLine[count];
        for (var i = 0; i < count; i++) {
            items[i] = this[removed + i];
        }
        Items = items;
        Start = 0;
        Count = count;
        Limit = limit;
        return removed;
    }

    public void Clear() {
        Items = [];
        Start = 0;
        Count = 0;
    }
}
