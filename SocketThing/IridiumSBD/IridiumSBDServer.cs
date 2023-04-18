using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketThing.IridiumSBD
{
    public class IridiumSBDServer : AppServer<IridiumSBDSession, IridiumSBDRequestInfo>
    {
        public IridiumSBDServer(int port) : base(new IridiumSBDReceiveFilterFactory())
        {
            NewRequestReceived += (IridiumSBDSession session, IridiumSBDRequestInfo requestInfo) =>
            {
                if (!string.IsNullOrEmpty(session.IMEI))
                {
                    requestInfo.IMEI = session.IMEI;
                }
                session.AcknowledgeData(requestInfo);
            };


            Thread output_thread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);


                    IEnumerable<IridiumSBDSession> sessions;
                    try
                    {
                        sessions = this.GetAllSessions();
                    }
                    catch
                    {
                        continue;
                    }

                    if (sessions == null)
                    {
                        return;
                    }

                    foreach (var s in sessions)
                    {
                        s.CheckOutput();
                    }

                }

            });
            output_thread.IsBackground = true;
            output_thread.Start();
        }
    }
}
