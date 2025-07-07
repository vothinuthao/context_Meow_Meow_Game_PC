// =============================================================================
// Two Sleepy Cats Studio - Input System Module
// 
// Author: Two Sleepy Cats Development Team
// Created: 2025
// 
// Sweet dreams and happy coding! 😸💤
// =============================================================================

using UnityEngine;

namespace TwoSleepyCats.TSCInputSystem
{
    public interface IInputStrategy
    {
        InputDeviceType DeviceType { get; }
        bool IsActive { get; }
        
        bool CanHandle(InputDeviceType deviceType);
        Vector2 GetMovement();
        bool GetAction(string actionName);
        bool GetActionDown(string actionName);
        bool GetActionUp(string actionName);
        
        void Initialize();
        void Cleanup();
        void Update();
    }
}