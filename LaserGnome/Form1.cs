using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using BeatDetector;

namespace Lasergnome
{
    public partial class Form1 : Form
    {
        Arduinome arduinome;
        Timer updateStateTimer = new Timer();
        int currentEffect = 0;
        int currentPattern = 0;
        IntPtr laserOSWindow = IntPtr.Zero;
        IEnumerable<XElement> buttonMapping;
        bool blinkState = false;
        List<int> blinkingButtons = new List<int>();
        List<int> staticButtons = new List<int>();

        bool globalRandomIsRunning = false;
        int globalRandomButton = -1;

        IEnumerable<XElement> programSteps;
        int programRepeat = -1;
        int programCurrentStep = 0;
        bool programRandom = false;
        int programGlobalRunfor = 0;
        Timer programTimer = new Timer();

        private const int WM_CLOSE = 16;
        private const int BN_CLICKED = 245;

        [DllImport("User32.Dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("User32.Dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(int hWnd, int msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        static extern IntPtr GetNextWindow(IntPtr hWnd, uint wCmd);

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        internal static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("user32.dll")]
        public static extern int PostMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(ref Point lpPoint);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);


        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        internal struct INPUT
        {
            public UInt32 Type;
            public MOUSEKEYBDHARDWAREINPUT Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
        }

        internal struct MOUSEINPUT
        {
            public Int32 X;
            public Int32 Y;
            public UInt32 MouseData;
            public UInt32 Flags;
            public UInt32 Time;
            public IntPtr ExtraInfo;
        }

        public static void ClickOnPoint(IntPtr wndHandle, Point clientPoint)
        {
            var oldPos = Cursor.Position;

            /// get screen coordinates
            // ClientToScreen(wndHandle, ref clientPoint);

            /// set cursor on coords, and press mouse
            Cursor.Position = new Point(clientPoint.X, clientPoint.Y);

            var inputMouseDown = new INPUT();
            inputMouseDown.Type = 0; /// input type mouse
            inputMouseDown.Data.Mouse.Flags = 0x0002; /// left button down

            var inputMouseUp = new INPUT();
            inputMouseUp.Type = 0; /// input type mouse
            inputMouseUp.Data.Mouse.Flags = 0x0004; /// left button up

            var inputs = new INPUT[] { inputMouseDown, inputMouseUp };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

            /// return mouse 
            Cursor.Position = oldPos;
        }

        public Form1()
        {
            InitializeComponent();

            arduinome = new Arduinome();

            arduinome.ButtonDown += ButtonDown;
            arduinome.ButtonUp += ButtonUp;

            updateStateTimer.Tick += UpdateStateTimer_Tick;
            updateStateTimer.Interval = 250;
            updateStateTimer.Enabled = true;
            updateStateTimer.Start();

            programTimer.Tick += programTimer_Tick;

            XDocument data = XDocument.Load(Application.StartupPath + "\\resources\\settings.xml");
            buttonMapping = data.Descendants("button");      
        }

        private void ButtonUp(object sender, ButtonEventArgs e)
        {
            HandleButtonUp(e.X, e.Y);
        }

        public void HandleButtonUp(int x, int y)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<int, int>(HandleButtonUp), new object[] { x, y });
                return;
            }

            arduinome.setLed((byte)x, (byte)y, false);
        }

        private void ButtonDown(object sender, ButtonEventArgs e)
        {
            HandleButtonDown(e.X, e.Y);
        }

        public void HandleButtonDown(int x, int y)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<int, int>(HandleButtonDown), new object[] { x, y });
                return;
            }

            arduinome.setLed((byte)x, (byte)y, true);

            // Effect button.
            if (y == 7) {
                SendEffectKey(x);
            }
            
            if (y < 7) {
                int buttonNumber = (y * 8) + x +1;
                if (buttonMapping.Any(a => a.Attribute("id").Value == buttonNumber.ToString()))
                {
                    XElement button = buttonMapping.First(a => a.Attribute("id").Value == buttonNumber.ToString());

                    // Run program.
                    if (button.Descendants("program").Count() == 1)
                    {
                        if (globalRandomIsRunning)
                        {
                            setRandomOff();
                            removeStaticButton(globalRandomButton);
                            globalRandomIsRunning = false;
                        }
                        removeBlinkingButton(currentPattern);
                        #region Programs
                        programSteps = button.Descendants("program").First().Descendants("step");
                        if(button.Descendants("program").First().Attribute("repeat") != null)
                        {
                            if (button.Descendants("program").First().Attribute("repeat").Value == "true")
                                programRepeat = -1;
                            else
                                programRepeat = int.Parse(button.Descendants("program").First().Attribute("repeat").Value);
                        }

                        if (button.Descendants("program").First().Attribute("random") != null)
                        {
                            if (button.Descendants("program").First().Attribute("random").Value == "true")
                                programRandom = true;
                            else
                                programRandom = false;
                        }

                        if (button.Descendants("program").First().Attribute("runfor") != null)
                            programGlobalRunfor = int.Parse(button.Descendants("program").First().Attribute("runfor").Value);
                        else
                            programGlobalRunfor = 5000;

                        programCurrentStep = 0;
                        runProgramStep();

                        // Set the blinking button.
                        currentPattern = buttonNumber - 1;
                        addBlinkingButton(buttonNumber - 1);
                        #endregion
                    }
                    else
                    {
                        #region Other commands
                        stopProgram();
                        if (button.Descendants("pattern").Count() == 1)
                        {
                            if (globalRandomIsRunning)
                            {
                                setRandomOff();
                                removeStaticButton(globalRandomButton);
                                globalRandomIsRunning = false;
                            }

                            SendKey(button.Descendants("pattern").First().Attribute("key").Value);
                            removeBlinkingButton(currentPattern);
                            currentPattern = buttonNumber - 1;
                            addBlinkingButton(currentPattern);
                        }

                        if (button.Descendants("effect").Count() == 1)
                        {
                            string effect = button.Descendants("effect").First().Attribute("key").Value;
                            SendEffectKey(int.Parse(effect));
                        }

                        if (button.Descendants("special").Count() == 1)
                        {
                            string func = button.Descendants("special").First().Attribute("key").Value;

                            switch (func)
                            {
                                case "[random]":
                                    removeBlinkingButton(currentPattern);
                                    globalRandomButton = buttonNumber - 1;
                                    Console.WriteLine(globalRandomIsRunning.ToString());
                                    if (globalRandomIsRunning)
                                    {
                                        setRandomOff();
                                        removeStaticButton(buttonNumber - 1);
                                        globalRandomIsRunning = false;
                                    }
                                    else
                                    {
                                        setRandomOn();
                                        addStaticButton(buttonNumber - 1);
                                        globalRandomIsRunning = true;
                                    }

                                    break;
                                case "[preview]":
                                    SendKey("+{s}");
                                    break;
                                case "[bringtofront]":
                                    if (laserOSWindow != IntPtr.Zero)
                                    {
                                        ShowWindow(laserOSWindow, 9);
                                        SetForegroundWindow(laserOSWindow);
                                    }
                                    break;
                            }
                        }
                        #endregion
                    }
                }
            }
        }

        private void resetAll()
        {
            stopProgram();
            removeBlinkingButton(currentPattern);

            if (globalRandomIsRunning)
            {
                setRandomOff();
                removeStaticButton(globalRandomButton);
                globalRandomIsRunning = false;
            }
        }

        private bool setRandomOn()
        {
            if (laserOSWindow != IntPtr.Zero)
            {
                Point oldPos = Cursor.Position;

                var bmp = new Bitmap(561, 310, PixelFormat.Format32bppArgb);
                Graphics graphics = Graphics.FromImage(bmp);

                var inputMouseDown = new INPUT();
                inputMouseDown.Type = 0; /// input type mouse
                inputMouseDown.Data.Mouse.Flags = 0x0002; /// left button down

                var inputMouseUp = new INPUT();
                inputMouseUp.Type = 0; /// input type mouse
                inputMouseUp.Data.Mouse.Flags = 0x0004; /// left button up
                var inputs = new INPUT[] { inputMouseDown, inputMouseUp };

                while (GetForegroundWindow() != laserOSWindow)
                    SetForegroundWindow(laserOSWindow);

                RECT rct = new RECT();
                GetWindowRect(laserOSWindow, ref rct);

                graphics.CopyFromScreen(rct.Left, rct.Top, 0, 0, new Size((rct.Right - rct.Left), (rct.Bottom - rct.Top)), CopyPixelOperation.SourceCopy);
                Color c = bmp.GetPixel(215, 245);

                // 88 = off
                // 121 = off
                // 171 = on
                if (c.B == 171) return true;

                Point cursor = new Point(rct.Left + 215, rct.Top + 245);
                while (Cursor.Position != cursor)
                    Cursor.Position = cursor;

                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
                graphics.CopyFromScreen(rct.Left, rct.Top, 0, 0, new Size((rct.Right - rct.Left), (rct.Bottom - rct.Top)), CopyPixelOperation.SourceCopy);

                c = bmp.GetPixel(215, 245);
                if (c.B != 171) return false;
            }

            return true;
        }

        private bool setRandomOff()
        {
            if (laserOSWindow != IntPtr.Zero)
            {
                Point oldPos = Cursor.Position;

                var bmp = new Bitmap(561, 310, PixelFormat.Format32bppArgb);
                Graphics graphics = Graphics.FromImage(bmp);

                var inputMouseDown = new INPUT();
                inputMouseDown.Type = 0; /// input type mouse
                inputMouseDown.Data.Mouse.Flags = 0x0002; /// left button down

                var inputMouseUp = new INPUT();
                inputMouseUp.Type = 0; /// input type mouse
                inputMouseUp.Data.Mouse.Flags = 0x0004; /// left button up
                var inputs = new INPUT[] { inputMouseDown, inputMouseUp };

                while (GetForegroundWindow() != laserOSWindow)
                    SetForegroundWindow(laserOSWindow);

                RECT rct = new RECT();
                GetWindowRect(laserOSWindow, ref rct);

                graphics.CopyFromScreen(rct.Left, rct.Top, 0, 0, new Size((rct.Right - rct.Left), (rct.Bottom - rct.Top)), CopyPixelOperation.SourceCopy);
                Color c = bmp.GetPixel(215, 245);

                // 88 = off
                // 121 = off
                // 171 = on
                if (c.B == 88 || c.B == 121) return true;

                Point cursor = new Point(rct.Left + 215, rct.Top + 245);
                while (Cursor.Position != cursor)
                    Cursor.Position = cursor;

                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
                graphics.CopyFromScreen(rct.Left, rct.Top, 0, 0, new Size((rct.Right - rct.Left), (rct.Bottom - rct.Top)), CopyPixelOperation.SourceCopy);

                c = bmp.GetPixel(215, 245);
                if (c.B != 88 && c.B != 121) return false;
            }

            return true;
        }

        private void sendPrevious()
        { 
            // send previous
            var inputMouseDown = new INPUT();
            inputMouseDown.Type = 0; /// input type mouse
            inputMouseDown.Data.Mouse.Flags = 0x0002; /// left button down

            var inputMouseUp = new INPUT();
            inputMouseUp.Type = 0; /// input type mouse
            inputMouseUp.Data.Mouse.Flags = 0x0004; /// left button up
            var inputs = new INPUT[] { inputMouseDown, inputMouseUp };

            while (GetForegroundWindow() != laserOSWindow)
                SetForegroundWindow(laserOSWindow);

            RECT rct = new RECT();
            GetWindowRect(laserOSWindow, ref rct);

            Point cursor = new Point(rct.Left + 281, rct.Top + 259);
            while (Cursor.Position != cursor)
                Cursor.Position = cursor;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void sendNext()
        {
            // send previous
            var inputMouseDown = new INPUT();
            inputMouseDown.Type = 0; /// input type mouse
            inputMouseDown.Data.Mouse.Flags = 0x0002; /// left button down

            var inputMouseUp = new INPUT();
            inputMouseUp.Type = 0; /// input type mouse
            inputMouseUp.Data.Mouse.Flags = 0x0004; /// left button up
            var inputs = new INPUT[] { inputMouseDown, inputMouseUp };

            while (GetForegroundWindow() != laserOSWindow)
                SetForegroundWindow(laserOSWindow);

            RECT rct = new RECT();
            GetWindowRect(laserOSWindow, ref rct);

            Point cursor = new Point(rct.Left + 340, rct.Top + 259);
            while (Cursor.Position != cursor)
                Cursor.Position = cursor;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void stopProgram()
        {
            if(programTimer.Enabled)
            {
                programTimer.Enabled = false;
                programTimer.Stop();
                removeBlinkingButton(currentPattern);
            }
        }

        private void runProgramStep()
        {
            if (globalRandomIsRunning)
            {
                setRandomOff();
                globalRandomIsRunning = false;
            }

            XElement step = programSteps.ElementAt(programCurrentStep);
            int time = programGlobalRunfor;
            if (step.Attribute("runfor") != null)
                time = int.Parse(step.Attribute("runfor").Value);

            if (step.Descendants("pattern").Count() == 1)
            {
                SendKey(step.Descendants("pattern").First().Attribute("key").Value);
            }

            if (step.Descendants("effect").Count() == 1)
            {
                string effect = step.Descendants("effect").First().Attribute("key").Value;
                SendEffectKey(int.Parse(effect));
            }

            if (step.Descendants("special").Count() == 1)
            {
                string func = step.Descendants("special").First().Attribute("key").Value;
                if(func == "[random]")
                {
                    setRandomOn();
                    globalRandomIsRunning = true;
                    sendNext();
                }
            }

            if (programRandom)
            {
                Random rnd = new Random();
                programCurrentStep = rnd.Next(0, (programSteps.Count() - 1));
            }
            else
            {
                if (programRepeat == -1)
                {
                    if (programSteps.Count() > programCurrentStep + 1)
                        programCurrentStep++;
                    else
                        programCurrentStep = 0;
                }
                else if (programRepeat > 0)
                {
                    if (programSteps.Count() > programCurrentStep + 1)
                        programCurrentStep++;
                    else
                    {
                        programCurrentStep = 0;
                        programRepeat--;
                    }
                }
            }

            if (programRepeat != 0)
            {
                programTimer.Interval = time;
                programTimer.Enabled = true;
                programTimer.Start();
            }
            else
                stopProgram();

        }

        private LaserOSStatus checkLaserOSStatus()
        {
            LaserOSStatus retStats = new LaserOSStatus() { LaserOn = false, AutoRandomMode = false, SimulatorOn = false };

            if(laserOSWindow != IntPtr.Zero)
            {
                Color c;
                var bmp = new Bitmap(561, 310, PixelFormat.Format32bppArgb);
                Graphics graphics = Graphics.FromImage(bmp);
/*
                IntPtr oldWindow = GetForegroundWindow();
                while (GetForegroundWindow() != laserOSWindow)
                    SetForegroundWindow(laserOSWindow);
*/
                RECT rct = new RECT();
                GetWindowRect(laserOSWindow, ref rct);

                graphics.CopyFromScreen(rct.Left, rct.Top, 0, 0, new Size((rct.Right - rct.Left), (rct.Bottom - rct.Top)), CopyPixelOperation.SourceCopy);

                c = bmp.GetPixel(68, 73);
                if (c.R == 151 && c.G == 255 && c.B == 15) // ARGB=(255, 248, 108, 134) == Laser off || ARGB=(255, 151, 255, 15) == Laser on
                    retStats.LaserOn = true;

                c = bmp.GetPixel(433, 78);
                if (c.R == 29 && c.G == 78 && c.B == 97) // ARGB=(255, 34, 59, 74) == Simulator off || ARGB=(255, 29, 78, 97) == Simulator on
                    retStats.SimulatorOn = true;

                c = bmp.GetPixel(215, 245);
                if (c.R == 153 && c.G == 159 && c.B == 171) // ARGB=(255, 153, 159, 171) == Random on || ARGB=(255, 51, 63, 88) || ARGB=(255, 92, 101, 121) == Random off
                    retStats.AutoRandomMode = true;
/*
                while (GetForegroundWindow() != oldWindow)
                    SetForegroundWindow(oldWindow);
 */
            }
            return retStats;
        }

        private void programTimer_Tick(object sender, EventArgs e)
        {
            runProgramStep();
        }

        private void addBlinkingButton(int buttonNo)
        {
            if(!blinkingButtons.Contains(buttonNo)) {
                blinkingButtons.Add(buttonNo);
            }
        }

        private void removeBlinkingButton(int buttonNo)
        {
            if (blinkingButtons.Contains(buttonNo))
            {
                blinkingButtons.Remove(buttonNo);
                int y = buttonNo / 8;
                int x = buttonNo - (y * 8);

                arduinome.setLed((byte)x, (byte)y, false);
            }
        }

        private void addStaticButton(int buttonNo)
        {
            if (!staticButtons.Contains(buttonNo))
            {
                staticButtons.Add(buttonNo);
                int y = buttonNo / 8;
                int x = buttonNo - (y * 8);

                arduinome.setLed((byte)x, (byte)y, true);
            }
        }

        private void removeStaticButton(int buttonNo)
        {
            if (staticButtons.Contains(buttonNo))
            {
                staticButtons.Remove(buttonNo);
                int y = buttonNo / 8;
                int x = buttonNo - (y * 8);

                arduinome.setLed((byte)x, (byte)y, false);
            }
        }

        private void SendKey(string data)
        {
            if (laserOSWindow != IntPtr.Zero)
            {
                IntPtr currentWindow = GetForegroundWindow();

                while (GetForegroundWindow() != laserOSWindow)
                    SetForegroundWindow(laserOSWindow);

                SendKeys.Send(data);
                SetForegroundWindow(currentWindow);
            }
        }

        private void SendEffectKey(int number)
        {
            if (number > -1 && number < 8)
            {
                SendKey(number.ToString());
                removeBlinkingButton(currentEffect + 56);
                currentEffect = number;
                addBlinkingButton(currentEffect + 56);
            }
        }

        private void UpdateStateTimer_Tick(object sender, EventArgs e)
        {
            #region Set static leds.
            foreach (int button in staticButtons)
            {
                int oldy = button / 8;
                int oldx = button - (oldy * 8);

                arduinome.setLed((byte)oldx, (byte)oldy, true);
            }
            #endregion

            #region Flash blinking leds.
            foreach (int button in blinkingButtons)
            {
                int oldy = button / 8;
                int oldx = button - (oldy * 8);

                arduinome.setLed((byte)oldx, (byte)oldy, blinkState);
            }
            blinkState = !blinkState;
            #endregion

            #region Check for laserOS application.
            laserOSWindow = IntPtr.Zero;
            foreach (string item in EnumDesktopWindows.GetDesktopWindowsCaptions())
            {
                if (item.StartsWith("LaserOS ") && item.EndsWith("Visualizer"))
                {
                    laserOSWindow = FindWindow(null, item);
                    break;
                }
            }

            if (laserOSWindow != IntPtr.Zero) isConnectedToLaserOS.Checked = true;
            else isConnectedToLaserOS.Checked = false;
            #endregion

            #region Get LaserOS status.
            if (laserOSWindow != IntPtr.Zero) 
            {
                LaserOSStatus stat = checkLaserOSStatus();
                laserOn.Checked = stat.LaserOn;
                simulatorActive.Checked = stat.SimulatorOn;
                runningAutoRandom.Checked = stat.AutoRandomMode;
            }
            #endregion
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            foreach (string item in SerialPort.GetPortNames())
            {
                comboBox1.Items.Add(item);
            }
            comboBox1.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!arduinome.IsOpen())
            {
                arduinome.Open(comboBox1.SelectedItem as string, 57200);
                button1.Text = "Close";
                comboBox1.Enabled = false;
            }
            else
            {
                arduinome.Close();
                button1.Text = "Open";
                comboBox1.Enabled = true;
            }
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            arduinome.setIntensity((byte)trackBar1.Value);
        }
    }

    class LaserOSStatus {
        public bool LaserOn { get; set; }
        public bool SimulatorOn { get; set; }
        public bool AutoRandomMode { get; set; }
    }
}
