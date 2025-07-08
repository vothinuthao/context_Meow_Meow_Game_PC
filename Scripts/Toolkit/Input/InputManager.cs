using OctoberStudio.UI;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TwoSleepyCats.TSCInputSystem;
using TwoSleepyCats.CSVReader.Core;
using Zenject;

namespace OctoberStudio.Input
{
    /// <summary>
    /// Completely rewritten InputManager using TSC Input System
    /// Replaces the old InputManager while maintaining interface compatibility
    /// </summary>
    public class InputManager : MonoBehaviour, IInputManager
    {
        private static InputManager instance;

        [Header("TSC System Integration")]
        [SerializeField] private InputAsset inputAsset;
        [SerializeField] private HighlightsParentBehavior highlightsParent;

        // TSC System Dependencies (injected via Zenject)
        [Inject] private IInputProcessor inputProcessor;
        [Inject] private IInputEventBus eventBus;
        [Inject] private IInputDetector inputDetector;
        [Inject] private InputSystemConfig tscConfig;

        // Legacy interface properties
        public InputType ActiveInput { get; private set; } = InputType.Keyboard;
        public InputAsset InputAsset => inputAsset;
        public Vector2 MovementValue { get; private set; }
        public JoystickBehavior Joystick => null; // Deprecated - always null
        public HighlightsParentBehavior Highlights => highlightsParent;

        public event UnityAction<InputType, InputType> onInputChanged;

        private InputSave save;

        #region Unity Lifecycle

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize InputAsset
            if (inputAsset == null)
            {
                inputAsset = new InputAsset();
            }

            GameController.RegisterInputManager(this);
            Debug.Log("[InputManager] TSC-powered InputManager initialized");
        }

        private void Start()
        {
            InitializeTSCSystem();
        }

        private void Update()
        {
            UpdateMovementValue();
            HandleSpaceKeyInput();
        }

        private void OnEnable()
        {
            inputAsset?.Enable();
        }

        private void OnDisable()
        {
            inputAsset?.Disable();
        }

        private void OnDestroy()
        {
            if (eventBus != null)
            {
                eventBus.OnDeviceChanged -= OnDeviceChanged;
                eventBus.OnMovementInput -= OnMovementInput;
            }
            inputAsset?.Dispose();
        }

        #endregion

        #region TSC System Integration

        private void InitializeTSCSystem()
        {
            // Load input configuration from CSV
            LoadInputConfiguration();

            // Initialize save system
            InitializeSave();

            // Subscribe to TSC events
            SubscribeToTSCEvents();

            // Set initial device based on TSC detection
            SetInitialInputDevice();

            Debug.Log("[InputManager] TSC System initialization completed");
        }

        private void LoadInputConfiguration()
        {
            try
            {
                // Ensure CSV configuration is loaded
                var configTask = CsvDataManager.Instance.LoadAsync<InputConfiguration>();
                configTask.Wait(); // Wait for configuration to load synchronously

                Debug.Log("[InputManager] Input configuration loaded from CSV");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InputManager] Failed to load input configuration: {ex.Message}");
            }
        }

        private void InitializeSave()
        {
            save = GameController.SaveManager.GetSave<InputSave>("Input");

            // Set initial input type from save or detect current device
            if (inputDetector != null)
            {
                ActiveInput = ConvertToLegacyInputType(inputDetector.CurrentDevice);
            }
            else
            {
                ActiveInput = save?.ActiveInput ?? InputType.Keyboard;
            }

            if (save != null)
            {
                save.ActiveInput = ActiveInput;
            }
        }

        private void SubscribeToTSCEvents()
        {
            if (eventBus != null)
            {
                eventBus.OnDeviceChanged += OnDeviceChanged;
                eventBus.OnMovementInput += OnMovementInput;
            }
        }

        private void SetInitialInputDevice()
        {
            // Set initial device based on current hardware
            if (Gamepad.current != null && tscConfig?.SupportGamepad == true)
            {
                ActiveInput = InputType.Gamepad;
            }
            else if (Keyboard.current != null && tscConfig?.SupportKeyboard == true)
            {
                ActiveInput = InputType.Keyboard;
            }
            else
            {
                ActiveInput = InputType.Keyboard; // Default fallback
            }

            UpdateHighlights();
        }

        #endregion

        #region TSC Event Handlers

        private void OnDeviceChanged(InputDeviceType newDeviceType)
        {
            var previousInput = ActiveInput;
            ActiveInput = ConvertToLegacyInputType(newDeviceType);

            // Update save
            if (save != null)
            {
                save.ActiveInput = ActiveInput;
            }

            UpdateHighlights();

            // Fire legacy event for backward compatibility
            onInputChanged?.Invoke(previousInput, ActiveInput);

            Debug.Log($"[InputManager] Device changed: {previousInput} -> {ActiveInput}");
        }

        private void OnMovementInput(Vector2 movement)
        {
            MovementValue = movement;
        }

        #endregion

        #region Input Handling

        private void UpdateMovementValue()
        {
            if (inputProcessor != null)
            {
                MovementValue = inputProcessor.GetMovementInput();
            }
            else
            {
                // Fallback to direct InputAsset reading
                MovementValue = inputAsset?.Gameplay.Movement.ReadValue<Vector2>() ?? Vector2.zero;
            }
        }

        private void HandleSpaceKeyInput()
        {
            // Handle Space key using TSC system
            if (inputProcessor?.GetActionInputDown("SpaceAction") == true)
            {
                OnSpaceKeyPressed();
            }
            
            // Fallback to direct InputAsset reading if TSC system not available
            else if (inputAsset?.Gameplay.SpaceAction.WasPressedThisFrame() == true)
            {
                OnSpaceKeyPressed();
            }
        }

        private void OnSpaceKeyPressed()
        {
            Debug.Log("[InputManager] Space key pressed");
            
            // Example implementation - customize based on your game needs
            HandleSpaceKeyAction();
        }

        private void HandleSpaceKeyAction()
        {
            // Add your space key functionality here
            // Examples:
            
            // 1. Pause/Resume game
            if (Time.timeScale > 0)
            {
                Time.timeScale = 0;
                Debug.Log("Game paused via Space key");
            }
            else
            {
                Time.timeScale = 1;
                Debug.Log("Game resumed via Space key");
            }

            // 2. Alternative: Trigger game event
            // GameEvents.SpaceKeyPressed?.Invoke();

            // 3. Alternative: Open quick menu
            // UIManager.ToggleQuickMenu();
        }

        #endregion

        #region Legacy Interface Methods

        public void Init()
        {
            // This method is called by GameController
            // TSC initialization happens in Start(), so we just ensure save is ready
            if (save == null)
            {
                InitializeSave();
            }
        }

        public void RegisterJoystick(JoystickBehavior joystick)
        {
            // Joystick system is deprecated - log warning and ignore
            Debug.LogWarning("[InputManager] Joystick system is deprecated. Ignoring joystick registration.");
        }

        public void RemoveJoystick()
        {
            // Joystick system is deprecated - no action needed
        }

        #endregion

        #region Utility Methods

        private InputType ConvertToLegacyInputType(InputDeviceType tscDeviceType)
        {
            return tscDeviceType switch
            {
                InputDeviceType.Keyboard => InputType.Keyboard,
                InputDeviceType.Gamepad => InputType.Gamepad,
                InputDeviceType.Touch => InputType.UIJoystick, // Legacy compatibility
                _ => InputType.Keyboard
            };
        }

        private void UpdateHighlights()
        {
            if (highlightsParent == null) return;

            // Show/hide highlights based on input type
            switch (ActiveInput)
            {
                case InputType.Keyboard:
                case InputType.Gamepad:
                    highlightsParent.EnableArrows();
                    break;
                case InputType.UIJoystick:
                    highlightsParent.DisableArrows();
                    break;
            }
        }

        #endregion

        #region Public API Extensions

        /// <summary>
        /// Check if a specific action was pressed this frame
        /// </summary>
        public bool GetActionDown(string actionName)
        {
            if (inputProcessor != null)
            {
                return inputProcessor.GetActionInputDown(actionName);
            }

            // Fallback to InputAsset
            var action = inputAsset?.UI.Get().FindAction(actionName);
            return action?.WasPressedThisFrame() ?? false;
        }

        /// <summary>
        /// Check if a specific action is currently held
        /// </summary>
        public bool GetAction(string actionName)
        {
            if (inputProcessor != null)
            {
                return inputProcessor.GetActionInput(actionName);
            }

            // Fallback to InputAsset
            var action = inputAsset?.UI.Get().FindAction(actionName);
            return action?.IsPressed() ?? false;
        }

        /// <summary>
        /// Check if a specific action was released this frame
        /// </summary>
        public bool GetActionUp(string actionName)
        {
            if (inputProcessor != null)
            {
                return inputProcessor.GetActionInputUp(actionName);
            }

            // Fallback to InputAsset
            var action = inputAsset?.UI.Get().FindAction(actionName);
            return action?.WasReleasedThisFrame() ?? false;
        }

        /// <summary>
        /// Check if Space key was pressed this frame
        /// </summary>
        public bool GetSpaceKeyDown()
        {
            return GetActionDown("SpaceAction");
        }

        /// <summary>
        /// Check if Space key is currently held
        /// </summary>
        public bool GetSpaceKey()
        {
            return GetAction("SpaceAction");
        }

        /// <summary>
        /// Check if Space key was released this frame
        /// </summary>
        public bool GetSpaceKeyUp()
        {
            return GetActionUp("SpaceAction");
        }

        #endregion
    }
}