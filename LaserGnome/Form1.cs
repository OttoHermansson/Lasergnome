using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApplication1;

namespace Lasergnome
{
    public partial class Form1 : Form
    {
        Arduinome arduinome;
        Timer updateStateTimer = new Timer();

        public Form1()
        {
            InitializeComponent();

            arduinome = new Arduinome();

            arduinome.ButtonDown += buttonPressed;

            updateStateTimer.Tick += UpdateStateTimer_Tick;
            updateStateTimer.Interval = 100;
            updateStateTimer.Enabled = true;
            updateStateTimer.Start();
        }

        private void buttonPressed(object sender, ButtonEventArgs e)
        {
            AppendTextBox(e.X.ToString() + ", " + e.Y.ToString() + Environment.NewLine);
        }

        public void AppendTextBox(string value)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendTextBox), new object[] { value });
                return;
            }
            textBox1.Text += value;
        }

        private void UpdateStateTimer_Tick(object sender, EventArgs e)
        {
            showButtonStatus();
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
            for (byte i = 0; i < 8; i++)
            {
                arduinome.setLed(0, 5, true);
            }
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
