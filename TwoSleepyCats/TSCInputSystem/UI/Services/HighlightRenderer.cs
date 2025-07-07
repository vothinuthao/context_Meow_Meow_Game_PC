// =============================================================================
// Two Sleepy Cats Studio - Input System Module
// 
// Author: Two Sleepy Cats Development Team
// Created: 2025
// 
// Sweet dreams and happy coding! 😸💤
// =============================================================================

using OctoberStudio.Pool;
using UnityEngine;

namespace TwoSleepyCats.TSCInputSystem
{
    public class HighlightRenderer : MonoBehaviour, IHighlightRenderer
    {
        [SerializeField] private GameObject defaultArrowPrefab;
        [SerializeField] private Transform arrowParent;
        
        private PoolComponent<RectTransform> arrowPool;
        private RectTransform leftArrow;
        private RectTransform rightArrow;
        private HighlightStyle currentStyle;
        
        private void Awake()
        {
            if (arrowParent == null)
                arrowParent = transform;
                
            InitializePool();
        }
        
        private void InitializePool()
        {
            var prefab = currentStyle?.arrowPrefab ?? defaultArrowPrefab;
            if (prefab != null)
            {
                arrowPool = new PoolComponent<RectTransform>(prefab, 2, arrowParent, true);
            }
        }
        
        public void ShowHighlight(Transform target)
        {
            if (target == null || arrowPool == null) return;
            
            // Get or create arrows
            if (leftArrow == null) leftArrow = arrowPool.GetEntity();
            if (rightArrow == null) rightArrow = arrowPool.GetEntity();
            
            // Position arrows
            PositionArrows(target);
            
            // Show arrows based on style
            if (currentStyle?.showArrows == true)
            {
                leftArrow.localScale = Vector3.one;
                rightArrow.localScale = new Vector3(-1, 1, 1);
            }
            else
            {
                leftArrow.localScale = Vector3.zero;
                rightArrow.localScale = Vector3.zero;
            }
        }
        
        public void HideHighlight()
        {
            if (leftArrow != null)
            {
                leftArrow.localScale = Vector3.zero;
                leftArrow.gameObject.SetActive(false);
                leftArrow = null;
            }
            
            if (rightArrow != null)
            {
                rightArrow.localScale = Vector3.zero;
                rightArrow.gameObject.SetActive(false);
                rightArrow = null;
            }
        }
        
        public void SetStyle(HighlightStyle style)
        {
            currentStyle = style;
            
            // Reinitialize pool if prefab changed
            if (style?.arrowPrefab != null && style.arrowPrefab != defaultArrowPrefab)
            {
                arrowPool = new PoolComponent<RectTransform>(style.arrowPrefab, 2, arrowParent, true);
            }
        }
        
        private void PositionArrows(Transform target)
        {
            if (!(target is RectTransform rectTarget)) return;
            
            // Set parent and reset transforms
            leftArrow.SetParent(rectTarget);
            rightArrow.SetParent(rectTarget);
            
            leftArrow.localPosition = Vector3.zero;
            rightArrow.localPosition = Vector3.zero;
            leftArrow.localRotation = Quaternion.identity;
            rightArrow.localRotation = Quaternion.identity;
            
            // Set anchors to left and right sides
            leftArrow.anchorMin = new Vector2(0, 0.5f);
            leftArrow.anchorMax = new Vector2(0, 0.5f);
            leftArrow.anchoredPosition = Vector2.zero;
            
            rightArrow.anchorMin = new Vector2(1, 0.5f);
            rightArrow.anchorMax = new Vector2(1, 0.5f);
            rightArrow.anchoredPosition = Vector2.zero;
            
            // Move to arrow parent for rendering
            leftArrow.SetParent(arrowParent);
            rightArrow.SetParent(arrowParent);
        }
    }
}