using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ProjectZein.Services
{
    public static class Injector
    {
        // P/Invoke definitions
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandleW(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        // Access rights
        private const int PROCESS_CREATE_THREAD = 0x0002;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_VM_OPERATION = 0x0008;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_READ = 0x0010;

        // Memory allocation
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint PAGE_READWRITE = 0x04;

        public static void LaunchAndInject(string gamePath, string arguments = "")
        {
            if (!File.Exists(gamePath))
            {
                throw new FileNotFoundException("Game executable not found.", gamePath);
            }

            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cobalt.dll");
            if (!File.Exists(dllPath))
            {
                throw new FileNotFoundException("Cobalt.dll not found.", dllPath);
            }

            // Using StringBuilder for arguments command line as hinted by requirements,
            // though ProcessStartInfo handles string args directly usually.
            // We'll proceed with standard Process.Start for simplicity in this managed environment.
            var startInfo = new ProcessStartInfo
            {
                FileName = gamePath,
                Arguments = arguments,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(gamePath)
            };

            var process = Process.Start(startInfo);
            if (process != null)
            {
                // Give it a moment to initialize or implement a loop to wait for input idle
                System.Threading.Thread.Sleep(2000);
                Inject(process.Id, dllPath);
            }
        }

        private static void Inject(int processId, string dllPath)
        {
            IntPtr hProcess = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, processId);

            if (hProcess == IntPtr.Zero)
            {
                return; // Failed to open process
            }

            try
            {
                IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandleW("kernel32.dll"), "LoadLibraryW");
                if (loadLibraryAddr == IntPtr.Zero) return;

                byte[] bytes = Encoding.Unicode.GetBytes(dllPath + "\0");
                IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)bytes.Length, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

                if (allocMemAddress == IntPtr.Zero) return;

                WriteProcessMemory(hProcess, allocMemAddress, bytes, (uint)bytes.Length, out _);

                CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }
    }
}
