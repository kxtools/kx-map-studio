using System.Windows.Input;
using System.Text.Json.Serialization;

namespace KXMapStudio.App.Services
{
    /// <summary>
    /// Defines the serializable hotkey configuration.
    /// </summary>
    public class HotkeyConfig
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Key AddMarkerKey { get; set; } = Key.F9;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ModifierKeys AddMarkerModifiers { get; set; } = ModifierKeys.Control | ModifierKeys.Alt;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Key UndoLastAddKey { get; set; } = Key.F10;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ModifierKeys UndoLastAddModifiers { get; set; } = ModifierKeys.Control | ModifierKeys.Alt;
    }
}
