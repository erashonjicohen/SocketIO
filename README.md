# SocketIO (.NET)

Documentación en español con ejemplos de uso para distintos ámbitos: sensores industriales, PLC, serial, TCP stream, UDP, impresoras, drawers (caja registradora) y scanners.

> Este repositorio expone una capa ligera sobre conexiones (TCP/UDP/Serial), codecs de framing y utilidades de diagnóstico. Los ejemplos de abajo muestran patrones de uso comunes para integrar dispositivos industriales y periféricos.

## Componentes principales

- **Transporte**
  - `SocketIO.Net.Transport.Sockets.TcpConnection` y `TcpListener` para TCP.
  - `SocketIO.Net.Transport.Sockets.UdpConnection` para UDP (datagramas).
  - `SocketIO.Net.Transport.Serial.SerialConnection` para RS-232/RS-485.
- **Runtime**
  - `SocketIO.Net.Runtime.Peer` combina `IConnection` + `IFrameCodec` para enviar y recibir frames.
- **Protocol / Codecs**
  - `NewlineCodec`, `DelimitedCodec`, `LengthFieldCodec`, `FixedLengthCodec`, `StxEtxCodec`, etc.
- **Diagnóstico**
  - `WireTapConnection`, `FrameAwareDumper` y `DumpSink` para inspección de tráfico.

## Ejemplos por ámbito

### 1) Sensores industriales (TCP con frames delimitados por salto de línea)

Muchos sensores envían lecturas terminadas en `\n` o `\r\n`.

```csharp
using SocketIO.Net.Protocol;
using SocketIO.Net.Runtime;
using SocketIO.Net.Transport.Sockets;
using System.Net;

var listener = new TcpListener(new IPEndPoint(IPAddress.Any, 9000));
await listener.StartAsync();

var connection = await listener.AcceptAsync();
var peer = new Peer(connection, new NewlineCodec());

await peer.ReceiveLoopAsync(async frame =>
{
    var text = System.Text.Encoding.ASCII.GetString(frame.Span);
    Console.WriteLine($"Sensor: {text}");
});
```

### 2) PLC (TCP con longitud prefijada)

En PLC es común tener un framing por longitud. `LengthFieldCodec` soporta el tamaño en el encabezado.

```csharp
using SocketIO.Net.Protocol;
using SocketIO.Net.Runtime;
using SocketIO.Net.Transport.Sockets;
using System.Net;

var listener = new TcpListener(new IPEndPoint(IPAddress.Any, 15000));
await listener.StartAsync();

var connection = await listener.AcceptAsync();

// Header de 2 bytes big-endian con longitud del payload.
var codec = new LengthFieldCodec(lengthFieldSize: 2, bigEndian: true, lengthOffset: 0, headerSize: 2, maxFrameBytes: 4096);
var peer = new Peer(connection, codec);

await peer.ReceiveLoopAsync(async frame =>
{
    Console.WriteLine($"PLC frame {frame.Length} bytes");
});
```

### 3) Serial (RS-232/RS-485) para sensores o PLCs legacy

```csharp
using SocketIO.Net.Protocol;
using SocketIO.Net.Runtime;
using SocketIO.Net.Transport.Serial;
using System.IO.Ports;

var connection = new SerialConnection(
    portName: "COM3",
    baudRate: 9600,
    dataBits: 8,
    parity: Parity.None,
    stopBits: StopBits.One,
    handshake: Handshake.None);

var peer = new Peer(connection, new NewlineCodec());

await peer.SendAsync(System.Text.Encoding.ASCII.GetBytes("READ\n"));

await peer.ReceiveLoopAsync(async frame =>
{
    var value = System.Text.Encoding.ASCII.GetString(frame.Span);
    Console.WriteLine($"Serial RX: {value}");
});
```

### 4) TCP stream con protocolo binario propio (PacketCodec)

Si tu protocolo ya incluye versión, tipo y payload, puedes usar `PacketCodec`.

```csharp
using SocketIO.Net.Abstractions;
using SocketIO.Net.Protocol;
using SocketIO.Net.Runtime;
using SocketIO.Net.Transport.Sockets;
using System.Net;

var listener = new TcpListener(new IPEndPoint(IPAddress.Any, 7000));
await listener.StartAsync();
var connection = await listener.AcceptAsync();

var peer = new Peer(connection, new LengthFieldCodec(4, bigEndian: true, lengthOffset: 0, headerSize: 4, maxFrameBytes: 64 * 1024));

await peer.ReceiveLoopAsync(async frame =>
{
    if (PacketCodec.TryDecode(frame.Span, out var packet))
    {
        Console.WriteLine($"Tipo={packet.Type} Seq={packet.Sequence} Bytes={packet.Payload.Length}");
    }
});

var outgoing = new Packet(
    Version: PacketCodec.CurrentVersion,
    Type: MessageType.Data,
    Flags: 0,
    Sequence: 1,
    Payload: System.Text.Encoding.UTF8.GetBytes("hello"));

await peer.SendAsync(PacketCodec.Encode(outgoing));
```

### 5) UDP (datagramas rápidos)

Uso típico en sensores que emiten broadcast/multicast o paquetes sin conexión.

```csharp
using SocketIO.Net.Transport.Sockets;
using System.Net;
using System.Net.Sockets;

var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
var remote = new IPEndPoint(IPAddress.Parse("192.168.1.60"), 9100);
socket.Connect(remote);

var connection = new UdpConnection(socket, remote);

var buffer = new byte[2048];
int read = await connection.ReceiveAsync(buffer);
Console.WriteLine($"UDP bytes: {read}");
```

### 6) Impresoras (ESC/POS por TCP o Serial)

Para impresoras térmicas, normalmente se envían comandos ESC/POS directos.

```csharp
using SocketIO.Net.Transport.Sockets;
using System.Net;
using System.Net.Sockets;
using System.Text;

var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
await socket.ConnectAsync("192.168.1.50", 9100);

var printer = new TcpConnection(socket);

var text = Encoding.ASCII.GetBytes("TICKET\n\n");
var cut = new byte[] { 0x1D, 0x56, 0x00 }; // Cut

await printer.SendAsync(text);
await printer.SendAsync(cut);
```

### 7) Drawer (cajón de dinero / cash drawer)

Muchos drawers se abren con pulso ESC/POS:

```csharp
using SocketIO.Net.Transport.Serial;
using System.IO.Ports;

var drawer = new SerialConnection(
    portName: "COM5",
    baudRate: 9600,
    dataBits: 8,
    parity: Parity.None,
    stopBits: StopBits.One,
    handshake: Handshake.None);

var openPulse = new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA };
await drawer.SendAsync(openPulse);
```

### 8) Scanner (código de barras por Serial, terminado en CR/LF)

```csharp
using SocketIO.Net.Protocol;
using SocketIO.Net.Runtime;
using SocketIO.Net.Transport.Serial;
using System.IO.Ports;

var scanner = new SerialConnection(
    portName: "COM4",
    baudRate: 115200,
    dataBits: 8,
    parity: Parity.None,
    stopBits: StopBits.One,
    handshake: Handshake.None);

var peer = new Peer(scanner, new NewlineCodec());

await peer.ReceiveLoopAsync(async frame =>
{
    var barcode = System.Text.Encoding.ASCII.GetString(frame.Span).Trim();
    Console.WriteLine($"Barcode: {barcode}");
});
```

## Diagnóstico y logging

Si necesitas inspeccionar tráfico sin cambiar la lógica, puedes envolver la conexión:

```csharp
using SocketIO.Net.Diagnostics;
using SocketIO.Net.Transport.Sockets;
using System.Net;
using System.Net.Sockets;

var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
await socket.ConnectAsync("192.168.1.20", 502);

var connection = new TcpConnection(socket);
var options = new DumpOptions
{
    Enabled = true,
    IncludeTimestamp = true,
    IncludeDirection = true,
    BytesPerLine = 16,
    MaxBytesPerMessage = 512,
    FilePath = "wiretap.log"
};

var sink = new DumpSink(options);
var tapped = new WireTapConnection(connection, sink, options);
```

## Consejos prácticos

- **Define el framing primero**: elige `NewlineCodec`, `LengthFieldCodec`, `FixedLengthCodec` o `DelimitedCodec` según el dispositivo.
- **Timeouts y reconexión**: al trabajar con PLCs o sensores, maneja reconexión a nivel de aplicación.
- **UDP**: no garantiza orden ni entrega; úsalo sólo si el dispositivo lo soporta.

---

Si necesitas ejemplos adicionales (Modbus, HL7, etc.), puedes extender los codecs o implementar un `IFrameCodec` propio.
