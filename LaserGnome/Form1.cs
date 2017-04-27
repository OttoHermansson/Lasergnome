using IniParser;
using IniParser.Model;
using interop.UIAutomationCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

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
                    SendKey(button.Descendants("pattern").First().Attribute("key").Value);

                    int oldy = currentPattern / 8;
                    int oldx = currentPattern - (oldy * 8);
                    arduinome.setLed((byte)oldx, (byte)oldy, false);
                    currentPattern = buttonNumber - 1;

                    if (button.Descendants("effect").Count() == 1)
                    {
                        string effect = button.Descendants("effect").First().Attribute("id").Value;
                        SendEffectKey(int.Parse(effect));
                    }
                }
            }
        }

        private void SendKey(string data)
        {
            if (laserOSWindow != IntPtr.Zero)
            {
                IntPtr currentWindow = GetForegroundWindow();
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
                currentEffect = number;
                arduinome.setRow(7, false, false, false, false, false, false, false, false);
            }
        }

        [DllImport("User32.Dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("User32.Dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        private void UpdateStateTimer_Tick(object sender, EventArgs e)
        {
            // Toggle selected effect led.
            arduinome.setLed((byte)currentEffect, 7, !arduinome.GetLedState(currentEffect, 7));

            int oldy = currentPattern / 8;
            int oldx = currentPattern - (oldy * 8);

            arduinome.setLed((byte)oldx, (byte)oldy, !arduinome.GetLedState(oldx, oldy));

            #region Check for laserOS application.
            laserOSWindow = IntPtr.Zero;
            string[] windows = EnumDesktopWindows.GetDesktopWindowsCaptions();
            foreach (string item in windows)
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
                if (arduinome.IsOpen()) {
                    button1.Text = "Close";
                    comboBox1.Enabled = false;
                }
            }
            else
            {
                arduinome.Close();
                if (!arduinome.IsOpen())
                {
                    button1.Text = "Open";
                    comboBox1.Enabled = true;
                }
            }
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            arduinome.setIntensity((byte)trackBar1.Value);
        }

        private IUIAutomation _automation;

        private void button2_Click_1(object sender, EventArgs e)
        {
            _automation = new CUIAutomation();
            IUIAutomationElement laserOSElement = _automation.ElementFromHandle(FindWindow(null, "LaserOS v0.9.0 BETA - Visualizer"));
            var children = laserOSElement.GetCachedChildren();
        }
    }
}
