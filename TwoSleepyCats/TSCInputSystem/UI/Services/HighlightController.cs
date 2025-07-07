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
    public class HighlightController : IHighlightController
    {
        public IHighlightable CurrentHighlight { get; private set; }
        
        private readonly IHighlightRenderer _renderer;
        private readonly InputSystemConfig _config;
        
        private HighlightStyle _currentStyle;
        private bool _highlightEnabled = true;
        
        public HighlightController(IHighlightRenderer highlightRenderer, InputSystemConfig configuration)
        {
            _renderer = highlightRenderer;
            _config = configuration;
            
            // Initialize default style
            _currentStyle = new HighlightStyle
            {
                showArrows = true,
                highlightColor = Color.white,
                animationSpeed = _config.HighlightAnimationSpeed
            };
            
            _renderer.SetStyle(_currentStyle);
            Debug.Log("[HighlightController] Initialized");
        }
        
        public void HighlightElement(IHighlightable element)
        {
            if (!_highlightEnabled || element == null || !element.CanHighlight)
                return;
            
            // Clear previous highlight
            ClearHighlight();
            
            // Set new highlight
            CurrentHighlight = element;
            element.OnHighlighted();
            
            if (_config.ShowHighlights)
            {
                _renderer.ShowHighlight(element.Transform);
            }
        }
        
        public void ClearHighlight()
        {
            if (CurrentHighlight != null)
            {
                CurrentHighlight.OnUnhighlighted();
                CurrentHighlight = null;
            }
            
            _renderer.HideHighlight();
        }
        
        public void RefreshHighlight()
        {
            if (CurrentHighlight != null && _highlightEnabled && _config.ShowHighlights)
            {
                _renderer.ShowHighlight(CurrentHighlight.Transform);
            }
        }
        
        public void SetHighlightStyle(HighlightStyle style)
        {
            _currentStyle = style;
            _renderer.SetStyle(style);
        }
        
        public void SetHighlightEnabled(bool enabled)
        {
            _highlightEnabled = enabled;
            
            if (!enabled)
            {
                ClearHighlight();
            }
        }
    }
}