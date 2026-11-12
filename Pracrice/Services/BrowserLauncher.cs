using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Pracrice.Services;

public static class BrowserLauncher
{
    private const int SwRestore = 9;
    private const int SwShowNormal = 1;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpShowWindow = 0x0040;
    private const string ReportTitleFragment = "ООО «РОСПИВ»";

    public static void Open(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Файл отчёта не найден.", path);
        }

        Process.Start(new ProcessStartInfo(path)
        {
            UseShellExecute = true,
            Verb = "open",
            WindowStyle = ProcessWindowStyle.Normal,
            WorkingDirectory = Path.GetDirectoryName(path) ?? AppContext.BaseDirectory
        });

        _ = Task.Run(() => BringToFront(ReportTitleFragment, 30, 200));
    }

    private static void BringToFront(string titleFragment, int attempts, int delayMilliseconds)
    {
        int currentProcessId = Environment.ProcessId;

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            Thread.Sleep(delayMilliseconds);
            IntPtr handle = FindWindow(titleFragment, currentProcessId);
            if (handle == IntPtr.Zero)
            {
                continue;
            }

            RestoreAndActivate(handle);
            if (!IsIconic(handle) && GetForegroundWindow() == handle)
            {
                return;
            }
        }
    }

    private static void RestoreAndActivate(IntPtr handle)
    {
        if (IsIconic(handle))
        {
            ShowWindow(handle, SwRestore);
        }

        ShowWindow(handle, SwShowNormal);
        SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0,
            SwpNoMove | SwpNoSize | SwpShowWindow);
        ForceForeground(handle);
    }

    private static IntPtr FindWindow(string titleFragment, int excludedProcessId)
    {
        IntPtr found = IntPtr.Zero;

        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle))
            {
                return true;
            }

            int length = GetWindowTextLength(handle);
            if (length <= 0)
            {
                return true;
            }

            var title = new StringBuilder(length + 1);
            GetWindowText(handle, title, title.Capacity);

            if (title.ToString().IndexOf(titleFragment, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return true;
            }

            GetWindowThreadProcessId(handle, out uint processId);
            if (processId == excludedProcessId)
            {
                return true;
            }

            found = handle;
            return false;
        }, IntPtr.Zero);

        return found;
    }

    private static void ForceForeground(IntPtr handle)
    {
        if (SetForegroundWindow(handle))
        {
            return;
        }

        IntPtr foreground = GetForegroundWindow();
        uint foregroundThread = GetWindowThreadProcessId(foreground, out _);
        uint targetThread = GetWindowThreadProcessId(handle, out _);

        if (foregroundThread == 0 || targetThread == 0 || foregroundThread == targetThread)
        {
            return;
        }

        AttachThreadInput(foregroundThread, targetThread, true);
        try
        {
            ShowWindow(handle, SwRestore);
            SetForegroundWindow(handle);
        }
        finally
        {
            AttachThreadInput(foregroundThread, targetThread, false);
        }
    }

    private delegate bool EnumWindowsProc(IntPtr handle, IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr handle, StringBuilder text, int maximumCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr handle, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr handle, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInput(uint attachThreadId, uint attachToThreadId, bool attach);
}
