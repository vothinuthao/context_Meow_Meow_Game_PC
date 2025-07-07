// =============================================================================
// Two Sleepy Cats Studio - Input System Module
// 
// Author: Two Sleepy Cats Development Team
// Created: 2025
// 
// Sweet dreams and happy coding! 😸💤
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TwoSleepyCats.TSCInputSystem
{
    public class InputDetector : IInputDetector, IDisposable
    {
        public InputDeviceType CurrentDevice { get; private set; }
        public event Action<InputDeviceType> OnDeviceChanged;
        
        private readonly InputSystemConfig _config;
        private readonly List<IInputStrategy> _strategies;
        private readonly Dictionary<InputDeviceType, float> _deviceActivity;
        
        private bool _isDetecting;
        private float _lastDetectionTime;
        
        public InputDetector(InputSystemConfig configuration, List<IInputStrategy> inputStrategies)
        {
            _config = configuration;
            _strategies = inputStrategies;
            _deviceActivity = new Dictionary<InputDeviceType, float>();
            
            InitializeDeviceActivity();
        }
        
        private void InitializeDeviceActivity()
        {
            foreach (var strategy in _strategies)
            {
                _deviceActivity[strategy.DeviceType] = 0f;
            }
            
            CurrentDevice = _config.DefaultDevice;
        }
        
        public void StartDetection()
        {
            if (_isDetecting) return;
            
            _isDetecting = true;
            
            // Subscribe to Unity Input System device events
            InputSystem.onDeviceChange += OnInputDeviceChange;
        }
        
        public void StopDetection()
        {
            if (!_isDetecting) return;
            
            _isDetecting = false;
            InputSystem.onDeviceChange -= OnInputDeviceChange;
        }
        
        public bool IsDeviceActive(InputDeviceType deviceType)
        {
            return _deviceActivity.ContainsKey(deviceType) && 
                   _deviceActivity[deviceType] > _config.DetectionSensitivity;
        }
        
        public void Update()
        {
            if (!_isDetecting || !_config.AutoSwitchDevices) return;
            
            if (Time.time - _lastDetectionTime < _config.DetectionSensitivity) return;
            
            var mostActiveDevice = DetectMostActiveDevice();
            if (mostActiveDevice != CurrentDevice && mostActiveDevice != InputDeviceType.None)
            {
                SwitchToDevice(mostActiveDevice);
            }
            
            _lastDetectionTime = Time.time;
        }
        
        private InputDeviceType DetectMostActiveDevice()
        {
            var maxActivity = 0f;
            var mostActiveDevice = InputDeviceType.None;
            
            foreach (var strategy in _strategies)
            {
                if (!strategy.IsActive) continue;
                
                var movement = strategy.GetMovement();
                var activity = movement.magnitude;
                
                // Check for any action inputs
                var actionActivity = CheckActionActivity(strategy);
                activity = Mathf.Max(activity, actionActivity);
                
                _deviceActivity[strategy.DeviceType] = activity;
                
                if (activity > maxActivity && activity > _config.DetectionSensitivity)
                {
                    maxActivity = activity;
                    mostActiveDevice = strategy.DeviceType;
                }
            }
            
            return mostActiveDevice;
        }
        
        private float CheckActionActivity(IInputStrategy strategy)
        {
            // Check common actions for activity
            string[] commonActions = { "Submit", "Cancel", "Navigate", "Click" };
            
            foreach (var action in commonActions)
            {
                if (strategy.GetAction(action) || strategy.GetActionDown(action))
                {
                    return 1f; // Max activity for action input
                }
            }
            
            return 0f;
        }
        
        private void SwitchToDevice(InputDeviceType newDevice)
        {
            var previousDevice = CurrentDevice;
            CurrentDevice = newDevice;
            OnDeviceChanged?.Invoke(newDevice);
        }
        
        private void OnInputDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                    HandleDeviceAdded(device);
                    break;
                case InputDeviceChange.Removed:
                    HandleDeviceRemoved(device);
                    break;
            }
        }
        
        private void HandleDeviceAdded(InputDevice device)
        {
            var deviceType = GetDeviceType(device);
            Debug.Log($"[InputDetector] Device added: {device.name} ({deviceType})");
            
            if (deviceType != InputDeviceType.None && _config.AutoSwitchDevices)
            {
                SwitchToDevice(deviceType);
            }
        }
        
        private void HandleDeviceRemoved(InputDevice device)
        {
            var deviceType = GetDeviceType(device);
            Debug.Log($"[InputDetector] Device removed: {device.name} ({deviceType})");
            
            if (deviceType == CurrentDevice)
            {
                // Switch to default or first available device
                var availableDevice = GetFirstAvailableDevice();
                if (availableDevice != InputDeviceType.None)
                {
                    SwitchToDevice(availableDevice);
                }
            }
        }
        
        private InputDeviceType GetDeviceType(InputDevice device)
        {
            return device switch
            {
                Keyboard => InputDeviceType.Keyboard,
                Mouse => InputDeviceType.Keyboard,
                Gamepad => InputDeviceType.Gamepad,
                Touchscreen => InputDeviceType.Touch,
                _ => InputDeviceType.None
            };
        }
        
        private InputDeviceType GetFirstAvailableDevice()
        {
            return _strategies.FirstOrDefault(s => s.IsActive)?.DeviceType ?? _config.DefaultDevice;
        }
        
        public void Dispose()
        {
            StopDetection();
        }
    }
}