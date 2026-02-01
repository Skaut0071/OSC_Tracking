using System;
using System.Net;
using System.Net.Sockets;

namespace OscJack
{
    /// <summary>
    /// Rozšíření OscClient pro odesílání 6 floatů (pozice + rotace pro SlimeVR VRSystem)
    /// </summary>
    public static class OscClientExtensions
    {
        private static byte[] _buffer = new byte[256];
        private static float[] _tempFloat = new float[1];
        private static byte[] _tempByte = new byte[4];

        /// <summary>
        /// Pošle OSC zprávu s 6 float argumenty (pro SlimeVR /tracking/vrsystem/xxx/pose)
        /// </summary>
        public static void Send(this OscClient client, string address, 
            float f1, float f2, float f3, float f4, float f5, float f6)
        {
            int length = 0;
            
            // Address
            length = AppendString(_buffer, length, address);
            
            // Type tag
            length = AppendString(_buffer, length, ",ffffff");
            
            // Data
            length = AppendFloat(_buffer, length, f1);
            length = AppendFloat(_buffer, length, f2);
            length = AppendFloat(_buffer, length, f3);
            length = AppendFloat(_buffer, length, f4);
            length = AppendFloat(_buffer, length, f5);
            length = AppendFloat(_buffer, length, f6);

            // Získej socket přes reflection (OscClient ho má jako private)
            var socketField = typeof(OscClient).GetField("_socket", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var socket = socketField?.GetValue(client) as Socket;
            socket?.Send(_buffer, length, SocketFlags.None);
        }

        private static int AppendString(byte[] buffer, int offset, string data)
        {
            int len = data.Length;
            for (int i = 0; i < len; i++)
                buffer[offset++] = (byte)data[i];

            // Padding to 4-byte boundary
            int len4 = (len + 4) & ~3;
            for (int i = len; i < len4; i++)
                buffer[offset++] = 0;

            return offset;
        }

        private static int AppendFloat(byte[] buffer, int offset, float data)
        {
            _tempFloat[0] = data;
            Buffer.BlockCopy(_tempFloat, 0, _tempByte, 0, 4);
            // Big-endian
            buffer[offset++] = _tempByte[3];
            buffer[offset++] = _tempByte[2];
            buffer[offset++] = _tempByte[1];
            buffer[offset++] = _tempByte[0];
            return offset;
        }
    }
}
