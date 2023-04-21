using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Net.Sockets;
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
    public static class TeltonikaIridiumSBDParser
    {
        public static string ExampleDataHex0 = "01004C01001CF99DD263333030323334303634303730383330000018000059282DBC03000B0036A2B8193D120000000302001C 59282BA391F5ABCDBFDD0180000059282BA891F5ABCDBFDD0180000059282BA391F5AB";
        public static byte[] ExampleData0 = ConvertHexStringToByteArray(ExampleDataHex0);

        public static string ExampleDataHex1 = "01-00-92-01-00-1C-13-10-CE-60-33-30-30-35-33-34-30-36-33-37-38-34-38-34-30-00-00-00-00-00-64-3F-7B-6F-03-00-0B-02-29-61-A4-93-28-7D-00-00-00-03-02-00-62-64-3F-79-7C-E8-A0-56-45-1A-E6-00-80-00-00-64-3F-79-CC-E8-A0-56-45-1A-E6-00-80-00-00-64-3F-7A-12-E8-A0-54-45-1A-E5-00-80-00-00-64-3F-7A-7D-E8-A0-53-45-1B-01-00-80-00-00-64-3F-7A-BF-E8-A0-57-45-1B-00-00-80-00-00-64-3F-7B-0D-E8-A0-57-45-1B-00-00-80-00-00-64-3F-7B-52-E8-A0-57-45-1B-00-00-80-00-00";
        public static byte[] ExampleData1 = ConvertHexStringToByteArray(ExampleDataHex1);



        public static string ExampleDataHex2 = "01-00-3E-01-00-1C-13-10-F1-35-33-30-30-35-33-34-30-36-33-37-38-34-38-34-30-00-00-01-00-00-64-3F-7B-A7-03-00-0B-02-29-63-4D-93-18-FA-00-00-00-02-02-00-0E-64-3F-7B-9E-E8-A0-57-45-1B-00-00-80-00-00";
        public static byte[] ExampleData2 = ConvertHexStringToByteArray(ExampleDataHex2);


        public static string ExampleDataHex3 = "01-00-5A-01-00-1C-13-11-75-69-33-30-30-35-33-34-30-36-33-37-38-34-38-34-30-00-00-02-00-00-64-3F-7C-6C-03-00-0B-02-29-66-56-93-22-67-00-00-00-07-02-00-2A-64-3F-7B-DA-E8-A0-57-45-1B-00-00-80-00-00-64-3F-7C-16-E8-A0-57-45-1B-00-00-80-00-00-64-3F-7C-57-E8-A0-68-45-1B-05-00-80-00-00";
        public static byte[] ExampleData3 = ConvertHexStringToByteArray(ExampleDataHex3);


        public static byte[] ConvertHexStringToByteArray(string hexString)
        {
            //Console.WriteLine(hexString);
            //Console.WriteLine(hexString.Length);

            hexString = hexString.Replace(" ", "");
            hexString = hexString.Replace("-", "");

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

            TeltonikaIridiumSBDMessage sbd = new TeltonikaIridiumSBDMessage();

            int pos;

            //pos = 0;
            //sbd.Decode(ExampleData0, ref pos);
            //Console.WriteLine(sbd);
            //Console.WriteLine();

            //pos = 0;
            //sbd.Decode(ExampleData1, ref pos);
            //Console.WriteLine(sbd);
            //Console.WriteLine();

            pos = 0;
            sbd.Decode(ExampleData2, ref pos);
            Console.WriteLine(sbd);
            Console.WriteLine();


            //pos = 0;
            //sbd.Decode(ExampleData3, ref pos);
            //Console.WriteLine(sbd);
            //Console.WriteLine();
        }


        public static void Test2()
        {

            // update the timestamps of the message to now
            long n = DateTimeOffset.Now.ToUnixTimeSeconds();
            uint n2 = (uint)n;

            byte[] b = BitConverter.GetBytes(n2);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            Buffer.BlockCopy(b, 0, ExampleData2, 30, b.Length);
            int p = 51;

            while (p < ExampleData2.Length - 4)
            {
                Buffer.BlockCopy(b, 0, ExampleData2, p, b.Length);

                p += 14;
            }


            // decode and output updated message (debugging)
            TeltonikaIridiumSBDMessage sbd = new TeltonikaIridiumSBDMessage();
            int pos;

            pos = 0;
            sbd.Decode(ExampleData2, ref pos);
            Console.WriteLine(sbd);
            Console.WriteLine();



            // send to datalistener
            TcpClient tcp = new TcpClient();

            tcp.Connect("127.0.0.1", 5010);

            BinaryWriter bw = new BinaryWriter(tcp.GetStream());

            bw.Write(ExampleData2, 0, ExampleData2.Length);

            tcp.Close();
        }
    }

    public class TeltonikaIridiumSBDMessage
    {
        public TeltonikaIridiumSBDMessage()
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


            EdgePayload = new TeltonikaIridiumSBDSingleReport[PayloadLength / 14];

            for (int i = 0; i < EdgePayload.Length; i++)
            {
                EdgePayload[i] = new TeltonikaIridiumSBDSingleReport();
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

        public TeltonikaIridiumSBDSingleReport[] EdgePayload { get; set; }

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

    public class TeltonikaIridiumSBDSingleReport
    {

        public TeltonikaIridiumSBDSingleReport()
        {
        }


        public void Decode(byte[] data, ref int position)
        {
            UInt32 time = TeltonikaIridiumSBDMessage.GetUInt32(data, ref position);

            Timestamp = DateTimeOffset.FromUnixTimeSeconds(time);

            byte b1, b2, b3;

            b1 = TeltonikaIridiumSBDMessage.GetByte(data, ref position);
            b2 = TeltonikaIridiumSBDMessage.GetByte(data, ref position);
            b3 = TeltonikaIridiumSBDMessage.GetByte(data, ref position);
            //Console.WriteLine($"{b1:x} {b2:x} {b3:x}");
            double lat = (double)b1 * 256.0 * 256.0 + (double)b2 * 256.0 + (double)b3;
            lat /= 46603.375;
            lat -= 180.0;

            Latitude = lat;

            b1 = TeltonikaIridiumSBDMessage.GetByte(data, ref position);
            b2 = TeltonikaIridiumSBDMessage.GetByte(data, ref position);
            b3 = TeltonikaIridiumSBDMessage.GetByte(data, ref position);
            //Console.WriteLine($"{b1:x} {b2:x} {b3:x}");
            double lon = (double)b1 * 256.0 * 256.0 + (double)b2 * 256.0 + (double)b3;
            lon /= 93206.75;
            lon -= 90.0;

            Longitude = lon;


            Flags = TeltonikaIridiumSBDMessage.GetByte(data, ref position);

            DIOStatus = TeltonikaIridiumSBDMessage.GetByte(data, ref position);

            byte reserved = TeltonikaIridiumSBDMessage.GetByte(data, ref position);

            byte s = TeltonikaIridiumSBDMessage.GetByte(data, ref position);

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
