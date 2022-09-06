using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketThing
{
    public static class TeltonikaCommand
    {

        public static byte[] MakeCodec12Command(string c)
        {
            // possibly should have used a MemoryStream and ByteWriter for this

            //string c = "getinfo";

            byte[] command = new byte[100];

            int idx = 0;

            // preamble
            idx += AddBigEndianInt32(command, idx, 0);


            // data size
            idx += AddBigEndianInt32(command, idx, c.Length + 8);




            // codec id
            command[idx] = 0x0c;
            idx++;

            // command quantity #1
            command[idx] = 1;
            idx++;

            // type (inquiry)
            command[idx] = 0x05;
            idx++;


            idx += AddBigEndianInt32(command, idx, c.Length);



            // command
            byte[] cb = System.Text.Encoding.ASCII.GetBytes(c);
            Buffer.BlockCopy(cb, 0, command, idx, cb.Length);
            idx += cb.Length;


            // command quantity #2
            command[idx] = 1;
            idx++;

            int crc = global::Teltonika.Codec.CRC.CalcCrc16(command, 8, idx - 8, 0xA001, 0);

            idx += AddBigEndianInt32(command, idx, crc);


            byte[] result = new byte[idx];

            Buffer.BlockCopy(command, 0, result, 0, idx);

            return result;
        }

        public static int AddBigEndianInt32(byte[] command, int idx, int number)
        {
            ////int s = message.Length;
            byte[] numberb = BitConverter.GetBytes(number);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(numberb);
            }

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

    }
}
