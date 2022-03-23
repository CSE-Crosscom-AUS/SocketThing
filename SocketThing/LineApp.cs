using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketThing
{
    public class LineAppSession : AppSession<LineAppSession>
    {
        protected override void OnSessionStarted()
        {
            //this.Send("Welcome to SuperSocket Line Server");
        }

        protected override void HandleUnknownRequest(StringRequestInfo requestInfo)
        {
            this.Send("Unknow request");
        }

        protected override void HandleException(Exception e)
        {
            this.Send("Application error: {0}", e.Message);
        }

        protected override void OnSessionClosed(CloseReason reason)
        {
            //add you logics which will be executed after the session is closed
            base.OnSessionClosed(reason);
        }
    }



    public class LineRequestParser : IRequestInfoParser<StringRequestInfo>
    {
        public StringRequestInfo ParseRequestInfo(string source)
        {
            return new StringRequestInfo("line", source, null);
        }
    }

    public class LineAppServer : AppServer<LineAppSession>
    {
        public LineAppServer() : base(new CommandLineReceiveFilterFactory(System.Text.Encoding.UTF8, new LineRequestParser()))
        {
        }
    }
}
