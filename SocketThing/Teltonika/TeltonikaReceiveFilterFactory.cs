﻿using SuperSocket.SocketBase;
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
        int bufferpos = 0;

        public int LeftBufferSize { get; private set; }

        public IReceiveFilter<TeltonikaRequestInfo> NextReceiveFilter { get; private set; }

        public FilterState State { get; private set; }

        public TeltonikaRequestInfo Filter(byte[] readBuffer, int offset, int length, bool toBeCopied, out int rest)
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

                TeltonikaRequestInfo r = new TeltonikaRequestInfo();
                r.IMEI = GetIMEI(buffer, 0, 17);
                Console.WriteLine($"got IMEI {r.IMEI}");

                bufferpos = 0;

                return r;
            }
            else if (bufferpos >= 8 && buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0x00 && buffer[3] == 0x00)
            {
                byte[] len = { buffer[4], buffer[5], buffer[6], buffer[7] };
                int avlDataLength = global::Teltonika.Codec.BytesSwapper.Swap(BitConverter.ToInt32(len, 0));

                int packetLength = 8 + avlDataLength + 4;

                if (bufferpos < packetLength)
                {
                    return default;
                }

                //if (bufferpos > packetLength)
                //{
                //    // trailing data?
                //    // consider disconnecting
                //}

                TeltonikaRequestInfo r = new TeltonikaRequestInfo();
                r.Data = DecodeTcpPacket(buffer);

                bufferpos = 0;

                return r;
            }

            return default;
        }

        public void Reset()
        {
            bufferpos = 0;
        }



        private static global::Teltonika.Codec.Model.TcpDataPacket DecodeTcpPacket(byte[] request)
        {
            var reader = new global::Teltonika.Codec.ReverseBinaryReader(new System.IO.MemoryStream(request));
            var decoder = new global::Teltonika.Codec.DataDecoder(reader);

            var packet = decoder.DecodeTcpData();

            foreach(var data in packet.AvlData.Data)
            {
                Console.WriteLine(DebugAvl(data));
            }




            return packet;
        }

        private static string DebugAvl(global::Teltonika.Codec.Model.AvlData data)
        {

            StringBuilder sb = new StringBuilder();
            try
            {
                sb.AppendLine($"Datetime: {data.DateTime}");
                sb.AppendLine($"DateTime.Kind: {data.DateTime.Kind}");
                sb.AppendLine();

                float x = data.GpsElement.X;
                float y = data.GpsElement.Y;

                var s = data.GpsElement.Satellites;

                x /= 10000000;
                y /= 10000000;

                sb.AppendLine($"GPS is {x} {y} ({s} satelites)");
                sb.AppendLine();

                foreach (var p in data.IoElement.Properties)
                {
                    sb.AppendLine($"property {p.Id} is {p.Value}");
                }
                sb.AppendLine();
            }
            catch
            {
                sb.AppendLine("GPS is error");
            }

            return sb.ToString();
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
