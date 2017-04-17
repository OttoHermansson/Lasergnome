using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Arduinome
{
    public delegate void processSerialDataDelegate(byte x, byte y, bool state);

    private SerialPort port = new SerialPort();
    private BitMatrix buttons;
    private BitMatrix leds;
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
        rebootDevice();
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

    public BitMatrix GetLedStates()
    {
        return leds;
    }

    public bool GetLedState(int x, int y)
    {
        return leds[x, y];
    }

    public Arduinome()
    {
        buttons = new BitMatrix(8, 8);
        leds = new BitMatrix(8, 8);

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
        if (state) OnButtonDown(x, y);
        if (!state) OnButtonUp(x, y);
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

    private void OnButtonDown(int x, int y)
    {
        ButtonEventArgs ev = new ButtonEventArgs(x, y);
        if (ButtonDown != null) ButtonDown(this, ev);
    }

    public delegate void ButtonDownEventHandler(object sender, ButtonEventArgs e);
    public event ButtonDownEventHandler ButtonDown;

    private void OnButtonUp(int x, int y)
    {
        ButtonEventArgs ev = new ButtonEventArgs(x, y);
        if (ButtonUp != null) ButtonUp(this, ev);
    }

    public delegate void ButtonUpEventHandler(object sender, ButtonEventArgs e);
    public event ButtonUpEventHandler ButtonUp;

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
            leds[x, y] = state;
        }
    }

    public void setColumn(byte x, bool y1, bool y2, bool y3, bool y4, bool y5, bool y6, bool y7, bool y8)
    {
        if (port.IsOpen)
        {
            byte data0 = (byte)(((byte)messageTypes.messageTypeLedSetRow << 4) | x);
            byte data1 = 0;

            if (y1) data1 += 1;
            if (y2) data1 += 2;
            if (y3) data1 += 4;
            if (y4) data1 += 8;
            if (y5) data1 += 16;
            if (y6) data1 += 32;
            if (y7) data1 += 64;
            if (y8) data1 += 128;

            leds[x, 0] = y1;
            leds[x, 1] = y2;
            leds[x, 2] = y3;
            leds[x, 3] = y4;
            leds[x, 4] = y5;
            leds[x, 5] = y6;
            leds[x, 6] = y7;
            leds[x, 7] = y8;

            port.Write(new byte[] { data0, data1 }, 0, 2);
        }
    }

    public void setRow(byte y, bool x1, bool x2, bool x3, bool x4, bool x5, bool x6, bool x7, bool x8 )
    {
        if (port.IsOpen)
        {
            byte data0 = (byte)(((byte)messageTypes.messageTypeLedSetRow << 4) | y);
            byte data1 = 0;

            if (x1) data1 += 1;
            if (x2) data1 += 2;
            if (x3) data1 += 4;
            if (x4) data1 += 8;
            if (x5) data1 += 16;
            if (x6) data1 += 32;
            if (x7) data1 += 64;
            if (x8) data1 += 128;

            leds[0, y] = x1;
            leds[1, y] = x2;
            leds[2, y] = x3;
            leds[3, y] = x4;
            leds[4, y] = x5;
            leds[5, y] = x6;
            leds[6, y] = x7;
            leds[7, y] = x8;

            port.Write(new byte[] { data0, data1 }, 0, 2);
        }
    }

    public void ledTest(bool state)
    {
        if (port.IsOpen)
        {
            byte data0 = (byte)((byte)messageTypes.messageTypeLedTest << 4);
            byte data1 = (byte)(state == true ? 1 : 0);

            port.Write(new byte[] { data0, data1 }, 0, 2);
        }
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

    public void rebootDevice()
    {
        if (port.IsOpen)
        {
            port.DiscardInBuffer();
            port.DiscardOutBuffer();
            port.DtrEnable = false;
            Thread.Sleep(10);
            port.DtrEnable = true;
            Thread.Sleep(500);
        }
    }
}

public class ButtonEventArgs : EventArgs
{
    private readonly int x = 0;
    private readonly int y = 0;

    // Constructor. 
    public ButtonEventArgs(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    // Properties. 
    public int X
    {
        get { return x; }
    }

    public int Y
    {
        get { return y; }
    }
}
