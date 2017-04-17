using System;

public class BitMatrix
{
    public BitMatrix(int rowCount, int columnCount)
    {
        m_RowCount = rowCount;
        m_ColumnCount = columnCount;

        // Calculate the needed number of bits and bytes
        int bitCount = m_RowCount * m_ColumnCount;
        int byteCount = bitCount >> 3;
        if (bitCount % 8 != 0)
        {
            byteCount++;
        }

        // Allocate the needed number of bytes
        m_Data = new byte[byteCount];
    }

    /// <summary>
    /// Gets the number of rows in this bit matrix.
    /// </summary>
    public int RowCount
    {
        get
        {
            return m_RowCount;
        }
    }
    /// <summary>
    /// Gets the number of columns in this bit matrix.
    /// </summary>
    public int ColumnCount
    {
        get
        {
            return m_ColumnCount;
        }
    }
    /// <summary>
    /// Gets/Sets the value at the specified row and column index.
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <param name="columnIndex"></param>
    /// <returns></returns>
    public bool this[int rowIndex, int columnIndex]
    {
        get
        {
            if (rowIndex < 0 || rowIndex >= m_RowCount)
                throw new ArgumentOutOfRangeException("rowIndex");

            if (columnIndex < 0 || columnIndex >= m_ColumnCount)
                throw new ArgumentOutOfRangeException("columnIndex");

            int pos = rowIndex * m_ColumnCount + columnIndex;
            int index = pos % 8;
            pos >>= 3;
            return (m_Data[pos] & (1 << index)) != 0;
        }
        set
        {
            if (rowIndex < 0 || rowIndex >= m_RowCount)
                throw new ArgumentOutOfRangeException("rowIndex");

            if (columnIndex < 0 || columnIndex >= m_ColumnCount)
                throw new ArgumentOutOfRangeException("columnIndex");

            int pos = rowIndex * m_ColumnCount + columnIndex;
            int index = pos % 8;
            pos >>= 3;
            bool oldVal = (m_Data[pos] & (1 << index)) != 0;
            m_Data[pos] &= (byte)(~(1 << index));

            if (value)
            {
                m_Data[pos] |= (byte)(1 << index);
            }

            if (oldVal != value)
                OnValueChanged(rowIndex, columnIndex, value);
        }
    }

    /// <summary>
    /// Change value event.
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <param name="columnIndex"></param>
    /// <param name="value"></param>
    private void OnValueChanged(int rowIndex, int columnIndex, bool value)
    {
        BitmatrixValueChangedEventArgs ev = new BitmatrixValueChangedEventArgs(rowIndex, columnIndex, value);
        if (ValueChanged != null) ValueChanged(this, ev);
    }

    public delegate void ValueChangedEventHandler(object sender, BitmatrixValueChangedEventArgs e);
    public event ValueChangedEventHandler ValueChanged;

    private int m_RowCount;
    private int m_ColumnCount;
    private byte[] m_Data;
}


public class BitmatrixValueChangedEventArgs : EventArgs
{
    private readonly int x = 0;
    private readonly int y = 0;
    private readonly bool state = false;

    // Constructor. 
    public BitmatrixValueChangedEventArgs(int x, int y, bool state)
    {
        this.x = x;
        this.y = y;
        this.state = state;
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

    public bool State
    {
        get { return state; }
    }
}