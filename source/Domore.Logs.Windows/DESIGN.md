# Alternative streaming log viewer

This branch starts from windows commit d873f623c6ef4f909f0e50778b232856596e4c9c.
It preserves the control's filtering, severity colors, templates, target
frameworks, and public dependency properties.

## Design

The pipeline is a locked, bounded pending-entry queue, a background-priority
dispatcher drain of at most 64 entries, a bounded ring of logical display-line
descriptors, and a viewport containing retained DrawingVisual objects.

A small dispatcher batch allows higher-priority input and rendering to run
between deliveries. Producers enqueue references and never wait for UI work.
Overflow keeps the newest pending entries. Retained history and pending entries
have separate bounds, both configured through LineLimit. They are count bounds;
the viewer does not impose a byte limit on individual entry strings.

Delivery visits entries and their message lines newest-first, stopping when the
history limit is filled, then reverses the descriptors once. A million-newline
entry with LineLimit=2 therefore stages two descriptors rather than a million.
The original strings stay referenced so line splitting avoids substring copies.

The presenter retains only viewport drawings and reuses their visual objects
when scrolling. WPF TextFormatter provides shaping, direction, and fallback.
Formatter continuations are drawn consecutively on the same logical row instead
of discarding the suffix or wrapping it into another history row. For rows that
span formatter continuations, Windows Uniscribe analyzes the full paragraph's
bidi levels and the WPF-shaped glyph runs are placed in paragraph visual order.
Ordinary rows keep the direct TextLine drawing path. One formatter
is created per realization pass and disposed after drawing; TextLine and
TextLineBreak resources are disposed promptly.

This keeps the small visual tree of the first implementation while removing
manual character-to-glyph mapping. A virtualized ListBox would simplify container
and input behavior, but also introduces containers, templates, bindings, and
collection change processing. No comparative ListBox benchmark was performed;
this implementation does not claim to be the fastest possible WPF architecture.

Rows support mouse selection, Shift-click/drag ranges, and Ctrl+C. Selection
uses stable line serials, so eviction does not silently select different rows.
The highlight draws separately from cached text, avoiding text reformatting on
selection changes. Selecting pauses following; End resumes it. Escape clears
selection. Without a selection, Copy copies the entire retained history.

## Validation

Release restore and a nonincremental build succeeded with zero warnings/errors.
The WPF suite passed 60 tests on each of net462, net48, net8.0-windows, and
net10.0-windows (240 test executions). The explicit performance workload is
excluded from ordinary suite runs and was executed separately on .NET 10.
Package creation also succeeded.

Against isolated original-branch binaries, ArabicUsesWpfShapingAndDirection
and LongUnicodeLineRetainsTrailingMarker at 10,000 and 100,000 characters failed
for the expected rendering defects; the 1,000-character control case passed.
All four cases pass against this implementation.

Additional tests cover Arabic shaping and RTL direction, full rendering of
1,000/10,000/100,000-character lines with emoji and a trailing marker, reverse
splitting at line boundaries, million-line input with a two-line bound,
dispatcher fairness and ordering, pending overflow, page-mode wheel distance,
and selection/copy through history eviction. Existing tests cover following,
scroll anchoring, filters, concurrent producers, font fallback, and viewport
virtualization.

The manual workload sends 64,000 mixed-Unicode lines in 1,000 batches, arranging
a 1000x600 viewport after each batch. In one local Windows x64 Release/.NET 10
run it took 5,824 ms, retaining 38 visible line visuals and 10,000 history lines.
An earlier per-line formatter version took 9,882 ms. The identical workload
using original-branch Release binaries in an isolated copy of the test output
took 5,346 ms. This does not establish that the new renderer is faster: in this
single comparison it was approximately 9% slower while providing shaping and
complete long-line rendering. These are single-process
scenario timings without statistical sampling, physical presentation latency,
or statistical comparison; they are not a throughput guarantee.

Run the manual workload from the solution directory:

    dotnet test tests/Domore.Logs.Windows.Tests/Domore.Logs.Windows.Tests.csproj --configuration Release --framework net10.0-windows --filter FullyQualifiedName~MeasureStreamingViewportWorkload --logger "console;verbosity=normal"

## Deliberate limits

The control displays fixed-height unwrapped rows. Horizontal extent tracks the
widest row drawn so far rather than eagerly measuring all history. Selection is
by row rather than by character. It is a recent-history display, not durable log
storage, and overflow can discard entries. Arbitrarily large individual strings
can still require substantial memory and formatting time when visible.

Interactive desktop testing, screen-reader text-pattern support, and a full
solution test run were not performed. The affected WPF projects and their
core references were built and tested; unchanged configuration-library behavior
was outside this implementation's scope.
