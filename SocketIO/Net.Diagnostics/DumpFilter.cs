using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIO.Net.Diagnostics
{
    public sealed class DumpFilter
    {
        // Dirección permitida
        public DumpDirection Direction { get; set; } = DumpDirection.Both;

        // Tamaño de mensaje/frame/datagrama
        public int? MinBytes { get; set; } = null;
        public int? MaxBytes { get; set; } = null;

        // Endpoint (string match, simple y práctico)
        // Ej: "127.0.0.1:" o ":9001" o "192.168."
        public string[]? AllowEndpointContains { get; set; } = null;
        public string[]? DenyEndpointContains { get; set; } = null;

        public bool Match(string dir, string endpoint, int bytes)
        {
            // direction
            var want = Direction;
            if (dir == "RX" && !want.HasFlag(DumpDirection.Rx)) return false;
            if (dir == "TX" && !want.HasFlag(DumpDirection.Tx)) return false;

            // size
            if (MinBytes.HasValue && bytes < MinBytes.Value) return false;
            if (MaxBytes.HasValue && bytes > MaxBytes.Value) return false;

            // endpoint allow
            if (AllowEndpointContains is { Length: > 0 })
            {
                bool any = false;
                foreach (var s in AllowEndpointContains)
                {
                    if (!string.IsNullOrEmpty(s) && endpoint.Contains(s, StringComparison.OrdinalIgnoreCase))
                    {
                        any = true;
                        break;
                    }
                }
                if (!any) return false;
            }

            // endpoint deny
            if (DenyEndpointContains is { Length: > 0 })
            {
                foreach (var s in DenyEndpointContains)
                {
                    if (!string.IsNullOrEmpty(s) && endpoint.Contains(s, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            return true;
        }
    }
}
