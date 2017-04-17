using System;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Lasergnome
{
    public partial class Form1 : Form
    {
        Arduinome arduinome;
        Timer updateStateTimer = new Timer();
        int currentEffect = 0;
        IntPtr laserOSWindow = IntPtr.Zero;

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
        }

        private void SendEffectKey(int number)
        {
            if (number > -1 && number < 8)
            {
                if (laserOSWindow != IntPtr.Zero)
                {
                    IntPtr currentWindow = GetForegroundWindow();
                    SetForegroundWindow(laserOSWindow);
                    SendKeys.Send(number.ToString());
                    SetForegroundWindow(currentWindow);
                }
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
            }
            else
            {
                arduinome.Close();
                button1.Text = "Open";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            laserOSWindow = FindWindow(null, "LaserOS ? - Visualizer");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            for (byte y = 0; y < 8; y++)
            {
                for (byte x = 0; x < 8; x++)
                {
                    arduinome.setLed(x, y, false);
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            arduinome.rebootDevice();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            arduinome.ledTest(true);
        }


        private void showButtonStatus()
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int id = ((y * 8) + x) + 1;

                    CheckBox ctn = this.Controls.Find("checkBox" + id.ToString(), true).First() as CheckBox;
                    ctn.Checked = arduinome.GetButtonState(x, y);
                    arduinome.setLed((byte)x, (byte)y, arduinome.GetButtonState(x, y));
                }
            }
        }
    }
}
