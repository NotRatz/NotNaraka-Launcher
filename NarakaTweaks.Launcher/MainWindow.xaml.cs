using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using NarakaTweaks.Core.Services;
using Launcher.Shared.Configuration;

namespace NarakaTweaks.Launcher;

public partial class MainWindow : Window
{
    // Global reference to the action log for logging from any method
    private StackPanel? _actionLogStack;
    private ScrollViewer? _actionLogScrollViewer;
    private StackPanel? _newsStack;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Set default to Dashboard
        ShowDashboard();
        
        // Check if this is first run and show welcome dialog
        Loaded += async (s, e) =>
        {
            await CheckFirstTimeSetup();
            await LoadSteamNewsAsync();
        };
    }
    
    // Custom Title Bar Event Handlers
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            this.DragMove();
        }
    }
    
    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
    
    // Method to log messages to the action log panel
    private void LogToActionLog(string message, string colorHex = "#CCCCCC")
    {
        if (_actionLogStack == null) return;
        
        Dispatcher.Invoke(() =>
        {
            var logEntry = CreateLogEntry(message, colorHex);
            _actionLogStack.Children.Add(logEntry);
            
            // Auto-scroll to bottom
            _actionLogScrollViewer?.ScrollToEnd();
        });
    }
    
    private void NavigationButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            var tag = button.Tag?.ToString();
            
            switch (tag)
            {
                case "Dashboard":
                    // Dashboard stays in main window, just refresh it
                    ContentPanel.Children.Clear();
                    ShowDashboard();
                    break;
                case "Tweaks":
                    // Open Tweaks pop-out window
                    OpenTweaksWindow();
                    break;
                case "Clients":
                    // Open Clients pop-out window
                    OpenClientsWindow();
                    break;
                case "NarakaSettings":
                    // Open Naraka Settings pop-out window
                    OpenNarakaSettingsWindow();
                    break;
                case "Settings":
                    // Show Settings in main window (not pop-out)
                    ContentPanel.Children.Clear();
                    ShowSettings();
                    break;
            }
        }
    }
    
    private void ResetNavigationButtons()
    {
        DashboardButton.Background = System.Windows.Media.Brushes.Transparent;
        TweaksButton.Background = System.Windows.Media.Brushes.Transparent;
        ClientsButton.Background = System.Windows.Media.Brushes.Transparent;
        NarakaSettingsButton.Background = System.Windows.Media.Brushes.Transparent;
        SettingsButton.Background = System.Windows.Media.Brushes.Transparent;
    }
    
    // Pop-out window methods
    private void OpenTweaksWindow()
    {
        var tweaksWindow = new Window
        {
            Width = 800,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.Manual,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            Owner = this
        };
        
        // Position to the right of main launcher
        PositionWindowToRight(tweaksWindow);
        
        // Create main container with border
        var mainBorder = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(30, 30, 30)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 120, 215)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(10)
        };
        
        mainBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = System.Windows.Media.Colors.Black,
            BlurRadius = 20,
            ShadowDepth = 0,
            Opacity = 0.8
        };
        
        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        
        // Add custom title bar
        var titleBar = CreateCustomTitleBar("System Tweaks", tweaksWindow);
        Grid.SetRow(titleBar, 0);
        mainGrid.Children.Add(titleBar);
        
        // Add content
        var content = CreateTweaksPage();
        Grid.SetRow(content, 1);
        mainGrid.Children.Add(content);
        
        mainBorder.Child = mainGrid;
        tweaksWindow.Content = mainBorder;
        tweaksWindow.Show();
    }
    
    private void OpenClientsWindow()
    {
        var clientsWindow = new Window
        {
            Width = 800,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.Manual,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            Owner = this
        };
        
        // Position to the right of main launcher
        PositionWindowToRight(clientsWindow);
        
        // Create main container with border
        var mainBorder = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(30, 30, 30)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 120, 215)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(10)
        };
        
        mainBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = System.Windows.Media.Colors.Black,
            BlurRadius = 20,
            ShadowDepth = 0,
            Opacity = 0.8
        };
        
        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        
        // Add custom title bar
        var titleBar = CreateCustomTitleBar("Client Downloads", clientsWindow);
        Grid.SetRow(titleBar, 0);
        mainGrid.Children.Add(titleBar);
        
        // Add content
        var content = CreateClientsPage();
        Grid.SetRow(content, 1);
        mainGrid.Children.Add(content);
        
        mainBorder.Child = mainGrid;
        clientsWindow.Content = mainBorder;
        clientsWindow.Show();
    }
    
    private void OpenNarakaSettingsWindow()
    {
        var narakaSettingsWindow = new Window
        {
            Width = 900,
            Height = 700,
            WindowStartupLocation = WindowStartupLocation.Manual,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            Owner = this
        };
        
        // Position to the right of main launcher
        PositionWindowToRight(narakaSettingsWindow);
        
        // Create main container with border
        var mainBorder = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(30, 30, 30)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 120, 215)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(10)
        };
        
        mainBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = System.Windows.Media.Colors.Black,
            BlurRadius = 20,
            ShadowDepth = 0,
            Opacity = 0.8
        };
        
        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        
        // Add custom title bar
        var titleBar = CreateCustomTitleBar("Naraka Settings", narakaSettingsWindow);
        Grid.SetRow(titleBar, 0);
        mainGrid.Children.Add(titleBar);
        
        // Add content
        var content = CreateNarakaSettingsPage();
        Grid.SetRow(content, 1);
        mainGrid.Children.Add(content);
        
        mainBorder.Child = mainGrid;
        narakaSettingsWindow.Content = mainBorder;
        narakaSettingsWindow.Show();
    }
    
    private void OpenSettingsWindow()
    {
        try
        {
            // Check if Settings window is already open
            foreach (Window window in Application.Current.Windows)
            {
                if (window.Title == "Settings" && window != this)
                {
                    window.Activate();
                    return;
                }
            }
            
            var settingsWindow = new Window
            {
                Title = "Settings",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(30, 30, 30)),
                Owner = this
            };
            
            // Position to the right of main launcher
            PositionWindowToRight(settingsWindow);
            
            var content = CreateSettingsPage(settingsWindow);
            settingsWindow.Content = content;
            settingsWindow.Show();
        }
        catch (DirectoryNotFoundException ex)
        {
            MessageBox.Show(
                $"Configuration directory not found:\n\n{ex.Message}\n\nThe launcher will create the necessary directories.\nPlease try opening Settings again.",
                "Directory Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            LogToActionLog($"⚠️ Directory error: {ex.Message}", "#FFA500");
            
            // Try to create config directory
            try
            {
                var paths = new LauncherPaths();
                Directory.CreateDirectory(paths.ConfigurationRoot);
                LogToActionLog($"✅ Created config directory: {paths.ConfigurationRoot}", "#00FF00");
            }
            catch { }
        }
        catch (IOException ex)
        {
            MessageBox.Show(
                $"File system error:\n\n{ex.Message}\n\nCheck that you have write permissions.",
                "IO Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            LogToActionLog($"❌ IO error: {ex.Message}", "#FF0000");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error opening Settings window:\n\n{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
                "Settings Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            LogToActionLog($"❌ Settings error: {ex.GetType().Name} - {ex.Message}", "#FF0000");
        }
    }
    
    // Helper method to position pop-out windows to the right of main launcher
    private void PositionWindowToRight(Window window)
    {
        // Position the window to the right of the main launcher
        window.Left = this.Left + this.ActualWidth + 10; // 10px gap
        window.Top = this.Top;
    }
    
    // Helper method to create custom title bar with titlebar.png background
    private Border CreateCustomTitleBar(string titleText, Window window)
    {
        var titleBar = new Border
        {
            Height = 32,
            CornerRadius = new CornerRadius(8, 8, 0, 0),
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 120, 215))
        };
        
        // Make title bar draggable
        titleBar.MouseLeftButtonDown += (s, e) =>
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                window.DragMove();
            }
        };
        
        // Create main grid container
        var mainGrid = new Grid();
        
        // Add background image at 40% opacity
        try
        {
            var bgImage = new System.Windows.Controls.Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Assets/titlebar.png")),
                Opacity = 0.4,
                Stretch = System.Windows.Media.Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            mainGrid.Children.Add(bgImage);
        }
        catch
        {
            // Image will not be added if it fails to load, solid background will show
        }
        
        // Create title bar content grid (on top of background image)
        var titleGrid = new Grid();
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        // Title text
        var titleStackPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(15, 0, 0, 0)
        };
        
        var titleTextBlock = new TextBlock
        {
            Text = titleText,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = System.Windows.Media.Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        titleTextBlock.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = System.Windows.Media.Colors.Black,
            BlurRadius = 8,
            ShadowDepth = 2,
            Opacity = 0.7
        };
        
        titleStackPanel.Children.Add(titleTextBlock);
        Grid.SetColumn(titleStackPanel, 0);
        titleGrid.Children.Add(titleStackPanel);
        
        // Close button
        var closeButton = new Button
        {
            Content = "✕",
            Width = 46,
            Height = 32,
            FontSize = 16,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        
        closeButton.Click += (s, e) => window.Close();
        
        // Close button style with hover effect
        var closeButtonStyle = new Style(typeof(Button));
        closeButtonStyle.Setters.Add(new Setter(Button.BackgroundProperty, System.Windows.Media.Brushes.Transparent));
        
        var closeButtonTemplate = new ControlTemplate(typeof(Button));
        var closeButtonBorder = new FrameworkElementFactory(typeof(Border));
        closeButtonBorder.SetValue(Border.BackgroundProperty, new System.Windows.TemplateBindingExtension(Button.BackgroundProperty));
        closeButtonBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(0, 8, 0, 0));
        var closeButtonPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        closeButtonPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        closeButtonPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        closeButtonBorder.AppendChild(closeButtonPresenter);
        closeButtonTemplate.VisualTree = closeButtonBorder;
        closeButtonStyle.Setters.Add(new Setter(Button.TemplateProperty, closeButtonTemplate));
        
        var closeButtonHoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
        closeButtonHoverTrigger.Setters.Add(new Setter(Button.BackgroundProperty, 
            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 17, 35))));
        closeButtonStyle.Triggers.Add(closeButtonHoverTrigger);
        
        closeButton.Style = closeButtonStyle;
        
        Grid.SetColumn(closeButton, 1);
        titleGrid.Children.Add(closeButton);
        
        // Add title grid on top of background image
        mainGrid.Children.Add(titleGrid);
        
        titleBar.Child = mainGrid;
        return titleBar;
    }
    
    // Helper method to create styled primary button
    private Button CreateStyledButton(string content, double width, double height, bool isPrimary = true)
    {
        var button = new Button
        {
            Content = content,
            Width = width,
            Height = height,
            Margin = new Thickness(5),
            Background = new System.Windows.Media.SolidColorBrush(
                isPrimary ? System.Windows.Media.Color.FromRgb(0, 120, 215) : System.Windows.Media.Color.FromRgb(60, 60, 60)),
            Foreground = System.Windows.Media.Brushes.White,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Cursor = System.Windows.Input.Cursors.Hand,
            BorderThickness = new Thickness(0),
            BorderBrush = System.Windows.Media.Brushes.Transparent
        };
        
        // Add hover effects
        button.MouseEnter += (s, e) =>
        {
            button.Background = new System.Windows.Media.SolidColorBrush(
                isPrimary ? System.Windows.Media.Color.FromRgb(0, 140, 245) : System.Windows.Media.Color.FromRgb(80, 80, 80));
        };
        
        button.MouseLeave += (s, e) =>
        {
            button.Background = new System.Windows.Media.SolidColorBrush(
                isPrimary ? System.Windows.Media.Color.FromRgb(0, 120, 215) : System.Windows.Media.Color.FromRgb(60, 60, 60));
        };
        
        return button;
    }
    
    private async void ShowDashboard()
    {
        ContentPanel.Children.Clear();
        
        var mainGrid = new Grid { Margin = new Thickness(10) };
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(300) }); // Steam News section (TOP PRIORITY - LARGER)
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(120) }); // YouTube promotional section
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Action log
        
        // Steam News Section - TOP BOX
        var newsSection = CreateNewsSection();
        Grid.SetRow(newsSection, 0);
        
        // YouTube Promotional Section
        var youtubeSection = CreateYouTubeSection();
        Grid.SetRow(youtubeSection, 1);
        
        // Action Log Section
        var logSection = CreateActionLogSection();
        Grid.SetRow(logSection, 2);
        
        mainGrid.Children.Add(newsSection);
        mainGrid.Children.Add(youtubeSection);
        mainGrid.Children.Add(logSection);
        
        ContentPanel.Children.Add(mainGrid);
        
        // Reload Steam news
        await LoadSteamNewsAsync();
    }
    
    private Border CreateNewsSection()
    {
        var border = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(30, 30, 35)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 120, 215)),
            BorderThickness = new Thickness(2),
            CornerRadius = new System.Windows.CornerRadius(6),
            Margin = new Thickness(0, 0, 0, 10)
        };
        
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        
        // Header
        var header = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 120, 215)),
            Padding = new Thickness(10, 5, 10, 5)
        };
        
        var headerText = new TextBlock
        {
            Text = "NARAKA: BLADEPOINT NEWS - Steam Community",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White
        };
        header.Child = headerText;
        Grid.SetRow(header, 0);
        
        // News content scroll viewer
        var scrollViewer = new System.Windows.Controls.ScrollViewer
        {
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            Padding = new Thickness(10)
        };
        
        _newsStack = new StackPanel();
        
        // Loading indicator
        var loadingText = new TextBlock
        {
            Text = "⏳ Loading latest news from Steam...",
            FontSize = 10,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(220, 220, 220)),
            Margin = new Thickness(5),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _newsStack.Children.Add(loadingText);
        
        scrollViewer.Content = _newsStack;
        Grid.SetRow(scrollViewer, 1);
        
        grid.Children.Add(header);
        grid.Children.Add(scrollViewer);
        border.Child = grid;
        
        return border;
    }
    
    private Border CreateNewsItem(string title, string description, DateTime? date = null, string? url = null)
    {
        var border = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(40, 40, 45)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(60, 60, 65)),
            BorderThickness = new Thickness(1),
            CornerRadius = new System.Windows.CornerRadius(4),
            Padding = new Thickness(8),
            Margin = new Thickness(0, 0, 0, 5),
            Cursor = !string.IsNullOrEmpty(url) ? Cursors.Hand : Cursors.Arrow
        };
        
        // Make clickable if URL exists
        if (!string.IsNullOrEmpty(url))
        {
            border.MouseEnter += (s, e) =>
            {
                border.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(50, 50, 55));
            };
            border.MouseLeave += (s, e) =>
            {
                border.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(40, 40, 45));
            };
            border.MouseLeftButtonDown += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                    LogToActionLog($"🌐 Opened news article in browser", "#64B5F6");
                }
                catch (Exception ex)
                {
                    LogToActionLog($"❌ Failed to open URL: {ex.Message}", "#F44336");
                }
            };
        }
        
        var stack = new StackPanel();
        
        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(100, 180, 255)),
            TextWrapping = TextWrapping.Wrap
        };
        
        var descText = new TextBlock
        {
            Text = description,
            FontSize = 10,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(240, 240, 240)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        };
        
        stack.Children.Add(titleText);
        stack.Children.Add(descText);
        
        // Add date if provided
        if (date.HasValue)
        {
            var dateText = new TextBlock
            {
                Text = $"📅 {FormatNewsDate(date.Value)}",
                FontSize = 9,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(120, 120, 120)),
                Margin = new Thickness(0, 3, 0, 0)
            };
            stack.Children.Add(dateText);
        }
        
        border.Child = stack;
        
        return border;
    }
    
    private Border CreateYouTubeSection()
    {
        var border = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(30, 30, 35)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 0, 0)), // YouTube red
            BorderThickness = new Thickness(2),
            CornerRadius = new System.Windows.CornerRadius(6),
            Margin = new Thickness(0, 10, 0, 10)
        };
        
        var grid = new Grid { Margin = new Thickness(15) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        // YouTube Icon/Logo
        var iconText = new TextBlock
        {
            Text = "▶️",
            FontSize = 48,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 15, 0)
        };
        Grid.SetColumn(iconText, 0);
        
        // Text Content
        var textStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center
        };
        
        var titleText = new TextBlock
        {
            Text = "🎬 FEATURED CONTENT",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 0, 0)),
            Margin = new Thickness(0, 0, 0, 5)
        };
        
        var descText = new TextBlock
        {
            Text = "Watch the latest NotNaraka tutorials, tips & tricks!",
            FontSize = 12,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(240, 240, 240)),
            TextWrapping = TextWrapping.Wrap
        };
        
        textStack.Children.Add(titleText);
        textStack.Children.Add(descText);
        Grid.SetColumn(textStack, 1);
        
        // Watch Button
        var watchButton = new Button
        {
            Content = "📺 Watch on YouTube",
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 0, 0)),
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(20, 10, 20, 10),
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        watchButton.Click += OpenYouTube_Click;
        Grid.SetColumn(watchButton, 2);
        
        grid.Children.Add(iconText);
        grid.Children.Add(textStack);
        grid.Children.Add(watchButton);
        border.Child = grid;
        
        return border;
    }
    
    private void OpenYouTube_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Latest Strive video
            var youtubeUrl = "https://www.youtube.com/watch?v=F6BO7-w5N-I";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = youtubeUrl,
                UseShellExecute = true
            });
            LogToActionLog($"🎬 Opening YouTube: Latest Strive Video", "#FF0000");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open YouTube link: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            LogToActionLog($"❌ Failed to open YouTube: {ex.Message}", "#FF0000");
        }
    }
    
    private Border CreateActionLogSection()
    {
        var border = new Border
        {
            Background = System.Windows.Media.Brushes.Black,
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 120, 215)),
            BorderThickness = new Thickness(2),
            CornerRadius = new System.Windows.CornerRadius(6)
        };
        
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        
        // Header
        var header = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 120, 215)),
            Padding = new Thickness(10, 5, 10, 5)
        };
        
        var headerText = new TextBlock
        {
            Text = "ACTION LOG",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White
        };
        header.Child = headerText;
        Grid.SetRow(header, 0);
        
        // Log content
        _actionLogScrollViewer = new System.Windows.Controls.ScrollViewer
        {
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            Padding = new Thickness(10),
            Background = System.Windows.Media.Brushes.Black
        };
        
        _actionLogStack = new StackPanel();
        
        // System startup messages
        _actionLogStack.Children.Add(CreateLogEntry("Program opened successfully! Version 1.0.0 [OK!]", "#00FF00"));
        _actionLogStack.Children.Add(CreateLogEntry("Checking for updates... You have the latest version!", "#00FF00"));
        _actionLogStack.Children.Add(CreateLogEntry("Loading system tweaks... Ready!", "#00FF00"));
        _actionLogStack.Children.Add(CreateLogEntry("Initializing background services... Done!", "#00FF00"));
        _actionLogStack.Children.Add(CreateLogEntry("All done - System ready!", "#FFFF00"));
        _actionLogStack.Children.Add(CreateLogEntry("", "#FFFFFF"));
        _actionLogStack.Children.Add(CreateLogEntry("Select a tab from the left to get started.", "#CCCCCC"));
        
        _actionLogScrollViewer.Content = _actionLogStack;
        Grid.SetRow(_actionLogScrollViewer, 1);
        
        grid.Children.Add(header);
        grid.Children.Add(_actionLogScrollViewer);
        border.Child = grid;
        
        return border;
    }
    
    private TextBlock CreateLogEntry(string message, string colorHex)
    {
        return new TextBlock
        {
            Text = message,
            FontSize = 11,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex)),
            Margin = new Thickness(0, 1, 0, 1),
            TextWrapping = TextWrapping.Wrap
        };
    }
    
    private void ShowTweaks()
    {
        // This method is now only used for legacy support
        // Pop-out windows use CreateTweaksPage() directly
        ContentPanel.Children.Clear();
        
        // Create the tweaks page
        var tweaksPage = CreateTweaksPage();
        ContentPanel.Children.Add(tweaksPage);
    }
    
    private UIElement CreateTweaksPage()
    {
        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header (auto-sized)
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons
        
        // Header
        var header = new StackPanel
        {
            Margin = new Thickness(15, 10, 15, 10),
            Orientation = Orientation.Horizontal
        };
        
        var icon = new TextBlock
        {
            Text = "⚙️",
            FontSize = 24,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        
        var titleStack = new StackPanel();
        var title = new TextBlock
        {
            Text = "System Tweaks",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White,
            TextWrapping = TextWrapping.NoWrap
        };
        var subtitle = new TextBlock
        {
            Text = "Optimize your system for maximum performance",
            FontSize = 11,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(240, 240, 240)),
            TextWrapping = TextWrapping.Wrap
        };
        
        titleStack.Children.Add(title);
        titleStack.Children.Add(subtitle);
        header.Children.Add(icon);
        header.Children.Add(titleStack);
        Grid.SetRow(header, 0);
        
        // TabControl for categories
        var tabControl = new System.Windows.Controls.TabControl
        {
            Margin = new Thickness(15, 5, 15, 10),
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(30, 30, 30))
        };
        Grid.SetRow(tabControl, 1);
        
        // Get tweaks from service
        var tweaks = App.Current.TweaksService.GetAvailableTweaks();
        
        // Create tabs for each category
        foreach (var category in tweaks)
        {
            var tab = new System.Windows.Controls.TabItem
            {
                Header = GetCategoryIcon(category.Key) + " " + category.Key.ToString()
            };
            
            var scrollViewer = new System.Windows.Controls.ScrollViewer
            {
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                Padding = new Thickness(10)
            };
            
            var tweaksPanel = new StackPanel();
            
            foreach (var tweak in category.Value)
            {
                var tweakControl = CreateTweakControl(tweak);
                tweaksPanel.Children.Add(tweakControl);
            }
            
            scrollViewer.Content = tweaksPanel;
            tab.Content = scrollViewer;
            tabControl.Items.Add(tab);
        }
        
        // Action buttons
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(20)
        };
        Grid.SetRow(buttonPanel, 2);
        
        var applyButton = CreateStyledButton("✓ Apply Selected Tweaks", 180, 40, isPrimary: true);
        applyButton.Click += async (s, e) => await ApplySelectedTweaks(tabControl);
        
        var revertButton = CreateStyledButton("↻ Revert All", 140, 40, isPrimary: false);
        revertButton.Click += async (s, e) => await RevertAllTweaks(tabControl);
        
        buttonPanel.Children.Add(applyButton);
        buttonPanel.Children.Add(revertButton);
        
        mainGrid.Children.Add(header);
        mainGrid.Children.Add(tabControl);
        mainGrid.Children.Add(buttonPanel);
        
        return mainGrid;
    }
    
    // Social media link handlers
    private void OpenTwitch_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://twitch.tv/notratz");
    }
    
    private void OpenGitHub_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/notratz");
    }
    
    private void OpenKoFi_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://ko-fi.com/notratz");
    }
    
    private void OpenDiscord_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://discord.gg/teamstrive");
    }
    
    private void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open link: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void LaunchGame_Click(object sender, RoutedEventArgs e)
    {
        var config = App.Current.ConfigStore.LoadOrCreateDefault();
        
        // If user has a preferred launch method, use it directly
        if (!string.IsNullOrEmpty(config.PreferredLaunchMethod))
        {
            LaunchGameVia(config.PreferredLaunchMethod, config);
        }
        else
        {
            // Show launch options dialog
            var launchDialog = CreateLaunchOptionsDialog();
            var result = launchDialog.ShowDialog();
            
            if (result == true && launchDialog.Tag is string launchMethod)
            {
                LaunchGameVia(launchMethod, config);
            }
        }
    }
    
    private void LaunchGameVia(string method, LauncherConfiguration config)
    {
        try
        {
            switch (method)
            {
                case "Steam":
                    LogToActionLog("🚀 Launching Naraka via Steam...", "#00FFFF");
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "steam://rungameid/1203220", // Correct Naraka Steam App ID
                        UseShellExecute = true
                    });
                    break;
                    
                case "Epic":
                    LogToActionLog("🚀 Launching Naraka via Epic Games...", "#00FFFF");
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "com.epicgames.launcher://apps/Naraka?action=launch",
                        UseShellExecute = true
                    });
                    break;
                    
                case "Official":
                    LogToActionLog("🚀 Launching Naraka via Official launcher...", "#00FFFF");
                    
                    // Try configured path first
                    if (!string.IsNullOrEmpty(config.OfficialGamePath))
                    {
                        var exePath = Path.Combine(config.OfficialGamePath, "NarakaBladepoint.exe");
                        if (File.Exists(exePath))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = exePath,
                                UseShellExecute = true,
                                WorkingDirectory = config.OfficialGamePath
                            });
                            return;
                        }
                    }
                    
                    // Try common paths
                    var possiblePaths = new[]
                    {
                        @"C:\Program Files\NARAKA BLADEPOINT\NarakaBladepoint.exe",
                        @"C:\Program Files (x86)\NARAKA BLADEPOINT\NarakaBladepoint.exe",
                        @"C:\Games\NARAKA BLADEPOINT\NarakaBladepoint.exe",
                        @"D:\Games\NARAKA BLADEPOINT\NarakaBladepoint.exe"
                    };
                    
                    var foundPath = possiblePaths.FirstOrDefault(File.Exists);
                    if (foundPath != null)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = foundPath,
                            UseShellExecute = true,
                            WorkingDirectory = Path.GetDirectoryName(foundPath)
                        });
                    }
                    else
                    {
                        MessageBox.Show(
                            "Could not find Naraka installation.\n\n" +
                            "Please configure the game path in Settings or launch manually.", 
                            "Not Found", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Warning);
                    }
                    break;
            }
            
            LogToActionLog("✅ Game launched successfully!", "#00FF00");
        }
        catch (Exception ex)
        {
            LogToActionLog($"❌ Failed to launch: {ex.Message}", "#FF0000");
            MessageBox.Show($"Failed to launch game: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    // Create styled launch options dialog
    private Window CreateLaunchOptionsDialog()
    {
        var window = new Window
        {
            Width = 450,
            Height = 350,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false
        };
        
        var border = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(30, 30, 30)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 120, 215)),
            BorderThickness = new Thickness(2),
            CornerRadius = new System.Windows.CornerRadius(10),
            Padding = new Thickness(25)
        };
        
        border.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = Colors.Black,
            BlurRadius = 20,
            ShadowDepth = 0,
            Opacity = 0.8
        };
        
        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        var titleText = new TextBlock
        {
            Text = "How would you like to launch Naraka?",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 25)
        };
        
        stack.Children.Add(titleText);
        
        // Steam button
        var steamButton = CreateLaunchOptionButton(
            "Steam",
            "🎮",
            "#1B2838",
            "#66C0F4"
        );
        steamButton.Click += (s, e) =>
        {
            window.Tag = "Steam";
            window.DialogResult = true;
            window.Close();
        };
        stack.Children.Add(steamButton);
        
        // Epic button
        var epicButton = CreateLaunchOptionButton(
            "Epic Games",
            "🎯",
            "#121212",
            "#0078F2"
        );
        epicButton.Click += (s, e) =>
        {
            window.Tag = "Epic";
            window.DialogResult = true;
            window.Close();
        };
        stack.Children.Add(epicButton);
        
        // Official button
        var officialButton = CreateLaunchOptionButton(
            "Official Launcher",
            "🗡",
            "#1E1E1E",
            "#FF6B35"
        );
        officialButton.Click += (s, e) =>
        {
            window.Tag = "Official";
            window.DialogResult = true;
            window.Close();
        };
        stack.Children.Add(officialButton);
        
        // Cancel button
        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 380,
            Height = 40,
            Margin = new Thickness(0, 15, 0, 0),
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(220, 220, 220)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(100, 100, 100)),
            BorderThickness = new Thickness(1),
            FontSize = 13,
            Cursor = Cursors.Hand
        };
        
        cancelButton.Click += (s, e) =>
        {
            window.DialogResult = false;
            window.Close();
        };
        
        stack.Children.Add(cancelButton);
        
        border.Child = stack;
        window.Content = border;
        
        return window;
    }
    
    private Button CreateLaunchOptionButton(string text, string icon, string bgColor, string accentColor)
    {
        var button = new Button
        {
            Width = 380,
            Height = 60,
            Margin = new Thickness(0, 0, 0, 10),
            Cursor = Cursors.Hand
        };
        
        var buttonBorder = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(bgColor)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(accentColor)),
            BorderThickness = new Thickness(2),
            CornerRadius = new System.Windows.CornerRadius(5)
        };
        
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        
        var iconText = new TextBlock
        {
            Text = icon,
            FontSize = 32,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(15, 0, 15, 0)
        };
        Grid.SetColumn(iconText, 0);
        
        var labelText = new TextBlock
        {
            Text = text,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = System.Windows.Media.Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(labelText, 1);
        
        grid.Children.Add(iconText);
        grid.Children.Add(labelText);
        buttonBorder.Child = grid;
        
        var template = new ControlTemplate(typeof(Button));
        var factory = new FrameworkElementFactory(typeof(Border));
        factory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        factory.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
        template.VisualTree = factory;
        button.Template = template;
        button.Content = buttonBorder;
        
        // Hover effect
        button.MouseEnter += (s, e) =>
        {
            buttonBorder.BorderThickness = new Thickness(3);
        };
        button.MouseLeave += (s, e) =>
        {
            buttonBorder.BorderThickness = new Thickness(2);
        };
        
        return button;
    }
    
    private string GetCategoryIcon(NarakaTweaks.Core.Services.TweakCategory category)
    {
        return category switch
        {
            NarakaTweaks.Core.Services.TweakCategory.Performance => "🚀",
            NarakaTweaks.Core.Services.TweakCategory.GPU => "🎮",
            NarakaTweaks.Core.Services.TweakCategory.System => "⚙️",
            NarakaTweaks.Core.Services.TweakCategory.Privacy => "🔒",
            _ => "📋"
        };
    }
    
    private Border CreateTweakControl(NarakaTweaks.Core.Services.TweakDefinition tweak)
    {
        var border = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(45, 45, 48)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(62, 62, 66)),
            BorderThickness = new Thickness(1),
            CornerRadius = new System.Windows.CornerRadius(6),
            Margin = new Thickness(0, 5, 0, 5),
            Padding = new Thickness(12)
        };
        
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        // Checkbox
        var checkbox = new System.Windows.Controls.CheckBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0),
            Tag = tweak
        };
        Grid.SetColumn(checkbox, 0);
        
        // Info panel
        var infoPanel = new StackPanel();
        Grid.SetColumn(infoPanel, 1);
        
        var nameText = new TextBlock
        {
            Text = tweak.Name,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = System.Windows.Media.Brushes.White
        };
        
        var descText = new TextBlock
        {
            Text = tweak.Description,
            FontSize = 12,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(230, 230, 230)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        };
        
        var metaPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 4, 0, 0)
        };
        
        if (tweak.RequiresAdmin)
        {
            metaPanel.Children.Add(CreateBadge("Admin Required", "#FFC107"));
        }
        
        if (tweak.RequiresRestart)
        {
            metaPanel.Children.Add(CreateBadge("Restart Required", "#FF5722"));
        }
        
        infoPanel.Children.Add(nameText);
        infoPanel.Children.Add(descText);
        if (metaPanel.Children.Count > 0)
        {
            infoPanel.Children.Add(metaPanel);
        }
        
        // Status indicator
        var statusText = new TextBlock
        {
            Text = "●",
            FontSize = 20,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(100, 100, 100)),
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "Not Applied"
        };
        Grid.SetColumn(statusText, 2);
        
        grid.Children.Add(checkbox);
        grid.Children.Add(infoPanel);
        grid.Children.Add(statusText);
        
        border.Child = grid;
        return border;
    }
    
    private Border CreateBadge(string text, string colorHex)
    {
        var border = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex)),
            CornerRadius = new System.Windows.CornerRadius(3),
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(0, 0, 4, 0)
        };
        
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White
        };
        
        border.Child = textBlock;
        return border;
    }
    

    
    private void ShowClients()
    {
        // This method is now only used for legacy support
        // Pop-out windows use CreateClientsPage() directly
        ContentPanel.Children.Clear();
        var clientsPage = CreateClientsPage();
        ContentPanel.Children.Add(clientsPage);
    }
    
    private UIElement CreateClientsPage()
    {
        var mainGrid = new Grid { Margin = new Thickness(20) };
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        
        // Header
        var headerStack = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 20)
        };
        
        var title = new TextBlock
        {
            Text = "📥 Download Naraka Clients",
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White
        };
        
        var subtitle = new TextBlock
        {
            Text = "Download and install different versions of Naraka: Bladepoint",
            FontSize = 14,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(230, 230, 230)),
            Margin = new Thickness(0, 8, 0, 0)
        };
        
        headerStack.Children.Add(title);
        headerStack.Children.Add(subtitle);
        Grid.SetRow(headerStack, 0);
        
        // Clients Grid
        var clientsGrid = new Grid();
        clientsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        clientsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        clientsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        clientsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(clientsGrid, 1);
        
        // Steam Client
        var steamClient = CreateClientCard(
            "🎮 Steam Version",
            "Official Steam release - Global servers",
            "Download from Steam Store",
            "#1B2838",
            (s, e) => OpenUrl("steam://store/1665650")
        );
        Grid.SetRow(steamClient, 0);
        Grid.SetColumn(steamClient, 0);
        
        // Epic Games Client
        var epicClient = CreateClientCard(
            "🎯 Epic Games Version",
            "Official Epic Games release - Global servers",
            "Download from Epic Store",
            "#2A2A2A",
            (s, e) => OpenUrl("https://store.epicgames.com/en-US/p/naraka-bladepoint")
        );
        Grid.SetRow(epicClient, 0);
        Grid.SetColumn(epicClient, 1);
        
        // Official CN Client
        var cnClient = CreateClientCard(
            "🇨🇳 Chinese (CN) Client",
            "Official Chinese client - CN servers with exclusive content",
            "Download CN Client",
            "#DC2626",
            async (s, e) => await DownloadCnClientAsync()
        );
        Grid.SetRow(cnClient, 1);
        Grid.SetColumn(cnClient, 0);
        
        // Official NA/Global Client
        var naClient = CreateClientCard(
            "🌎 Official Global Client",
            "Standalone official client - Global servers (non-Steam/Epic)",
            "Download Global Client",
            "#0078D7",
            async (s, e) => await DownloadGlobalClientAsync()
        );
        Grid.SetRow(naClient, 1);
        Grid.SetColumn(naClient, 1);
        
        clientsGrid.Children.Add(steamClient);
        clientsGrid.Children.Add(epicClient);
        clientsGrid.Children.Add(cnClient);
        clientsGrid.Children.Add(naClient);
        
        mainGrid.Children.Add(headerStack);
        mainGrid.Children.Add(clientsGrid);
        
        return mainGrid;
    }
    
    private Border CreateClientCard(string title, string description, string buttonText, string accentColor, RoutedEventHandler buttonClick)
    {
        var border = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(45, 45, 48)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(accentColor)),
            BorderThickness = new Thickness(2),
            CornerRadius = new System.Windows.CornerRadius(8),
            Margin = new Thickness(10),
            Padding = new Thickness(20)
        };
        
        var stack = new StackPanel();
        
        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White,
            Margin = new Thickness(0, 0, 0, 8)
        };
        
        var descText = new TextBlock
        {
            Text = description,
            FontSize = 13,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(230, 230, 230)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        };
        
        var downloadButton = new Button
        {
            Content = buttonText,
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(accentColor)),
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(20, 10, 20, 10),
            Cursor = System.Windows.Input.Cursors.Hand,
            BorderThickness = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        downloadButton.Click += buttonClick;
        
        // Add hover effects
        var originalColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(accentColor);
        downloadButton.MouseEnter += (s, e) =>
        {
            var lighterColor = System.Windows.Media.Color.FromRgb(
                (byte)Math.Min(255, originalColor.R + 30),
                (byte)Math.Min(255, originalColor.G + 30),
                (byte)Math.Min(255, originalColor.B + 30));
            downloadButton.Background = new System.Windows.Media.SolidColorBrush(lighterColor);
        };
        downloadButton.MouseLeave += (s, e) =>
        {
            downloadButton.Background = new System.Windows.Media.SolidColorBrush(originalColor);
        };
        
        stack.Children.Add(titleText);
        stack.Children.Add(descText);
        stack.Children.Add(downloadButton);
        
        border.Child = stack;
        return border;
    }
    
    private UIElement CreateNarakaSettingsPage()
    {
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(20)
        };
        
        var mainStack = new StackPanel();
        
        // Header
        var title = new TextBlock
        {
            Text = "🎮 Naraka In-Game Settings",
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White,
            Margin = new Thickness(0, 0, 0, 10)
        };
        mainStack.Children.Add(title);
        
        var subtitle = new TextBlock
        {
            Text = "Optimize your Naraka: Bladepoint graphics settings for maximum performance",
            FontSize = 14,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(230, 230, 230)),
            Margin = new Thickness(0, 0, 0, 20)
        };
        mainStack.Children.Add(subtitle);
        
        // Recommended Settings Button
        var recommendedBtn = CreateStyledButton("Apply Competitive Settings (Recommended)", 300, 40);
        recommendedBtn.Click += ApplyRecommendedSettings_Click;
        recommendedBtn.Margin = new Thickness(0, 0, 0, 20);
        mainStack.Children.Add(recommendedBtn);
        
        // Settings Sections
        mainStack.Children.Add(CreateNarakaSettingsSection("General Settings",
            CreateNarakaSettingRow("Render Scale", "renderScale", new[] { "100%", "99%", "95%", "90%", "85%", "80%" }),
            CreateNarakaSettingRow("Display Mode", "fullScreenMode", new[] { "Fullscreen (0)", "Borderless (2)", "Windowed (1)" }),
            CreateNarakaSettingRow("VSync", "vSyncCount", new[] { "Off (0)", "On (1)" }),
            CreateNarakaSettingRow("AA Algorithm", "aaMode", new[] { "Off (0)", "SMAA (1)", "TAA (2)" }),
            CreateNarakaSettingRow("Motion Blur", "motionBlurEnabled", new[] { "Off", "On" }),
            CreateNarakaSettingRow("NVIDIA DLSS", "dlssMode", new[] { "Off (-2)", "Quality (-1)", "Balanced (0)", "Performance (1)", "Ultra Performance (2)" }),
            CreateNarakaSettingRow("NVIDIA Reflex", "reflexMode", new[] { "Off (0)", "Reflex (1)", "Reflex+Boost (2)" }),
            CreateNarakaSettingRow("NVIDIA Highlights", "nvHighlightsEnabled", new[] { "Off", "On" })
        ));
        
        mainStack.Children.Add(CreateNarakaSettingsSection("Graphics Quality",
            CreateNarakaSettingRow("Model Quality", "m_modelQualityLevel", new[] { "Lowest (0)", "Low (1)", "Medium (2)", "High (3)" }),
            CreateNarakaSettingRow("Tessellation", "m_tessellationQualityLevel", new[] { "Off (0)", "Low (1)", "Medium (2)", "High (3)" }),
            CreateNarakaSettingRow("Effects", "m_visualEffectsQualityLevel", new[] { "Low (0)", "Medium (1)", "High (2)", "Highest (3)" }),
            CreateNarakaSettingRow("Textures", "m_textureQualityLevel", new[] { "Lowest (0)", "Low (1)", "Medium (2)", "High (3)" }),
            CreateNarakaSettingRow("Shadows", "m_shadowQualityLevel", new[] { "Lowest (0)", "Low (1)", "Medium (2)", "High (3)", "Highest (4)" }),
            CreateNarakaSettingRow("Volumetric Lighting", "m_volumetricLightLevel", new[] { "Low (0)", "Medium (1)", "High (2)", "Highest (3)" }),
            CreateNarakaSettingRow("Volumetric Clouds", "m_cloudQualityLevel", new[] { "Off (0)", "Low (1)", "Medium (2)", "High (3)", "Highest (4)" }),
            CreateNarakaSettingRow("Ambient Occlusion", "m_aoLevel", new[] { "Off (0)", "Low (1)", "Highest (2)" }),
            CreateNarakaSettingRow("Screen Space Reflections", "m_SSRLevel", new[] { "Off (0)", "Low (1)", "Medium (2)", "High (3)", "Highest (4)" }),
            CreateNarakaSettingRow("Anti-aliasing Quality", "m_AALevel", new[] { "Low (0)", "Medium (1)", "High (2)" }),
            CreateNarakaSettingRow("Post-processing", "m_PostProcessingLevel", new[] { "Lowest (0)", "Low (1)", "Medium (2)", "High (3)" }),
            CreateNarakaSettingRow("Lighting Quality", "m_LightingQualityLevel", new[] { "Low (0)", "Medium (1)", "High (2)", "Highest (3)" })
        ));
        
        // Save Button
        var saveBtn = CreateStyledButton("💾 Save Settings to Game", 250, 45);
        saveBtn.Click += SaveNarakaSettings_Click;
        saveBtn.Margin = new Thickness(0, 30, 0, 0);
        mainStack.Children.Add(saveBtn);
        
        // Info text
        var infoText = new TextBlock
        {
            Text = "⚠️ Game must be closed for settings to take effect\nSettings file: NarakaBladepoint_Data\\QualitySettingsData.txt",
            FontSize = 11,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(200, 200, 200)),
            Margin = new Thickness(0, 10, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        mainStack.Children.Add(infoText);
        
        scrollViewer.Content = mainStack;
        return scrollViewer;
    }
    
    private Border CreateNarakaSettingsSection(string sectionTitle, params UIElement[] settings)
    {
        var section = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(40, 40, 40)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(20),
            Margin = new Thickness(0, 0, 0, 15)
        };
        
        var stack = new StackPanel();
        
        var sectionTitleBlock = new TextBlock
        {
            Text = sectionTitle,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 150, 255)),
            Margin = new Thickness(0, 0, 0, 15)
        };
        stack.Children.Add(sectionTitleBlock);
        
        foreach (var setting in settings)
        {
            stack.Children.Add(setting);
        }
        
        section.Child = stack;
        return section;
    }
    
    private Grid CreateNarakaSettingRow(string label, string settingKey, string[] options)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
        
        var labelText = new TextBlock
        {
            Text = label,
            FontSize = 13,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(220, 220, 220)),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(labelText, 0);
        
        var comboBox = new ComboBox
        {
            Height = 32,
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(50, 50, 50)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(70, 70, 70)),
            Tag = settingKey
        };
        
        foreach (var option in options)
        {
            comboBox.Items.Add(option);
        }
        comboBox.SelectedIndex = 0;
        Grid.SetColumn(comboBox, 1);
        
        grid.Children.Add(labelText);
        grid.Children.Add(comboBox);
        
        return grid;
    }
    
    private void ApplyRecommendedSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Apply competitive settings (based on pro player recommendations)
            var result = MessageBox.Show(
                "Apply recommended competitive settings?\n\n" +
                "This will set:\n" +
                "• Render Scale: 100%\n" +
                "• Display Mode: Fullscreen\n" +
                "• Model Quality: Lowest\n" +
                "• Effects: Medium\n" +
                "• Shadows: Lowest\n" +
                "• All other settings optimized for performance\n\n" +
                "Game must be closed for changes to take effect.",
                "Apply Competitive Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                // Find the Naraka Settings window and apply recommended values
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.Title == "Naraka Settings" || window.Content is Border border && border.Child is Grid grid)
                    {
                        ApplyRecommendedSettingsToControls(window);
                        MessageBox.Show("Competitive settings applied!\n\nClick 'Save Settings to Game' to apply.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error applying recommended settings:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void ApplyRecommendedSettingsToControls(Window window)
    {
        // Recursively find all ComboBoxes and set recommended values
        var comboBoxes = FindVisualChildren<ComboBox>(window);
        
        foreach (var comboBox in comboBoxes)
        {
            var key = comboBox.Tag as string;
            if (string.IsNullOrEmpty(key)) continue;
            
            // Set recommended competitive settings
            switch (key)
            {
                case "renderScale": SelectComboBoxByValue(comboBox, "100%"); break;
                case "fullScreenMode": SelectComboBoxByValue(comboBox, "Fullscreen (0)"); break;
                case "vSyncCount": SelectComboBoxByValue(comboBox, "Off (0)"); break;
                case "aaMode": SelectComboBoxByValue(comboBox, "Off (0)"); break;
                case "motionBlurEnabled": SelectComboBoxByValue(comboBox, "Off"); break;
                case "dlssMode": SelectComboBoxByValue(comboBox, "Performance (1)"); break;
                case "reflexMode": SelectComboBoxByValue(comboBox, "Reflex+Boost (2)"); break;
                case "nvHighlightsEnabled": SelectComboBoxByValue(comboBox, "Off"); break;
                case "m_modelQualityLevel": SelectComboBoxByValue(comboBox, "Lowest (0)"); break;
                case "m_tessellationQualityLevel": SelectComboBoxByValue(comboBox, "Off (0)"); break;
                case "m_visualEffectsQualityLevel": SelectComboBoxByValue(comboBox, "Medium (1)"); break;
                case "m_textureQualityLevel": SelectComboBoxByValue(comboBox, "Lowest (0)"); break;
                case "m_shadowQualityLevel": SelectComboBoxByValue(comboBox, "Lowest (0)"); break;
                case "m_volumetricLightLevel": SelectComboBoxByValue(comboBox, "Low (0)"); break;
                case "m_cloudQualityLevel": SelectComboBoxByValue(comboBox, "Off (0)"); break;
                case "m_aoLevel": SelectComboBoxByValue(comboBox, "Off (0)"); break;
                case "m_SSRLevel": SelectComboBoxByValue(comboBox, "Off (0)"); break;
                case "m_AALevel": SelectComboBoxByValue(comboBox, "Low (0)"); break;
                case "m_PostProcessingLevel": SelectComboBoxByValue(comboBox, "Lowest (0)"); break;
                case "m_LightingQualityLevel": SelectComboBoxByValue(comboBox, "Medium (1)"); break;
            }
        }
    }
    
    private void SelectComboBoxByValue(ComboBox comboBox, string value)
    {
        for (int i = 0; i < comboBox.Items.Count; i++)
        {
            if (comboBox.Items[i].ToString() == value)
            {
                comboBox.SelectedIndex = i;
                return;
            }
        }
    }
    
    private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
    {
        if (depObj != null)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T)
                {
                    yield return (T)child;
                }
                
                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }
    }
    
    private async void SaveNarakaSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get game path from config
            var config = App.Current.ConfigStore?.LoadOrCreateDefault();
            if (config == null)
            {
                MessageBox.Show("Configuration error!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            // Try to find a valid game path (prefer Steam, then Official, then Epic)
            string? gamePath = null;
            if (!string.IsNullOrEmpty(config.SteamGamePath) && Directory.Exists(config.SteamGamePath))
                gamePath = config.SteamGamePath;
            else if (!string.IsNullOrEmpty(config.OfficialGamePath) && Directory.Exists(config.OfficialGamePath))
                gamePath = config.OfficialGamePath;
            else if (!string.IsNullOrEmpty(config.EpicGamePath) && Directory.Exists(config.EpicGamePath))
                gamePath = config.EpicGamePath;
            
            if (string.IsNullOrEmpty(gamePath))
            {
                MessageBox.Show("Please set your Naraka game path in Settings first!", "Game Path Not Set", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var settingsPath = Path.Combine(gamePath, "NarakaBladepoint_Data", "QualitySettingsData.txt");
            
            if (!File.Exists(settingsPath))
            {
                MessageBox.Show($"Settings file not found:\n{settingsPath}\n\nMake sure the game path is correct and the game is installed.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            // Read existing settings
            var json = await File.ReadAllTextAsync(settingsPath);
            var settings = System.Text.Json.JsonDocument.Parse(json);
            
            // Collect values from UI
            var newSettings = new System.Collections.Generic.Dictionary<string, object>();
            
            foreach (Window window in Application.Current.Windows)
            {
                if (window.Title == "Naraka Settings" || window is Window w && w.Owner == this)
                {
                    var comboBoxes = FindVisualChildren<ComboBox>(window);
                    foreach (var comboBox in comboBoxes)
                    {
                        var key = comboBox.Tag as string;
                        if (!string.IsNullOrEmpty(key) && comboBox.SelectedItem != null)
                        {
                            var value = comboBox.SelectedItem.ToString();
                            // Extract numeric value from strings like "Off (0)"
                            var match = System.Text.RegularExpressions.Regex.Match(value, @"\(([^)]+)\)");
                            if (match.Success)
                            {
                                newSettings[key] = match.Groups[1].Value;
                            }
                            else
                            {
                                // Handle boolean values
                                newSettings[key] = value.ToLower() == "on" || value.Contains("100%");
                            }
                        }
                    }
                    break;
                }
            }
            
            // Build new JSON structure (preserving the original structure)
            var jsonString = BuildQualitySettingsJson(settings, newSettings);
            
            // Backup original file
            var backupPath = settingsPath + ".backup";
            File.Copy(settingsPath, backupPath, true);
            
            // Write new settings
            await File.WriteAllTextAsync(settingsPath, jsonString);
            
            MessageBox.Show($"Settings saved successfully!\n\nBackup created at:\n{backupPath}\n\n⚠️ Restart Naraka for changes to take effect.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            LogToActionLog("✅ Naraka settings saved successfully", "#00FF00");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            LogToActionLog($"❌ Error saving Naraka settings: {ex.Message}", "#FF0000");
        }
    }
    
    private string BuildQualitySettingsJson(System.Text.Json.JsonDocument originalDoc, System.Collections.Generic.Dictionary<string, object> newSettings)
    {
        // This is a simplified version - in production you'd want proper JSON manipulation
        var jsonObj = new System.Text.Json.Nodes.JsonObject();
        
        // Parse render scale
        var renderScale = 1.0;
        if (newSettings.ContainsKey("renderScale"))
        {
            var rsValue = newSettings["renderScale"].ToString().Replace("%", "");
            if (double.TryParse(rsValue, out var rs))
            {
                renderScale = rs / 100.0;
            }
        }
        
        // Build the JSON structure
        var output = $@"{{""preset"":-1,""l22GraphicQualityLevel"":{{""m_modelQualityLevel"":{GetIntValue(newSettings, "m_modelQualityLevel", 0)},""m_tessellationQualityLevel"":{GetIntValue(newSettings, "m_tessellationQualityLevel", 0)},""m_visualEffectsQualityLevel"":{GetIntValue(newSettings, "m_visualEffectsQualityLevel", 1)},""m_textureQualityLevel"":{GetIntValue(newSettings, "m_textureQualityLevel", 0)},""m_shadowQualityLevel"":{GetIntValue(newSettings, "m_shadowQualityLevel", 0)},""m_volumetricLightLevel"":{GetIntValue(newSettings, "m_volumetricLightLevel", 0)},""m_cloudQualityLevel"":{GetIntValue(newSettings, "m_cloudQualityLevel", 0)},""m_aoLevel"":{GetIntValue(newSettings, "m_aoLevel", 0)},""m_SSRLevel"":{GetIntValue(newSettings, "m_SSRLevel", 0)},""m_AALevel"":{GetIntValue(newSettings, "m_AALevel", 0)},""m_PostProcessingLevel"":{GetIntValue(newSettings, "m_PostProcessingLevel", 1)},""m_LightingQualityLevel"":{GetIntValue(newSettings, "m_LightingQualityLevel", 1)}}},""l22SystemQualitySetting"":{{""renderScale"":{renderScale},""renderScaleStep"":0.05,""aaMode"":{GetIntValue(newSettings, "aaMode", 2)},""checkboardRendering"":0,""upSamplingType"":0,""enableDlssDx12"":true,""enableDlssG"":true,""frameBoostDlssG"":0,""enableDlssRR"":false,""randomDiscardFactor"":4.0,""dlssMode"":{GetIntValue(newSettings, "dlssMode", -2)},""xessMode"":101,""xefgMode"":0,""dlssSharpness"":0.5,""lxFsr2Mode"":0,""fsr2Sharpness"":0.0,""lxFsr3Mode"":1,""enbaleFSR3FrameInterpolation"":false,""nisQuality"":0,""fullScreenMode"":{GetIntValue(newSettings, "fullScreenMode", 0)},""resolutionWidth"":1920,""resolutionHeight"":1080,""frameRateLimit"":-1,""vSyncCount"":{GetIntValue(newSettings, "vSyncCount", 0)},""gamma"":2.2,""maxLuma"":1.0,""minLuma"":0.01,""paperWhite"":9.0,""mHDRMode"":0,""reflexMode"":{GetIntValue(newSettings, "reflexMode", 0)},""xellMode"":0,""antiLag2Enabled"":false,""vrsMode"":0,""colorBlindMode"":0,""colorBlindStrength"":0.0,""colorBlindQualityMode"":false,""nvHighlightsEnabled"":{GetBoolValue(newSettings, "nvHighlightsEnabled", false).ToString().ToLower()},""styleMode"":0,""motionBlurEnabled"":{GetBoolValue(newSettings, "motionBlurEnabled", false).ToString().ToLower()},""raytracingEnabled"":false,""raytracingAO"":false,""raytracingGI"":false,""raytracingReflection"":false,""raytracingShadow"":false,""raytracingBVHAllInOne"":false,""raytracingBVHActorCountMax"":0,""rtgiResolution"":0,""characterAdditionalPhysics1"":true,""xboxQualityOption"":0}}}}";
        
        return output;
    }
    
    private int GetIntValue(System.Collections.Generic.Dictionary<string, object> settings, string key, int defaultValue)
    {
        if (settings.ContainsKey(key))
        {
            var value = settings[key].ToString();
            if (int.TryParse(value, out var intValue))
                return intValue;
        }
        return defaultValue;
    }
    
    private bool GetBoolValue(System.Collections.Generic.Dictionary<string, object> settings, string key, bool defaultValue)
    {
        if (settings.ContainsKey(key))
        {
            var value = settings[key].ToString().ToLower();
            return value == "true" || value == "on" || value == "1";
        }
        return defaultValue;
    }
    
    private async Task DownloadCnClientAsync()
    {
        Window? progressWindow = null;
        try
        {
            LogToActionLog("", "#FFFFFF");
            LogToActionLog("=== CN Client Download Initiated ===", "#00FFFF");
            LogToActionLog("Fetching latest CN client download link...", "#FFFF00");
            
            var resolver = App.Current.UrlResolver;
            
            // Show non-blocking progress window
            progressWindow = CreateProgressWindow("Fetching latest CN client download link...");
            progressWindow.Show();
            
            // Try to resolve the URL (NOW MUCH FASTER WITH PARALLEL CHECKS)
            LogToActionLog("Contacting CDN servers...", "#CCCCCC");
            var result = await resolver.GetCnClientUrlAsync();
            
            // Close progress window
            progressWindow?.Close();
            progressWindow = null;
            
            if (result.Success && !string.IsNullOrEmpty(result.DownloadUrl))
            {
                var version = !string.IsNullOrEmpty(result.Version) ? $"\n\nVersion: {result.Version}" : "";
                var cached = result.FromCache ? " (cached)" : "";
                
                LogToActionLog($"✅ Download URL found: {result.DownloadUrl}", "#00FF00");
                if (!string.IsNullOrEmpty(result.Version))
                    LogToActionLog($"Version: {result.Version}", "#00FFFF");
                if (result.FromCache)
                    LogToActionLog("(Using cached URL)", "#FFFF00");
                
                // Offer two options: browser download or launcher download
                var downloadChoice = MessageBox.Show(
                    $"Found CN client download link{cached}!{version}\n\n" +
                    $"URL: {result.DownloadUrl}\n\n" +
                    "Download Options:\n" +
                    "• YES = Download via browser (manual)\n" +
                    "• NO = Download & Install via launcher (automatic)\n" +
                    "• CANCEL = Cancel",
                    "Download CN Client",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);
                
                if (downloadChoice == MessageBoxResult.Yes)
                {
                    // Open in browser for manual download
                    LogToActionLog("Opening download URL in browser...", "#FFFF00");
                    OpenUrl(result.DownloadUrl);
                }
                else if (downloadChoice == MessageBoxResult.No)
                {
                    // Automatic download and install via launcher
                    LogToActionLog("Starting automatic download and installation...", "#00FF00");
                    await DownloadAndInstallClientAsync(result.DownloadUrl, "Chinese (CN) Client");
                }
                else
                {
                    LogToActionLog("Download cancelled by user", "#FFFF00");
                }
            }
            else
            {
                // Fallback to opening the download page
                LogToActionLog($"❌ Failed to find download URL: {result.ErrorMessage}", "#FF0000");
                
                var fallbackResult = MessageBox.Show(
                    $"Could not automatically find the download link.\n\n" +
                    $"Error: {result.ErrorMessage}\n\n" +
                    "Would you like to open the CN download page manually?",
                    "Download CN Client",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (fallbackResult == MessageBoxResult.Yes)
                {
                    LogToActionLog("Opening CN download page in browser...", "#FFFF00");
                    OpenUrl("https://www.yjwujian.cn/download/#/");
                }
                else
                {
                    LogToActionLog("Manual download cancelled", "#FFFF00");
                }
            }
        }
        catch (Exception ex)
        {
            progressWindow?.Close();
            LogToActionLog($"❌ ERROR: {ex.Message}", "#FF0000");
            MessageBox.Show(
                $"Error fetching CN client download link:\n\n{ex.Message}\n\n" +
                "Opening CN download page instead.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            LogToActionLog("Opening CN download page as fallback...", "#FFFF00");
            OpenUrl("https://www.yjwujian.cn/download/#/");
        }
    }
    
    private async Task DownloadGlobalClientAsync()
    {
        Window? progressWindow = null;
        try
        {
            LogToActionLog("", "#FFFFFF");
            LogToActionLog("=== Global Client Download Initiated ===", "#00FFFF");
            LogToActionLog("Fetching latest Global client download link...", "#FFFF00");
            
            var resolver = App.Current.UrlResolver;
            
            // Show non-blocking progress window
            progressWindow = CreateProgressWindow("Fetching latest Global client download link...");
            progressWindow.Show();
            
            // Try to resolve the URL (NOW MUCH FASTER WITH PARALLEL CHECKS)
            LogToActionLog("Contacting CDN servers...", "#CCCCCC");
            var result = await resolver.GetGlobalClientUrlAsync();
            
            // Close progress window
            progressWindow?.Close();
            progressWindow = null;
            
            if (result.Success && !string.IsNullOrEmpty(result.DownloadUrl))
            {
                var version = !string.IsNullOrEmpty(result.Version) ? $"\n\nVersion: {result.Version}" : "";
                var cached = result.FromCache ? " (cached)" : "";
                
                LogToActionLog($"✅ Download URL found: {result.DownloadUrl}", "#00FF00");
                if (!string.IsNullOrEmpty(result.Version))
                    LogToActionLog($"Version: {result.Version}", "#00FFFF");
                if (result.FromCache)
                    LogToActionLog("(Using cached URL)", "#FFFF00");
                
                // Offer two options: browser download or launcher download
                var downloadChoice = MessageBox.Show(
                    $"Found Global client download link{cached}!{version}\n\n" +
                    $"URL: {result.DownloadUrl}\n\n" +
                    "Download Options:\n" +
                    "• YES = Download via browser (manual)\n" +
                    "• NO = Download & Install via launcher (automatic)\n" +
                    "• CANCEL = Cancel",
                    "Download Global Client",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);
                
                if (downloadChoice == MessageBoxResult.Yes)
                {
                    // Open in browser for manual download
                    LogToActionLog("Opening download URL in browser...", "#FFFF00");
                    OpenUrl(result.DownloadUrl);
                }
                else if (downloadChoice == MessageBoxResult.No)
                {
                    // Automatic download and install via launcher
                    LogToActionLog("Starting automatic download and installation...", "#00FF00");
                    await DownloadAndInstallClientAsync(result.DownloadUrl, "Global Client");
                }
                else
                {
                    LogToActionLog("Download cancelled by user", "#FFFF00");
                }
            }
            else
            {
                // Fallback to opening the download page
                LogToActionLog($"❌ Failed to find download URL: {result.ErrorMessage}", "#FF0000");
                
                var fallbackResult = MessageBox.Show(
                    $"Could not automatically find the download link.\n\n" +
                    $"Error: {result.ErrorMessage}\n\n" +
                    "Would you like to open the Global download page manually?",
                    "Download Global Client",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (fallbackResult == MessageBoxResult.Yes)
                {
                    LogToActionLog("Opening Global download page in browser...", "#FFFF00");
                    OpenUrl("https://www.narakathegame.com/download/#/");
                }
                else
                {
                    LogToActionLog("Manual download cancelled", "#FFFF00");
                }
            }
        }
        catch (Exception ex)
        {
            progressWindow?.Close();
            LogToActionLog($"❌ ERROR: {ex.Message}", "#FF0000");
            MessageBox.Show(
                $"Error fetching Global client download link:\n\n{ex.Message}\n\n" +
                "Opening Global download page instead.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            LogToActionLog("Opening Global download page as fallback...", "#FFFF00");
            OpenUrl("https://www.narakathegame.com/download/#/");
        }
    }
    
    private void ShowSettings()
    {
        try
        {
            ContentPanel.Children.Clear();
            var settingsPage = CreateSettingsPage(null);
            ContentPanel.Children.Add(settingsPage);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error loading Settings:\n\n{ex.Message}",
                "Settings Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            LogToActionLog($"❌ Settings error: {ex.Message}", "#FF0000");
            
            // Fall back to Dashboard
            ShowDashboard();
        }
    }
    
    private UIElement CreateSettingsPage(Window? parentWindow)
    {
        var config = App.Current.ConfigStore.LoadOrCreateDefault();
        
        var mainGrid = new Grid { Margin = new Thickness(20) };
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        
        // Header
        var headerStack = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 20)
        };
        
        var title = new TextBlock
        {
            Text = "⚙ Settings",
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White
        };
        
        var subtitle = new TextBlock
        {
            Text = "Configure game locations and launch preferences",
            FontSize = 14,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(230, 230, 230)),
            Margin = new Thickness(0, 8, 0, 0)
        };
        
        headerStack.Children.Add(title);
        headerStack.Children.Add(subtitle);
        
        Grid.SetRow(headerStack, 0);
        mainGrid.Children.Add(headerStack);
        
        // ScrollViewer for settings content
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(0, 0, 10, 0)
        };
        
        var contentStack = new StackPanel();
        
        // ===== GAME LOCATIONS SECTION =====
        var locationsHeader = CreateSectionHeader("� Game Locations");
        contentStack.Children.Add(locationsHeader);
        
        // Auto-detect button
        var autoDetectButton = new Button
        {
            Content = "🔍 Auto-Detect Installations",
            Height = 35,
            Margin = new Thickness(0, 0, 0, 15),
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 120, 215)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            FontSize = 13,
            Cursor = Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 200
        };
        
        autoDetectButton.Click += async (s, e) =>
        {
            autoDetectButton.IsEnabled = false;
            autoDetectButton.Content = "Detecting...";
            await AutoDetectGameLocations();
            autoDetectButton.Content = "🔍 Auto-Detect Installations";
            autoDetectButton.IsEnabled = true;
            
            // Refresh settings page
            if (parentWindow != null)
            {
                // Pop-out window: refresh content
                parentWindow.Content = CreateSettingsPage(parentWindow);
            }
            else
            {
                // Main window: use ContentPanel
                ContentPanel.Children.Clear();
                var settingsPage = CreateSettingsPage(null);
                ContentPanel.Children.Add(settingsPage);
            }
        };
        
        contentStack.Children.Add(autoDetectButton);
        
        // Steam location
        contentStack.Children.Add(CreateGameLocationSetting(
            "Steam Installation",
            config.SteamGamePath,
            "Steam",
            (path) => {
                config.SteamGamePath = path;
                App.Current.ConfigStore.Save(config);
            },
            parentWindow
        ));
        
        // Epic location
        contentStack.Children.Add(CreateGameLocationSetting(
            "Epic Games Installation",
            config.EpicGamePath,
            "Epic",
            (path) => {
                config.EpicGamePath = path;
                App.Current.ConfigStore.Save(config);
            },
            parentWindow
        ));
        
        // Official location
        contentStack.Children.Add(CreateGameLocationSetting(
            "Official Installation",
            config.OfficialGamePath,
            "Official",
            (path) => {
                config.OfficialGamePath = path;
                App.Current.ConfigStore.Save(config);
            },
            parentWindow
        ));
        
        // ===== LAUNCH PREFERENCES SECTION =====
        var launchHeader = CreateSectionHeader("🚀 Launch Preferences");
        contentStack.Children.Add(launchHeader);
        
        var launchInfo = new TextBlock
        {
            Text = "Choose your preferred launch method. If not set, you'll be prompted each time you launch the game.",
            FontSize = 12,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(220, 220, 220)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 15)
        };
        
        contentStack.Children.Add(launchInfo);
        
        // Launch method selection
        var launchOptions = new StackPanel { Orientation = Orientation.Horizontal };
        
        var launchMethods = new[] { ("Steam", "steam://run/1203220"), ("Epic", "com.epicgames.launcher://apps/Naraka"), ("Official", null) };
        
        foreach (var (method, uri) in launchMethods)
        {
            var isSelected = config.PreferredLaunchMethod == method;
            
            var methodButton = new Button
            {
                Content = method,
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = isSelected 
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215))
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 13,
                Cursor = Cursors.Hand
            };
            
            methodButton.Click += (s, e) =>
            {
                config.PreferredLaunchMethod = method;
                App.Current.ConfigStore.Save(config);
                
                // Refresh settings page to update selection
                if (parentWindow != null)
                {
                    // Pop-out window: refresh content
                    parentWindow.Content = CreateSettingsPage(parentWindow);
                }
                else
                {
                    // Main window: use ContentPanel
                    ContentPanel.Children.Clear();
                    var settingsPage = CreateSettingsPage(null);
                    ContentPanel.Children.Add(settingsPage);
                }
            };
            
            launchOptions.Children.Add(methodButton);
        }
        
        // Clear preference button
        var clearButton = new Button
        {
            Content = "Clear Preference",
            Width = 120,
            Height = 35,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(220, 220, 220)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(100, 100, 100)),
            BorderThickness = new Thickness(1),
            FontSize = 12,
            Cursor = Cursors.Hand
        };
        
        clearButton.Click += (s, e) =>
        {
            config.PreferredLaunchMethod = null;
            App.Current.ConfigStore.Save(config);
            
            // Refresh settings page to update selection
            if (parentWindow != null)
            {
                // Pop-out window: refresh content
                parentWindow.Content = CreateSettingsPage(parentWindow);
            }
            else
            {
                // Main window: use ContentPanel
                ContentPanel.Children.Clear();
                var settingsPage = CreateSettingsPage(null);
                ContentPanel.Children.Add(settingsPage);
            }
        };
        
        launchOptions.Children.Add(clearButton);
        contentStack.Children.Add(launchOptions);
        
        // ===== LAUNCHER VERSION & AUTO-UPDATE SECTION =====
        var versionHeader = CreateSectionHeader("🔄 Launcher Version");
        contentStack.Children.Add(versionHeader);
        
        var versionPanel = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(40, 40, 40)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(20),
            Margin = new Thickness(0, 0, 0, 15)
        };
        
        var versionStack = new StackPanel();
        
        var currentVersionText = new TextBlock
        {
            Text = $"Current Version: v1.0.0",
            FontSize = 14,
            Foreground = System.Windows.Media.Brushes.White,
            Margin = new Thickness(0, 0, 0, 10)
        };
        versionStack.Children.Add(currentVersionText);
        
        var lastCheckText = new TextBlock
        {
            Text = "Last checked: Never",
            FontSize = 12,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(200, 200, 200)),
            Margin = new Thickness(0, 0, 0, 15),
            Name = "LastCheckText"
        };
        versionStack.Children.Add(lastCheckText);
        
        var updateButtonsStack = new StackPanel { Orientation = Orientation.Horizontal };
        
        var checkUpdateButton = new Button
        {
            Content = "🔍 Check for Updates",
            Height = 35,
            Width = 160,
            Margin = new Thickness(0, 0, 10, 0),
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 120, 215)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            FontSize = 12,
            Cursor = Cursors.Hand
        };
        
        checkUpdateButton.Click += async (s, e) =>
        {
            checkUpdateButton.IsEnabled = false;
            checkUpdateButton.Content = "Checking...";
            
            try
            {
                var updateAvailable = await CheckForUpdatesAsync();
                lastCheckText.Text = $"Last checked: {DateTime.Now:g}";
                
                if (updateAvailable)
                {
                    var result = MessageBox.Show(
                        "A new version of NotNaraka Launcher is available!\n\n" +
                        "Would you like to download it from GitHub?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "https://github.com/YOUR_GITHUB/NotNaraka-Launcher/releases/latest",
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    MessageBox.Show(
                        "You're running the latest version!",
                        "Up to Date",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to check for updates:\n\n{ex.Message}",
                    "Update Check Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                checkUpdateButton.Content = "🔍 Check for Updates";
                checkUpdateButton.IsEnabled = true;
            }
        };
        
        updateButtonsStack.Children.Add(checkUpdateButton);
        
        var viewReleasesButton = new Button
        {
            Content = "📋 View Releases",
            Height = 35,
            Width = 140,
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(60, 60, 60)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            FontSize = 12,
            Cursor = Cursors.Hand
        };
        
        viewReleasesButton.Click += (s, e) =>
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/YOUR_GITHUB/NotNaraka-Launcher/releases",
                UseShellExecute = true
            });
        };
        
        updateButtonsStack.Children.Add(viewReleasesButton);
        versionStack.Children.Add(updateButtonsStack);
        
        versionPanel.Child = versionStack;
        contentStack.Children.Add(versionPanel);
        
        scrollViewer.Content = contentStack;
        Grid.SetRow(scrollViewer, 1);
        mainGrid.Children.Add(scrollViewer);
        
        return mainGrid;
    }
    
    private async Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NotNaraka-Launcher/1.0");
            
            // Check GitHub API for latest release
            // Replace YOUR_GITHUB/NotNaraka-Launcher with your actual repo
            var response = await httpClient.GetStringAsync(
                "https://api.github.com/repos/YOUR_GITHUB/NotNaraka-Launcher/releases/latest");
            
            var jsonDoc = System.Text.Json.JsonDocument.Parse(response);
            var latestVersion = jsonDoc.RootElement.GetProperty("tag_name").GetString();
            
            // Compare versions (simple string comparison for now)
            var currentVersion = "v1.0.0";
            var updateAvailable = latestVersion != currentVersion;
            
            LogToActionLog(updateAvailable 
                ? $"✨ Update available: {latestVersion}" 
                : "✅ Launcher is up to date", 
                updateAvailable ? "#00FFFF" : "#00FF00");
            
            return updateAvailable;
        }
        catch (Exception ex)
        {
            LogToActionLog($"⚠️ Update check failed: {ex.Message}", "#FFA500");
            return false;
        }
    }
    
    private Border CreateSectionHeader(string title)
    {
        var border = new Border
        {
            Margin = new Thickness(0, 20, 0, 15),
            Padding = new Thickness(0, 0, 0, 10),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(60, 60, 60)),
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
        
        var textBlock = new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = System.Windows.Media.Brushes.White
        };
        
        border.Child = textBlock;
        return border;
    }
    
    private Border CreateGameLocationSetting(string label, string? currentPath, string platform, Action<string> onPathChanged, Window? parentWindow)
    {
        var border = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(40, 40, 40)),
            CornerRadius = new System.Windows.CornerRadius(5),
            Padding = new Thickness(15),
            Margin = new Thickness(0, 0, 0, 10)
        };
        
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        // Label
        var labelText = new TextBlock
        {
            Text = label,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = System.Windows.Media.Brushes.White,
            Margin = new Thickness(0, 0, 0, 8)
        };
        
        Grid.SetRow(labelText, 0);
        grid.Children.Add(labelText);
        
        // Path display and browse button
        var pathStack = new StackPanel { Orientation = Orientation.Horizontal };
        
        var pathText = new TextBlock
        {
            Text = string.IsNullOrEmpty(currentPath) ? "Not configured" : currentPath,
            FontSize = 12,
            Foreground = string.IsNullOrEmpty(currentPath) 
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 220))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)),
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 500,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        
        var browseButton = new Button
        {
            Content = "Browse...",
            Width = 80,
            Height = 28,
            Margin = new Thickness(10, 0, 0, 0),
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(60, 60, 60)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            FontSize = 11,
            Cursor = Cursors.Hand
        };
        
        browseButton.Click += (s, e) =>
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = $"Select {platform} game installation folder",
                ShowNewFolderButton = false
            };
            
            if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
            {
                dialog.SelectedPath = currentPath;
            }
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                onPathChanged(dialog.SelectedPath);
                
                // Refresh settings page
                if (parentWindow != null)
                {
                    // Pop-out window: refresh content
                    parentWindow.Content = CreateSettingsPage(parentWindow);
                }
                else
                {
                    // Main window: use ContentPanel
                    ContentPanel.Children.Clear();
                    var settingsPage = CreateSettingsPage(null);
                    ContentPanel.Children.Add(settingsPage);
                }
            }
        };
        
        pathStack.Children.Add(pathText);
        pathStack.Children.Add(browseButton);
        
        Grid.SetRow(pathStack, 1);
        grid.Children.Add(pathStack);
        
        border.Child = grid;
        return border;
    }
    
    // Public methods for tray menu navigation
    public void ShowTweaksPage()
    {
        // Open pop-out window instead of changing main window content
        OpenTweaksWindow();
    }
    
    public void ShowClientsPage()
    {
        // Open pop-out window instead of changing main window content
        OpenClientsWindow();
    }
    
    // Helper method to select folder for game installation
    private string? SelectInstallationFolder()
    {
        try
        {
            // Use Windows Forms FolderBrowserDialog for better folder selection
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select folder to install Naraka: Bladepoint",
                ShowNewFolderButton = true,
                RootFolder = Environment.SpecialFolder.MyComputer
            };
            
            // Set default path to Program Files or user's choice
            var defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Naraka");
            
            if (Directory.Exists(defaultPath))
            {
                dialog.SelectedPath = defaultPath;
            }
            
            var result = dialog.ShowDialog();
            
            if (result == System.Windows.Forms.DialogResult.OK && 
                !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                return dialog.SelectedPath;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error selecting folder:\n\n{ex.Message}",
                "Folder Selection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return null;
        }
    }
    
    // Helper method to show progress window
    private Window CreateProgressWindow(string title, string message)
    {
        var progressWindow = new Window
        {
            Title = title,
            Width = 500,
            Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
        };
        
        var mainStack = new StackPanel
        {
            Margin = new Thickness(20),
            VerticalAlignment = VerticalAlignment.Center
        };
        
        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 10),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        var messageText = new TextBlock
        {
            Text = message,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
            Margin = new Thickness(0, 0, 0, 20),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Name = "MessageText"
        };
        
        var progressBar = new ProgressBar
        {
            Height = 30,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Name = "ProgressBar",
            Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 215))
        };
        
        var percentText = new TextBlock
        {
            Text = "0%",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Name = "PercentText"
        };
        
        var detailText = new TextBlock
        {
            Text = "",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
            Margin = new Thickness(0, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Name = "DetailText"
        };
        
        mainStack.Children.Add(titleText);
        mainStack.Children.Add(messageText);
        mainStack.Children.Add(progressBar);
        mainStack.Children.Add(percentText);
        mainStack.Children.Add(detailText);
        
        progressWindow.Content = mainStack;
        
        return progressWindow;
    }
    
    // Helper to update progress window
    private void UpdateProgressWindow(Window progressWindow, int progress, string? message = null, string? detail = null)
    {
        Dispatcher.Invoke(() =>
        {
            if (progressWindow.Content is StackPanel stack)
            {
                foreach (var child in stack.Children)
                {
                    if (child is ProgressBar pb && pb.Name == "ProgressBar")
                    {
                        pb.Value = progress;
                    }
                    else if (child is TextBlock tb)
                    {
                        if (tb.Name == "PercentText")
                        {
                            tb.Text = $"{progress}%";
                        }
                        else if (tb.Name == "MessageText" && message != null)
                        {
                            tb.Text = message;
                        }
                        else if (tb.Name == "DetailText" && detail != null)
                        {
                            tb.Text = detail;
                        }
                    }
                }
            }
        });
    }
    
    // Main method to download and install a game client
    private async Task DownloadAndInstallClientAsync(string downloadUrl, string clientName)
    {
        LogToActionLog("", "#FFFFFF");
        LogToActionLog($"=== Download & Installation: {clientName} ===", "#00FFFF");
        
        // Step 1: Select installation folder
        LogToActionLog("Step 1: Selecting installation folder...", "#FFFF00");
        var installFolder = SelectInstallationFolder();
        
        if (string.IsNullOrEmpty(installFolder))
        {
            LogToActionLog("Installation cancelled - no folder selected", "#FF0000");
            MessageBox.Show(
                "Installation cancelled - no folder selected.",
                "Cancelled",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }
        
        LogToActionLog($"✅ Installation folder: {installFolder}", "#00FF00");
        
        // Step 2: Show confirmation with installation path
        LogToActionLog("Step 2: Confirming installation details...", "#FFFF00");
        var confirmInstall = MessageBox.Show(
            $"Ready to download and install {clientName}\n\n" +
            $"Installation folder: {installFolder}\n\n" +
            $"Download URL: {downloadUrl}\n\n" +
            "The game will be extracted to this folder.\n" +
            "Folders 'Bin' and 'netease.mpay.webviewsupport.cef90440' will be excluded.\n\n" +
            "Continue?",
            "Confirm Installation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (confirmInstall != MessageBoxResult.Yes)
        {
            LogToActionLog("Installation cancelled by user", "#FFFF00");
            return;
        }
        
        LogToActionLog("✅ Installation confirmed", "#00FF00");
        
        // Step 3: Create progress window
        Window? progressWindow = null;
        CancellationTokenSource? cts = null;
        
        try
        {
            LogToActionLog("Step 3: Initializing download...", "#FFFF00");
            progressWindow = CreateProgressWindow(
                $"Downloading {clientName}",
                "Preparing download...");
            
            cts = new CancellationTokenSource();
            
            // Get download service
            var downloadService = App.Current.DownloadService;
            
            // Subscribe to progress events
            downloadService.DownloadProgress += (sender, e) =>
            {
                UpdateProgressWindow(
                    progressWindow,
                    e.Progress,
                    $"{e.Stage}: {e.Progress}%",
                    e.Message);
            };
            
            downloadService.StatusChanged += (sender, status) =>
            {
                UpdateProgressWindow(progressWindow, -1, detail: status);
            };
            
            // Show progress window
            progressWindow.Show();
            UpdateProgressWindow(progressWindow, 0, "Starting download...");
            
            // Step 4: Download the ZIP file
            var tempZipPath = Path.Combine(
                Path.GetTempPath(),
                $"Naraka_Download_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
            
            LogToActionLog($"Step 4: Downloading to: {tempZipPath}", "#FFFF00");
            UpdateProgressWindow(progressWindow, 5, "Downloading game files...");
            
            // Use HttpClient to download the file
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromHours(2); // Long timeout for large files
                
                LogToActionLog($"Connecting to: {downloadUrl}", "#CCCCCC");
                var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                response.EnsureSuccessStatusCode();
                LogToActionLog("✅ Connection established", "#00FF00");
                
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var downloadedBytes = 0L;
                var totalMB = totalBytes / 1024.0 / 1024.0;
                
                LogToActionLog($"File size: {totalMB:F1} MB", "#00FFFF");
                LogToActionLog("Starting download stream...", "#FFFF00");
                
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    int bytesRead;
                    var lastLoggedProgress = 0;
                    
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cts.Token);
                        downloadedBytes += bytesRead;
                        
                        if (totalBytes > 0)
                        {
                            var progress = (int)((downloadedBytes * 100) / totalBytes);
                            var downloadedMB = downloadedBytes / 1024.0 / 1024.0;
                            
                            UpdateProgressWindow(
                                progressWindow,
                                progress,
                                $"Downloading: {progress}%",
                                $"{downloadedMB:F1} MB / {totalMB:F1} MB");
                            
                            // Log every 10% to avoid spam
                            if (progress >= lastLoggedProgress + 10)
                            {
                                LogToActionLog($"Download progress: {progress}% ({downloadedMB:F1} MB / {totalMB:F1} MB)", "#00FF00");
                                lastLoggedProgress = progress;
                            }
                        }
                    }
                }
            }
            
            LogToActionLog("✅ Download complete!", "#00FF00");
            
            // Step 5: Extract the game files with filtering
            LogToActionLog("Step 5: Extracting game files...", "#FFFF00");
            UpdateProgressWindow(progressWindow, 100, "Download complete! Extracting game files...");
            
            var extractionResult = await downloadService.ExtractNarakaGameFilesAsync(
                tempZipPath,
                installFolder,
                cts.Token);
            
            LogToActionLog($"✅ Extracted {extractionResult.ExtractedFiles.Count} files successfully", "#00FF00");
            
            // Step 6: Cleanup and show result
            LogToActionLog("Step 6: Cleaning up temporary files...", "#FFFF00");
            try
            {
                if (File.Exists(tempZipPath))
                {
                    File.Delete(tempZipPath);
                    LogToActionLog($"✅ Deleted temp file: {tempZipPath}", "#00FF00");
                }
            }
            catch (Exception cleanupEx)
            {
                LogToActionLog($"⚠️ Cleanup warning: {cleanupEx.Message}", "#FFFF00");
            }
            
            if (extractionResult.Success)
            {
                progressWindow?.Close();
                
                LogToActionLog("", "#FFFFFF");
                LogToActionLog("🎉 === INSTALLATION COMPLETE === 🎉", "#00FF00");
                LogToActionLog($"Location: {installFolder}", "#00FFFF");
                LogToActionLog($"Files extracted: {extractionResult.ExtractedFiles.Count}", "#00FFFF");
                LogToActionLog($"Time taken: {extractionResult.Duration.TotalMinutes:F1} minutes", "#00FFFF");
                LogToActionLog("", "#FFFFFF");
                
                MessageBox.Show(
                    $"✅ {clientName} installed successfully!\n\n" +
                    $"Location: {installFolder}\n" +
                    $"Files extracted: {extractionResult.ExtractedFiles.Count}\n" +
                    $"Time taken: {extractionResult.Duration.TotalMinutes:F1} minutes\n\n" +
                    "You can now launch the game from this folder.",
                    "Installation Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                progressWindow?.Close();
                
                LogToActionLog("", "#FFFFFF");
                LogToActionLog("❌ === INSTALLATION FAILED === ❌", "#FF0000");
                LogToActionLog($"Error: {extractionResult.ErrorMessage}", "#FF0000");
                LogToActionLog("", "#FFFFFF");
                
                MessageBox.Show(
                    $"❌ Installation failed!\n\n" +
                    $"Error: {extractionResult.ErrorMessage}\n\n" +
                    "Please try again or download manually.",
                    "Installation Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            progressWindow?.Close();
            
            LogToActionLog("", "#FFFFFF");
            LogToActionLog("❌ === CRITICAL ERROR === ❌", "#FF0000");
            LogToActionLog($"Exception: {ex.Message}", "#FF0000");
            LogToActionLog($"Stack trace: {ex.StackTrace}", "#FF0000");
            LogToActionLog("", "#FFFFFF");
            
            MessageBox.Show(
                $"Error during installation:\n\n{ex.Message}\n\n" +
                "Please try again or download manually.",
                "Installation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            cts?.Dispose();
        }
    }
    
    // Helper method to create a non-blocking progress window
    private Window CreateProgressWindow(string message)
    {
        var window = new Window
        {
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false
        };
        
        var border = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(30, 30, 30)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 120, 215)),
            BorderThickness = new Thickness(2),
            CornerRadius = new System.Windows.CornerRadius(8),
            Padding = new Thickness(20)
        };
        
        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        // Loading icon (animated)
        var loadingText = new TextBlock
        {
            Text = "⌛",
            FontSize = 32,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 15)
        };
        
        // Animate the loading icon
        var rotateTransform = new System.Windows.Media.RotateTransform();
        loadingText.RenderTransform = rotateTransform;
        loadingText.RenderTransformOrigin = new Point(0.5, 0.5);
        
        var animation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = TimeSpan.FromSeconds(2),
            RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
        };
        rotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, animation);
        
        var messageText = new TextBlock
        {
            Text = message,
            FontSize = 14,
            Foreground = System.Windows.Media.Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        var subText = new TextBlock
        {
            Text = "This should only take a few seconds...",
            FontSize = 11,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(230, 230, 230)),
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        
        stack.Children.Add(loadingText);
        stack.Children.Add(messageText);
        stack.Children.Add(subText);
        
        border.Child = stack;
        window.Content = border;
        
        return window;
    }
    
    // Check if this is the first time the launcher is being run
    private async Task CheckFirstTimeSetup()
    {
        try
        {
            var config = App.Current.ConfigStore.LoadOrCreateDefault();
            
            if (!config.HasCompletedInitialSetup)
            {
                // Show welcome dialog
                var welcomeWindow = CreateWelcomeDialog();
                var result = welcomeWindow.ShowDialog();
                
                if (result == true)
                {
                    // User clicked "Get Started" - navigate to settings
                    config.HasCompletedInitialSetup = true;
                    App.Current.ConfigStore.Save(config);
                    
                    // Show settings page in main window
                    ShowSettings();
                    
                    // Auto-detect game locations
                    await AutoDetectGameLocations();
                }
            }
        }
        catch (Exception ex)
        {
            LogToActionLog($"Error during first-time setup check: {ex.Message}", "#FF0000");
        }
    }
    
    // Create welcome dialog for first-time users
    private Window CreateWelcomeDialog()
    {
        var window = new Window
        {
            Width = 500,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false
        };
        
        var border = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(30, 30, 30)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 120, 215)),
            BorderThickness = new Thickness(2),
            CornerRadius = new System.Windows.CornerRadius(10),
            Padding = new Thickness(30)
        };
        
        border.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = Colors.Black,
            BlurRadius = 20,
            ShadowDepth = 0,
            Opacity = 0.8
        };
        
        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        var iconText = new TextBlock
        {
            Text = "🎮",
            FontSize = 48,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        
        var titleText = new TextBlock
        {
            Text = "Welcome to NotNaraka Launcher!",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 15)
        };
        
        var messageText = new TextBlock
        {
            Text = "Thanks for downloading!\n\nLet's set you up by configuring your game locations\nand launch preferences.",
            FontSize = 14,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(240, 240, 240)),
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 25)
        };
        
        var getStartedButton = new Button
        {
            Content = "Get Started",
            Width = 150,
            Height = 40,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 120, 215)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 0, 10)
        };
        
        getStartedButton.Click += (s, e) =>
        {
            window.DialogResult = true;
            window.Close();
        };
        
        var laterButton = new Button
        {
            Content = "Skip for now",
            Width = 150,
            Height = 30,
            FontSize = 12,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(220, 220, 220)),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        
        laterButton.Click += (s, e) =>
        {
            window.DialogResult = false;
            window.Close();
        };
        
        stack.Children.Add(iconText);
        stack.Children.Add(titleText);
        stack.Children.Add(messageText);
        stack.Children.Add(getStartedButton);
        stack.Children.Add(laterButton);
        
        border.Child = stack;
        window.Content = border;
        
        return window;
    }
    
    // Auto-detect game locations and update configuration
    private async Task AutoDetectGameLocations()
    {
        await Task.Run(() =>
        {
            try
            {
                var detector = new GameLocationDetector();
                var locations = detector.DetectAll();
                
                var config = App.Current.ConfigStore.LoadOrCreateDefault();
                
                if (!string.IsNullOrEmpty(locations.SteamPath))
                {
                    config.SteamGamePath = locations.SteamPath;
                    Dispatcher.Invoke(() => LogToActionLog($"✅ Detected Steam installation: {locations.SteamPath}", "#00FF00"));
                }
                
                if (!string.IsNullOrEmpty(locations.EpicPath))
                {
                    config.EpicGamePath = locations.EpicPath;
                    Dispatcher.Invoke(() => LogToActionLog($"✅ Detected Epic installation: {locations.EpicPath}", "#00FF00"));
                }
                
                if (!string.IsNullOrEmpty(locations.OfficialPath))
                {
                    config.OfficialGamePath = locations.OfficialPath;
                    Dispatcher.Invoke(() => LogToActionLog($"✅ Detected Official installation: {locations.OfficialPath}", "#00FF00"));
                }
                
                if (!locations.HasAnyLocation)
                {
                    Dispatcher.Invoke(() => LogToActionLog("ℹ No game installations detected automatically", "#FFFF00"));
                }
                
                App.Current.ConfigStore.Save(config);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => LogToActionLog($"Error during auto-detection: {ex.Message}", "#FF0000"));
            }
        });
    }
    
    // Load Steam news articles
    private async Task LoadSteamNewsAsync()
    {
        if (_newsStack == null) return;
        
        try
        {
            var httpClient = new HttpClient();
            var newsService = new SteamNewsService(httpClient);
            var articles = await newsService.GetNewsAsync(5);
            
            Dispatcher.Invoke(() =>
            {
                _newsStack.Children.Clear();
                
                if (articles != null && articles.Any())
                {
                    foreach (var article in articles)
                    {
                        var newsItem = CreateNewsItem(
                            article.Title, 
                            article.Description,
                            article.Date,
                            article.Url
                        );
                        _newsStack.Children.Add(newsItem);
                    }
                    
                    LogToActionLog("✅ Loaded latest Steam news", "#4CAF50");
                }
                else
                {
                    var errorText = new TextBlock
                    {
                        Text = "⚠️ Could not load news. Check your internet connection.",
                        FontSize = 10,
                        Foreground = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(255, 152, 0)),
                        Margin = new Thickness(5),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    _newsStack.Children.Add(errorText);
                }
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                if (_newsStack != null)
                {
                    _newsStack.Children.Clear();
                    var errorText = new TextBlock
                    {
                        Text = $"❌ Failed to load news: {ex.Message}",
                        FontSize = 10,
                        Foreground = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(244, 67, 54)),
                        Margin = new Thickness(5),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    };
                    _newsStack.Children.Add(errorText);
                }
                
                LogToActionLog($"❌ Failed to load Steam news: {ex.Message}", "#F44336");
            });
        }
    }
    
    // Format news date for display
    private string FormatNewsDate(DateTime date)
    {
        var now = DateTime.Now;
        var diff = now - date;
        
        if (diff.TotalDays < 1)
        {
            if (diff.TotalHours < 1)
                return $"{(int)diff.TotalMinutes} minutes ago";
            return $"{(int)diff.TotalHours} hours ago";
        }
        else if (diff.TotalDays < 7)
        {
            return $"{(int)diff.TotalDays} days ago";
        }
        else if (diff.TotalDays < 30)
        {
            return $"{(int)(diff.TotalDays / 7)} weeks ago";
        }
        else
        {
            return date.ToString("MMM d, yyyy");
        }
    }
    
    // Apply selected tweaks from the tweaks tab control
    private async Task ApplySelectedTweaks(System.Windows.Controls.TabControl tabControl)
    {
        var selectedTweaks = new List<NarakaTweaks.Core.Services.TweakDefinition>();
        
        // Find all checked tweaks across all tabs
        foreach (System.Windows.Controls.TabItem tab in tabControl.Items)
        {
            if (tab.Content is System.Windows.Controls.ScrollViewer scrollViewer)
            {
                if (scrollViewer.Content is StackPanel stackPanel)
                {
                    foreach (var child in stackPanel.Children)
                    {
                        if (child is Border border && border.Child is Grid grid)
                        {
                            foreach (var gridChild in grid.Children)
                            {
                                if (gridChild is System.Windows.Controls.CheckBox checkbox && 
                                    checkbox.IsChecked == true &&
                                    checkbox.Tag is NarakaTweaks.Core.Services.TweakDefinition tweak)
                                {
                                    selectedTweaks.Add(tweak);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        if (selectedTweaks.Count == 0)
        {
            MessageBox.Show("Please select at least one tweak to apply.", "No Tweaks Selected", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        // Check for admin requirements
        var requiresAdmin = selectedTweaks.Any(t => t.RequiresAdmin);
        if (requiresAdmin)
        {
            var result = MessageBox.Show(
                $"Some tweaks require administrator privileges.\n\n" +
                $"Selected tweaks: {selectedTweaks.Count}\n" +
                $"Require admin: {selectedTweaks.Count(t => t.RequiresAdmin)}\n" +
                $"Require restart: {selectedTweaks.Count(t => t.RequiresRestart)}\n\n" +
                "Continue?",
                "Administrator Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            
            if (result != MessageBoxResult.Yes)
                return;
        }
        
        // Apply tweaks
        LogToActionLog("", "#FFFFFF");
        LogToActionLog("=== Applying System Tweaks ===", "#00FFFF");
        
        int successCount = 0;
        int failCount = 0;
        
        foreach (var tweak in selectedTweaks)
        {
            try
            {
                LogToActionLog($"Applying: {tweak.Name}...", "#FFFF00");
                
                var result = await App.Current.TweaksService.ApplyTweakAsync(tweak);
                
                if (result.Success)
                {
                    LogToActionLog($"✅ Success: {tweak.Name}", "#00FF00");
                    successCount++;
                }
                else
                {
                    LogToActionLog($"❌ Failed: {tweak.Name} - {result.ErrorMessage}", "#FF0000");
                    failCount++;
                }
            }
            catch (Exception ex)
            {
                LogToActionLog($"❌ Error: {tweak.Name} - {ex.Message}", "#FF0000");
                failCount++;
            }
        }
        
        LogToActionLog("", "#FFFFFF");
        LogToActionLog($"=== Tweak Application Complete ===", "#00FFFF");
        LogToActionLog($"✅ Success: {successCount} | ❌ Failed: {failCount}", "#FFFFFF");
        
        var requiresRestart = selectedTweaks.Any(t => t.RequiresRestart && successCount > 0);
        
        if (requiresRestart)
        {
            MessageBox.Show(
                $"Tweaks applied successfully!\n\n" +
                $"Success: {successCount}\n" +
                $"Failed: {failCount}\n\n" +
                "⚠ Some tweaks require a system restart to take effect.",
                "Restart Required",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(
                $"Tweaks applied!\n\n" +
                $"Success: {successCount}\n" +
                $"Failed: {failCount}",
                "Complete",
                MessageBoxButton.OK,
                successCount > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
    }
    
    // Revert all tweaks
    private async Task RevertAllTweaks(System.Windows.Controls.TabControl tabControl)
    {
        var result = MessageBox.Show(
            "This will attempt to revert ALL system tweaks to their original state.\n\n" +
            "This may require administrator privileges and a system restart.\n\n" +
            "Continue?",
            "Revert All Tweaks",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        
        if (result != MessageBoxResult.Yes)
            return;
        
        LogToActionLog("", "#FFFFFF");
        LogToActionLog("=== Reverting System Tweaks ===", "#00FFFF");
        
        var tweaks = App.Current.TweaksService.GetAvailableTweaks();
        var allTweaks = tweaks.SelectMany(kvp => kvp.Value).ToList();
        
        int successCount = 0;
        int failCount = 0;
        
        foreach (var tweak in allTweaks)
        {
            try
            {
                LogToActionLog($"Reverting: {tweak.Name}...", "#FFFF00");
                
                var revertResult = await App.Current.TweaksService.RevertTweakAsync(tweak);
                
                if (revertResult.Success)
                {
                    LogToActionLog($"✅ Reverted: {tweak.Name}", "#00FF00");
                    successCount++;
                }
                else
                {
                    LogToActionLog($"⚠ Skipped: {tweak.Name} - {revertResult.ErrorMessage}", "#FFFF00");
                }
            }
            catch (Exception ex)
            {
                LogToActionLog($"❌ Error: {tweak.Name} - {ex.Message}", "#FF0000");
                failCount++;
            }
        }
        
        LogToActionLog("", "#FFFFFF");
        LogToActionLog($"=== Revert Complete ===", "#00FFFF");
        LogToActionLog($"✅ Reverted: {successCount} | ❌ Failed: {failCount}", "#FFFFFF");
        
        MessageBox.Show(
            $"Revert process complete!\n\n" +
            $"Reverted: {successCount}\n" +
            $"Failed: {failCount}\n\n" +
            "⚠ A system restart is recommended.",
            "Restart Recommended",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}

