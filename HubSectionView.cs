using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsITToolkit.Games.Pong;
using WindowsITToolkit.Games.TwentyFortyEight;

namespace WindowsITToolkit;

public sealed class HubSectionView : UserControl
{
    private HubSettings _settings;
    private readonly Action<HubSettings> _settingsChanged;
    private readonly Action<GameCatalogItem> _playRequested;
    private readonly StackPanel _content;

    public HubSectionView(
        string section,
        HubSettings settings,
        Action<HubSettings> settingsChanged,
        Action<GameCatalogItem> playRequested)
    {
        _settings = settings;
        _settingsChanged = settingsChanged;
        _playRequested = playRequested;

        var root = new DockPanel();
        _content = new StackPanel();
        var heading = new StackPanel { Margin = new Thickness(0, 4, 0, 22) };
        heading.Children.Add(CreateText(SectionTitle(section), 11, FontWeights.Bold, "HubAccentBrush"));
        heading.Children.Add(CreateText(SectionSubtitle(section), 29, FontWeights.ExtraBold, "HubTextBrush", 6));
        DockPanel.SetDock(heading, Dock.Top);
        root.Children.Add(heading);

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _content
        };
        root.Children.Add(scrollViewer);
        Content = root;

        switch (section)
        {
            case "favorites":
                BuildGameList(_settings.FavoriteGameIds, "No favorites yet. Add games from the library using the star button.");
                break;
            case "recent":
                BuildGameList(_settings.RecentlyPlayedGameIds, "Your recently played games will appear here.");
                break;
            case "statistics":
                BuildStatistics();
                break;
            case "settings":
                BuildSettings();
                break;
            case "about":
                BuildAbout();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(section), section, "Unknown hub section.");
        }
    }

    private void BuildGameList(IEnumerable<string> gameIds, string emptyMessage)
    {
        var catalog = GameCatalog.AllGames.ToDictionary(game => game.GameId, StringComparer.OrdinalIgnoreCase);
        var games = gameIds
            .Where(catalog.ContainsKey)
            .Select(id => catalog[id])
            .Where(game => game.Availability == GameAvailability.Available)
            .ToArray();
        if (games.Length == 0)
        {
            _content.Children.Add(CreateCard(CreateText(emptyMessage, 14, FontWeights.Medium, "HubMutedBrush")));
            return;
        }

        foreach (var game in games)
        {
            var button = new Button
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        CreateText(game.DisplayName, 18, FontWeights.Bold, "HubTextBrush"),
                        CreateText(game.Description, 12, FontWeights.Normal, "HubMutedBrush", 5)
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(18),
                Margin = new Thickness(0, 0, 0, 12),
                Style = (Style)FindResource("HubButtonStyle"),
                IsEnabled = game.Availability == GameAvailability.Available
            };
            button.Click += (_, _) => _playRequested(game);
            _content.Children.Add(button);
        }
    }

    private void BuildStatistics()
    {
        var totalLaunches = _settings.GameLaunchCounts.Values.Sum();
        AddMetric("Game sessions", totalLaunches.ToString());
        foreach (var game in GameCatalog.AllGames)
        {
            var launches = _settings.GameLaunchCounts.GetValueOrDefault(game.GameId);
            AddMetric($"{game.DisplayName} sessions", launches.ToString());
        }

        var snake = new HighScoreService().Load();
        AddMetric("Snake high score", snake.Succeeded ? snake.Score.ToString() : "Unavailable");
        var best2048 = new TwentyFortyEightBestScoreService().Load();
        AddMetric("2048 best score", best2048.Succeeded ? best2048.Score.ToString() : "Unavailable");

        AddRecordMetric("Minesweeper best times", new LocalGameRecordStore("minesweeper").Load().Records);
        AddRecordMetric("Memory Match best times", new LocalGameRecordStore("memory-match").Load().Records);

        var pong = new PongStatisticsService().Load();
        AddMetric("Pong matches", pong.Succeeded ? pong.Statistics.MatchesPlayed.ToString() : "Unavailable");
        if (pong.Succeeded)
        {
            AddMetric("Pong player victories", pong.Statistics.PlayerWins.ToString());
            AddMetric("Pong computer victories", pong.Statistics.ComputerWins.ToString());
            AddMetric("Pong best rally", pong.Statistics.BestRallyHits.ToString());
        }

        _content.Children.Add(CreateText(
            "Session counts record each time a game is opened. Results not currently tracked by a game are intentionally not inferred.",
            12, FontWeights.Normal, "HubMutedBrush", 12));
    }

    private void AddRecordMetric(string title, IReadOnlyDictionary<string, LocalGameRecord> records)
    {
        var times = records
            .Where(entry => entry.Value.BestTimeSeconds.HasValue)
            .Select(entry => $"{entry.Key}: {TimeSpan.FromSeconds(entry.Value.BestTimeSeconds!.Value):m\\:ss}")
            .ToArray();
        AddMetric(title, times.Length == 0 ? "No records yet" : string.Join(" · ", times));
        var moves = records
            .Where(entry => entry.Value.FewestMoves.HasValue)
            .Select(entry => $"{entry.Key}: {entry.Value.FewestMoves!.Value}")
            .ToArray();
        if (moves.Length > 0)
        {
            AddMetric(title.Replace("best times", "fewest moves", StringComparison.Ordinal), string.Join(" · ", moves));
        }
    }

    private void AddMetric(string label, string value)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        row.Children.Add(CreateText(label, 13, FontWeights.SemiBold, "HubMutedBrush"));
        var valueText = CreateText(value, 14, FontWeights.Bold, "HubTextBrush");
        Grid.SetColumn(valueText, 1);
        row.Children.Add(valueText);
        _content.Children.Add(CreateCard(row));
    }

    private void BuildSettings()
    {
        var settingsCard = new StackPanel();
        var reduceAnimations = new CheckBox
        {
            Content = "Reduce decorative animations",
            IsChecked = _settings.ReduceAnimations,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = GetBrush("HubTextBrush"),
            Margin = new Thickness(0, 0, 0, 16)
        };
        reduceAnimations.Checked += (_, _) => UpdateSetting(reduceAnimations.IsChecked == true, _settings.ConfirmExit);
        reduceAnimations.Unchecked += (_, _) => UpdateSetting(reduceAnimations.IsChecked == true, _settings.ConfirmExit);
        settingsCard.Children.Add(reduceAnimations);

        var confirmExit = new CheckBox
        {
            Content = "Ask before exiting the application",
            IsChecked = _settings.ConfirmExit,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = GetBrush("HubTextBrush")
        };
        confirmExit.Checked += (_, _) => UpdateSetting(_settings.ReduceAnimations, confirmExit.IsChecked == true);
        confirmExit.Unchecked += (_, _) => UpdateSetting(_settings.ReduceAnimations, confirmExit.IsChecked == true);
        settingsCard.Children.Add(confirmExit);
        _content.Children.Add(CreateCard(settingsCard));

        var restoreDefaults = new Button
        {
            Content = "RESTORE DEFAULT SETTINGS",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(18, 11, 18, 11),
            Margin = new Thickness(0, 16, 0, 0),
            Style = (Style)FindResource("HubButtonStyle")
        };
        restoreDefaults.Click += (_, _) =>
        {
            _settings = _settings with { ReduceAnimations = false, ConfirmExit = false };
            reduceAnimations.IsChecked = false;
            confirmExit.IsChecked = false;
            _settingsChanged(_settings);
        };
        _content.Children.Add(restoreDefaults);
    }

    private void UpdateSetting(bool reduceAnimations, bool confirmExit)
    {
        _settings = _settings with { ReduceAnimations = reduceAnimations, ConfirmExit = confirmExit };
        _settingsChanged(_settings);
    }

    private void BuildAbout()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "Development build";
        var about = new StackPanel();
        about.Children.Add(CreateText("Windows Arcade Hub", 22, FontWeights.Bold, "HubTextBrush"));
        about.Children.Add(CreateText(
            $"Version {version} · Windows desktop application",
            13, FontWeights.SemiBold, "HubAccentBrush", 8));
        about.Children.Add(CreateText(
            "A collection of quick, locally played arcade and puzzle games. Game progress and preferences are stored in your Windows LocalApplicationData folder.",
            14, FontWeights.Normal, "HubMutedBrush", 14));
        about.Children.Add(CreateText(
            "Built with .NET 10 and Windows Presentation Foundation. No online account is required to play.",
            13, FontWeights.Normal, "HubMutedBrush", 5));
        _content.Children.Add(CreateCard(about));
    }

    private Border CreateCard(UIElement content) => new()
    {
        Child = content,
        Padding = new Thickness(20),
        Margin = new Thickness(0, 0, 0, 12),
        CornerRadius = new CornerRadius(14),
        Background = GetBrush("HubCardBrush"),
        BorderBrush = GetBrush("HubBorderBrush"),
        BorderThickness = new Thickness(1),
        Effect = (System.Windows.Media.Effects.Effect)FindResource("HubCardShadow")
    };

    private TextBlock CreateText(
        string text,
        double size,
        FontWeight weight,
        string brushKey,
        double topMargin = 0) => new()
    {
        Text = text,
        FontSize = size,
        FontWeight = weight,
        Foreground = GetBrush(brushKey),
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, topMargin, 0, 0)
    };

    private Brush GetBrush(string key) => (Brush)FindResource(key);

    private static string SectionTitle(string section) => section switch
    {
        "favorites" => "YOUR SHORTLIST",
        "recent" => "PICK UP WHERE YOU LEFT OFF",
        "statistics" => "YOUR ARCADE ACTIVITY",
        "settings" => "PERSONALIZE YOUR HUB",
        "about" => "ABOUT THIS APP",
        _ => section.ToUpperInvariant()
    };

    private static string SectionSubtitle(string section) => section switch
    {
        "favorites" => "Favorite games.",
        "recent" => "Recently played.",
        "statistics" => "Your stats at a glance.",
        "settings" => "Settings.",
        "about" => "Made for a little downtime.",
        _ => "Windows Arcade Hub"
    };
}
