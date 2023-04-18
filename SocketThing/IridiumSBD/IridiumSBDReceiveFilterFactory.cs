using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace SocketThing.IridiumSBD
{
    public class IridiumSBDReceiveFilterFactory : IReceiveFilterFactory<IridiumSBDRequestInfo>, IReceiveFilterFactory
    {
        public IReceiveFilter<IridiumSBDRequestInfo> CreateFilter(IAppServer appServer, IAppSession appSession, IPEndPoint remoteEndPoint)
        {
            return new IridiumSBDReceiveFilter();
        }
    }
}
