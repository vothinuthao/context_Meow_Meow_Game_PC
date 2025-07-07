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
    public interface IInputProcessor
    {
        Vector2 GetMovementInput();
        bool GetActionInput(string actionName);
        bool GetActionInputDown(string actionName);
        bool GetActionInputUp(string actionName);
        
        void SetActiveStrategy(IInputStrategy strategy);
        IInputStrategy GetActiveStrategy();
        void Update();
    }
}