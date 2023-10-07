using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace appnav.Utils;

public delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);
public static class WindowWinApi
{
    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const int SW_RESTORE = 9;
    public const int GWL_STYLE = -16;
    public const uint WS_MINIMIZE = 0x20000000;

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    //SetWindowPos(processToActivate.MainWindowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);


    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    public static string GetWindowText(IntPtr hWnd)
    {
        const int nChars = 256;
        StringBuilder buff = new StringBuilder(nChars);

        if (GetWindowText(hWnd, buff, nChars) > 0)
        {
            return buff.ToString();
        }

        return "";
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumWindowsProc enumProc, int lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public static string GetProcessName(IntPtr hwnd)
    {
        // Get process ID
        GetWindowThreadProcessId(hwnd, out uint pid);

        // Get process by ID
        Process proc = Process.GetProcessById((int)pid);

        // Get executable path
        return proc.ProcessName;
    }

    private static bool EnumTheWindows(IntPtr hWnd, int lParam)
    {
        int size = GetWindowTextLength(hWnd);

        if (size++ > 0 && IsWindowVisible(hWnd))
        {
            StringBuilder sb = new StringBuilder(size);

            GetWindowText(hWnd, sb, size);

            var procName = GetProcessName(hWnd);

            WindowInfoList.Add(new WindowInfo() { WindowHadler = hWnd, WindowText = sb.ToString(), ProcessName = procName });

            Debug.WriteLine(sb.ToString());
        }

        return true;
    }


    public static void EnumAllWindows()
    {
        WindowInfoList.Clear();

        EnumWindows(new EnumWindowsProc(EnumTheWindows), 0);
    }

    public static List<WindowInfo> WindowInfoList { get; set; } = new List<WindowInfo>();
}

public class WindowInfo
{
    public IntPtr WindowHadler { get; set; }

    public string WindowText { get; set; } = "";

    public string ProcessName { get; set; } = "";

    public string DisplayName
    {
        get
        {
            return ProcessName == "ApplicationFrameHost" ? WindowText : ProcessName + " --- " + WindowText;
        }
    }
}