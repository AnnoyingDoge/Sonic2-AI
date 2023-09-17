using System;

using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;

using System.Net.WebSockets;
using System.Text;
using System.Threading;

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

        WebClient webClient = new WebClient();

        private ApiContainer APIs
            => _maybeAPIContainer!;

        //button to restart (load save state 1)
        Button restartButton = new Button() { AutoSize = true, Text = "Restart" };

        Button connectButton = new Button() { AutoSize = true, Text = "Connect", Location = new Point(50, 100) };

        Button startJump = new Button() { AutoSize = true, Text = "Hold Jump", Location = new Point(50, 50) };
        Button startRight = new Button() { AutoSize = true, Text = "Hold Right", Location = new Point(100, 50) };




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
            loadSaveState();
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            webClient.UpdateMessage(fitness.ToString());
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
                webClient.UpdateMessage(fitness.ToString());
            }

            //Draw GUI stuff so we can see what's going on
            DrawGUIElements();   
        }
    }

    
    class WebClient
    {
        private Uri uri = new Uri("ws://localhost:8001/ws");
        private string message = "msg";
        private ArraySegment<byte> messageBytes;

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

        public void UpdateMessage(string message)
        {
            this.message = message;
            messageBytes = new ArraySegment<byte>(Encoding.ASCII.GetBytes(message));
        }

        public void Run()
        {
            Connect();
        }

        async void Connect()
        {
            using (var ws = new ClientWebSocket())
            {
                //connect
                await ws.ConnectAsync(uri, CancellationToken.None);
                //buffer stuff ??? for receiving a message
                var buffer = new byte[256];
                var buffer_segment = new ArraySegment<byte>(buffer);

                while (ws.State == WebSocketState.Open)
                {
                    //send a message
                    await ws.SendAsync(messageBytes, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}
