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
            m_Data[pos] &= (byte)(~(1 << index));

            if (value)
            {
                m_Data[pos] |= (byte)(1 << index);
            }
        }
    }

    private int m_RowCount;
    private int m_ColumnCount;
    private byte[] m_Data;
}