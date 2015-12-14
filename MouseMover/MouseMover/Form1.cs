using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using System.Runtime.InteropServices;

namespace MouseMover
{
    public partial class MainForm : Form
    {
        //Import the LastInputInfo function
        [DllImport("User32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        //Import the SendInput function
        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, InputProvider[] pInputs, int cbSize);

        //A brace of structures for holding input:
        //Mouse-movement based on code from http://csharp-tricks-en.blogspot.co.uk/2011/07/control-mouse.html
        struct MouseInput   
        {
            public int X;
            public int Y;
            public uint mouseData;
            public uint mouseFlag;  //whether it's a wheel, button press, movement, etc.
            public uint time; // time of the event
            public IntPtr extraInfo; // further information
        }

        struct InputProvider    //Feeds the data to the SendInput method
        {
            public int type; // type of the input, 0 for mouse  
            public MouseInput data; // mouse data
        }

        bool running = false;
        static uint GetLastInputTime()
        {   //Calculates the time since last input in seconds
            //Sourced from http://www.pinvoke.net/default.aspx/user32.GetLastInputInfo
            uint idleTime = 0;
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            uint envTicks = (uint)Environment.TickCount;

            if (GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime;
                idleTime = envTicks - lastInputTick;
            }
            return ((idleTime > 0) ? (idleTime / 1000) : 0);
        }

        System.Timers.Timer watchTimer = new System.Timers.Timer(60000);    //For now just check once a minute. Maybe later make this user-configurable
        public MainForm()
        {
            InitializeComponent();
            watchTimer.Enabled = false;
            watchTimer.Elapsed += new ElapsedEventHandler(watchTimer_Elapsed);
        }

        void watchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {   //When the timer expires, check the time since the last input
            if (GetLastInputTime() > 60)    //Basically means we'll only move once every two minutes; that's fine
            {
                Console.WriteLine("User has been idle for " + GetLastInputTime().ToString() + " seconds.");
                
                //Move the mouse slightly
                MouseInput movement = new MouseInput();
                movement.X = 0;
                movement.Y = 0;
                movement.mouseData = 0;
                movement.time = 0;
                movement.mouseFlag = 0x8000 | 0x0001; //Those are constants for absolute screen position and "mouse move" respectively
                InputProvider[] MouseEvent = new InputProvider[1];
                MouseEvent[0].type = 0;
                MouseEvent[0].data = movement;
                SendInput((uint)MouseEvent.Length, MouseEvent, Marshal.SizeOf(MouseEvent[0].GetType()));
            }
        }

        private void startButton_Click(object sender, EventArgs e)
        {   //Start or stop the monitoring
            if (!running)
            {
                watchTimer.Enabled = true;
                watchTimer.Stop();
                watchTimer.Start();
                startButton.Text = "Stop";
                running = true;
            }
            else
            {
                watchTimer.Enabled = false;
                watchTimer.Stop();
                startButton.Text="Start";
                running = false;
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {   //Hide this in the system tray instead of in the taskbar
            if (WindowState==FormWindowState.Minimized)
            {
                notifyIcon.Visible = true;
                Hide();
            }
            else
            {
                notifyIcon.Visible = false;
            }
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }
    }
}
