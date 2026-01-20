using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace FortniteLauncher
{
    public partial class MainWindow : Window
    {
        private string GamePath = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Fortnite Executable|FortniteClient-Win64-Shipping.exe";
            if (dialog.ShowDialog() == true)
            {
                GamePath = dialog.FileName;
                GamePathBox.Text = GamePath;
            }
        }

        private void DiscordLoginBtn_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for Discord Login.
            // In a real implementation, you would use Discord Game SDK to Authenticate.
            // For now, we simulate a login by using the Discord Username if available, or just a success message.
            MessageBox.Show("Discord Login Simulation: Authenticated as 'DiscordUser'", "Discord Login");
            UsernameBox.Text = "DiscordUser";
        }

        private async void LaunchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(GamePath) || !File.Exists(GamePath))
            {
                StatusText.Text = "Error: Invalid Game Path";
                MessageBox.Show("Please select a valid FortniteClient-Win64-Shipping.exe file.", "Error");
                return;
            }

            StatusText.Text = "Launching...";
            LaunchBtn.IsEnabled = false;

            try
            {
                string cobaltPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cobalt.dll");

                // If Cobalt.dll is not in the build folder, try to find it in the project root for dev purposes
                if (!File.Exists(cobaltPath))
                {
                    // Fallback search logic or alert
                    string rootCobalt = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\Cobalt.dll"));
                     if (File.Exists(rootCobalt))
                     {
                         cobaltPath = rootCobalt;
                     }
                     else
                     {
                         MessageBox.Show("Cobalt.dll not found! Please place Cobalt.dll in the same folder as the launcher.", "Error");
                         LaunchBtn.IsEnabled = true;
                         return;
                     }
                }

                // Arguments for Fortnite 1.10
                string username = UsernameBox.Text;
                string args = $"-epicapp=Fortnite -epicenv=Prod -epiclocale=en-us -epicportal -nobe -fromfl=ecl -skippatchcheck -mcpusername={username} -password=unused";

                await Task.Run(() =>
                {
                    Injector.LaunchAndInject(GamePath, args, cobaltPath);
                });

                StatusText.Text = "Launched!";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error launching game.";
                MessageBox.Show($"Error: {ex.Message}", "Launch Error");
            }
            finally
            {
                LaunchBtn.IsEnabled = true;
            }
        }
    }
}
