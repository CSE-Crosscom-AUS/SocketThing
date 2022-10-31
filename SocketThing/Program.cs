using SocketThing.Teltonika;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Teltonika.Codec;
using Teltonika.Codec.Model;
using static System.Collections.Specialized.BitVector32;

namespace SocketThing
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Go();
        }




        static void Go()
        {

            Teltonika.TeltonikaServer teltonikaServer = StartTeltonika(5000);
            AppServer exampleServer = StartExample(5001);
            LineAppServer testServer = StartTest(5002);

            teltonikaServer.NewSessionConnected += (Teltonika.TeltonikaSession session) =>
            {
                SendTestOdo(session);

            };




            while (teltonikaServer?.State == ServerState.Running || exampleServer?.State == ServerState.Running || testServer?.State == ServerState.Running)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));


                //if (teltonikaServer.GetAllSessions().Any())
                //{
                //    var s = teltonikaServer.GetAllSessions()?.First();

                //    if (s != null)
                //    {
                //        SendTestOdo(s);
                //    }
                //}

            }
        }



        private static void TestCommand(string d)
        {
            byte[] command = HexStringToBytes(d);

            int crc = global::Teltonika.Codec.CRC.CalcCrc16(command, 0, command.Length, 0xA001, 0);
            int crc2 = TeltonikaCommand.Crc(command, 0, command.Length);

            Console.WriteLine(d);
            Console.WriteLine($"crc  is {crc:X4} {crc}");
            Console.WriteLine($"crc2 is {crc2:X4} {crc2}");
            Console.WriteLine();
        }

        private static void SendTestOdo(TeltonikaSession session)
        {
            var now = DateTime.Now;

            var o = now.Hour * 100 * 100 + now.Minute * 100 + now.Second;

            var c = $"odoset:{o}";
            Console.WriteLine(c);

            var c2 = TeltonikaCommand.MakeCodec12Command(c);
            //var c = TeltonikaCommand.MakeCodec12Command($"odoset:{o}");

            var encoding = Encoding.GetEncoding("ISO-8859-1");
            session.Send(encoding.GetString(c2));
            //session.Send()
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



        public static byte[] HexStringToBytes(string s)
        {
            if (s.Length % 2 != 0)
            {
                throw new ArgumentException("must be a hex string with even number of bytes");
            }

            byte[] bytes = new byte[s.Length / 2];

            for (int i = 0; i < s.Length / 2; i++)
            {
                bytes[i] = (byte)(HexDigit(s[i * 2]) * 16 + HexDigit(s[i * 2 + 1]));
            }

            return bytes;
        }


        public static byte HexDigit(char c)
        {
            c = char.ToLower(c);

            switch (c)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'a': return 10;
                case 'b': return 11;
                case 'c': return 12;
                case 'd': return 13;
                case 'e': return 14;
                case 'f': return 15;
                default: throw new ArgumentException($"Argument {c} is not a hex digit.");
            }
        }
    }



}
