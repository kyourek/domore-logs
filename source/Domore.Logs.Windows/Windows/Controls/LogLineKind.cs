namespace Domore.Windows.Controls;

internal enum LogLineKind {
    /*
     * The entry header alone, followed by indented message lines.
     */
    Header,

    /*
     * The entry header followed by a line of its only message.
     */
    Headed,

    /*
     * An indented line of one of several messages.
     */
    Indented
}
