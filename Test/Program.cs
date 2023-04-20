using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {

            TeltonikaIridiumSBDParser.Test();

            return;



            TcpListener tcp = new TcpListener(System.Net.IPAddress.Any, 5010);

            tcp.Start();

            while (true)
            {
                TcpClient client = tcp.AcceptTcpClient();

                HandleClient(client);
            }
        }


        static object _NextIndexLock = new object();
        static int _NextIndex = 0;
        static int NextIndex()
        {
            lock (_NextIndexLock)
            {
                _NextIndex++;

                return _NextIndex;
            }
        }

        public static void HandleClient(TcpClient client)
        {
            int ThreadIndex = NextIndex();

            Thread thread = null;

            thread = new Thread(() =>
            {
                byte[] bytes = new byte[1024];


                BinaryReader sr = new BinaryReader(client.GetStream());

                while (true)
                {
                    int count = sr.Read(bytes, 0, bytes.Length);

                    if (count == 0)
                    {
                        Console.WriteLine($"{thread.Name}: closed");

                        return;
                    }

                    Console.WriteLine($"{thread.Name}: {BitConverter.ToString(bytes, 0, count)}");
                }
            });
            thread.IsBackground = true;

            thread.Name = $"thread-{ThreadIndex}";
            thread.Start();
        }
    }
}
