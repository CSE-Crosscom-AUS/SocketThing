using SuperSocket.SocketBase.Protocol;

namespace SocketThing.IridiumSBD
{
    public class IridiumSBDRequestInfo : IRequestInfo
    {
        public string Key => "IridiumSBD";

        public string IMEI { get; set; }
        public byte[] SBDData { get; set; }
    }
}
