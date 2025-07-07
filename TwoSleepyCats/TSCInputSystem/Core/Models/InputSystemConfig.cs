using TwoSleepyCats.CSVReader.Core;

namespace TwoSleepyCats.TSCInputSystem
{
    /// <summary>
    /// Typed configuration wrapper
    /// </summary>
    public class InputSystemConfig
    {
        // Device Detection
        public float DetectionSensitivity { get; set; } = 0.1f;
        public bool AutoSwitchDevices { get; set; } = true;
        public InputDeviceType DefaultDevice { get; set; } = InputDeviceType.Keyboard;

        // UI Navigation
        public float NavigationDelay { get; set; } = 0.2f;
        public bool ShowHighlights { get; set; } = true;
        public float HighlightAnimationSpeed { get; set; } = 1f;

        // Supported Devices
        public bool SupportKeyboard { get; set; } = true;
        public bool SupportGamepad { get; set; } = true;
        public bool SupportTouch { get; set; } = true;

        // Performance
        public int InputUpdateFrequency { get; set; } = 60;
        public bool UseInputEvents { get; set; } = true;

        public static InputSystemConfig LoadFromCsv()
        {
            var csvData = CsvDataManager.Instance.Get<InputConfiguration>();
            var config = new InputSystemConfig();

            foreach (var setting in csvData)
            {
                ApplySetting(config, setting.SettingName, setting.Value);
            }

            return config;
        }

        private static void ApplySetting(InputSystemConfig config, string settingName, string value)
        {
            switch (settingName.ToLowerInvariant())
            {
                case "detection_sensitivity":
                    config.DetectionSensitivity = float.Parse(value);
                    break;
                case "auto_switch_devices":
                    config.AutoSwitchDevices = bool.Parse(value);
                    break;
                case "default_device":
                    config.DefaultDevice = System.Enum.Parse<InputDeviceType>(value);
                    break;
                case "navigation_delay":
                    config.NavigationDelay = float.Parse(value);
                    break;
                case "show_highlights":
                    config.ShowHighlights = bool.Parse(value);
                    break;
                case "highlight_animation_speed":
                    config.HighlightAnimationSpeed = float.Parse(value);
                    break;
                case "support_keyboard":
                    config.SupportKeyboard = bool.Parse(value);
                    break;
                case "support_gamepad":
                    config.SupportGamepad = bool.Parse(value);
                    break;
                case "support_touch":
                    config.SupportTouch = bool.Parse(value);
                    break;
                case "input_update_frequency":
                    config.InputUpdateFrequency = int.Parse(value);
                    break;
                case "use_input_events":
                    config.UseInputEvents = bool.Parse(value);
                    break;
            }
        }
    }
}