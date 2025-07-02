using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;
using Microsoft.Extensions.Logging;

namespace KXMapStudio.App.Services
{
    /// <summary>
    /// Manages global hotkey registration and events.
    /// </summary>
    public class GlobalHotkeyService : IDisposable
    {
        private const string HotkeysConfigFileName = "hotkeys.json";
        private readonly ILogger<GlobalHotkeyService> _logger;
        private HotkeyConfig _config;

        public event EventHandler? AddMarkerHotkeyPressed;
        public event EventHandler? UndoLastAddHotkeyPressed;

        public string AddMarkerHotkeyText => FormatHotkey(_config.AddMarkerModifiers, _config.AddMarkerKey);
        public string UndoLastAddHotkeyText => FormatHotkey(_config.UndoLastAddModifiers, _config.UndoLastAddKey);

        public GlobalHotkeyService(ILogger<GlobalHotkeyService> logger)
        {
            _logger = logger;
            _config = LoadOrCreateConfig();

        }

        private HotkeyConfig LoadOrCreateConfig()
        {
            try
            {
                if (File.Exists(HotkeysConfigFileName))
                {
                    var json = File.ReadAllText(HotkeysConfigFileName);
                    var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() }, PropertyNameCaseInsensitive = true };
                    var config = JsonSerializer.Deserialize<HotkeyConfig>(json, options);
                    if (config != null)
                    {
                        _logger.LogInformation("Loaded hotkey configuration from {File}.", HotkeysConfigFileName);
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load hotkey configuration from {File}. Using default.", HotkeysConfigFileName);
            }

            var defaultConfig = new HotkeyConfig();
            defaultConfig.AddMarkerKey = Key.F9;
            defaultConfig.AddMarkerModifiers = ModifierKeys.None;
            defaultConfig.UndoLastAddKey = Key.F10;
            defaultConfig.UndoLastAddModifiers = ModifierKeys.None;

            SaveConfig(defaultConfig);
            _logger.LogInformation("Created default hotkey configuration and saved to {File}.", HotkeysConfigFileName);
            return defaultConfig;
        }

        private void SaveConfig(HotkeyConfig config)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(HotkeysConfigFileName, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save hotkey configuration to {File}.", HotkeysConfigFileName);
            }
        }

        public void RegisterHotkeys()
        {
            UnregisterHotkeys();

            try
            {
                HotkeyManager.Current.AddOrReplace("AddMarker", _config.AddMarkerKey, _config.AddMarkerModifiers, HotkeyManager_Hotkey);
                _logger.LogInformation("Registered AddMarker hotkey: {Hotkey}", AddMarkerHotkeyText);
            }
            catch (HotkeyAlreadyRegisteredException)
            {
                _logger.LogWarning("AddMarker hotkey {Hotkey} is already registered by another application.", AddMarkerHotkeyText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register AddMarker hotkey.");
            }

            try
            {
                HotkeyManager.Current.AddOrReplace("UndoLastAdd", _config.UndoLastAddKey, _config.UndoLastAddModifiers, HotkeyManager_Hotkey);
                _logger.LogInformation("Registered UndoLastAdd hotkey: {Hotkey}", UndoLastAddHotkeyText);
            }
            catch (HotkeyAlreadyRegisteredException)
            {
                _logger.LogWarning("UndoLastAdd hotkey {Hotkey} is already registered by another application.", UndoLastAddHotkeyText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register UndoLastAdd hotkey.");
            }
        }

        private void UnregisterHotkeys()
        {
            HotkeyManager.Current.Remove("AddMarker");
            HotkeyManager.Current.Remove("UndoLastAdd");
            _logger.LogInformation("Unregistered global hotkeys.");
        }

        /// <summary>
        /// Handles registered hotkey presses.
        /// </summary>
        private void HotkeyManager_Hotkey(object? sender, HotkeyEventArgs e)
        {
            if (e.Name == "AddMarker")
            {
                AddMarkerHotkeyPressed?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.Name == "UndoLastAdd")
            {
                UndoLastAddHotkeyPressed?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private string FormatHotkey(ModifierKeys modifiers, Key key)
        {
            if (modifiers == ModifierKeys.None)
            {
                return key.ToString();
            }

            var modifierStrings = new List<string>();
            if (modifiers.HasFlag(ModifierKeys.Control))
            {
                modifierStrings.Add("Ctrl");
            }

            if (modifiers.HasFlag(ModifierKeys.Shift))
            {
                modifierStrings.Add("Shift");
            }

            if (modifiers.HasFlag(ModifierKeys.Alt))
            {
                modifierStrings.Add("Alt");
            }

            if (modifiers.HasFlag(ModifierKeys.Windows))
            {
                modifierStrings.Add("Win");
            }

            if (modifierStrings.Any())
            {
                return $"{string.Join("+", modifierStrings)}+{key}";
            }
            return key.ToString();
        }

        public void Dispose()
        {
            UnregisterHotkeys();
            GC.SuppressFinalize(this);
        }
    }
}
