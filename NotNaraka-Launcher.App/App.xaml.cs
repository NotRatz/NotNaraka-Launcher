using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Windows;
using NotNarakaLauncher.App.ViewModels;
using NotNarakaLauncher.App.Views.Pages;
using NotNarakaLauncher.Overlay.ViewModels;
using NotNarakaLauncher.Overlay.Views.Pages;
using NotNarakaLauncher.App.Views; // SplashWindow
using NotNarakaLauncher.App.Views.Windows;
using NotNarakaLauncher.App.Services;
using NotNarakaLauncher.Core.Services;
using NotNarakaLauncher.Core;
using NotNarakaLauncher.Core.Overlay;
using NotNarakaLauncher.Core.Services; // Ensure this is here
using System;
using System.IO;
using System.Windows.Threading;
using NotNarakaLauncher.Services;
using H.NotifyIcon;
using NotNarakaLauncher.App.Helpers;
using System.Drawing; // For Icon
using System.Windows.Media.Imaging; // For BitmapImage
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace NotNarakaLauncher.App
{
    public partial class App : Application
    {
        private static readonly string LogFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NotNaraka", "Logs");
        
        private static readonly string CrashLogPath = Path.Combine(LogFolder, "crash.txt");
        private static readonly string AppLogPath = Path.Combine(LogFolder, "launcher_log.txt");

        private static IHost? _host;
        private TaskbarIcon? _trayIcon; 
        private static int _firstChanceSampleCount;
        private bool _isTransitioning = false;

        public static IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("Host not initialized");

        public App()
        {
            try
            {
                System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.Batch;
            }
            catch { }
            
            Directory.CreateDirectory(LogFolder);
            
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            
            // In-process first-chance tracing (filtered + sampled)
            try
            {
                AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
                AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
            }
            catch { }

            Log("App constructor called");
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // [ADMIN CHECK] Enforce Administrator Privileges
                if (!IsRunningAsAdmin())
                {
                    // 1. Try UAC Bypass (Scheduled Task)
                    if (UacBypassService.IsTaskInstalledFromRegistry())
                    {
                         Log("Launching via UAC Bypass (Scheduled Task)...");
                         // We can't use DI here easily as host isn't built, but we can verify via registry and launch manually
                         // Or create a temp service instance. Since UacBypassService works standalone...
                         var uac = new UacBypassService(null);
                         if (uac.LaunchViaScheduledTask())
                         {
                             Shutdown();
                             return;
                         }
                    }

                    // 2. Fallback to Prompt (Standard RunAs)
                    var processInfo = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName)
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        Arguments = string.Join(" ", e.Args)
                    };

                    try
                    {
                        Process.Start(processInfo);
                    }
                    catch (Exception)
                    {
                        // User declined UAC prompting or failed
                        MessageBox.Show("This application requires Administrator privileges to function correctly. Please grant access.", "Admin Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    Shutdown();
                    return;
                }

                Log("OnStartup begin");
                InitializeSingleInstance();
                
                _host = Microsoft.Extensions.Hosting.Host
                    .CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        Log("Configuring services...");
                        
                        services.AddSingleton<ISettingsService, SettingsService>();
                        services.AddSingleton<ILocalizationService, LocalizationService>();
                        services.AddSingleton<INavigationService, NavigationService>();
                        
                        services.AddLazySingleton<ITelemetryService, TelemetryService>();
                        services.AddLazySingleton<IActionLogService, ActionLogService>();
                        services.AddLazySingleton<ICarouselService, CarouselService>();
                        services.AddLazySingleton<ITweakService, TweakService>();
                        services.AddLazySingleton<IUpdateService, UpdateService>();
                        services.AddLazySingleton<INotificationService, NotificationService>();
                        
                        services.AddLazySingleton<ITwitchAuthService, TwitchAuthService>();
                        services.AddLazySingleton<ITwitchDropsService, TwitchDropsService>();
                        services.AddLazySingleton<ITwitchPubSubService, TwitchPubSubService>();
                        services.AddLazySingleton<ITwitchEventSubService, TwitchEventSubService>();
                        services.AddLazySingleton<ITwitchWebScraper, TwitchWebScraper>();
                        
                        services.AddTransient<IInstalledClientsService, InstalledClientsService>();
                        services.AddTransient<IClientSwapService, ClientSwapService>();
                        services.AddTransient<IInstallerService, InstallerService>();
                        services.AddTransient<IInstallerService, InstallerService>();
                        services.AddSingleton<IPlatformService, PlatformService>();
                        services.AddTransient<IUacBypassService, UacBypassService>(); // [NEW] UAC Bypass
                        
                        services.AddLazySingleton<IDiscordRpcService, DiscordRpcService>();
                        services.AddLazySingleton<IDiscordGameSdkService, DiscordGameSdkService>(); // [NEW] For Voice Events
                        services.AddLazySingleton<IWebhookService, WebhookService>();
                        services.AddSingleton<IInstallationTracker, InstallationTracker>(); 
                        
                        services.AddLazySingleton<ISystemMonitorService, SystemMonitorService>();
                        services.AddTransient<IProcessTreeService, ProcessTreeService>();
                        services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
                        services.AddTransient<IGpuOverclockService, GpuOverclockService>();
                        services.AddTransient<IGameDetectionService, GameDetectionService>();
                        services.AddHttpClient(); // Required for IHttpClientFactory
                        services.AddSingleton<IThemeService, ThemeService>();
                        
                        services.AddLazySingleton<IPerformanceScoreService, PerformanceScoreService>();
                        services.AddLazySingleton<IGamingModeService, GamingModeService>();
                        services.AddSingleton<ITourService, TourService>();
                        services.AddLazySingleton<IPremiumService, PremiumService>();
                        services.AddSingleton<IBiosService, BiosService>();
                        services.AddSingleton<ILauncherStatsCardService, LauncherStatsCardService>();

                        services.AddSingleton<MainWindow>();
                        services.AddSingleton<MainWindowViewModel>();

                        services.AddTransient<DashboardPage>();
                        services.AddTransient<DashboardViewModel>();
                        services.AddTransient<SettingsPage>();
                        services.AddTransient<TweaksPage>();
                        services.AddTransient<TwitchDropsPage>();
                        services.AddTransient<MatchHistoryPage>();
                        services.AddTransient<MemoryTimerPage>();
                        services.AddTransient<OnboardingPage>();
                        services.AddTransient<QAPage>();
                        services.AddTransient<ReplayViewerPage>();
                        services.AddTransient<SmartNotificationsPage>();
                        services.AddTransient<StatsCardsPage>();
                        services.AddTransient<StatsSummaryPage>();
                        services.AddTransient<UpdatesPage>();
                        services.AddTransient<WebViewPage>();
                        services.AddTransient<AboutPage>();
                        services.AddTransient<PerformanceTuningPage>();
                        services.AddTransient<CachedDownloadsPage>();
                        services.AddTransient<ClientSwapPage>();
                        services.AddTransient<ConfigureOverlayPage>();
                        services.AddTransient<HeroStatsPage>();
                        services.AddTransient<LeaderboardsPage>();
                        services.AddTransient<GamesPage>();
                        
                        // NEW Dashboards and WWM Pages
                        services.AddTransient<TweaksDashboardPage>();
                        services.AddTransient<NarakaDashboardPage>();
                        services.AddTransient<WwmDashboardPage>();
                        services.AddTransient<WwmCommunityPage>();
                        services.AddTransient<WwmTrackerPage>();
                        services.AddTransient<WwmComboDetailPage>();
                        services.AddTransient<WwmCreateContentPage>();
                        
                        services.AddTransient<BiosPage>(sp => 
                        {
                            return ActivatorUtilities.CreateInstance<BiosPage>(sp);
                        });
                        services.AddSingleton<IBiosExportService, BiosExportService>();
                        services.AddSingleton<IMotherboardSupportService, MotherboardSupportService>();
                        services.AddLazySingleton<INarakaApiService, NarakaApiService>();
                        services.AddSingleton<IEtwBridge, EtwBridge>();
                        services.AddSingleton<ICloudContentService, CloudContentService>();
            services.AddSingleton<IWwmInputService, WwmInputService>();
            services.AddSingleton<IWwmDataService, WwmDataService>();
            services.AddSingleton<IContentFilterService, ContentFilterService>();
                        services.AddSingleton<IAntiCheatDetectionService, AntiCheatDetectionService>();
                        services.AddSingleton<IProfileManagerService, ProfileManagerService>();
                        services.AddTransient<NvidiaProfileImporter>(); /* Added for NVPI Import */
                        services.AddSingleton<ForegroundProcessMonitor>();
                        services.AddSingleton<IOverlayService, GameOverlayService>();

                        services.AddTransient<ReplayViewerViewModel>();
                        services.AddTransient<SmartNotificationsViewModel>();
                        services.AddTransient<StatsSummaryViewModel>();
                        services.AddTransient<MatchHistoryViewModel>();
                        services.AddTransient<TwitchDropsViewModel>();
                        services.AddTransient<ClientSwapViewModel>();
                        services.AddTransient<HeroStatsViewModel>();
                        services.AddTransient<PerformanceInsightsViewModel>();
                        services.AddTransient<PlayerLookupViewModel>();
                        services.AddTransient<BiosViewModel>();
                        services.AddTransient<StatsCardsViewModel>();
                        services.AddTransient<TweaksViewModel>();
                        services.AddTransient<GamesViewModel>();
                        
                        // NEW ViewModels
                        services.AddTransient<SettingsViewModel>();
                        services.AddTransient<LeaderboardsViewModel>();
                        services.AddTransient<NarakaDashboardViewModel>();
                        services.AddTransient<TweaksDashboardViewModel>();
                        services.AddTransient<WwmCommunityViewModel>();
                        services.AddTransient<WwmDashboardViewModel>();
                        services.AddTransient<WwmTrackerViewModel>();
                        services.AddTransient<WwmComboDetailViewModel>();
                        services.AddTransient<WwmCreateContentViewModel>();

                        Log("Services configured");
                    }).Build();

                Log("Host built, starting...");
                await _host.StartAsync();
                Log("Host started");
                
                AppServiceHost.Services = _host.Services;

                _trayIcon = (TaskbarIcon)TryFindResource("TrayIcon");
                if (_trayIcon != null)
                {
                    _trayIcon.TrayLeftMouseDown += (s, e) => LaunchMainWindow();
                    _trayIcon.TrayMouseDoubleClick += (s, e) => LaunchMainWindow();
                    _trayIcon.ForceCreate();
                }

                ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var discord = _host.Services.GetRequiredService<IDiscordRpcService>();
                var discordGame = _host.Services.GetRequiredService<IDiscordGameSdkService>(); // [FIX] Resolve Game SDK
                _ = Task.Run(() => 
                {
                    try 
                    { 
                        discord.Initialize(); 
                        // discordGame.Initialize(); // [LAZY LOAD] Removed to prevent eager auth prompt. 
                        // Now initialized in StatsOverlayViewModel only if Discord Widget exists.
                    } 
                    catch (Exception ex) { Log($"Discord Init Failed: {ex.Message}"); }
                });

                // [FIX] Initialize ETW Service (MetricServices.exe)
                // This ensures the service is installed/started and R0 dependencies are managed
                try 
                { 
                    var etw = _host.Services.GetRequiredService<IEtwBridge>();
                    etw.Start(); 
                }
                catch (Exception ex) { Log($"ETW Service Start Failed: {ex.Message}"); }

                // [FIX] Start Installation Tracker (Hourly Webhook Checks)
                var tracker = _host.Services.GetRequiredService<IInstallationTracker>();
                _ = Task.Run(async () => 
                {
                    try
                    {
                        // Initial Check (Delay slightly to let app settle)
                        await Task.Delay(5000);
                        await tracker.DetectChangesAsync();

                        // Hourly Loop
                        while (true)
                        {
                            await Task.Delay(TimeSpan.FromHours(1));
                            await tracker.DetectChangesAsync();
                        }
                    }
                    catch (Exception ex) { Log($"Tracker Loop Failed: {ex.Message}"); }
                });

                var settings = _host.Services.GetRequiredService<ISettingsService>();
                
                // Track Launch Count and Member Since date
                if (settings.Current.FirstLaunchDate == null)
                {
                    settings.Current.FirstLaunchDate = DateTime.Now;
                }
                settings.Current.LaunchCount++;
                settings.Save(settings.Current);

                var loc = _host.Services.GetRequiredService<ILocalizationService>();
                if (Resources.Contains("Loc")) Resources.Remove("Loc"); 
                Resources.Add("Loc", loc);

                bool splashSafeguardRequested = false;

                if (settings.Current.EnableSplashAnimation)
                {
                    Log($"Launching Splash Window (Monitor: {settings.Current.SelectedMonitorIndex})");
                    try
                    {
                        var splash = new SplashWindow();
                        
                        // Position Splash on Selected Monitor
                        int monitorIndex = settings.Current.SelectedMonitorIndex;
                        var display = DisplayHelper.GetDisplayByIndex(monitorIndex);
                        
                        // Ensure dimensions are initialized (fallback if needed)
                        if (double.IsNaN(splash.Width) || splash.Width == 0) splash.Width = 800;
                        if (double.IsNaN(splash.Height) || splash.Height == 0) splash.Height = 450;

                        if (display != null)
                        {
                            Log($"Splash Window setup complete");
                            // Removed manual positioning to allow WindowStartupLocation.CenterScreen to work
                        }
                        else
                        {
                            splash.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        }

                        splash.Show();
                        Log("Splash is visible, launching MainWindow transition...");
                        
                        // DO NOT BLOCK: Launch window transition on a separate logic flow
                        // We also run dependencies check in parallel here while splash is up
                        // WAIT for splash task to finish to know if Safeguard was requested or not
                        Log("Checking dependencies (Parallel with Splash)...");
                        await BootstrapHelper.EnsureDependenciesAsync(); // Non-blocking async check
                        
                        if (splash.BootSequenceTask != null) 
                        {
                            await splash.BootSequenceTask; // Wait for user interaction or timeout
                            splashSafeguardRequested = splash.SafeguardRequested;
                        }
                        
                        Log("Dependencies check complete.");
                        LaunchMainWindow(splash);
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to create/show Splash Window: {ex.Message}");
                        await BootstrapHelper.EnsureDependenciesAsync();
                        LaunchMainWindow();
                    }
                }
                else
                {
                    Log("Splash disabled, launching MainWindow");
                    await BootstrapHelper.EnsureDependenciesAsync(); // Await if no splash to hide latency
                    LaunchMainWindow();
                }

                if (settings.Current.GpuOverclock != null && settings.Current.GpuOverclock.ApplyOnStartup)
                {
                     // [SAFEGUARD REFACTOR] 
                     // Logic: If user requested Safeguard (ENTER during splash), show prompt.
                     // If NOT requested, AUTO-APPLY.
                     // If Splash disabled, we might want to default to auto-apply or show small toast. 
                     // Assuming auto-apply if splash disabled for seamlessness.
                     
                     bool shouldApply = true;

                     if (splashSafeguardRequested)
                     {
                         // Show Dialog
                         bool confirmed = NotNarakaLauncher.App.Views.Windows.WwmMessageBoxWindow.Show(
                             "SAFEGUARD INTERRUPT: GPU Settings Manager\n\nDo you want to apply your previous overclock settings?\n\nSelect 'SKIP' to load safely without applying.", 
                             "GPU Safeguard",
                             "APPLY SETTINGS",
                             "SKIP (SAFE MODE)");
                         
                         shouldApply = confirmed;
                     }

                     if (shouldApply)
                     {
                         Log("Applying GPU Overclock Settings on Startup (Auto or Confirmed)");
                         var gpuService = _host.Services.GetRequiredService<IGpuOverclockService>();
                         if (gpuService.IsSupported)
                         {
                             try
                             {
                                 await gpuService.ApplyCustomAsync(
                                     settings.Current.GpuOverclock.CoreOffset, 
                                     settings.Current.GpuOverclock.MemoryOffset, 
                                     settings.Current.GpuOverclock.PowerLimit, 
                                     settings.Current.GpuOverclock.FanSpeed);
                                     
                                 if (settings.Current.GpuOverclock.IsCustomFanCurve && settings.Current.GpuOverclock.IsCustomFanEnabled)
                                 {
                                     gpuService.ApplyFanCurve(settings.Current.GpuOverclock.FanCurve);
                                 }
                             }
                             catch (Exception ex)
                             {
                                 Log($"Failed to apply GPU settings: {ex.Message}");
                             }
                         } 
                     } 
                     else
                     {
                         Log("Skipping GPU Overclock (User Safeguard Request). Disabling Auto-Apply.");
                         settings.Current.GpuOverclock.ApplyOnStartup = false;
                         settings.Save(settings.Current);
                     }
                }

                base.OnStartup(e);
                Log("OnStartup complete");
                
                _ = Task.Run(() =>
                {
                    try
                    {
                        System.Threading.Thread.Sleep(15000);
                        var process = System.Diagnostics.Process.GetCurrentProcess();
                        process.MaxWorkingSet = process.MaxWorkingSet;
                        Log("Working set trim triggered");
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to optimize working set: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                LogCrash("OnStartup", ex);
                MessageBox.Show($"Startup failed:\n\n{ex.Message}\n\nCheck logs at:\n{LogFolder}", 
                    "NotNaraka Crash", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void LaunchOverlay()
        {
            var settings = _host?.Services.GetRequiredService<ISettingsService>();
            if (settings == null) return;

            if (settings.Current.OverlaySettings != null && settings.Current.OverlaySettings.Enabled)
            {
                Log("Launching External Overlay Process...");
                try
                {
                    var overlayPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NotNarakaOverlay.exe");
                    
                    // Kill any existing instances first
                    foreach(var p in Process.GetProcessesByName("NotNarakaOverlay"))
                    {
                        try { p.Kill(); } catch {}
                    }

                    if (File.Exists(overlayPath))
                    {
                         // [FIX] Launch without stealing focus from main window
                         var psi = new ProcessStartInfo(overlayPath)
                         {
                             UseShellExecute = false,
                             CreateNoWindow = true,
                             WindowStyle = ProcessWindowStyle.Hidden
                         };
                         Process.Start(psi);
                         Log("Overlay Process Launched: " + overlayPath);
                    }
                    else
                    {
                         Log("Overlay EXE not found at: " + overlayPath);
                    }
                }
                catch (Exception ex)
                {
                     Log("Error launching overlay: " + ex.Message);
                }
            }
        }

        private void OnTrayOpenClick(object sender, RoutedEventArgs e)
        {
            if (MainWindow != null)
            {
                BringToFront(MainWindow, true);
            }
            else
            {
                LaunchMainWindow();
            }
        }

        private void OnTrayExitClick(object sender, RoutedEventArgs e)
        {
            _trayIcon?.Dispose();
            Shutdown();
        }

        private async void OnTrayCleanMemoryClick(object sender, RoutedEventArgs e)
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                var process = System.Diagnostics.Process.GetCurrentProcess();
                process.MaxWorkingSet = process.MaxWorkingSet;
                Log("Memory Cleaned: Application memory has been optimized.");
            }
            catch (Exception ex)
            {
                Log("Error cleaning memory: " + ex.Message);
            }
        }

        private void OnTrayOverlaySettingsClick(object sender, RoutedEventArgs e)
        {
             SettingsWindow.ShowSingleInstance(MainWindow);
             if (SettingsWindow.CurrentInstance != null)
             {
                 BringToFront(SettingsWindow.CurrentInstance, true);
             }
        }

        private void LaunchMainWindow(SplashWindow? splash = null)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            try
            {
                MainWindow mainWindow;
                try
                {
                    mainWindow = _host.Services.GetRequiredService<MainWindow>();
                }
                catch (Exception ex)
                {
                    var errorDetails = new System.Text.StringBuilder();
                    errorDetails.AppendLine("Failed to launch Main Window:");
                    var current = ex;
                    while (current != null)
                    {
                        errorDetails.AppendLine($"{current.GetType().Name}: {current.Message}");
                        current = current.InnerException;
                    }
                    errorDetails.AppendLine(ex.ToString());
                    var logPath = System.IO.Path.Combine(LogFolder, "mainwindow_error.log");
                    System.IO.File.WriteAllText(logPath, errorDetails.ToString());
                    MessageBox.Show($"Failed to launch Main Window:\n{ex.Message}\nDetails: {logPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw;
                }

                var settings = _host.Services.GetRequiredService<ISettingsService>();
                
                // Force to Primary Monitor (Robust Method)
                mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                
                var displays = DisplayHelper.GetAllDisplays();
                var primary = displays.FirstOrDefault(d => d.IsPrimary) ?? displays.FirstOrDefault();
                
                mainWindow.Width = 1605;
                mainWindow.Height = 925;

                if (primary != null)
                {
                    mainWindow.Left = primary.X + (primary.Width - mainWindow.Width) / 2;
                    mainWindow.Top = primary.Y + (primary.Height - mainWindow.Height) / 2;
                    Log($"Forcing Startup on Primary Monitor (Index: {primary.Index}, Bounds: {primary.X},{primary.Y} {primary.Width}x{primary.Height})");
                }
                else
                {
                    mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                Application.Current.MainWindow = mainWindow;

                if (splash != null)
                {
                    Log("MainWindow Ready - Starting Splash Synchronized Transition");
                    // Guard against double transition triggers
                    bool transitionStarted = false;
                    var transitionLock = new object();

                    Action startTransition = () =>
                    {
                        lock(transitionLock) {
                            if (transitionStarted) return;
                            transitionStarted = true;
                        }

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                if (splash.BootSequenceTask != null) await splash.BootSequenceTask;
                                
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    Log("Transition: Setting MainWindow Opacity to 0");
                                    mainWindow.Opacity = 0;
                                    mainWindow.Show();
                                    Log("Transition: Calling StartApplication");
                                    mainWindow.StartApplication();
                                    Log("Transition: StartApplication Returned");
                                    ShutdownMode = ShutdownMode.OnMainWindowClose;
                                    Log("Transition: MainWindow Loaded & Initialized.");
                                });

                                 await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    mainWindow.Opacity = 1;
                                    mainWindow.WindowState = WindowState.Normal;
                                    BringToFront(mainWindow, true);
                                    Log("Transition: MainWindow Visibility & Z-Order fixed before splash close.");
                                });

                                await splash.AnimateCloseAsync();
                                
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    Log("Application Ready.");
                                    mainWindow.StartTheme(); // Start Theme on Main Window
                                    LaunchOverlay();
                                });
                            }
                            catch (Exception ex)
                            {
                                Log($"Splash Transition Error: {ex.Message}");
                                await Application.Current.Dispatcher.InvokeAsync(() => 
                                {
                                    try { splash.Close(); } catch { }
                                    mainWindow.Opacity = 1;
                                    mainWindow.WindowState = WindowState.Normal;
                                    BringToFront(mainWindow, true);
                                    LaunchOverlay();
                                });
                            }
                        });
                    };

                    mainWindow.ContentRendered += (s, args) => startTransition();
                    mainWindow.Loaded += (s, args) => startTransition();
                    mainWindow.Opacity = 0;
                    mainWindow.Show();
                }
                else
                {
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Show();
                    BringToFront(mainWindow, false);
                    mainWindow.StartApplication();
                    LaunchOverlay();
                }
                ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            catch (Exception ex)
            {
                LogCrash("LaunchMainWindow", ex);
                Shutdown(1);
            }
        }

        private System.Threading.Mutex? _appMutex;
        private static readonly string MutexName = "Global\\NotNarakaLauncher_SingleInstance_Mutex";

        private void InitializeSingleInstance()
        {
            try 
            {
                _appMutex = new System.Threading.Mutex(true, MutexName, out bool createdNew);
                if (!createdNew)
                {
                    MessageBox.Show("NotNaraka Launcher is already running.", "NotNaraka Launcher", MessageBoxButton.OK, MessageBoxImage.Information);
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Log($"Mutex error: {ex.Message}");
            }
        }

        private bool IsRunningAsAdmin()
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            Log("OnExit called");
            _trayIcon?.Dispose();
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            base.OnExit(e);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash("DispatcherUnhandledException", e.Exception);
            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogCrash("UnhandledException", ex);
            }
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogCrash("UnobservedTaskException", e.Exception);
            e.SetObserved();
        }

        public static void Log(string message)
        {
            try
            {
                var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                var line = $"[{DateTime.Now:HH:mm:ss.fff}] [T{threadId}] {message}";
                File.AppendAllText(AppLogPath, line + Environment.NewLine);
                System.Diagnostics.Debug.WriteLine(line);
            }
            catch { }
        }

        private static bool _isReportingCrash = false;

        private static void LogCrash(string source, Exception ex)
        {
            try
            {
                var crash = $"""
                    ==========================================
                    CRASH: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                    SOURCE: {source}
                    ==========================================
                    {ex}
                    ==========================================
                    
                    """;
                File.AppendAllText(CrashLogPath, crash);
                Log($"CRASH [{source}]: {ex.Message}");

                // Trigger Crash Reporter UI
                if (!_isReportingCrash)
                {
                    _isReportingCrash = true;
                    
                    // Locate ETWHelper / MetricServices
                    var helperName = "NotNaraka_MetricServices.exe";
                    var helperPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, helperName);
                    
                     if (!File.Exists(helperPath))
                     {
                         // 1. Check if we are in 'app' subfolder and helper is in a parallel 'MetricServices' folder
                         var parent = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.FullName;
                         if (parent != null)
                         {
                             var check = Path.Combine(parent, helperName);
                             if (File.Exists(check)) helperPath = check;
                             else 
                             {
                                 var checkSub = Path.Combine(parent, "MetricServices", helperName);
                                 if (File.Exists(checkSub)) helperPath = checkSub;
                             }
                         }
                     }

                     // 2. Check ProgramData Fallback (Standard Install Path)
                     if (!File.Exists(helperPath))
                     {
                         var commonData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                         var checkPD = Path.Combine(commonData, "NotNaraka", "MetricServices", helperName);
                         if (File.Exists(checkPD)) helperPath = checkPD;
                         else
                         {
                             var checkPD2 = Path.Combine(commonData, "NotNaraka_MetricServices", helperName);
                             if (File.Exists(checkPD2)) helperPath = checkPD2;
                         }
                     }

                    if (File.Exists(helperPath))
                    {
                        var msg = ex.Message.Replace("\"", "'"); // Simple escape
                        var stack = ex.ToString().Replace("\"", "'");
                        
                        var psi = new ProcessStartInfo
                        {
                            FileName = helperPath,
                            Arguments = $"--crash-report --error \"{msg}\" --stack \"{stack}\"",
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                        
                        // Give it a moment to start before we die
                        System.Threading.Thread.Sleep(2000);
                    }
                    else
                    {
                         MessageBox.Show($"Application Crashed:\n{ex.Message}\n\n(Crash Reporter not found at {helperPath})", "NotNaraka Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch { }
        }

        private static void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
        {
            try
            {
                var ex = e.Exception;
                if (ex == null) return;

                bool interesting = ex is NullReferenceException;

                if (!interesting && ex is EntryPointNotFoundException)
                    interesting = true;

                if (!interesting && ex is System.Runtime.InteropServices.COMException comEx)
                {
                    if (comEx.HResult == unchecked((int)0x8001010E))
                        interesting = true;
                }

                if (!interesting) return;

                if (Interlocked.Increment(ref _firstChanceSampleCount) > 30) return;

                Log($"FirstChance captured: {ex.GetType().Name} HR=0x{ex.HResult:X8} MSG='{ex.Message}'\n{ex.StackTrace}");
            }
            catch { }
        }

        private void BringToFront(Window window, bool activate)
        {
            if (activate)
            {
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;
                
                window.Show();
                window.Activate();
                window.Focus();
            }
            else
            {
                // Just Show() without stealing focus, avoiding aggressive Z-order changes
                // that might push the window behind other apps.
                window.Show();
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void OnTrayContextMenuOpen(object sender, RoutedEventArgs e)
        {
            // Fix for Context Menu closing/positioning issues
            // By setting foreground window to the tray icon's message window (or main window).
            // However, Hardcodet.NotifyIcon usually handles this.
            // A common fix for "wrong monitor" is ensuring the app is DPI aware, which it should be.
            // But forcing focus to the specific HwndSource of the menu might help.
        }

        // We removed the aggressive SetWindowPos P/Invoke declarations as they are no longer used.
    }
}
