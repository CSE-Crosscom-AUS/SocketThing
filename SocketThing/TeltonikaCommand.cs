using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketThing
{
    public static class TeltonikaCommand
    {

        private static Encoding Charset = Encoding.GetEncoding("ISO-8859-1");

        public static byte[] MakeCodec12Command(string command)
        {

            return MakeCodec12Command(new List<string> { command, });
        }

        public static byte[] MakeCodec12Command(List<string> commands)
        {
            // possibly should have used a MemoryStream and ByteWriter for this

            //string c = "getinfo";

            if (commands == null || commands.Count == 0)
            {
                throw new ArgumentNullException("Require at least one command");
            }

            if (commands.Count > 255)
            {
                throw new ArgumentException("Too many commands");
            }


            byte[] command = new byte[100];

            int idx = 0;

            // preamble
            idx += AddBigEndianInt32(command, idx, 0);

            int data_size = 0;
            data_size += 1; // codec id
            data_size += 1; // command qty
            foreach (string c in commands)
            {
                data_size += 1; // command type
                data_size += 4; // command length
                data_size += c.Length;
            }
            data_size += 1; // command qty

            // data size
            idx += AddBigEndianInt32(command, idx, data_size);




            // codec id
            command[idx] = 0x0c;
            idx++;

            // command quantity #1
            command[idx] = (byte)commands.Count;
            idx++;


            foreach (string c in commands)
            {
                // type (inquiry)
                command[idx] = 0x05;
                idx++;


                idx += AddBigEndianInt32(command, idx, c.Length);



                // command
                byte[] cb = Charset.GetBytes(c);
                Buffer.BlockCopy(cb, 0, command, idx, cb.Length);
                idx += cb.Length;
            }



            // command quantity #2
            command[idx] = (byte)commands.Count;
            idx++;


            //int crc = global::Teltonika.Codec.CRC.CalcCrc16(command, 8, idx - 8, 0xA001, 0);

            int crc = Crc(command, 8, idx - 8);


            //idx += AddBigEndianInt32(command, idx, crc);

            command[idx] = 0;
            idx++;

            command[idx] = 0;
            idx++;


            byte[] crcb = BitConverter.GetBytes(crc);

            //Console.WriteLine(crcb.Length);

            command[idx] = crcb[1];
            idx++;

            command[idx] = crcb[0];
            idx++;

            byte[] result = new byte[idx];

            Buffer.BlockCopy(command, 0, result, 0, idx);

            var s = BitConverter.ToString(result);
            s = s.Replace("-", "");
            Console.WriteLine($"Made Command: {s}");

            return result;
        }

        public static int AddBigEndianInt32(byte[] command, int idx, int number)
        {
            if (BitConverter.IsLittleEndian)
            {
                number = BinaryPrimitives.ReverseEndianness(number);
            }

            byte[] numberb = BitConverter.GetBytes(number);


            Buffer.BlockCopy(numberb, 0, command, idx, numberb.Length);
            return numberb.Length;
        }





        public static void DebugByteArray(byte[] bytes)
        {
            Console.WriteLine("---------------------------");

            for (int i = 0; i < bytes.Length; i++)
            {
                Console.WriteLine($"{i}:{bytes[i].ToString("x2")}");
            }
        }


        public static int Crc(byte[] buffer, int start, int length)
        {
            int crc = 0;

            for (int byte_num = start; byte_num < start + length; byte_num++)
            {
                crc ^= buffer[byte_num];

                for (int bit_number = 0; bit_number < 8; bit_number++)
                {
                    int carry = crc & 1;

                    crc >>= 1;

                    if (carry == 1)
                    {
                        crc ^= 0xa001;
                    }
                }
            }

            return crc;
        }

    }
}
