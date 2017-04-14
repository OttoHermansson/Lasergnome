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

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public delegate void processSerialDataDelegate(byte x, byte y, bool state);
        public processSerialDataDelegate serialDelegate;


        private byte[] arduinomoneData = new byte[2];
        SerialPort port = new SerialPort();
        Timer updateStateTimer = new Timer();

        BitMatrix buttons;
        processSerialDataDelegate del;
        public Form1()
        {
            InitializeComponent();
            port.DataReceived += port_DataReceived;
            del = new processSerialDataDelegate(processSerialDataMethod);
            buttons = new BitMatrix(8, 8);
            updateStateTimer.Tick += UpdateStateTimer_Tick;
            updateStateTimer.Interval = 100;
            updateStateTimer.Enabled = true;
            updateStateTimer.Start();
        }

        private void UpdateStateTimer_Tick(object sender, EventArgs e)
        {
            showButtonStatus();
        }

        public void processSerialDataMethod(byte x, byte y, bool state)
        {
            buttons[x, y] = state;
        }

        void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (port.BytesToRead == 2)
            {
                byte[] retByte = new byte[2];

                retByte[0] = (byte)port.ReadByte();
                retByte[1] = (byte)port.ReadByte();

                byte x = (byte)((retByte[1] >> 4) & 15);
                byte y = (byte)(retByte[1] & 15);
                bool state = retByte[0] == 0 ? false : true;

                del.Invoke(x, y, state);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.serialDelegate = new processSerialDataDelegate(processSerialDataMethod);

            comboBox1.Items.Clear();
            foreach (string item in SerialPort.GetPortNames())
            {
                comboBox1.Items.Add(item);
            }
            comboBox1.SelectedIndex = 0;
        }





        private void button1_Click(object sender, EventArgs e) {
            if (!port.IsOpen)
            {
                port.BaudRate = 57200;
                port.PortName = comboBox1.SelectedItem as string;
                port.Open();
                button1.Text = "Close";
            }
            else {
                port.Close();
                button1.Text = "Open";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            for (byte i = 0; i < 8; i++)
            {
                setLed(1, i, true);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            for (byte i = 0; i < 8; i++)
            {
                setLed(1, i, false);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            showButtonStatus();
        }

        private void showButtonStatus()
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int id = ((y * 8) + x) + 1;

                    CheckBox ctn = this.Controls.Find("checkBox" + id.ToString(), true).First() as CheckBox;
                    ctn.Checked = buttons[x, y];
                }
            }
        }


        private void setIntensity(byte intensity)
        {
            if (port.IsOpen)
            {
                byte data0 = (byte)((byte)messageTypes.messageTypeLedIntensity << 4);
                byte data1 = intensity;

                port.Write(new byte[] { data0, data1 }, 0, 2);
            }
        }

        private void setLed(byte x, byte y, bool state) {
            if(port.IsOpen)
            {
                byte onoff = (byte)(state == true ? 1 : 0);

                byte data0 = (byte)(((byte)messageTypes.messageTypeLedStateChange << 4) | onoff);
                byte data1 = (byte)((x << 4) | y);

                port.Write(new byte[] { data0, data1 }, 0, 2);
            }
        }

        private void ledTest(bool state)
        {
            byte data0 = (byte)((byte)messageTypes.messageTypeLedTest << 4);
            byte data1 = (byte)(state == true ? 1 : 0);

            port.Write(new byte[] { data0, data1 }, 0, 2);
        }

        private void enabled(bool state) {
            if (port.IsOpen)
            {
                byte data0 = (byte)((byte)messageTypes.messageTypeShutdown << 4);
                byte data1 = (byte)(state == true ? 1 : 0);

                port.Write(new byte[] { data0, data1 }, 0, 2);
            }
        }

    }

    enum messageTypes
    {
        messageTypeButtonPress = 0,
        messageTypeAdcVal = 1,
        messageTypeLedStateChange = 2,
        messageTypeLedIntensity = 3,
        messageTypeLedTest = 4,
        messageTypeAdcEnable = 5,
        messageTypeShutdown = 6,
        messageTypeLedSetRow = 7,
        messageTypeLedSetColumn = 8,
        messageTypeEncEnable = 9,
        messageTypeEncVal = 10,
        messageTypeTiltVal = 12,
        messageTypeTiltEvent = 13,
        messageNumTypes = 14
    }
}
