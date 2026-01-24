using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ProjectZein
{
    public static class Injector
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
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
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
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
            public ushort wShowWindow;
            public ushort cbReserved2;
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
            public uint dwProcessId;
            public uint dwThreadId;
        }

        const uint CREATE_SUSPENDED = 0x00000004;
        const uint MEM_COMMIT = 0x1000;
        const uint MEM_RESERVE = 0x2000;
        const uint PAGE_READWRITE = 0x04;

        public static void LaunchAndInject(string exePath, string dllPath)
        {
            STARTUPINFO si = new STARTUPINFO();
            si.cb = (uint)Marshal.SizeOf(si);
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

            // Use StringBuilder for command line if needed, or pass null if only using lpApplicationName
            StringBuilder commandLine = null;

            bool success = CreateProcess(
                exePath,
                commandLine,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                CREATE_SUSPENDED,
                IntPtr.Zero,
                System.IO.Path.GetDirectoryName(exePath),
                ref si,
                out pi);

            if (!success)
            {
                throw new Exception($"Failed to create process. Error: {Marshal.GetLastWin32Error()}");
            }

            try
            {
                // 2. Allocate memory in remote process for DLL path
                byte[] dllBytes = Encoding.Unicode.GetBytes(dllPath);
                // Add null terminator (Unicode)
                byte[] dllBytesWithNull = new byte[dllBytes.Length + 2];
                Array.Copy(dllBytes, dllBytesWithNull, dllBytes.Length);

                uint size = (uint)dllBytesWithNull.Length;

                IntPtr allocMemAddress = VirtualAllocEx(pi.hProcess, IntPtr.Zero, size, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

                if (allocMemAddress == IntPtr.Zero)
                {
                    throw new Exception($"Failed to allocate memory. Error: {Marshal.GetLastWin32Error()}");
                }

                // 3. Write DLL path to allocated memory
                IntPtr bytesWritten;
                if (!WriteProcessMemory(pi.hProcess, allocMemAddress, dllBytesWithNull, size, out bytesWritten))
                {
                     throw new Exception($"Failed to write process memory. Error: {Marshal.GetLastWin32Error()}");
                }

                // 4. Get address of LoadLibraryW
                IntPtr kernel32Handle = GetModuleHandle("kernel32.dll");
                IntPtr loadLibraryAddr = GetProcAddress(kernel32Handle, "LoadLibraryW");

                if (loadLibraryAddr == IntPtr.Zero)
                {
                    throw new Exception($"Failed to find LoadLibraryW address. Error: {Marshal.GetLastWin32Error()}");
                }

                // 5. Create Remote Thread
                IntPtr threadId;
                IntPtr hRemoteThread = CreateRemoteThread(pi.hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, out threadId);

                if (hRemoteThread == IntPtr.Zero)
                {
                    throw new Exception($"Failed to create remote thread. Error: {Marshal.GetLastWin32Error()}");
                }

                // Close thread handle as we don't need to wait for it in this simple launcher
                CloseHandle(hRemoteThread);

                // 6. Resume main thread
                if (ResumeThread(pi.hThread) == uint.MaxValue)
                {
                     throw new Exception($"Failed to resume thread. Error: {Marshal.GetLastWin32Error()}");
                }
            }
            finally
            {
                // Clean up handles
                if (pi.hProcess != IntPtr.Zero) CloseHandle(pi.hProcess);
                if (pi.hThread != IntPtr.Zero) CloseHandle(pi.hThread);
            }
        }
    }
}
