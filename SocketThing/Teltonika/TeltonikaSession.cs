using SuperSocket.SocketBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace SocketThing.Teltonika
{
    public class TeltonikaSession : AppSession<TeltonikaSession, TeltonikaRequestInfo>
    {
        public string IMEI { get; set; }

        private bool AcknowledgedIMEI = false;
        private int AcknowledgedReportCount = 0;
        private int SendCount = 0;



        protected override void OnSessionStarted()
        {
            Charset = Encoding.GetEncoding("ISO-8859-1");

            //this.Send("Welcome to SuperSocket Teltonika Server");
            Console.WriteLine($"New Session! {SessionID}");
        }

        protected override void HandleException(Exception e)
        {
            Console.WriteLine($"Exception occured {e}");
        }

        protected override void OnSessionClosed(CloseReason reason)
        {
            //add you logics which will be executed after the session is closed
            base.OnSessionClosed(reason);

            Console.WriteLine($"Session closed {IMEI} {reason}");
        }


        internal void AcknowledgeData(TeltonikaRequestInfo requestInfo)
        {

            if (!AcknowledgedIMEI)
            {
                if (string.IsNullOrEmpty(requestInfo.IMEI))
                {
                    Console.WriteLine("we should never get here");
                }
                else
                {

                    byte[] response = new byte[] { 0x01 };

                    Console.WriteLine("Acknowledge IMEI");
                    base.Send(response, 0, response.Length); // send 0x01?

                    AcknowledgedIMEI = true;
                    IMEI = requestInfo.IMEI;

                    Console.WriteLine($"New Connection IMEI: {IMEI}");


                    //this.Flush();
                }

            }
            else
            {



                Console.WriteLine($"More data! IMEI: {IMEI}");
                //Int32 count = 1;
                Int32 count = requestInfo.Data.AvlData.DataCount;

                Console.WriteLine($"Acknowledge {count}");

                if (count > 0)
                {
                    byte[] response = BitConverter.GetBytes(global::Teltonika.Codec.BytesSwapper.Swap(count));

                    base.Send(response, 0, response.Length); // send requestInfo.Data.DataCount as 32 bit int in the byte order specified in example

                }

                AcknowledgedReportCount += count;

                //this.FlushOutput();
            }
        }


        private Queue<(byte[], int, int)> outputQueue = new Queue<(byte[], int, int)>();

        public override void Send(string message)
        {
            var b = Encoding.GetEncoding("ISO-8859-1").GetBytes(message);
            this.Send(b, 0, b.Length);
        }

        public override void Send(byte[] data, int offset, int length)
        {
            outputQueue.Enqueue((data, offset, length));
        }

        public void FlushOutput()
        {
            if (!AcknowledgedIMEI)
            {
                return;
            }

            if (AcknowledgedReportCount <= 0)
            {
                return;
            }

            Console.WriteLine($"Flush {outputQueue.Count}!");

            if (outputQueue.Count > 0)
            {
                var a = outputQueue.Dequeue();

                string s = BitConverter.ToString(a.Item1, a.Item2, a.Item3).Replace("-", "");
                Console.WriteLine($"Sending {SendCount}: " + s);
                base.Send(a.Item1, a.Item2, a.Item3);
                SendCount++;
            }
        }


        public void CheckOutput()
        {
            if (DateTime.Now - this.LastActiveTime > TimeSpan.FromSeconds(2) && outputQueue.Count > 0 && AcknowledgedReportCount > 0)
            {
                FlushOutput();
            }
        }

    }
}
