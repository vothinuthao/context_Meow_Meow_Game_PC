// =============================================================================
// Two Sleepy Cats Studio - Input System Module
// 
// Author: Two Sleepy Cats Development Team
// Created: 2025
// 
// Sweet dreams and happy coding! 😸💤
// =============================================================================

using System;
using UnityEngine;

namespace TwoSleepyCats.TSCInputSystem
{
    public interface IInputEventBus
    {
        // Movement events
        event Action<Vector2> OnMovementInput;
        event Action OnMovementStarted;
        event Action OnMovementStopped;
        
        // Action events
        event Action<string> OnActionInput;
        event Action<string> OnActionInputDown;
        event Action<string> OnActionInputUp;
        
        // Device events
        event Action<InputDeviceType> OnDeviceChanged;
        event Action<InputDeviceType> OnDeviceConnected;
        event Action<InputDeviceType> OnDeviceDisconnected;
        
        // Publishing methods
        void PublishMovement(Vector2 movement);
        void PublishAction(string actionName, InputActionType actionType);
        void PublishDeviceChange(InputDeviceType deviceType);
    }
}