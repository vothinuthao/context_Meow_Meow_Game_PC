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
    public class KeyboardInputStrategy : IInputStrategy
    {
        public InputDeviceType DeviceType => InputDeviceType.Keyboard;
        public bool IsActive => Keyboard.current != null && Mouse.current != null;
        
        private readonly InputAsset _inputAsset;
        private InputAsset.UIActions _uiActions;
        private InputAsset.GameplayActions _gameplayActions;
        
        public KeyboardInputStrategy(InputAsset asset)
        {
            _inputAsset = asset;
        }
        
        public bool CanHandle(InputDeviceType deviceType)
        {
            return deviceType == InputDeviceType.Keyboard;
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
            
            _uiActions.Enable();
            _gameplayActions.Enable();
        }
        
        public void Cleanup()
        {
            _uiActions.Disable();
            _gameplayActions.Disable();
        }
        
        public void Update()
        {
            // Handle any per-frame keyboard-specific logic
        }
        
        private InputAction FindAction(string actionName)
        {
            // Try UI actions first
            var uiAction = _uiActions.Get().FindAction(actionName);
            if (uiAction != null) return uiAction;
            
            // Then try gameplay actions
            var gameplayAction = _gameplayActions.Get().FindAction(actionName);
            return gameplayAction;
        }
    }
}