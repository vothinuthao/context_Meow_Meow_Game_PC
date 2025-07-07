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
    public interface IHighlightController
    {
        IHighlightable CurrentHighlight { get; }
        
        void HighlightElement(IHighlightable element);
        void ClearHighlight();
        void RefreshHighlight();
        
        void SetHighlightStyle(HighlightStyle style);
        void SetHighlightEnabled(bool enabled);
    }
    
    public interface IHighlightable
    {
        Transform Transform { get; }
        bool CanHighlight { get; }
        Bounds GetBounds();
        
        void OnHighlighted();
        void OnUnhighlighted();
        void OnSelected();
    }
    
    public interface IHighlightRenderer
    {
        void ShowHighlight(Transform target);
        void HideHighlight();
        void SetStyle(HighlightStyle style);
    }
    
    [System.Serializable]
    public class HighlightStyle
    {
        public bool showArrows = true;
        public bool showOutline = false;
        public bool showGlow = false;
        public Color highlightColor = Color.white;
        public float animationSpeed = 1f;
        public GameObject arrowPrefab;
    }
}