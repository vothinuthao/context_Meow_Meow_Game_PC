// =============================================================================
// Two Sleepy Cats Studio - Input System Module
// 
// Author: Two Sleepy Cats Development Team
// Created: 2025
// 
// Sweet dreams and happy coding! 😸💤
// =============================================================================

using System;

namespace TwoSleepyCats.TSCInputSystem
{
    public interface IInputDetector
    {
        InputDeviceType CurrentDevice { get; }
        event Action<InputDeviceType> OnDeviceChanged;
        void StartDetection();
        void StopDetection();
        bool IsDeviceActive(InputDeviceType deviceType);
        void Update();
    }
}