using SuperSocket.SocketBase.Protocol;

namespace SocketThing.Teltonika
{
    public class TeltonikaRequestInfo : IRequestInfo
    {
        public string Key => "Teltonika";

        public string IMEI { get; set; }
        public global::Teltonika.Codec.Model.TcpDataPacket Data { get; set; }
    }
}
