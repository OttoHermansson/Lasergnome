using System;

public class ButtonPressEventArgs : EventArgs
{
    private readonly byte x = 0;
    private readonly byte y = 0;
    private readonly bool state = false;

    // Constructor. 
    public ButtonPressEventArgs(byte x, byte y, bool state)
    {
        this.x = x;
        this.y = y;
        this.state = state;
    }

    // Properties. 
    public byte X
    {
        get { return x; }
    }

    public byte Y
    {
        get { return Y; }
    }

    public bool State
    {
        get { return state; }
    }
}