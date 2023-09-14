using System;

using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;

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

        private ApiContainer APIs
            => _maybeAPIContainer!;

        public CSharpToolForm()
        {
            ClientSize = new Size(480, 320);
            SuspendLayout();
            Controls.Add(new Label { AutoSize = true, Text = "Hello, world!" });
            ResumeLayout(performLayout: false);
            PerformLayout();
        }

        public uint GetCameraPosition()
        {
            return APIs.Memory.ReadU16(0xEE00);
        }

        public override void Restart()
        {
            // executed once after the constructor, and again every time a rom is loaded or reloaded
        }

        protected override void UpdateAfter()
        {
            APIs.Memory.UseMemoryDomain(APIs.Memory.MainMemoryName);
            position = (int)GetCameraPosition();

            APIs.Memory.SetBigEndian(true);
            APIs.Gui.Text(50, 150, "Position: " + position);
            APIs.Gui.Text(50, 100, "Position2: " + APIs.Memory.ReadS16(0xEE00));
            APIs.Gui.Text(50, 50, "Position3: " + APIs.Memory.ReadFloat(0xEE00));


            // executed after every frame (except while turboing, use FastUpdateAfter for that)
        }
    }
}
