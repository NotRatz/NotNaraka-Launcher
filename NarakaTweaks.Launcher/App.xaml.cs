using System;
using System.Threading.Tasks;
using System.Windows;
using H.NotifyIcon;
using NarakaTweaks.AntiCheat;
using NarakaTweaks.Core.Services;
using Launcher.Shared.Configuration;
using Launcher.Shared.Storage;

namespace NarakaTweaks.Launcher;

public partial class App : Application
{
    // Core services
    private AntiCheatService? _antiCheatService;
    private TweaksService? _tweaksService;
    private ClientDownloadService? _downloadService;
    private GameClientSwitcher? _clientSwitcher;
    private AutoUpdateService? _updateService;
    private DownloadUrlResolver? _urlResolver;
    private LauncherConfigurationStore? _configStore;
    
    // System tray
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    
    // Singleton instance for accessing services from ViewModels
    public static new App Current => (App)Application.Current;
    
    public AntiCheatService AntiCheatService => _antiCheatService!;
    public TweaksService TweaksService => _tweaksService!;
    public ClientDownloadService DownloadService => _downloadService!;
    public GameClientSwitcher ClientSwitcher => _clientSwitcher!;
    public AutoUpdateService UpdateService => _updateService!;
    public DownloadUrlResolver UrlResolver => _urlResolver!;
    public LauncherConfigurationStore ConfigStore => _configStore!;
    public new MainWindow? MainWindow => _mainWindow;
    
    public App()
    {
        this.InitializeComponent();
        
        // Initialize services
        InitializeServices();
        
        // Initialize system tray
        InitializeSystemTray();
    }
    
    private void InitializeServices()
    {
        try
        {
            // Initialize configuration store
            var paths = new LauncherPaths();
            var fileSystem = new FileSystem();
            
            // Ensure required directories exist
            fileSystem.EnsureDirectory(paths.ConfigurationRoot);
            fileSystem.EnsureDirectory(paths.CacheRoot);
            
            _configStore = new LauncherConfigurationStore(paths, fileSystem);
            
            // Initialize anti-cheat service
            _antiCheatService = new AntiCheatService(new AntiCheatConfiguration
            {
                ScanIntervalMinutes = 30 // Background scan every 30 minutes
            });
            
            // Subscribe to anti-cheat events
            _antiCheatService.StatusChanged += OnAntiCheatStatusChanged;
            _antiCheatService.ThreatDetected += OnThreatDetected;
            
            // Initialize tweaks service
            _tweaksService = new TweaksService();
            _tweaksService.StatusChanged += OnTweaksStatusChanged;
            
            // Initialize download service
            _downloadService = new ClientDownloadService();
            _downloadService.StatusChanged += OnDownloadStatusChanged;
            
            // Initialize client switcher
            _clientSwitcher = new GameClientSwitcher();
            _clientSwitcher.StatusChanged += OnSwitcherStatusChanged;
            
            // Initialize download URL resolver
            _urlResolver = new DownloadUrlResolver();
            _urlResolver.StatusChanged += OnUrlResolverStatusChanged;
            
            // Initialize auto-update service
            _updateService = new AutoUpdateService();
            // TODO: Configure with your GitHub repo
            // _updateService.Configure("YourUsername", "NarakaTweaks");
            _updateService.StatusChanged += OnUpdateStatusChanged;
            _updateService.UpdateAvailable += OnUpdateAvailable;
            
            // Start background anti-cheat monitoring
            _antiCheatService.Start();
            
            LogMessage("All services initialized successfully");
        }
        catch (Exception ex)
        {
            LogMessage($"Service initialization error: {ex.Message}");
        }
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        try
        {
            // Create and show main window explicitly
            _mainWindow = new MainWindow();
            _mainWindow.StateChanged += OnMainWindowStateChanged;
            _mainWindow.Closing += OnMainWindowClosing;
            _mainWindow.Show();
            LogMessage("MainWindow created and shown successfully");
            
            // Check for updates in background (don't await to not block startup)
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000); // Wait 2 seconds after startup
                await _updateService!.CheckForUpdatesAsync();
            });
        }
        catch (Exception ex)
        {
            // Log to file for debugging
            System.IO.File.WriteAllText("C:\\Users\\Admin\\Desktop\\launcher_error.txt", 
                $"Startup Error:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}\n\nInner Exception:\n{ex.InnerException}");
            Environment.Exit(1);
        }
    }
    
    private void InitializeSystemTray()
    {
        try
        {
            _trayIcon = new TaskbarIcon
            {
                ToolTipText = "NarakaTweaks Launcher",
                ContextMenu = new System.Windows.Controls.ContextMenu
                {
                    Items =
                    {
                        new System.Windows.Controls.MenuItem { Header = "Show Launcher", Tag = "show" },
                        new System.Windows.Controls.Separator(),
                        new System.Windows.Controls.MenuItem { Header = "Quick Actions", IsEnabled = false },
                        new System.Windows.Controls.MenuItem { Header = "  Apply Tweaks", Tag = "tweaks" },
                        new System.Windows.Controls.MenuItem { Header = "  Manage Clients", Tag = "clients" },
                        new System.Windows.Controls.Separator(),
                        new System.Windows.Controls.MenuItem { Header = "Exit", Tag = "exit" }
                    }
                }
            };
            
            // Set icon from embedded resource
            var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/icon.ico"))?.Stream;
            if (iconStream != null)
            {
                _trayIcon.Icon = new System.Drawing.Icon(iconStream);
            }
            
            // Hook up menu item clicks
            foreach (var item in _trayIcon.ContextMenu.Items)
            {
                if (item is System.Windows.Controls.MenuItem menuItem && menuItem.Tag != null)
                {
                    menuItem.Click += OnTrayMenuItemClick;
                }
            }
            
            // Double-click to show window
            _trayIcon.TrayMouseDoubleClick += (s, e) => ShowMainWindow();
            
            LogMessage("System tray initialized successfully");
        }
        catch (Exception ex)
        {
            LogMessage($"System tray initialization error: {ex.Message}");
        }
    }
    
    private void OnTrayMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem menuItem)
        {
            switch (menuItem.Tag?.ToString())
            {
                case "show":
                    ShowMainWindow();
                    break;
                case "tweaks":
                    ShowMainWindow();
                    _mainWindow?.ShowTweaksPage();
                    break;
                case "clients":
                    ShowMainWindow();
                    _mainWindow?.ShowClientsPage();
                    break;
                case "exit":
                    Shutdown();
                    break;
            }
        }
    }
    
    private void OnMainWindowStateChanged(object? sender, EventArgs e)
    {
        if (_mainWindow?.WindowState == WindowState.Minimized)
        {
            _mainWindow.Hide();
            _trayIcon?.ShowNotification("NarakaTweaks", "Minimized to system tray");
        }
    }
    
    private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Minimize to tray instead of closing
        e.Cancel = true;
        _mainWindow?.Hide();
        _trayIcon?.ShowNotification("NarakaTweaks", "Running in background. Right-click tray icon to exit.");
    }
    
    public void ShowMainWindow()
    {
        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        // Anti-cheat service will be disposed automatically
        base.OnExit(e);
    }
    
    private void OnAntiCheatStatusChanged(object? sender, string status)
    {
        LogMessage($"[Anti-Cheat] {status}");
        // TODO: Update UI status indicator
    }
    
    private void OnThreatDetected(object? sender, DetectionEventArgs e)
    {
        LogMessage($"[THREAT] {e.Detection.Severity}: {e.Detection.Description}");
        // TODO: Show notification to user
        // TODO: Log to Discord webhook if configured
    }
    
    private void OnTweaksStatusChanged(object? sender, string status)
    {
        LogMessage($"[Tweaks] {status}");
    }
    
    private void OnDownloadStatusChanged(object? sender, string status)
    {
        LogMessage($"[Download] {status}");
    }
    
    private void OnUrlResolverStatusChanged(object? sender, string status)
    {
        LogMessage($"[URL Resolver] {status}");
    }
    
    private void OnSwitcherStatusChanged(object? sender, string status)
    {
        LogMessage($"[Switcher] {status}");
    }
    
    private void OnUpdateStatusChanged(object? sender, string status)
    {
        LogMessage($"[Update] {status}");
    }
    
    private async void OnUpdateAvailable(object? sender, UpdateAvailableEventArgs e)
    {
        LogMessage($"[Update] New version available: v{e.LatestVersion}");
        
        // Show update notification in system tray
        _trayIcon?.ShowNotification(
            "Update Available", 
            $"Version {e.LatestVersion} is available. Right-click tray icon to update.");
        
        // Add update menu item dynamically
        if (_trayIcon?.ContextMenu != null)
        {
            var updateMenuItem = new System.Windows.Controls.MenuItem 
            { 
                Header = $"📥 Update to v{e.LatestVersion}", 
                Tag = "update",
                FontWeight = FontWeights.Bold
            };
            updateMenuItem.Click += async (s, args) => await InstallUpdateAsync(e.DownloadUrl);
            
            _trayIcon.ContextMenu.Items.Insert(0, updateMenuItem);
            _trayIcon.ContextMenu.Items.Insert(1, new System.Windows.Controls.Separator());
        }
    }
    
    private async Task InstallUpdateAsync(string downloadUrl)
    {
        try
        {
            _trayIcon?.ShowNotification("Downloading Update", "Please wait...");
            
            var success = await _updateService!.DownloadAndInstallUpdateAsync(downloadUrl);
            
            if (success)
            {
                // The app will restart automatically
                Shutdown();
            }
            else
            {
                _trayIcon?.ShowNotification("Update Failed", "Could not install update. Please try again later.");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Update installation error: {ex.Message}");
            _trayIcon?.ShowNotification("Update Error", ex.Message);
        }
    }
    
    private void LogMessage(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        // TODO: Write to log file
    }
}
