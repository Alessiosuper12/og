using System.Collections.Generic;
using System.Windows.Controls;

namespace ProjectZein.Views
{
    public partial class LeaderboardView : UserControl
    {
        public LeaderboardView()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            var data = new List<PlayerStats>
            {
                new PlayerStats { Rank = 1, Name = "Ninja", Points = 9999, Wins = 500 },
                new PlayerStats { Rank = 2, Name = "Tfue", Points = 8888, Wins = 450 },
                new PlayerStats { Rank = 3, Name = "ZeinUser", Points = 7777, Wins = 300 },
                new PlayerStats { Rank = 4, Name = "PlayerOne", Points = 5000, Wins = 100 },
            };
            LeaderboardGrid.ItemsSource = data;
        }

        public class PlayerStats
        {
            public int Rank { get; set; }
            public string Name { get; set; }
            public int Points { get; set; }
            public int Wins { get; set; }
        }
    }
}
