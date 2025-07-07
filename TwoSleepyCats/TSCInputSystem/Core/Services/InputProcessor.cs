// =============================================================================
// Two Sleepy Cats Studio - Input System Module
// 
// Author: Two Sleepy Cats Development Team
// Created: 2025
// 
// Sweet dreams and happy coding! 😸💤
// =============================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TwoSleepyCats.TSCInputSystem
{
    public class InputProcessor : IInputProcessor
    {
        private IInputStrategy _activeStrategy;
        private readonly Dictionary<InputDeviceType, IInputStrategy> _strategyMap;
        private readonly InputSystemConfig _config;
        public InputProcessor(List<IInputStrategy> strategies, InputSystemConfig configuration)
        {
            _config = configuration;
            _strategyMap = strategies.ToDictionary(s => s.DeviceType);
            foreach (var strategy in strategies)
            {
                strategy.Initialize();
            }
        }
        
        public Vector2 GetMovementInput()
        {
            return _activeStrategy?.GetMovement() ?? Vector2.zero;
        }
        
        public bool GetActionInput(string actionName)
        {
            return _activeStrategy?.GetAction(actionName) ?? false;
        }
        
        public bool GetActionInputDown(string actionName)
        {
            return _activeStrategy?.GetActionDown(actionName) ?? false;
        }
        
        public bool GetActionInputUp(string actionName)
        {
            return _activeStrategy?.GetActionUp(actionName) ?? false;
        }
        
        public void SetActiveStrategy(IInputStrategy strategy)
        {
            if (_activeStrategy == strategy) return;
            
            _activeStrategy?.Cleanup();
            _activeStrategy = strategy;
            _activeStrategy?.Initialize();
        }
        
        public IInputStrategy GetActiveStrategy()
        {
            return _activeStrategy;
        }
        
        public void Update()
        {
            _activeStrategy?.Update();
        }
        
        public IInputStrategy GetStrategy(InputDeviceType deviceType)
        {
            return _strategyMap.GetValueOrDefault(deviceType);
        }
    }
}