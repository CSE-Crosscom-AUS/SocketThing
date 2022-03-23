using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketThing.Teltonika
{
    public class TeltonikaServer : AppServer<TeltonikaSession, TeltonikaRequestInfo>
    {
        public TeltonikaServer() : base(new TeltonikaReceiveFilterFactory())
        {
            NewRequestReceived += (TeltonikaSession session, TeltonikaRequestInfo requestInfo) =>
            {
                if (!string.IsNullOrEmpty(session.IMEI))
                {
                    requestInfo.IMEI = session.IMEI;
                }
                session.AcknowledgeData(requestInfo);
            };
        }

    }
}
