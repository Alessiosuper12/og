using System;
using System.Windows;
using Microsoft.Win32;

namespace ProjectZein
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                PathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            string gamePath = PathTextBox.Text;
            if (string.IsNullOrWhiteSpace(gamePath))
            {
                MessageBox.Show("Please select the Fortnite executable.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Assuming Cobalt.dll is in the same directory as the launcher
                string dllPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cobalt.dll");

                if (!System.IO.File.Exists(dllPath))
                {
                     MessageBox.Show($"Cobalt.dll not found at: {dllPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                     return;
                }

                Injector.LaunchAndInject(gamePath, dllPath);
                MessageBox.Show("Launched and Injected!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
