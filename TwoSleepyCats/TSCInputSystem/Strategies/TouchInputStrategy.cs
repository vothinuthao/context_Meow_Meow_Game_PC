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
    public interface IJoystickProvider
    {
        Vector2 GetJoystickValue();
        bool IsJoystickActive();
    }
    
    public class TouchInputStrategy : IInputStrategy
    {
        public InputDeviceType DeviceType => InputDeviceType.Touch;
        public bool IsActive => Touchscreen.current != null;
        
        private readonly IJoystickProvider _joystickProvider;
        private readonly InputAsset _inputAsset;
        
        public TouchInputStrategy(InputAsset asset, IJoystickProvider provider = null)
        {
            _inputAsset = asset;
            _joystickProvider = provider;
        }
        
        public bool CanHandle(InputDeviceType deviceType)
        {
            return deviceType == InputDeviceType.Touch;
        }
        
        public Vector2 GetMovement()
        {
            if (_joystickProvider != null && _joystickProvider.IsJoystickActive())
            {
                return _joystickProvider.GetJoystickValue();
            }
            return _inputAsset?.UI.Point.ReadValue<Vector2>() ?? Vector2.zero;
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
            Debug.Log("[TouchInputStrategy] Initialized");
        }
        
        public void Cleanup()
        {
            Debug.Log("[TouchInputStrategy] Cleaned up");
        }
        
        public void Update()
        {
            // Handle touch-specific logic
        }
        
        private InputAction FindAction(string actionName)
        {
            // For touch, primarily use UI actions
            return _inputAsset?.UI.Get().FindAction(actionName);
        }
    }
}