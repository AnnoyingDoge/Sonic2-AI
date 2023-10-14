using System;

using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;

using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Net.MyStuff.CSharpTool
{
    [ExternalTool("CSharpTool")] // this appears in the Tools > External Tools submenu in EmuHawk
    public sealed class CSharpToolForm : ToolFormBase, IExternalToolForm
    {
        public ApiContainer? _maybeAPIContainer { get; set; }
        // ...
        protected override string WindowTitleStatic // required when superclass is ToolFormBase
            => "CSharpTool";

        int position = 0;
        int fitness = 0;

        int waitForReset = 0;

        WebClient webClient = new WebClient();

        private ApiContainer APIs
            => _maybeAPIContainer!;

        //button to restart (load save state 1)
        Button restartButton = new Button() { AutoSize = true, Text = "Restart" };

        Button connectButton = new Button() { AutoSize = true, Text = "Connect", Location = new Point(50, 100) };

        Button startJump = new Button() { AutoSize = true, Text = "Hold Jump", Location = new Point(50, 50) };
        Button startRight = new Button() { AutoSize = true, Text = "Hold Right", Location = new Point(100, 50) };


        

        private string inputMessage = "";

        dpadDirection dpadDir = dpadDirection.NONE;
        private bool jumpBool = false;

        public CSharpToolForm()
        {
            //add EventHandler to Click action, calls method on click
            restartButton.Click += new EventHandler(RestartButton_Click);
            connectButton.Click += new EventHandler(ConnectButton_Click);

            startJump.Click += new EventHandler(StartJump_Click);
            startRight.Click += new EventHandler(HoldRight_Click);


            //create tool window
            ClientSize = new Size(480, 320);
            SuspendLayout();

            Controls.Add(restartButton);
            Controls.Add(connectButton);

            Controls.Add(startJump);
            Controls.Add(startRight);
            ResumeLayout(performLayout: false);
            PerformLayout();

        }

        /// <summary>
        /// load save state 1 on restart button click
        /// </summary>
        private void RestartButton_Click(object sender, EventArgs e)
        {
            waitForReset = 1;
            loadSaveState();
            position = 0;
            fitness = 0;
            waitForReset = 0;
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            //webClient.UpdateMessage(fitness.ToString());
            webClient.Run();
        }

        private void StartJump_Click(object sender, EventArgs e)
        {
            holdingJump(true);
        }

        private void HoldRight_Click(object sender, EventArgs e)
        {
            holdingRight(true);
        }

        /// <summary>
        /// Get position of camera in level
        /// </summary>
        /// <returns>Camera postiion as unit</returns>
        public uint GetCameraPosition()
        {
            APIs.Memory.UseMemoryDomain(APIs.Memory.MainMemoryName); //position information stored in main RAM of game
            APIs.Memory.SetBigEndian(true); //position information uses big Endian
            return APIs.Memory.ReadU16(0xEE00);
        }


        public void DrawGUIElements()
        {
            APIs.Gui.Text(50, 75, "Position: " + position);
            APIs.Gui.Text(50, 50, "Fitness : " + fitness);
            APIs.Gui.Text(50, 100, inputMessage);
            APIs.Gui.Text(50, 125, dpadDir.ToString() + " " + jumpBool);
        }

        /// <summary>
        /// load save state of level beginning (training starts here)
        /// </summary>
        public void loadSaveState()
        {
            APIs.SaveState.LoadSlot(1);
        }


        #region input

        public void holdingJump(bool holding)
        {
            APIs.Joypad.Set("A", holding, 1);
        }
        public void holdingRight(bool holding)
        {
            APIs.Joypad.Set("Right", holding, 1);
        }

        #endregion

        public override void Restart()
        {
            // executed once after the constructor, and again every time a rom is loaded or reloaded
            loadSaveState();
        }

        // executed after every frame (except while turboing, use FastUpdateAfter for that)
        protected override void UpdateAfter()
        {
            position = (int)GetCameraPosition();


            //fitness increases when position is greater than fitness, i.e. fitness score = furthest position
            if (position > fitness)
            {
                fitness = position;
            }
            webClient.makePacket(new int[] { fitness, position, waitForReset });

            //get input stuff from websocket
            inputMessage = webClient.getMessage();

            parseInputMessage(inputMessage);

            doInputs();

            //Draw GUI stuff so we can see what's going on
            DrawGUIElements();
        }

        /// <summary>
        /// Take an input message from websocket and turn into enum and boolean
        /// </summary>
        /// <param name="msg"></param>
        public void parseInputMessage(string msg)
        {
            int splitIndex = -1;
            for(int i = 0; i < msg.Length; i++)
            {
                if (msg[i].Equals('|'))
                {
                    splitIndex = i;
                    break;
                }
            }
            if(splitIndex != -1)
            {
                string[] parts = msg.Split('|');
                Int32.TryParse(parts[0], out int i);
                dpadDir = (dpadDirection)(i);
                Boolean.TryParse(parts[1], out bool b);
                jumpBool = b;
            }
        }

        private void doInputs()
        {
            bool move = true;
            string dpad = "Up";
            switch (dpadDir)
            {
                case dpadDirection.NONE:
                    move = false;
                    break;
                case dpadDirection.LEFT:
                    dpad = "Left";
                    break;
                case dpadDirection.RIGHT:
                    dpad = "Right";
                    break;
                case dpadDirection.UP:
                    dpad = "Up";
                    break;
                case dpadDirection.DOWN:
                    dpad = "Down";
                    break;
            }

            APIs.Joypad.Set(dpad, move, 1);
            APIs.Joypad.Set("A", jumpBool, 1);

        }

        public void dpadUpdate(dpadDirection d)
        {
            dpadDir = d;
        }

        public enum dpadDirection
        {
            NONE = 0,
            LEFT = 1, 
            RIGHT = 2,
            UP = 3,
            DOWN = 4
        }
    }

    
    class WebClient
    {
        //stuff to connect / send
        private Uri uri = new Uri("ws://localhost:8001/ws");
        private string message = "msg";
        private ArraySegment<byte> messageBytes;

        private string receivedMessage = "";

        #region constructors
        public WebClient()
        {
            messageBytes = new ArraySegment<byte>(Encoding.ASCII.GetBytes(message));
        }

        public WebClient(Uri uri)
        {
            this.uri = uri;
            messageBytes = new ArraySegment<byte>(Encoding.ASCII.GetBytes(message));
        }

        public WebClient(Uri uri, string message)
        {
            this.uri = uri;
            UpdateMessage(message);
        }
        #endregion

        public void makePacket(int[] data)
        {
            //packet we build
            List<byte> packet = new List<byte>();

            for (int i = 0; i < data.Length; i++)
            {
                //add 4 byte pattern before integers
                for(int j = 0; j < 4; j++)
                {
                    packet.Add(byte.MaxValue);
                }

                //bytes of int
                byte[] byteArr = BitConverter.GetBytes(data[i]);

                //reverse to big endian if we are in little endian
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(byteArr);

                //put all bytes into message
                foreach (byte b in byteArr)
                {
                    packet.Add(b);
                }
            }

            ////strings
            //for (int i = 0; i < strings.Length; i++)
            //{
            //    //add 4 byte pattern before strings
            //    for (int j = 0; j < 3; j++)
            //    {
            //        packet.Add(byte.MaxValue);
            //    }
            //    packet.Add(byte.MinValue);

            //    //bytes of string
            //    byte[] byteArr = Encoding.UTF8.GetBytes(strings[i]);

            //    //put all bytes into message
            //    foreach (byte b in byteArr)
            //    {
            //        packet.Add(b);
            //    }

            //}



            //convert packet to arraysegment
            messageBytes = new ArraySegment<byte>(packet.ToArray());


        }

        /// <summary>
        /// update the message being sent
        /// </summary>
        /// <param name="message"> update message being sent</param>
        public void UpdateMessage(string message)
        {
            this.message = message;
            messageBytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
        }

        public string getMessage()
        {
            return receivedMessage;
        }

        public void Run()
        {
            _ = Connect();
        }

        async Task<int> Connect()
        {
            using (var ws = new ClientWebSocket())
            {
                //connect
                await ws.ConnectAsync(uri, CancellationToken.None);
                //buffer stuff for receiving a message
                var buffer = new byte[256];
                var buffer_segment = new ArraySegment<byte>(buffer);

                while (ws.State == WebSocketState.Open)
                {
                    //send a message
                    await ws.SendAsync(messageBytes, WebSocketMessageType.Binary, true, CancellationToken.None);
                    
                    WebSocketReceiveResult received = await ws.ReceiveAsync(buffer_segment, CancellationToken.None);

                    //get rid of extra bytes on message
                    byte[] receivedBytes = new byte[received.Count];
                    for(int i = 0; i < received.Count; i++)
                    {
                        receivedBytes[i] = buffer_segment.Array[i];
                    }
                    //convert resulting message to string
                    receivedMessage = Encoding.UTF8.GetString(receivedBytes);

                    buffer_segment = new ArraySegment<byte>(buffer);
                }
            }

            return 0;
        }
    }
}
