using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace FortniteLauncher
{
    public static class Injector
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CreateProcess(
            string lpApplicationName,
            StringBuilder lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
        {
            public uint cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        const uint CREATE_SUSPENDED = 0x00000004;
        const uint MEM_COMMIT = 0x1000;
        const uint MEM_RESERVE = 0x2000;
        const uint PAGE_READWRITE = 0x04;
        const uint PROCESS_ALL_ACCESS = 0x1F0FFF;

        public static void LaunchAndInject(string exePath, string arguments, string dllPath)
        {
            STARTUPINFO si = new STARTUPINFO();
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            si.cb = (uint)Marshal.SizeOf(si);

            // CreateProcess requires a mutable string buffer for the command line if it's not const
            StringBuilder commandLine = new StringBuilder($"\"{exePath}\" {arguments}");
            string workingDir = Path.GetDirectoryName(exePath);

            // Launch the process in a suspended state
            bool success = CreateProcess(null, commandLine, IntPtr.Zero, IntPtr.Zero, false, CREATE_SUSPENDED, IntPtr.Zero, workingDir, ref si, out pi);

            if (!success)
            {
                throw new Exception($"Failed to create process. Error: {Marshal.GetLastWin32Error()}");
            }

            try
            {
                Inject(pi.hProcess, dllPath);
            }
            catch (Exception ex)
            {
                // Ensure we kill the process if injection fails
                Process.GetProcessById(pi.dwProcessId).Kill();
                throw new Exception($"Injection failed: {ex.Message}");
            }
            finally
            {
                // Always resume the thread, even if injection failed (though we killed it above, resuming a dead thread is harmless or fails safely)
                // If it succeeded, we must resume.
                if (Process.GetProcessById(pi.dwProcessId) != null && !Process.GetProcessById(pi.dwProcessId).HasExited)
                {
                    ResumeThread(pi.hThread);
                }

                CloseHandle(pi.hThread);
                CloseHandle(pi.hProcess);
            }
        }

        private static void Inject(IntPtr hProcess, string dllPath)
        {
            // We use LoadLibraryW for Unicode support
            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");
            if (loadLibraryAddr == IntPtr.Zero) throw new Exception("Could not find LoadLibraryW");

            // Allocate memory for the DLL path in the target process
            // Note: Unicode string takes 2 bytes per char
            uint size = (uint)((dllPath.Length + 1) * 2);
            IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, size, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (allocMemAddress == IntPtr.Zero) throw new Exception("Could not allocate memory in target process");

            // Write the DLL path to the allocated memory
            byte[] bytes = Encoding.Unicode.GetBytes(dllPath);
            IntPtr bytesWritten;
            bool writeSuccess = WriteProcessMemory(hProcess, allocMemAddress, bytes, (uint)bytes.Length, out bytesWritten);
            if (!writeSuccess) throw new Exception("Could not write to target process memory");

            // Create a remote thread that calls LoadLibraryW with the path we wrote
            IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
            if (hThread == IntPtr.Zero) throw new Exception("Could not create remote thread");

            // Wait for the DLL to load
            WaitForSingleObject(hThread, 10000);
            CloseHandle(hThread);
        }
    }
}
