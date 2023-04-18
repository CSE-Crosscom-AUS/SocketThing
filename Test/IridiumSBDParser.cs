using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Test
{

    /// <summary>
    /// see https://wiki.teltonika-gps.com/view/FMC640_Short_Burst_Data
    /// see https://wiki.teltonika-gps.com/view/Iridium_Edge_Communication_Protocol
    /// </summary>
    public static class IridiumSBDParser
    {
        public static string ExampleDataHex = "01004C01001CF99DD263333030323334303634303730383330000018000059282DBC03000B0036A2B8193D120000000302001C 59282BA391F5ABCDBFDD0180000059282BA891F5ABCDBFDD0180000059282BA391F5AB";
        public static byte[] ExampleData = ConvertHexStringToByteArray(ExampleDataHex);


        public static byte[] ConvertHexStringToByteArray(string hexString)
        {
            Console.WriteLine(hexString);
            Console.WriteLine(hexString.Length);

            hexString = hexString.Replace(" ", "");

            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException("Argument must have an even length");
            }

            byte[] data = new byte[hexString.Length / 2];

            for (int i = 0; i < hexString.Length / 2; i++)
            {
                data[i] = byte.Parse(hexString.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data;
        }

        public static void Test()
        {

            SBDPacket sbd = new SBDPacket();

            int pos = 0;
            sbd.Decode(ExampleData, ref pos);

            Console.WriteLine(sbd);
        }
    }

    public class SBDPacket
    {
        public SBDPacket()
        {
        }

        public void Decode(byte[] data, ref int pos)
        {

            if (data.Length < 3)
            {
                throw new ArgumentException("not enough data");
            }

            if (data[0] != 0x01)
            {
                Console.WriteLine($"WARNING: SBD protocol version not 1 (is {data[0]})");
            }

            ProtocolVersion = GetByte(data, ref pos);
            MessageLength = GetUInt16(data, ref pos);

            if (data.Length < MessageLength)
            {
                throw new ArgumentException("not enough data (message length too long)");
            }


            MOHeaderIEI = GetByte(data, ref pos);

            MOHeaderLength = GetUInt16(data, ref pos);

            CDRReference = GetUInt32(data, ref pos);

            //string imei = "";
            //for (int i = 0; i < 15; i++)
            //{
            //    imei += (char)data[pos];
            //    pos++;
            //}
            //IMEI = imei;
            IMEI = GetAsciiString(data, 15, ref pos);

            Status = GetByte(data, ref pos);

            MOMSN = GetUInt16(data, ref pos);

            MTMSN = GetUInt16(data, ref pos);

            TimeOfSession = DateTimeOffset.FromUnixTimeSeconds(GetUInt32(data, ref pos));

            //DateTimeOffset d = DateTimeOffset.FromUnixTimeSeconds(TimeOfSession);
            //DateTimeOffset d2 = d.ToLocalTime();
            //Console.WriteLine(d);
            //Console.WriteLine(d2);

            MOLocationInformationIEI = GetByte(data, ref pos);

            MOLocationInformationLength = GetUInt16(data, ref pos);

            //MOLatLon = GetByteArray(data, 7, ref pos);

            (MOX, MOY) = GetSBDLatLon(data, ref pos);

            CEPRadius = GetUInt32(data, ref pos);

            PayloadIEI = GetByte(data, ref pos);

            PayloadLength = GetUInt16(data, ref pos);


            EdgePayload = new Edge[PayloadLength / 14];

            for (int i = 0; i < EdgePayload.Length; i++)
            {
                EdgePayload[i] = new Edge();
                EdgePayload[i].Decode(data, ref pos);
            }


        }

        public static (double, double) GetSBDLatLon(byte[] data, ref int pos)
        {
            //if (data == null || data.Length < 7)
            //{
            //    throw new ArgumentException("DecodeSBDLatLon data too short (needs 7 bytes)");
            //}

            const byte b0 = 0b10000000;
            const byte b1 = 0b01000000;
            const byte b2 = 0b00100000;
            const byte b3 = 0b00010000;
            const byte b4 = 0b00001000;
            const byte b5 = 0b00000100;
            const byte b6 = 0b00000010;
            const byte b7 = 0b00000001;

            if ((data[pos] & b0) != 0 || (data[pos] & b1) != 0 || (data[pos] & b2) != 0 || (data[pos] & b3) != 0)
            {
                Console.WriteLine("Warning: reseved bits not zero");
            }

            if ((data[pos] & b4) != 0 || (data[pos] & b5) != 0)
            {
                Console.WriteLine("Warning: format bits not zero");
            }

            double ns = (data[pos] & b6) == 0 ? 1 : -1;
            double ew = (data[pos] & b7) == 0 ? 1 : -1;

            byte lat = data[pos + 1];

            int lat_millimin = data[pos + 2] * 256 + data[pos + 3];

            double lat_min = lat_millimin / 60000.0;

            double x = ns * (lat + lat_min);


            byte lon = data[pos + 4];

            int lon_millimin = data[pos + 5] * 256 + data[pos + 6];

            double lon_min = lon_millimin / 60000.0;

            double y = ew * (lon + lon_min);

            pos += 7;


            return (x, y);
        }


        public static byte GetByte(byte[] data, ref int start)
        {
            var result = data[start];
            start += 1;
            return result;
        }

        public static UInt16 GetUInt16(byte[] data, ref int start)
        {
            if (BitConverter.IsLittleEndian)
            {
                byte[] tmp = new byte[sizeof(UInt16)];
                Buffer.BlockCopy(data, start, tmp, 0, sizeof(UInt16));
                Array.Reverse(tmp);

                start += sizeof(UInt16);
                return BitConverter.ToUInt16(tmp, 0);
            }

            var result = BitConverter.ToUInt16(data, start);
            start += sizeof(UInt16);
            return result;
        }

        public static UInt32 GetUInt32(byte[] data, ref int start)
        {
            if (BitConverter.IsLittleEndian)
            {
                byte[] tmp = new byte[sizeof(UInt32)];
                Buffer.BlockCopy(data, start, tmp, 0, sizeof(UInt32));
                Array.Reverse(tmp);

                start += sizeof(UInt32);
                return BitConverter.ToUInt32(tmp, 0);
            }

            var result = BitConverter.ToUInt32(data, start);
            start += sizeof(UInt32);
            return result;
        }


        public static byte[] GetByteArray(byte[] data, int length, ref int start)
        {
            byte[] bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = data[start];
                start++;
            }

            return bytes;
        }


        public static string GetAsciiString(byte[] data, int length, ref int start)
        {
            string s = "";
            for (int i = 0; i < length; i++)
            {
                s += (char)data[start];
                start++;
            }

            return s;
        }


        public byte ProtocolVersion { get; set; }

        public UInt16 MessageLength { get; set; }

        public byte MOHeaderIEI { get; set; }
        public UInt16 MOHeaderLength { get; set; }
        public UInt32 CDRReference { get; set; }
        public string IMEI { get; set; }

        public byte Status { get; set; }

        public UInt16 MOMSN { get; set; }
        public UInt16 MTMSN { get; set; }
        public DateTimeOffset TimeOfSession { get; set; }

        public byte MOLocationInformationIEI { get; set; }
        public UInt16 MOLocationInformationLength { get; set; }
        //public byte[] MOLatLon { get; set; }

        public double MOX { get; set; }
        public double MOY { get; set; }

        public UInt32 CEPRadius { get; set; }
        public byte PayloadIEI { get; set; }
        public UInt16 PayloadLength { get; set; }

        public byte[] PayloadData { get; set; }

        public UInt32 Timestamp { get; set; }

        public Edge[] EdgePayload { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"            ProtocolVersion: {ProtocolVersion}");
            sb.AppendLine($"              MessageLength: {MessageLength}");
            sb.AppendLine($"                MOHeaderIEI: {MOHeaderIEI}");
            sb.AppendLine($"             MOHeaderLength: {MOHeaderLength}");
            sb.AppendLine($"               CDRReference: {CDRReference}");
            sb.AppendLine($"                       IMEI: {IMEI}");
            sb.AppendLine($"                     Status: {Status}");
            sb.AppendLine($"                      MOMSN: {MOMSN}");
            sb.AppendLine($"                      MTMSN: {MTMSN}");
            sb.AppendLine($"              TimeOfSession: {TimeOfSession}");
            sb.AppendLine($"   MOLocationInformationIEI: {MOLocationInformationIEI}");
            sb.AppendLine($"MOLocationInformationLength: {MOLocationInformationLength}");
            sb.AppendLine($"                        MOX: {MOX}");
            sb.AppendLine($"                        MOY: {MOY}");
            sb.AppendLine($"                  CEPRadius: {CEPRadius}");
            sb.AppendLine($"                 PayloadIEI: {PayloadIEI}");
            sb.AppendLine($"              PayloadLength: {PayloadLength}");
            sb.AppendLine($"                PayloadData: {PayloadData}");
            sb.AppendLine();


            for (int i = 0; i < EdgePayload.Length; i++)
            {
                sb.AppendLine($"payload {i}");
                sb.Append(EdgePayload[i]);
            }

            sb.AppendLine();


            return sb.ToString();
        }
    }

    public class Edge
    {

        public Edge()
        {
        }


        public void Decode(byte[] data, ref int position)
        {
            UInt32 time = SBDPacket.GetUInt32(data, ref position);

            Timestamp = DateTimeOffset.FromUnixTimeSeconds(time);

            byte b1, b2, b3;

            b1 = SBDPacket.GetByte(data, ref position);
            b2 = SBDPacket.GetByte(data, ref position);
            b3 = SBDPacket.GetByte(data, ref position);
            //Console.WriteLine($"{b1:x} {b2:x} {b3:x}");
            double lat = (double)b1 * 256.0 * 256.0 + (double)b2 * 256.0 + (double)b3;
            lat /= 46603.375;
            lat -= 180.0;

            Latitude = lat;

            b1 = SBDPacket.GetByte(data, ref position);
            b2 = SBDPacket.GetByte(data, ref position);
            b3 = SBDPacket.GetByte(data, ref position);
            Console.WriteLine($"{b1:x} {b2:x} {b3:x}");
            double lon = (double)b1 * 256.0 * 256.0 + (double)b2 * 256.0 + (double)b3;
            lon /= 93206.75;
            lon -= 90.0;

            Longitude = lon;


            Flags = SBDPacket.GetByte(data, ref position);

            DIOStatus = SBDPacket.GetByte(data, ref position);

            byte reserved = SBDPacket.GetByte(data, ref position);

            byte s = SBDPacket.GetByte(data, ref position);

            if (s == 255)
            {
                Speed = null;
            }
            else
            {
                Speed = s;
            }
        }



        public DateTimeOffset Timestamp { get; set; }
        public double Longitude { get; set; }


        public double Latitude { get; set; }


        public byte Flags { get; set; }

        public string Cause
        {
            get
            {
                switch (Flags & 0x0f)
                {
                    case 0: return "Periodic";
                    case 1: return "DIN1";
                    case 2: return "DIN2";
                    case 3: return "DIN3";
                    case 4: return "DIN4";
                    case 5: return "DOUT1";
                    case 6: return "DOUT2";
                    case 7: return "DOUT3";
                    case 8: return "DOUT4";
                    case 9: return "Speed";
                    default: return "";
                }
            }
        }


        public byte DIOStatus { get; set; }

        public bool Din1 => (DIOStatus & (1 << 7)) != 0;
        public bool Din2 => (DIOStatus & (1 << 6)) != 0;
        public bool Din3 => (DIOStatus & (1 << 5)) != 0;
        public bool Din4 => (DIOStatus & (1 << 4)) != 0;
        public bool Dout1 => (DIOStatus & (1 << 3)) != 0;
        public bool Dout2 => (DIOStatus & (1 << 2)) != 0;
        public bool Dout3 => (DIOStatus & (1 << 1)) != 0;
        public bool Dout4 => (DIOStatus & (1 << 0)) != 0;

        public double? Speed { get; set; }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Timestamp: {Timestamp}");
            sb.AppendLine($" Latitude: {Latitude}");
            sb.AppendLine($"Longitude: {Longitude}");
            sb.AppendLine($"    Flags: {Flags:X}");
            sb.AppendLine($"    Cause: {Cause}");



            sb.AppendLine($"DIOStatus: {DIOStatus:X}");

            sb.AppendLine($"     Din1: {Din1}");
            sb.AppendLine($"     Din2: {Din2}");
            sb.AppendLine($"     Din3: {Din3}");
            sb.AppendLine($"     Din4: {Din4}");

            sb.AppendLine($"    Dout1: {Dout1}");
            sb.AppendLine($"    Dout2: {Dout2}");
            sb.AppendLine($"    Dout3: {Dout3}");
            sb.AppendLine($"    Dout4: {Dout4}");

            sb.AppendLine($"    Speed: {Speed}");

            sb.AppendLine();

            return sb.ToString();
        }


    }
}
