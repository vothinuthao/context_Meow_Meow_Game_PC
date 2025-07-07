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
    public class InputEventBus : IInputEventBus
    {
        public event Action<Vector2> OnMovementInput;
        public event Action OnMovementStarted;
        public event Action OnMovementStopped;
        
        public event Action<string> OnActionInput;
        public event Action<string> OnActionInputDown;
        public event Action<string> OnActionInputUp;
        
        public event Action<InputDeviceType> OnDeviceChanged;
        public event Action<InputDeviceType> OnDeviceConnected;
        public event Action<InputDeviceType> OnDeviceDisconnected;
        
        private Vector2 _lastMovement;
        private bool _wasMoving;
        
        public void PublishMovement(Vector2 movement)
        {
            var isMoving = movement.magnitude > 0.1f;
            
            if (isMoving && !_wasMoving)
            {
                OnMovementStarted?.Invoke();
                _wasMoving = true;
            }
            else if (!isMoving && _wasMoving)
            {
                OnMovementStopped?.Invoke();
                _wasMoving = false;
            }
            
            if (isMoving)
            {
                OnMovementInput?.Invoke(movement);
            }
            
            _lastMovement = movement;
        }
        
        public void PublishAction(string actionName, InputActionType actionType)
        {
            switch (actionType)
            {
                case InputActionType.Started:
                    OnActionInputDown?.Invoke(actionName);
                    break;
                case InputActionType.Performed:
                    OnActionInput?.Invoke(actionName);
                    break;
                case InputActionType.Canceled:
                    OnActionInputUp?.Invoke(actionName);
                    break;
            }
        }
        
        public void PublishDeviceChange(InputDeviceType deviceType)
        {
            OnDeviceChanged?.Invoke(deviceType);
        }
        
        public void PublishDeviceConnected(InputDeviceType deviceType)
        {
            OnDeviceConnected?.Invoke(deviceType);
        }
        
        public void PublishDeviceDisconnected(InputDeviceType deviceType)
        {
            OnDeviceDisconnected?.Invoke(deviceType);
        }
    }
}