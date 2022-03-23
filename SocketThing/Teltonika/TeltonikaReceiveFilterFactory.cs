using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SocketThing.Teltonika
{
    //class TeltonikaReceiveFilterFactory
    //{
    //}

    public class TeltonikaReceiveFilterFactory : IReceiveFilterFactory<TeltonikaRequestInfo>, IReceiveFilterFactory
    {
        public IReceiveFilter<TeltonikaRequestInfo> CreateFilter(IAppServer appServer, IAppSession appSession, IPEndPoint remoteEndPoint)
        {
            return new TeltonikaReceiveFilter();
        }
    }


    public class TeltonikaReceiveFilter : IReceiveFilter<TeltonikaRequestInfo>
    {
        byte[] buffer = new byte[4096];
        int length = 0;

        public int LeftBufferSize { get; private set; }

        public IReceiveFilter<TeltonikaRequestInfo> NextReceiveFilter { get; private set; }

        public FilterState State { get; private set; }

        public TeltonikaRequestInfo Filter(byte[] readBuffer, int offset, int length, bool toBeCopied, out int rest)
        {
            Console.WriteLine($"Filter {offset} {length} {toBeCopied}");

            if (length >= 2 && readBuffer[offset + 0] == 0x00 && readBuffer[offset + 1] == 0x0f)
            {
                // IMEI packet
                // [00 0F 38 36 37 36 34 38 30 34 33 30 30 38 35 35 34]

                // get imei
                if (length < 17)
                {
                    rest = length;
                    return default;
                }

                TeltonikaRequestInfo r = new TeltonikaRequestInfo();
                r.IMEI = GetIMEI(readBuffer, offset, 17);
                rest = length - 17;
                Console.WriteLine($"rest is {rest}");
                return r;

            }
            else if (length >= 8 && readBuffer[offset + 0] == 0x00 && readBuffer[offset + 1] == 0x00 && readBuffer[offset + 2] == 0x00 && readBuffer[offset + 3] == 0x00)
            {
                byte[] len = { readBuffer[offset + 4], readBuffer[offset + 5], readBuffer[offset + 6], readBuffer[offset + 7] };
                int avlDataLength = global::Teltonika.Codec.BytesSwapper.Swap(BitConverter.ToInt32(len, 0));

                int packetLength = 8 + avlDataLength + 4;

                if (length < packetLength)
                {
                    rest = length;
                    return default;
                }

                Console.WriteLine($"More data! {length} {packetLength} {avlDataLength}");

                Array.Copy(readBuffer, offset, buffer, 0, length);

                TeltonikaRequestInfo r = new TeltonikaRequestInfo();
                r.Data = DecodeTcpPacket(buffer);

                rest = length - packetLength;
                return r;
            }


            // should we try and disconnect if we get here and there's bad data?


            rest = length;
            return default;
        }

        public void Reset()
        {
            // FIXME
            length = 0;
        }



        private static global::Teltonika.Codec.Model.TcpDataPacket DecodeTcpPacket(byte[] request)
        {
            var reader = new global::Teltonika.Codec.ReverseBinaryReader(new System.IO.MemoryStream(request));
            var decoder = new global::Teltonika.Codec.DataDecoder(reader);

            var packet = decoder.DecodeTcpData();

            try
            {
                float x = packet.AvlData.Data.First().GpsElement.X;
                float y = packet.AvlData.Data.First().GpsElement.Y;

                var s = packet.AvlData.Data.First().GpsElement.Satellites;

                x /= 10000000;
                y /= 10000000;

                Console.WriteLine($"GPS is {x} {y} ({s} satelites)");

                foreach (var p in packet.AvlData.Data.First().IoElement.Properties)
                {
                    Console.WriteLine($"property {p.Id} is {p.Value}");
                }
            }
            catch
            {
                Console.WriteLine("GPS is error");
            }


            return packet;
        }



        private static string GetIMEI(byte[] bytes, int offset, int length)
        {
            string bytess = String.Join("", bytes.Skip(offset).Take(length).Select(x => x.ToString("X2")).ToArray());

            string imei = "";
            for (int i = 0; i < 15; i++)
            {
                imei += bytess[5 + 2 * i];
            }

            return imei;
        }
    }


    public class TeltonikaRequestInfo : IRequestInfo
    {
        public string Key => "Teltonika";

        public string IMEI { get; set; }
        public global::Teltonika.Codec.Model.TcpDataPacket Data { get; set; }
    }
}
