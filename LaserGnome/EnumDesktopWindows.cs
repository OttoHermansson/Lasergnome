using System;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;

/// &lt;summary>
/// EnumDesktopWindows Demo - shows the caption of all desktop windows.
/// Authors: Svetlin Nakov, Martin Kulov 
/// Bulgarian Association of Software Developers - http://www.devbg.org/en/
/// &lt;/summary>
public class EnumDesktopWindows
{
    const int MAXTITLE = 255;

    private static ArrayList mTitlesList;

    private delegate bool EnumDelegate(IntPtr hWnd, int lParam);

    [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows",
     ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool _EnumDesktopWindows(IntPtr hDesktop,
    EnumDelegate lpEnumCallbackFunction, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "GetWindowText",
     ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int _GetWindowText(IntPtr hWnd,
    StringBuilder lpWindowText, int nMaxCount);

    private static bool EnumWindowsProc(IntPtr hWnd, int lParam)
    {
        string title = GetWindowText(hWnd);
        mTitlesList.Add(title);
        return true;
    }

    /// <summary>
    /// Returns the caption of a windows by given HWND identifier.
    /// </summary>
    public static string GetWindowText(IntPtr hWnd)
    {
        StringBuilder title = new StringBuilder(MAXTITLE);
        int titleLength = _GetWindowText(hWnd, title, title.Capacity + 1);
        title.Length = titleLength;

        return title.ToString();
    }

    /// <summary>
    /// Returns the caption of all desktop windows.
    /// </summary>
    public static string[] GetDesktopWindowsCaptions()
    {
        mTitlesList = new ArrayList();
        EnumDelegate enumfunc = new EnumDelegate(EnumWindowsProc);
        IntPtr hDesktop = IntPtr.Zero; // current desktop
        bool success = _EnumDesktopWindows(hDesktop, enumfunc, IntPtr.Zero);

        if (success)
        {
            // Copy the result to string array
            string[] titles = new string[mTitlesList.Count];
            mTitlesList.CopyTo(titles);
            return titles;
        }
        else
        {
            // Get the last Win32 error code
            int errorCode = Marshal.GetLastWin32Error();

            string errorMessage = String.Format(
            "EnumDesktopWindows failed with code {0}.", errorCode);
            throw new Exception(errorMessage);
        }
    }
}