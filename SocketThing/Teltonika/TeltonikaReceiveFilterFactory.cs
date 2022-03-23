using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace SocketThing.Teltonika
{
    public class TeltonikaReceiveFilterFactory : IReceiveFilterFactory<TeltonikaRequestInfo>, IReceiveFilterFactory
    {
        public IReceiveFilter<TeltonikaRequestInfo> CreateFilter(IAppServer appServer, IAppSession appSession, IPEndPoint remoteEndPoint)
        {
            return new TeltonikaReceiveFilter();
        }
    }
}
