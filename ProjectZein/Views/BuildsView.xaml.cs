using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ProjectZein.Services;

namespace ProjectZein.Views
{
    public partial class BuildsView : UserControl
    {
        public BuildsView()
        {
            InitializeComponent();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Fortnite Executable (*.exe)|*.exe";
            if (dialog.ShowDialog() == true)
            {
                PathBox.Text = dialog.FileName;
            }
        }

        private void Launch_Click(object sender, RoutedEventArgs e)
        {
            string path = PathBox.Text;
            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show("Seleziona il percorso dell'eseguibile di Fortnite.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Inject Cobalt.dll logic
                Injector.LaunchAndInject(path);
                MessageBox.Show("Gioco avviato con successo!", "Project Zein", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Errore durante l'avvio: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
