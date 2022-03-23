using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketThing
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Teltonika.TeltonikaServer teltonikaServer = StartTeltonika(5000);
            AppServer exampleServer = StartExample(5001);
            LineAppServer testServer = StartTest(5002);
            


            while (teltonikaServer?.State == ServerState.Running || exampleServer?.State == ServerState.Running || testServer?.State == ServerState.Running)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }



        static Teltonika.TeltonikaServer StartTeltonika(int port)
        {
            Teltonika.TeltonikaServer server = new Teltonika.TeltonikaServer();


            //server.NewSessionConnected += new SessionHandler<LineAppSession>((LineAppSession session) =>
            //{
            //    session.Send("Welcome to LineAppSession");
            //});


            //server.NewRequestReceived += new RequestHandler<LineAppSession, StringRequestInfo>((LineAppSession session, StringRequestInfo request) =>
            //{
            //    session.Send($"{request.Key}: {request.Body}");
            //});


            //server.SessionClosed += new SessionHandler<LineAppSession, CloseReason>((LineAppSession session, CloseReason reason) =>
            //{
            //    Console.WriteLine("bye!");
            //});


            if (!server.Setup(port))
            {
                return null;
            }

            if (!server.Start())
            {
                return null;
            }


            return server;
        }




        static LineAppServer StartTest(int port)
        {
            LineAppServer server = new LineAppServer();


            server.NewSessionConnected += new SessionHandler<LineAppSession>((LineAppSession session) =>
            {
                session.Send("Welcome to LineAppSession");
            });


            server.NewRequestReceived += new RequestHandler<LineAppSession, StringRequestInfo>((LineAppSession session, StringRequestInfo request) =>
            {
                session.Send($"{request.Key}: {request.Body}");
            });


            server.SessionClosed += new SessionHandler<LineAppSession, CloseReason>((LineAppSession session, CloseReason reason) =>
            {
                Console.WriteLine("bye!");
            });


            if (!server.Setup(port))
            {
                return null;
            }

            if (!server.Start())
            {
                return null;
            }


            return server;
        }



        static AppServer StartExample(int port)
        {
            AppServer appServer = new AppServer();

            appServer.NewSessionConnected += new SessionHandler<AppSession>((AppSession session) =>
            {
                session.Send("Welcome to SuperSocket Telnet Server");
            });

            appServer.NewRequestReceived += new RequestHandler<AppSession, StringRequestInfo>(appServer_NewRequestReceived_Echo);
            appServer.NewRequestReceived += new RequestHandler<AppSession, StringRequestInfo>(appServer_NewRequestReceived);




            //Setup the appServer
            if (!appServer.Setup(port)) //Setup with listening port
            {
                Console.WriteLine("Failed to setup!");
                return null;
            }


            //Try to start the appServer
            if (!appServer.Start())
            {
                Console.WriteLine("Failed to start!");
                return null;
            }

            Console.WriteLine("The server started successfully!");


            //Stop the appServer
            //appServer.Stop();

            return appServer;
        }


        //static void appServer_NewSessionConnected(AppSession session)
        //{
        //    session.Send("Welcome to SuperSocket Telnet Server");
        //}


        static void appServer_NewRequestReceived(AppSession session, StringRequestInfo requestInfo)
        {
            switch (requestInfo.Key.ToUpper())
            {
                case ("ECHO"):
                    session.Send(requestInfo.Body);
                    break;

                case ("ADD"):
                    session.Send(requestInfo.Parameters.Select(p => Convert.ToInt32(p)).Sum().ToString());
                    break;

                case ("MULT"):

                    var result = 1;

                    foreach (var factor in requestInfo.Parameters.Select(p => Convert.ToInt32(p)))
                    {
                        result *= factor;
                    }

                    session.Send(result.ToString());
                    break;
            }
        }


        static void appServer_NewRequestReceived_Echo(AppSession session, StringRequestInfo requestInfo)
        {
            string p = string.Join(" ", requestInfo.Parameters);
            session.Send($"ECHO: '{requestInfo.Key}' '{requestInfo.Body}' '{p}'");
        }
    }



}
