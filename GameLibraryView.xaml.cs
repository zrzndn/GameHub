using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace WindowsITToolkit;

public partial class GameLibraryView : UserControl
{
    private readonly ICollectionView _games;
    private string _selectedCategory = "All";
    private HubSettings _settings;

    public GameLibraryView(HubSettings? settings = null)
    {
        InitializeComponent();
        _settings = settings ?? new HubSettings();
        _games = CollectionViewSource.GetDefaultView(GameCatalog.AllGames);
        _games.Filter = FilterGame;
        DataContext = this;
        foreach (var filter in new[]
        {
            AllGamesFilter,
            ArcadeFilter,
            PuzzleFilter,
            StrategyFilter
        })
        {
            filter.Checked += CategoryFilter_Checked;
        }

        AllGamesFilter.IsChecked = true;
        CategoryFilter_Checked(AllGamesFilter, new RoutedEventArgs());
        UpdateEmptyState();
    }

    public static GameIconConverter IconConverter { get; } = new();

    public ICollectionView Games => _games;

    public event Action<GameCatalogItem>? PlayRequested;
    public event Action<string>? FavoriteToggled;

    public void UpdateSettings(HubSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        UpdateFavoriteButtons(this);
    }

    private void GameLibraryView_Loaded(object sender, RoutedEventArgs e) => UpdateFavoriteButtons(this);

    private void GameCard_Loaded(object sender, RoutedEventArgs e)
    {
        if (_settings.ReduceAnimations || sender is not Border card)
        {
            return;
        }

        card.BeginAnimation(OpacityProperty, new DoubleAnimation(0.55, 1, TimeSpan.FromMilliseconds(180)));
    }

    private bool FilterGame(object item)
    {
        if (item is not GameCatalogItem game)
        {
            return false;
        }

        var matchesCategory = _selectedCategory == "All" ||
            string.Equals(game.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase);
        var query = SearchTextBox?.Text.Trim();
        var matchesSearch = string.IsNullOrEmpty(query) ||
            game.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase);

        return matchesCategory && matchesSearch;
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _games.Refresh();
        UpdateEmptyState();
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: GameCatalogItem { Availability: GameAvailability.Available } game })
        {
            PlayRequested?.Invoke(game);
        }
    }

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: GameCatalogItem game })
        {
            FavoriteToggled?.Invoke(game.GameId);
        }
    }

    private void UpdateFavoriteButtons(DependencyObject parent)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is Button { DataContext: GameCatalogItem game, Content: not null } button &&
                button.ClickMode == ClickMode.Release)
            {
                var isFavoriteButton = button.ToolTip?.ToString() == "Add to favorites" ||
                    button.ToolTip?.ToString() == "Remove from favorites";
                if (isFavoriteButton)
                {
                    var isFavorite = _settings.FavoriteGameIds.Contains(game.GameId, StringComparer.OrdinalIgnoreCase);
                    button.Content = isFavorite ? "★" : "☆";
                    button.ToolTip = isFavorite ? "Remove from favorites" : "Add to favorites";
                    button.Foreground = (System.Windows.Media.Brush)FindResource(
                        isFavorite ? "HubAccentBrush" : "HubMutedBrush");
                }
            }

            UpdateFavoriteButtons(child);
        }
    }

    private void CategoryFilter_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: string category })
        {
            return;
        }

        _selectedCategory = category;
        ResultsHeading.Text = category == "All"
            ? "ALL GAMES"
            : $"{category.ToUpperInvariant()} GAMES";
        _games.Refresh();
        UpdateEmptyState();
    }

    private void ArtworkImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Image image)
        {
            image.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateEmptyState()
    {
        if (EmptyState is not null)
        {
            EmptyState.Visibility = _games.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public sealed class GameIconConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is not string gameId || string.IsNullOrEmpty(gameId))
            {
                return "?";
            }

            return gameId == "2048"
                ? "2K"
                : char.ToUpperInvariant(gameId[0]).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
