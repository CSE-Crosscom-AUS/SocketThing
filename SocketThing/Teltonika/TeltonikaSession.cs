using SuperSocket.SocketBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketThing.Teltonika
{
    public class TeltonikaSession : AppSession<TeltonikaSession, TeltonikaRequestInfo>
    {
        public string IMEI { get; set; }

        private bool AcknowledgedIMEI = false;


        protected override void OnSessionStarted()
        {
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
                    this.Send(response, 0, response.Length); // send 0x01?
                    AcknowledgedIMEI = true;
                    IMEI = requestInfo.IMEI;

                    Console.WriteLine($"New Connection IMEI: {IMEI}");




                    //global::Teltonika.Codec.Model.Command odo = new global::Teltonika.Codec.Model.Command(0x0C, System.Text.Encoding.ASCII.GetBytes("odoset:121212"));
                    //byte[] message = global::Teltonika.Codec.Codecs.Codec12.Encode(odo);
                    //this.Send(message, 0, message.Length);

                    int distance_metres = 666666;
                    byte[] command = TeltonikaCommand.MakeCodec12Command($"odoset:{distance_metres}");
                    this.Send(command, 0, command.Length);
                }

            }
            else
            {
                Console.WriteLine($"More data! IMEI: {IMEI}");
                //Int32 count = 1;
                Int32 count = requestInfo.Data.AvlData.DataCount;

                Console.WriteLine($"Acknowledge {count}");

                byte[] response = BitConverter.GetBytes(global::Teltonika.Codec.BytesSwapper.Swap(count));
                

                this.Send(response, 0, response.Length); // send requestInfo.Data.DataCount as 32 bit int in the byte order specified in example
            }
        }
    }
}
