// =============================================================================
// Two Sleepy Cats Studio - Input System Module
// 
// Author: Two Sleepy Cats Development Team
// Created: 2025
// 
// Sweet dreams and happy coding! 😸💤
// =============================================================================

namespace TwoSleepyCats.TSCInputSystem
{
    public enum InputDeviceType
    {
        None = 0,
        Keyboard = 1,
        Gamepad = 2,
        Touch = 4
    }
    
    public enum InputActionType
    {
        Started,
        Performed,
        Canceled
    }
}