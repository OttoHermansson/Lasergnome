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
        bool blinkState = false;
        List<int> blinkingButtons = new List<int>();
        IEnumerable<XElement> programSteps;
        int programRepeat = -1;
        int currentProgramstep = 0;
        Timer programTimer = new Timer();


        [DllImport("User32.Dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("User32.Dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

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
                        programSteps = button.Descendants("program").First().Descendants("step");
                        if(button.Descendants("program").First().Attribute("repeat") != null)
                        {
                            if (button.Descendants("program").First().Attribute("repeat").Value == "true")
                                programRepeat = -1;
                            else
                                programRepeat = int.Parse(button.Descendants("program").First().Attribute("repeat").Value);
                        }
                        
                        currentProgramstep = 0;
                        runProgramStep();

                        // Set the blinking button.
                        removeBlinkingButton(currentPattern);
                        currentPattern = buttonNumber - 1;
                        addBlinkingButton(buttonNumber - 1);
                    }
                    else
                    {
                        stopProgram();
                        #region Other commands
                        if (button.Descendants("pattern").Count() == 1)
                        {
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
                                    SendKey("+{a}");
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

                            if (button.Attribute("holdkey") == null || button.Attribute("holdkey").Value == "true")
                            {
                                removeBlinkingButton(currentPattern);
                                currentPattern = buttonNumber - 1;
                                addBlinkingButton(currentPattern);
                            }
                        }
                        #endregion
                    }
                }
            }
        }

        private void stopProgram()
        {
            programTimer.Enabled = false;
            programTimer.Stop();
        }

        private void runProgramStep()
        {
            XElement step = programSteps.ElementAt(currentProgramstep);
            int time = int.Parse(step.Attribute("runfor").Value);

            if (step.Descendants("pattern").Count() == 1)
            {
                SendKey(step.Descendants("pattern").First().Attribute("key").Value);
            }

            if (step.Descendants("effect").Count() == 1)
            {
                string effect = step.Descendants("effect").First().Attribute("key").Value;
                SendEffectKey(int.Parse(effect));
            }

            if (programRepeat == -1)
            {
                if (programSteps.Count() > currentProgramstep + 1)
                    currentProgramstep++;
                else
                    currentProgramstep = 0;
            }
            else if (programRepeat > 0)
            {
                if (programSteps.Count() > currentProgramstep + 1)
                    currentProgramstep++;
                else
                {
                    currentProgramstep = 0;
                    programRepeat--;
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
                removeBlinkingButton(currentEffect + 56);
                currentEffect = number;
                addBlinkingButton(currentEffect + 56);
            }
        }

        private void UpdateStateTimer_Tick(object sender, EventArgs e)
        {
            #region Flash blinking led.
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
}
