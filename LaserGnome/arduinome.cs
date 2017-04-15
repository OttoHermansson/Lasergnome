using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Arduinome
{
    public delegate void processSerialDataDelegate(byte x, byte y, bool state);

    private SerialPort port = new SerialPort();
    private BitMatrix buttons;
    processSerialDataDelegate del;

    public enum messageTypes
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

    public void PortName(string portName)
    {
        port.PortName = portName;
    }

    public void BaudRate(int baudRate)
    {
        port.BaudRate = baudRate;
    }

    public bool IsOpen()
    {
        return port.IsOpen;
    }

    public void Open()
    {
        port.Open();
    }

    public void Open(string portName, int baudRate)
    {
        port.BaudRate = baudRate;
        port.PortName = portName;
        port.Open();
    }

    public void Close()
    {
        port.Close();
    }

    public BitMatrix GetButtonStates()
    {
        return buttons;
    }

    public bool GetButtonState(int x, int y)
    {
        return buttons[x, y];
    }


    public Arduinome()
    {
        buttons = new BitMatrix(8, 8);

        port.DataBits = 8;
        port.Parity = Parity.None;
        port.StopBits = StopBits.One;
        port.Handshake = Handshake.None;
        port.ReceivedBytesThreshold = 2;

        port.DataReceived += serialDataReceived;
        del = new processSerialDataDelegate(processSerialDataMethod);
    }


    private void processSerialDataMethod(byte x, byte y, bool state)
    {
        buttons[x, y] = state;
    }

    void serialDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        SerialPort sp = (SerialPort)sender;

        byte[] input = new byte[sp.BytesToRead];
        sp.Read(input, 0, input.Length);

        if (input.Length % 2 == 0) // Even number only.
        {
            for (int i = 0; i < input.Length; i += 2)
            {
                byte[] retByte = new byte[2];

                retByte[0] = input[i];
                retByte[1] = input[i + 1];

                byte x = (byte)((retByte[1] >> 4) & 15);
                byte y = (byte)(retByte[1] & 15);
                bool state = retByte[0] == 0 ? false : true;

                del.Invoke(x, y, state);
            }
        }
    }


    public void setIntensity(byte intensity)
    {
        if (port.IsOpen)
        {
            byte data0 = (byte)((byte)messageTypes.messageTypeLedIntensity << 4);
            byte data1 = intensity;

            port.Write(new byte[] { data0, data1 }, 0, 2);
        }
    }

    public void setLed(byte x, byte y, bool state)
    {
        if (port.IsOpen)
        {
            byte onoff = (byte)(state == true ? 1 : 0);
            byte data0 = (byte)(((byte)messageTypes.messageTypeLedStateChange << 4) | onoff);
            byte data1 = (byte)((x << 4) | y);

            port.Write(new byte[] { data0, data1 }, 0, 2);
        }
    }

    public void ledTest(bool state)
    {
        byte data0 = (byte)((byte)messageTypes.messageTypeLedTest << 4);
        byte data1 = (byte)(state == true ? 1 : 0);

        port.Write(new byte[] { data0, data1 }, 0, 2);
    }

    public void enabled(bool state)
    {
        if (port.IsOpen)
        {
            byte data0 = (byte)((byte)messageTypes.messageTypeShutdown << 4);
            byte data1 = (byte)(state == true ? 1 : 0);

            port.Write(new byte[] { data0, data1 }, 0, 2);
        }
    }
}

