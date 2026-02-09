using System;
using System.Text;
using System.Collections.Generic;

namespace KiSoftOneService.Models
{
    /// <summary>
    /// Representa un paquete de datos para comunicación TCP/IP con KiSoft One
    /// Estructura: <LF> + número de bytes (5) + datos + <CR>
    /// </summary>
    public class DataPacket
    {
        private const char START_CHAR = '\n';  // LF (Line Feed)
        private const char END_CHAR = '\r';    // CR (Carriage Return)
        private const string ENCODING_TYPE = "UTF-8";

        public string RecordIdentifier { get; set; }
        public Dictionary<string, object> Fields { get; set; }

        public DataPacket()
        {
            Fields = new Dictionary<string, object>();
        }

        /// <summary>
        /// Serializa el paquete a formato de transmisión
        /// </summary>
        public byte[] Serialize()
        {
            var dataBuilder = new StringBuilder();
            
            // Agregar identificador de registro
            dataBuilder.Append(RecordIdentifier);
            
            // Agregar campos en orden
            foreach (var field in Fields.Values)
            {
                if (field != null)
                {
                    dataBuilder.Append(field.ToString());
                }
            }

            string dataContent = dataBuilder.ToString();
            byte[] dataBytes = Encoding.UTF8.GetBytes(dataContent);
            
            // Calcular número de bytes: datos + longitud del número de bytes (5 bytes)
            int totalBytes = dataBytes.Length + 5;
            string byteCount = totalBytes.ToString("D5");

            // Construir paquete completo
            var packet = new StringBuilder();
            packet.Append(START_CHAR);
            packet.Append(byteCount);
            packet.Append(dataContent);
            packet.Append(END_CHAR);

            return Encoding.UTF8.GetBytes(packet.ToString());
        }

        /// <summary>
        /// Deserializa un paquete recibido
        /// </summary>
        public static DataPacket Deserialize(byte[] rawData)
        {
            try
            {
                string packetString = Encoding.UTF8.GetString(rawData);
                
                // Verificar caracteres de inicio y fin
                if (packetString[0] != '\n' || packetString[packetString.Length - 1] != '\r')
                    throw new InvalidOperationException("Formato de paquete inválido");

                // Extraer número de bytes
                string byteCountStr = packetString.Substring(1, 5);
                if (!int.TryParse(byteCountStr, out int byteCount))
                    throw new InvalidOperationException("Número de bytes inválido");

                // Extraer datos
                string dataContent = packetString.Substring(6, packetString.Length - 7);
                
                // Extraer identificador de registro (primeros 3 caracteres)
                var packet = new DataPacket
                {
                    RecordIdentifier = dataContent.Substring(0, 3)
                };

                return packet;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deserializando paquete: {ex.Message}");
            }
        }

        public void AddField(string fieldName, string value, int length)
        {
            if (string.IsNullOrEmpty(value))
                value = new string(' ', length);
            else if (value.Length < length)
                value = value.PadRight(length);
            else if (value.Length > length)
                value = value.Substring(0, length);

            Fields[fieldName] = value;
        }

        public void AddNumericField(string fieldName, int value, int length)
        {
            string formattedValue = value.ToString("D" + length);
            Fields[fieldName] = formattedValue;
        }
    }

    /// <summary>
    /// Mensaje de estado de un registro de datos
    /// </summary>
    public class StatusMessage
    {
        public string RecordIdentifier { get; set; }
        public string State { get; set; }
        
        public bool IsSuccess => State == "00";

        public static StatusMessage CreateSuccess(string recordId)
        {
            return new StatusMessage
            {
                RecordIdentifier = recordId,
                State = "00"
            };
        }

        public static StatusMessage CreateError(string recordId, string errorCode)
        {
            return new StatusMessage
            {
                RecordIdentifier = recordId,
                State = errorCode
            };
        }

        public byte[] Serialize()
        {
            var dataBuilder = new StringBuilder();
            dataBuilder.Append(RecordIdentifier);
            dataBuilder.Append(State);

            string dataContent = dataBuilder.ToString();
            byte[] dataBytes = Encoding.UTF8.GetBytes(dataContent);
            
            int totalBytes = dataBytes.Length + 5;
            string byteCount = totalBytes.ToString("D5");

            var packet = new StringBuilder();
            packet.Append('\n');
            packet.Append(byteCount);
            packet.Append(dataContent);
            packet.Append('\r');

            return Encoding.UTF8.GetBytes(packet.ToString());
        }
    }
}
