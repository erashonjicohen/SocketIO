using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIO.Net.Diagnostics
{
    public sealed class DumpOptions
    {
        public bool Enabled { get; set; } = true;

        public bool WriteToConsole { get; set; } = true;
        public string? FilePath { get; set; } = null;

        public int MaxBytesPerMessage { get; set; } = 4096;
        public int BytesPerLine { get; set; } = 16;

        public bool IncludeTimestamp { get; set; } = true;
        public bool IncludeDirection { get; set; } = true;

        public DumpFilter? Filter { get; set; } = null;
    }
}
