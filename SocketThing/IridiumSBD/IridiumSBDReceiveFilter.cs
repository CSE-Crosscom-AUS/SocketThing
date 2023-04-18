using SuperSocket.SocketBase.Protocol;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SocketThing.IridiumSBD
{
    public class IridiumSBDReceiveFilter : IReceiveFilter<IridiumSBDRequestInfo>
    {
        byte[] buffer = new byte[4096];
        int bufferpos = 0;

        public int LeftBufferSize { get; private set; }

        public IReceiveFilter<IridiumSBDRequestInfo> NextReceiveFilter { get; private set; }

        public FilterState State { get; private set; }

        public IridiumSBDRequestInfo Filter(byte[] readBuffer, int offset, int length, bool toBeCopied, out int rest)
        {
            Console.WriteLine($"Filter {offset} {length} {toBeCopied}");

            rest = 0;

            if (bufferpos + length > buffer.Length)
            {
                // we really just want to have the client disconnect here
                // the data we've been sent by this device doesn't make sense
                State = FilterState.Error;
                //throw new Exception("data too long");
                return default;
            }


            Array.Copy(readBuffer, offset, buffer, bufferpos, length);

            bufferpos += length;



            if (bufferpos >= 2 && buffer[0] == 0x00 && buffer[1] == 0x0f)
            {
                // IMEI packet
                // [00 0F 38 36 37 36 34 38 30 34 33 30 30 38 35 35 34]

                // get imei
                if (bufferpos < 17)
                {
                    return default;
                }

                //if (bufferpos > 17)
                //{
                //    // we have trailing data, consider disconnecting
                //}

                IridiumSBDRequestInfo r = new IridiumSBDRequestInfo();
                r.IMEI = GetIMEI(buffer, 0, 17);
                Console.WriteLine($"got IMEI {r.IMEI}");

                bufferpos = 0;

                return r;
            }
            else if (bufferpos >= 8 && buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0x00 && buffer[3] == 0x00)
            {
                // byte[] len = { buffer[4], buffer[5], buffer[6], buffer[7] };
                // int avlDataLength = global::Teltonika.Codec.BytesSwapper.Swap(BitConverter.ToInt32(len, 0));

                // int packetLength = 8 + avlDataLength + 4;

                // if (bufferpos < packetLength)
                // {
                // return default;
                // }

                // //if (bufferpos > packetLength)
                // //{
                // //    // trailing data?
                // //    // consider disconnecting
                // //}

                // TeltonikaRequestInfo r = new TeltonikaRequestInfo();
                // r.Data = DecodeTcpPacket(buffer, 0, packetLength);

                // bufferpos = 0;

                // Console.WriteLine($"Recieved: {BitConverter.ToString(buffer, 0, packetLength)}");

                return default;
            }

            return default;
        }

        public void Reset()
        {
            bufferpos = 0;
        }



        // private static global::Teltonika.Codec.Model.TcpDataPacket DecodeTcpPacket(byte[] request, int offset, int length)
        // {
            // var reader = new global::Teltonika.Codec.ReverseBinaryReader(new System.IO.MemoryStream(request, offset, length));
            // var decoder = new global::Teltonika.Codec.DataDecoder(reader);

            // var packet = decoder.DecodeTcpData();


            // if (packet.AvlData.Data != null)
            // {
                // foreach (var data in packet.AvlData.Data)
                // {
                    // Console.WriteLine(DebugAvl(data));
                // }
            // }
            // else
            // {
                // Console.WriteLine("packet.AvlData.Data is null!");
            // }





            // return packet;
        // }





        private static string GetIMEI(byte[] bytes, int offset, int length)
        {
            string bytess = String.Join("", bytes.Skip(offset).Take(length).Select(x => x.ToString("X2")).ToArray());

            string imei = "";
            for (int i = 0; i < 15; i++)
            {
                if (bytess[4 + 2 * i] == '3')
                {
                    imei += bytess[5 + 2 * i];
                }
            }

            return imei;
        }


        public static string ByteArrayToHexString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }
    }
}
