using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace FortniteLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // P/Invoke constants
        private const int PROCESS_CREATE_THREAD = 0x0002;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_VM_OPERATION = 0x0008;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_READ = 0x0010;

        private const uint MEM_COMMIT = 0x00001000;
        private const uint MEM_RESERVE = 0x00002000;
        private const uint PAGE_READWRITE = 4;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Path to the game executable - Assumption: it's in a known location or relative
                // For Season 1, the binary name might differ, but commonly it is FortniteClient-Win64-Shipping.exe
                string gamePath = "FortniteClient-Win64-Shipping.exe";

                // Allow user to pick file if not found?
                if (!File.Exists(gamePath))
                {
                    StatusText.Text = "Game executable not found!";
                    return;
                }

                StatusText.Text = "Launching...";

                Process process = new Process();
                process.StartInfo.FileName = gamePath;
                // Add any necessary arguments here. -fltoken, -skippatchcheck, etc.
                process.StartInfo.Arguments = "-fltoken=dummy -skippatchcheck -nobe -fromfl=eac -noeac";
                process.StartInfo.UseShellExecute = false; // Important for some scenarios
                process.Start();

                // Wait for the process to start up a bit before injecting
                System.Threading.Thread.Sleep(2000);

                string dllName = "Cobalt.dll";
                if (File.Exists(dllName))
                {
                    InjectDLL(process, dllName);
                    StatusText.Text = "Game Launched & Injected!";
                }
                else
                {
                    StatusText.Text = "Game Launched (DLL not found)!";
                }

                // Close launcher after launch?
                // Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                MessageBox.Show(ex.ToString());
            }
        }

        private void InjectDLL(Process process, string dllPath)
        {
            // Get full path of the DLL
            string fullDllPath = Path.GetFullPath(dllPath);

            // Open the target process with the necessary access
            IntPtr hProcess = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, process.Id);

            if (hProcess == IntPtr.Zero)
            {
                StatusText.Text = "Failed to open process for injection.";
                return;
            }

            // Allocate memory for the DLL path in the target process
            IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)((fullDllPath.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

            if (allocMemAddress == IntPtr.Zero)
            {
                StatusText.Text = "Failed to allocate memory in target process.";
                return;
            }

            // Write the DLL path into the allocated memory
            UIntPtr bytesWritten;
            byte[] bytes = Encoding.Default.GetBytes(fullDllPath);
            WriteProcessMemory(hProcess, allocMemAddress, bytes, (uint)bytes.Length, out bytesWritten);

            // Get the address of LoadLibraryA
            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            if (loadLibraryAddr == IntPtr.Zero)
            {
                StatusText.Text = "Failed to find LoadLibraryA.";
                return;
            }

            // Create a remote thread that calls LoadLibraryA with the DLL path as an argument
            CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
        }
    }
}
