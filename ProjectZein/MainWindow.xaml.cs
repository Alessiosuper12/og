using System.Windows;
using System.Windows.Controls;
using ProjectZein.Views;

namespace ProjectZein
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Default view
            MainContent.Content = new HomeView();
        }

        private void Nav_Home_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new HomeView();
        }

        private void Nav_Builds_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new BuildsView();
        }

        private void Nav_Leaderboard_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new LeaderboardView();
        }

        private void Nav_Settings_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new SettingsView();
        }

        private void Discord_Click(object sender, RoutedEventArgs e)
        {
            // In a real app, this would open a browser to the Discord OAuth2 URL.
            // Since we don't have a Client ID, we simulate the login.
            var btn = sender as Button;
            if (btn != null)
            {
                btn.Content = "LOGGED IN AS USER";
                btn.IsEnabled = false;
                MessageBox.Show("Accesso Discord simulato con successo!", "Project Zein", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
