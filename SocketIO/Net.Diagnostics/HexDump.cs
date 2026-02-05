using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIO.Net.Diagnostics
{
    public static class HexDump
    {
        public static string Format(ReadOnlySpan<byte> data, int bytesPerLine = 16)
        {
            var sb = new StringBuilder(data.Length * 4);

            for (int i = 0; i < data.Length; i += bytesPerLine)
            {
                var line = data.Slice(i, Math.Min(bytesPerLine, data.Length - i));

                sb.Append(i.ToString("X8")).Append("  ");

                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j < line.Length) sb.Append(line[j].ToString("X2")).Append(' ');
                    else sb.Append("   ");
                    if (j == 7) sb.Append(' ');
                }

                sb.Append(" |");

                for (int j = 0; j < line.Length; j++)
                {
                    var b = line[j];
                    sb.Append(b is >= 32 and <= 126 ? (char)b : '.');
                }

                sb.AppendLine("|");
            }

            return sb.ToString();
        }
    }
}
