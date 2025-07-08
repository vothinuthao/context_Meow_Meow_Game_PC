using TwoSleepyCats.CSVReader.Core;

namespace TwoSleepyCats.TSCInputSystem
{
    /// <summary>
    /// Fixed InputSystemConfig that uses synchronous CSV loading to prevent async/await deadlock
    /// Safe for use in Zenject installers and other synchronous initialization contexts
    /// </summary>
    public class InputSystemConfig
    {
        // Configuration properties
        public float DetectionSensitivity { get; set; } = 0.1f;
        public bool AutoSwitchDevices { get; set; } = true;
        public InputDeviceType DefaultDevice { get; set; } = InputDeviceType.Keyboard;
        public float NavigationDelay { get; set; } = 0.2f;
        public bool ShowHighlights { get; set; } = true;
        public float HighlightAnimationSpeed { get; set; } = 1f;
        public bool SupportKeyboard { get; set; } = true;
        public bool SupportGamepad { get; set; } = true;
        public bool SupportTouch { get; set; } = false;
        public int InputUpdateFrequency { get; set; } = 60;
        public bool UseInputEvents { get; set; } = true;

        private static InputSystemConfig _cachedConfig;
        private static readonly object _lockObject = new object();

        /// <summary>
        /// Load configuration from CSV using synchronous method to prevent deadlock
        /// This method is safe for use in Zenject installers and Unity initialization contexts
        /// </summary>
        public static InputSystemConfig LoadFromCsv()
        {
            lock (_lockObject)
            {
                if (_cachedConfig != null)
                {
                    return _cachedConfig;
                }

                var config = new InputSystemConfig();

                try
                {
                    UnityEngine.Debug.Log("[InputSystemConfig] Loading configuration using synchronous CSV reader");
                    
                    var (csvData, errors) = CsvReader<InputConfiguration>.LoadSync();

                    if (errors.HasCriticalErrors)
                    {
                        errors.LogToConsole();
                    }
                    else
                    {
                        if (errors.HasErrors || errors.HasWarnings)
                        {
                            UnityEngine.Debug.LogWarning($"[InputSystemConfig] CSV loaded with {errors.Errors.Count} issues:");
                            errors.LogToConsole();
                        }

                        // Apply settings from CSV data
                        foreach (var setting in csvData)
                        {
                            ApplySetting(config, setting.SettingName, setting.Value);
                        }

                        UnityEngine.Debug.Log($"[InputSystemConfig] Successfully loaded {csvData.Count} configuration settings");
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[InputSystemConfig] Exception during CSV loading: {ex.Message}");
                    UnityEngine.Debug.LogWarning("[InputSystemConfig] Using default configuration due to exception");
                }

                // Validate configuration
                if (!config.ValidateConfiguration())
                {
                    UnityEngine.Debug.LogWarning("[InputSystemConfig] Configuration validation failed, using corrected values");
                }

                _cachedConfig = config;
                return config;
            }
        }

        /// <summary>
        /// Apply individual setting to configuration
        /// </summary>
        private static void ApplySetting(InputSystemConfig config, string settingName, string value)
        {
            try
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
                        config.DefaultDevice = System.Enum.Parse<InputDeviceType>(value, true);
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
                    default:
                        UnityEngine.Debug.LogWarning($"[InputSystemConfig] Unknown setting: {settingName}");
                        break;
                }
                
                UnityEngine.Debug.Log($"[InputSystemConfig] Applied setting: {settingName} = {value}");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[InputSystemConfig] Failed to parse setting '{settingName}' = '{value}': {ex.Message}");
            }
        }

        /// <summary>
        /// Validate configuration values are within acceptable ranges
        /// </summary>
        public bool ValidateConfiguration()
        {
            bool isValid = true;

            if (DetectionSensitivity < 0.01f || DetectionSensitivity > 1.0f)
            {
                UnityEngine.Debug.LogWarning($"[InputSystemConfig] Invalid DetectionSensitivity: {DetectionSensitivity}. Correcting to 0.1");
                DetectionSensitivity = 0.1f;
                isValid = false;
            }

            if (NavigationDelay < 0.0f || NavigationDelay > 1.0f)
            {
                UnityEngine.Debug.LogWarning($"[InputSystemConfig] Invalid NavigationDelay: {NavigationDelay}. Correcting to 0.2");
                NavigationDelay = 0.2f;
                isValid = false;
            }

            if (HighlightAnimationSpeed < 0.1f || HighlightAnimationSpeed > 10.0f)
            {
                UnityEngine.Debug.LogWarning($"[InputSystemConfig] Invalid HighlightAnimationSpeed: {HighlightAnimationSpeed}. Correcting to 1.0");
                HighlightAnimationSpeed = 1.0f;
                isValid = false;
            }

            if (InputUpdateFrequency < 30 || InputUpdateFrequency > 120)
            {
                UnityEngine.Debug.LogWarning($"[InputSystemConfig] Invalid InputUpdateFrequency: {InputUpdateFrequency}. Correcting to 60");
                InputUpdateFrequency = 60;
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Clear cached configuration to force reload
        /// </summary>
        public static void ClearCache()
        {
            lock (_lockObject)
            {
                _cachedConfig = null;
                UnityEngine.Debug.Log("[InputSystemConfig] Configuration cache cleared");
            }
        }

        /// <summary>
        /// Get current configuration or create default if none loaded
        /// </summary>
        public static InputSystemConfig GetCurrent()
        {
            return _cachedConfig ?? LoadFromCsv();
        }
    }
}