using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NotNarakaLauncher.Core.Services;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;

namespace NotNarakaLauncher.App.ViewModels
{
    public partial class InstallRow : ObservableObject
    {
        [ObservableProperty]
        private string _client = ""; // "Steam", "Epic", "Xbox", "official", "global"

        [ObservableProperty]
        private string _path = "";
        
        [ObservableProperty]
        private string _displayName = "";
    }

    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IUpdateService? _updateService;
        private readonly IInstalledClientsService? _installedClientsService;
        private readonly ITweakService? _tweakService;

        private readonly IPremiumService? _premiumService;
        private readonly ICloudContentService? _cloudService;
        private readonly IUacBypassService? _uacBypassService;

        [ObservableProperty]
        private string _versionString = "v2.0.0.0";
        
        [ObservableProperty]
        private bool _hasNvidiaGpu;
        
        [ObservableProperty]
        private bool _hasAmdGpu;

        public ObservableCollection<InstallRow> InstallRows { get; } = new();

        // Preferred Client Options
        public List<string> PreferredClientOptions { get; } = new() 
        { 
            "Steam", "Epic", "Xbox", "Official", "Global"
        };
        
        // Canonical map for legacy compat
        private static readonly Dictionary<string, string> CanonicalMap = new(StringComparer.OrdinalIgnoreCase)
        {
            {"Steam", "Steam"},
            {"Epic", "Epic"},
            {"Xbox", "Xbox"},
            {"Official", "official"},
            {"Global", "global"}
        };
        
        private static readonly Dictionary<string, string> ReverseCanonicalMap = new(StringComparer.OrdinalIgnoreCase)
        {
            {"Steam", "Steam"},
            {"Epic", "Epic"},
            {"Xbox", "Xbox"},
            {"official", "Official"},
            {"global", "Global"}
        };

        public SettingsViewModel(
            ISettingsService settingsService, 
            IUpdateService? updateService,
            IInstalledClientsService? installedClientsService,
            ITweakService? tweakService,
            IPremiumService? premiumService = null,
            ICloudContentService? cloudService = null,
            IUacBypassService? uacBypassService = null)
        {
            _settingsService = settingsService;
            _updateService = updateService;
            _installedClientsService = installedClientsService;
            _tweakService = tweakService;
            _premiumService = premiumService;
            _cloudService = cloudService;
            _uacBypassService = uacBypassService;
            
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            VersionString = version != null ? $"v{version}" : "v2.0.0.0";

            // Detect GPU vendor
            if (_tweakService != null)
            {
                var vendor = _tweakService.GetGpuVendor();
                HasNvidiaGpu = vendor == Core.Services.GpuVendor.Nvidia;
                HasAmdGpu = vendor == Core.Services.GpuVendor.Amd;
            }

            LoadInstalls();
        }

        public string PreferredClient
        {
            get
            {
                var stored = _settingsService.Current.PreferredClient;
                if (!string.IsNullOrEmpty(stored) && ReverseCanonicalMap.TryGetValue(stored, out var display))
                    return display;
                return "Steam"; // Default
            }
            set
            {
                if (CanonicalMap.TryGetValue(value, out var canonical))
                {
                    if (_settingsService.Current.PreferredClient != canonical)
                    {
                        _settingsService.Current.PreferredClient = canonical;
                        OnPropertyChanged();
                        SaveSettings();
                    }
                }
            }
        }

        public bool LaunchOnStartup
        {
            get => _settingsService.Current.LaunchOnStartup;
            set
            {
                if (_settingsService.Current.LaunchOnStartup != value)
                {
                    _settingsService.Current.LaunchOnStartup = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool MinimizeToTray
        {
            get => _settingsService.Current.MinimizeToTray;
            set
            {
                if (_settingsService.Current.MinimizeToTray != value)
                {
                    _settingsService.Current.MinimizeToTray = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool EnableDiscordRpc
        {
            get => _settingsService.Current.EnableDiscordRpc;
            set
            {
                if (_settingsService.Current.EnableDiscordRpc != value)
                {
                    _settingsService.Current.EnableDiscordRpc = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool IsUacBypassEnabled
        {
            get => _uacBypassService?.IsScheduledTaskInstalled ?? false;
            set
            {
                if (_uacBypassService == null) return;
                
                try 
                {
                    bool success;
                    if (value) success = _uacBypassService.InstallScheduledTask();
                    else success = _uacBypassService.UninstallScheduledTask();

                    if (success)
                    {
                        OnPropertyChanged();
                        // No setting to save, state is system-driven (Task Scheduled or not)
                    }
                    else
                    {
                        // Revert UI if failed
                        OnPropertyChanged(); 
                    }
                }
                catch { OnPropertyChanged(); }
            }
        }

        /// <summary>
        /// Premium status for gating features like Discord RPC toggle
        /// </summary>
        public bool IsPremium => _premiumService?.IsPremium ?? false;

        /// <summary>
        /// Whether the Discord RPC toggle can be changed (premium only can disable)
        /// </summary>
        public bool CanToggleDiscordRpc => IsPremium;



        public bool EnableSplashAnimation
        {
            get => _settingsService.Current.EnableSplashAnimation;
            set
            {
                if (_settingsService.Current.EnableSplashAnimation != value)
                {
                    _settingsService.Current.EnableSplashAnimation = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public double UiScale
        {
            get => _settingsService.Current.UiScale;
            set
            {
                if (Math.Abs(_settingsService.Current.UiScale - value) > 0.01)
                {
                    _settingsService.Current.UiScale = Math.Max(0.5, Math.Min(2.0, value));
                    OnPropertyChanged();
                    SaveSettings();
                    // Apply UI scale change
                    Application.Current.Resources["UiScale"] = _settingsService.Current.UiScale;
                }
            }
        }

        public double TimerResolutionMs
        {
            get => _settingsService.Current.Tweaks.TimerResolutionMs;
            set
            {
                if (Math.Abs(_settingsService.Current.Tweaks.TimerResolutionMs - value) > 0.01)
                {
                    _settingsService.Current.Tweaks.TimerResolutionMs = Math.Max(0.1, Math.Min(15.6, value));
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool AutoOptimizeEnabled
        {
            get => _settingsService.Current.Tweaks.AutoOptimizeEnabled;
            set
            {
                if (_settingsService.Current.Tweaks.AutoOptimizeEnabled != value)
                {
                    _settingsService.Current.Tweaks.AutoOptimizeEnabled = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public int AutoOptimizeIntervalMinutes
        {
            get => _settingsService.Current.Tweaks.AutoOptimizeIntervalMinutes;
            set
            {
                if (_settingsService.Current.Tweaks.AutoOptimizeIntervalMinutes != value)
                {
                    _settingsService.Current.Tweaks.AutoOptimizeIntervalMinutes = Math.Max(1, Math.Min(60, value));
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool AutoOptimizeNormalOnly
        {
            get => _settingsService.Current.Tweaks.AutoOptimizeNormalOnly;
            set
            {
                if (_settingsService.Current.Tweaks.AutoOptimizeNormalOnly != value)
                {
                    _settingsService.Current.Tweaks.AutoOptimizeNormalOnly = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public string EditorHotkey
        {
            get => _settingsService.Current.OverlaySettings?.EditorHotkey ?? "Shift+F1";
            set
            {
                if (_settingsService.Current.OverlaySettings != null && _settingsService.Current.OverlaySettings.EditorHotkey != value)
                {
                    _settingsService.Current.OverlaySettings.EditorHotkey = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool OverlayEnabled
        {
            get => _settingsService.Current.OverlaySettings?.Enabled ?? true;
            set
            {
                if (_settingsService.Current.OverlaySettings != null && _settingsService.Current.OverlaySettings.Enabled != value)
                {
                    _settingsService.Current.OverlaySettings.Enabled = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool AutoAttach
        {
            get => _settingsService.Current.OverlaySettings != null && !_settingsService.Current.OverlaySettings.UseProcessSelection;
            set
            {
                if (_settingsService.Current.OverlaySettings != null)
                {
                    _settingsService.Current.OverlaySettings.UseProcessSelection = !value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public double CursorSensitivity
        {
            get => _settingsService.Current.OverlaySettings?.CursorSensitivity ?? 1.0;
            set
            {
                if (_settingsService.Current.OverlaySettings != null && Math.Abs(_settingsService.Current.OverlaySettings.CursorSensitivity - value) > 0.01)
                {
                    _settingsService.Current.OverlaySettings.CursorSensitivity = (float)value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        [ObservableProperty]
        private bool _isRecordingHotkey;

        [RelayCommand]
        private void StartRecordingHotkey()
        {
            IsRecordingHotkey = true;
        }

        [RelayCommand]
        private void StopRecordingHotkey()
        {
            IsRecordingHotkey = false;
        }

        public void SetHotkey(string key, string modifiers)
        {
             string hotkey = string.IsNullOrEmpty(modifiers) ? key : $"{modifiers}+{key}";
             EditorHotkey = hotkey;
             IsRecordingHotkey = false;
        }

        [RelayCommand]
        private void CleanMemory()
        {
            try
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                
                var process = Process.GetCurrentProcess();
                process.MinWorkingSet = (IntPtr)(50 * 1024 * 1024); // 50MB
                
                MessageBox.Show("Memory cleaned successfully.", "Memory Clean", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cleaning memory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ApplyTimerResolution()
        {
            if (_tweakService == null) return;
            try
            {
                _tweakService.SetTimerResolution(TimerResolutionMs);
                MessageBox.Show($"Timer resolution set to {TimerResolutionMs}ms", "Timer Resolution", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting timer resolution: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ResetTimerResolution()
        {
            TimerResolutionMs = 0.5; // Default
            if (_tweakService != null)
            {
                try
                {
                    _tweakService.SetTimerResolution(0.5);
                }
                catch { }
            }
        }

        [RelayCommand]
        private void StartAutoOptimize()
        {
            if (_tweakService == null) return;
            try
            {
                _tweakService.StartAutoOptimize(AutoOptimizeIntervalMinutes, AutoOptimizeNormalOnly);
                MessageBox.Show($"Auto-optimize started (interval: {AutoOptimizeIntervalMinutes} minutes)", "Auto-Optimize", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting auto-optimize: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void StopAutoOptimize()
        {
            if (_tweakService == null) return;
            try
            {
                _tweakService.StopAutoOptimize();
                MessageBox.Show("Auto-optimize stopped", "Auto-Optimize", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping auto-optimize: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSettings()
        {
            _settingsService.Save(_settingsService.Current);
        }

        private void LoadInstalls()
        {
            InstallRows.Clear();
            var detected = _installedClientsService?.GetInstalledClients() ?? new Dictionary<string, string>();
            
            // Add known clients from detected or settings
            foreach (var kv in detected)
            {
                 // Add detected ones
                 AddToInstallRows(kv.Key, kv.Value);
            }
            
            // Add configured ones if not already added
            foreach (var kv in _settingsService.Current.InstallLocations)
            {
                if (!detected.ContainsKey(kv.Key))
                {
                    AddToInstallRows(kv.Key, kv.Value);
                }
            }

            // Ensure all preferred options are present even if empty
            foreach (var opt in CanonicalMap)
            {
                if (!InstallRows.Any(r => r.Client.Equals(opt.Value, StringComparison.OrdinalIgnoreCase)))
                {
                    InstallRows.Add(new InstallRow 
                    { 
                        Client = opt.Value, 
                        Path = "", 
                        DisplayName = opt.Key 
                    });
                }
            }
        }

        private void AddToInstallRows(string client, string path)
        {
             var display = ReverseCanonicalMap.ContainsKey(client) ? ReverseCanonicalMap[client] : client;
             var existing = InstallRows.FirstOrDefault(r => r.Client.Equals(client, StringComparison.OrdinalIgnoreCase));
             if (existing != null)
             {
                 existing.Path = path;
             }
             else
             {
                 InstallRows.Add(new InstallRow { Client = client, Path = path, DisplayName = display });
             }
        }

        [RelayCommand]
        private void DetectInstalls()
        {
            if (_installedClientsService == null) return;
            
            try 
            {
                var detected = _installedClientsService.GetInstalledClients();
                var changed = false;
                foreach (var kv in detected)
                {
                    if (!_settingsService.Current.InstallLocations.ContainsKey(kv.Key) || 
                        _settingsService.Current.InstallLocations[kv.Key] != kv.Value)
                    {
                        _settingsService.Current.InstallLocations[kv.Key] = kv.Value;
                        changed = true;
                    }
                }
                
                if (changed) SaveSettings();
                LoadInstalls();
                MessageBox.Show($"Detected {detected.Count} installations.", "Detection Complete");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error detecting installs: {ex.Message}");
            }
        }

        [RelayCommand]
        private void BrowseInstallLocation(InstallRow row)
        {
            if (row == null) return;
            
            var dialog = new OpenFolderDialog();
            dialog.Title = $"Select Installation Folder for {row.DisplayName}";
            if (dialog.ShowDialog() == true)
            {
                row.Path = dialog.FolderName;
                _settingsService.Current.InstallLocations[row.Client] = row.Path;
                SaveSettings();
            }
        }
        
        [RelayCommand]
        private void OpenInstallLocation(InstallRow row)
        {
             if (row == null || string.IsNullOrWhiteSpace(row.Path)) return;
             try
             {
                 Process.Start(new ProcessStartInfo { FileName = row.Path, UseShellExecute = true });
             }
             catch { }
        }

        [RelayCommand]
        private async Task CheckUpdate()
        {
            if (_updateService == null) 
            {
                MessageBox.Show("Update service unavailable.", "Error");
                return;
            }

            try 
            {
                var updateAvailable = await _updateService.CheckForUpdatesAsync();
                if (updateAvailable)
                {
                    if (MessageBox.Show("New version available. Update now?", "Update", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        await _updateService.PerformUpdateAsync();
                    }
                }
                else
                {
                    MessageBox.Show("You are on the latest version.", "Up to Date");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Check failed: {ex.Message}", "Error");
            }
        }



        [RelayCommand]
        private async Task UploadSettingsAsync()
        {
            if (!IsPremium)
            {
                MessageBox.Show("Cloud Sync is a Premium feature.", "Premium Required");
                return;
            }
            if (_cloudService == null) return;

            try
            {
                 // Create payload from current settings
                 // For MVP, we just serialize the whole settings object to a dict or similar, 
                 // but SyncPayload expects Dictionary<string, object>.
                 // We'll manually map critical fields or serialize/deserialize.
                 var current = _settingsService.Current;
                 var dict = new Dictionary<string, object>
                 {
                     ["UiScale"] = current.UiScale,
                     ["OverlayEnabled"] = current.OverlayEnabled,
                     ["DiscordRpc"] = current.EnableDiscordRpc,
                     // Add more as needed
                 };

                 var payload = new SyncPayload
                 {
                     UpdatedAt = DateTime.UtcNow.Ticks,
                     Settings = dict
                 };

                 var success = await _cloudService.UploadSettingsSyncAsync(payload);
                 MessageBox.Show(success ? "Settings uploaded successfully." : "Upload failed.", "Cloud Sync");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Upload failed: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task DownloadSettingsAsync()
        {
             if (!IsPremium)
            {
                MessageBox.Show("Cloud Sync is a Premium feature.", "Premium Required");
                return;
            }
            if (_cloudService == null) return;

            try
            {
                var payload = await _cloudService.GetSettingsSyncAsync();
                if (payload != null && payload.Settings != null)
                {
                    // Apply settings
                    if (payload.Settings.ContainsKey("UiScale") && payload.Settings["UiScale"] is JsonElement uiScaleElem && uiScaleElem.TryGetDouble(out var scale))
                        _settingsService.Current.UiScale = scale;

                    if (payload.Settings.ContainsKey("OverlayEnabled") && payload.Settings["OverlayEnabled"] is JsonElement overlayElem && (overlayElem.ValueKind == JsonValueKind.True || overlayElem.ValueKind == JsonValueKind.False))
                        _settingsService.Current.OverlayEnabled = overlayElem.GetBoolean();
                    
                    if (payload.Settings.ContainsKey("DiscordRpc") && payload.Settings["DiscordRpc"] is JsonElement rpcElem && (rpcElem.ValueKind == JsonValueKind.True || rpcElem.ValueKind == JsonValueKind.False))
                    {
                        bool val = rpcElem.GetBoolean();
                        _settingsService.Current.EnableDiscordRpc = val;
                    }

                    _settingsService.Save(_settingsService.Current);
                    OnPropertyChanged(""); // Refresh all
                    MessageBox.Show("Settings downloaded and applied.", "Cloud Sync");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Download failed: {ex.Message}");
            }
        }

        [RelayCommand]
        private void OpenDiscord()
        {
             try
            {
                Process.Start(new ProcessStartInfo("https://discord.gg/notnaraka") { UseShellExecute = true });
            }
            catch { }
        }
    }
}
