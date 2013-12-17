using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Web;

namespace KinectCraft
{
    public delegate void SocksDelegate(string message);
    class WoollySocks
    {
        private StreamWriter swSender;
        private StreamReader srReceiver;
        private TcpClient tcpServer;
        private Thread thrMessaging;
        private IPAddress ipAddr;
        private bool Connected;
        public bool WaitForConnect = false;
        public SocksDelegate NewMessage;

        public WoollySocks() {}

        public void InitializeConnection()
        {
            // Parse the IP address

            string ipAdress = "127.0.0.1";
            ipAddr = IPAddress.Parse(ipAdress);

            /*
             * Commands:
             * move_foreward - Moveforeward
             * move_back - MovesBack
             * strafe_left - strafes left
             * strafe_right - strafes right
             * foreward[var] - Moveforeward or back variable
             * strafe[var] - strafes variable
             * idle - stop moving (affects foreward back and strafing
             * jump - start jumping
             * !jump - stop jumping
             * sneak - start sneaking
             * !sneak - stop sneaking
             * attack - Left clicks (only in world not GUI!)
             * use - Right clicks (only in world not GUI!)
             * !attack - Stop Left clicking
             * !use - Stop Right clicking
             * spinMode[spinOnly] - Switch to SpinOnly Mode
             * spinMode[look] - Switch to Look Mode
             * click_left - Left Clicks in GUI
             * click_right - Right Clicks in GUI
             * shift - holds shift key GUI
             * !shift - releases shift key GUI
             * skelLeft - Called when the skeleton leaves the frame
             * ready1 - Called when the reset points are ready'd
             * ready2 - Called when the idle points are reset
             * ready3 - Called when the reset is complete and hands are back below the head
             */

            /*
             * Receiving Commands:
             * fullscreen[true] - Is Fullscreen
             * fullscreen[false] - Not Fullscreen
             * calibrate - Begin calibration
             * setmode[lookspin] - Set Rotation Mode to Look and Spin
             * setmode[spin] - Set Rotation Mode to SpinOnly
             * setmode[hybrid] - Set Rotation Mode to Hybrid
             * setmode[switch] - Set Rotation Mode to Switch
             */

            // Start a new TCP connections to the chat server
            tcpServer = new TcpClient();
            tryConnect:
            try
            {
                tcpServer.Connect(ipAddr, 9001);
                swSender = new StreamWriter(tcpServer.GetStream());
                Connected=true;
            }
            catch (Exception e2)
            {
                if (WaitForConnect)
                    goto tryConnect;
                MessageBox.Show("Cannot Connect, No Server Was Found.");
            }
        }


        
       public void SendMessage(String p)
        {
            if (p != "")
            {
                p = HttpUtility.UrlEncode(p, System.Text.Encoding.UTF8);
                swSender.WriteLine(p);
                
                swSender.Flush();

            }

        }

        private void ReceiveMessages()
        {
            // Receive the response from the server
            srReceiver = new StreamReader(tcpServer.GetStream());
            while (Connected)
            {
                String con = srReceiver.ReadLine();
                string StringMessage = HttpUtility.UrlDecode(con, System.Text.Encoding.UTF8);

                if (NewMessage != null)
                    NewMessage(StringMessage);
            }
        }
    }
}
