// =============================================================================
// Two Sleepy Cats Studio - Input System Module
// 
// Author: Two Sleepy Cats Development Team
// Created: 2025
// 
// Sweet dreams and happy coding! 😸💤
// =============================================================================

using OctoberStudio.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TwoSleepyCats.TSCInputSystem
{
    public class GamepadInputStrategy : IInputStrategy
    {
        public InputDeviceType DeviceType => InputDeviceType.Gamepad;
        public bool IsActive => Gamepad.current != null;
        
        private readonly InputAsset _inputAsset;
        private InputAsset.UIActions _uiActions;
        private InputAsset.GameplayActions _gameplayActions;
        private InputAsset.GamepadDetectionActions _detectionActions;
        
        public GamepadInputStrategy(InputAsset asset)
        {
            _inputAsset = asset;
        }
        
        public bool CanHandle(InputDeviceType deviceType)
        {
            return deviceType == InputDeviceType.Gamepad;
        }
        
        public Vector2 GetMovement()
        {
            return _gameplayActions.Movement.ReadValue<Vector2>();
        }
        
        public bool GetAction(string actionName)
        {
            var action = FindAction(actionName);
            return action?.IsPressed() ?? false;
        }
        
        public bool GetActionDown(string actionName)
        {
            var action = FindAction(actionName);
            return action?.WasPressedThisFrame() ?? false;
        }
        
        public bool GetActionUp(string actionName)
        {
            var action = FindAction(actionName);
            return action?.WasReleasedThisFrame() ?? false;
        }
        
        public void Initialize()
        {
            if (_inputAsset == null) return;
            
            _uiActions = _inputAsset.UI;
            _gameplayActions = _inputAsset.Gameplay;
            _detectionActions = _inputAsset.GamepadDetection;
            
            _uiActions.Enable();
            _gameplayActions.Enable();
            _detectionActions.Enable();
            
            Debug.Log("[GamepadInputStrategy] Initialized");
        }
        
        public void Cleanup()
        {
            _uiActions.Disable();
            _gameplayActions.Disable();
            _detectionActions.Disable();
        }
        
        public void Update()
        {
            // Handle gamepad-specific logic like vibration, battery level, etc.
        }
        
        private InputAction FindAction(string actionName)
        {
            // Try UI actions first
            var uiAction = _uiActions.Get().FindAction(actionName);
            if (uiAction != null) return uiAction;
            
            // Then try gameplay actions
            var gameplayAction = _gameplayActions.Get().FindAction(actionName);
            if (gameplayAction != null) return gameplayAction;
            
            // Finally try detection actions
            var detectionAction = _detectionActions.Get().FindAction(actionName);
            return detectionAction;
        }
    }
}