using System.Runtime.InteropServices;

namespace Visualizations.Core.Audio;

/// <summary>
/// Hledání kořenového procesu aplikace. Spotify (Chromium/CEF) běží jako strom procesů a zvuk
/// vydává jeden z podprocesů – process loopback proto cílí na kořen a zahrne celý strom.
/// </summary>
internal static class ProcessTree
{
    /// <summary>
    /// Vrátí PID procesu s daným názvem, jehož rodič není stejná aplikace; <c>null</c>, když aplikace neběží.
    /// </summary>
    public static int? FindRootProcessId(string processName)
    {
        string exeName = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? processName
            : processName + ".exe";

        var parents = new Dictionary<uint, uint>();
        foreach (var entry in EnumerateProcesses())
        {
            if (string.Equals(entry.ExeFile, exeName, StringComparison.OrdinalIgnoreCase))
            {
                parents[entry.ProcessId] = entry.ParentProcessId;
            }
        }

        foreach (var (pid, parent) in parents)
        {
            if (!parents.ContainsKey(parent))
            {
                return (int)pid;
            }
        }

        return null;
    }

    private static List<(uint ProcessId, uint ParentProcessId, string ExeFile)> EnumerateProcesses()
    {
        var result = new List<(uint, uint, string)>();
        nint snapshot = CreateToolhelp32Snapshot(SnapProcess, 0);
        if (snapshot == InvalidHandleValue)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        try
        {
            var entry = new ProcessEntry32W { dwSize = (uint)Marshal.SizeOf<ProcessEntry32W>() };
            for (bool ok = Process32FirstW(snapshot, ref entry); ok; ok = Process32NextW(snapshot, ref entry))
            {
                result.Add((entry.th32ProcessID, entry.th32ParentProcessID, entry.szExeFile));
            }
        }
        finally
        {
            CloseHandle(snapshot);
        }

        return result;
    }

    private const uint SnapProcess = 0x00000002;
    private static readonly nint InvalidHandleValue = -1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry32W
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public nint th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool Process32FirstW(nint hSnapshot, ref ProcessEntry32W lppe);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool Process32NextW(nint hSnapshot, ref ProcessEntry32W lppe);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(nint hObject);
}
