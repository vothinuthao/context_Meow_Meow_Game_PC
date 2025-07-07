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
    public interface IUIInputHandler
    {
        bool IsNavigationEnabled { get; set; }
        
        void HandleNavigation(Vector2 direction);
        void HandleSubmit();
        void HandleCancel();
        void HandleBack();
        
        void SetNavigationMode(UINavigationMode mode);
        void FocusElement(IHighlightable element);
    }
    
    public enum UINavigationMode
    {
        Automatic,      // Auto-detect best navigation
        Grid,           // Grid-based navigation
        List,           // List-based navigation
        Manual          // Manual control
    }
}