// =============================================================================
// Two Sleepy Cats Studio - Input System Module
// 
// Author: Two Sleepy Cats Development Team
// Created: 2025
// 
// Sweet dreams and happy coding! 😸💤
// =============================================================================

using UnityEngine;
using UnityEngine.EventSystems;

namespace TwoSleepyCats.TSCInputSystem
{
    public class UIInputHandler : IUIInputHandler
    {
        public bool IsNavigationEnabled { get; set; } = true;
        
        private readonly IInputEventBus _eventBus;
        private readonly IHighlightController _highlightController;
        private readonly InputSystemConfig _config;
        
        private UINavigationMode _navigationMode = UINavigationMode.Automatic;
        private float _lastNavigationTime;
        
        public UIInputHandler(IInputEventBus inputEventBus, IHighlightController highlight, InputSystemConfig configuration)
        {
            _eventBus = inputEventBus;
            _highlightController = highlight;
            _config = configuration;
            _eventBus.OnMovementInput += OnMovementInput;
            _eventBus.OnActionInputDown += OnActionInputDown;
            _eventBus.OnDeviceChanged += OnDeviceChanged;
        }
        
        private void OnMovementInput(Vector2 direction)
        {
            if (IsNavigationEnabled && CanNavigate())
            {
                HandleNavigation(direction);
            }
        }
        
        private void OnActionInputDown(string actionName)
        {
            if (!IsNavigationEnabled) return;
            
            switch (actionName)
            {
                case "Submit":
                    HandleSubmit();
                    break;
                case "Cancel":
                    HandleCancel();
                    break;
                case "Back":
                    HandleBack();
                    break;
            }
        }
        
        private void OnDeviceChanged(InputDeviceType deviceType)
        {
            bool showHighlights = deviceType != InputDeviceType.Touch && _config.ShowHighlights;
            _highlightController.SetHighlightEnabled(showHighlights);
        }
        
        public void HandleNavigation(Vector2 direction)
        {
            if (!CanNavigate()) return;
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return;
            
            var currentSelected = eventSystem.currentSelectedGameObject;
            if (currentSelected == null)
            {
                SelectFirstAvailable();
                return;
            }
            var axisEventData = new AxisEventData(eventSystem)
            {
                moveVector = direction,
                moveDir = GetMoveDirection(direction)
            };
            ExecuteEvents.Execute(currentSelected, axisEventData, ExecuteEvents.moveHandler);
            _lastNavigationTime = Time.time;
        }
        
        public void HandleSubmit()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem?.currentSelectedGameObject != null)
            {
                var submitEventData = new BaseEventData(eventSystem);
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, submitEventData, ExecuteEvents.submitHandler);
            }
        }
        
        public void HandleCancel()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem?.currentSelectedGameObject != null)
            {
                var cancelEventData = new BaseEventData(eventSystem);
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, cancelEventData, ExecuteEvents.cancelHandler);
            }
        }
        
        public void HandleBack()
        {
            HandleCancel();
        }
        
        public void SetNavigationMode(UINavigationMode mode)
        {
            _navigationMode = mode;
        }
        
        public void FocusElement(IHighlightable element)
        {
            if (element?.Transform != null)
            {
                var selectable = element.Transform.GetComponent<UnityEngine.UI.Selectable>();
                if (selectable != null)
                {
                    selectable.Select();
                    _highlightController.HighlightElement(element);
                }
            }
        }
        
        private bool CanNavigate()
        {
            return Time.time - _lastNavigationTime >= _config.NavigationDelay;
        }
        
        private void SelectFirstAvailable()
        {
            var selectables = UnityEngine.UI.Selectable.allSelectablesArray;
            foreach (var selectable in selectables)
            {
                if (selectable.IsInteractable())
                {
                    selectable.Select();
                    break;
                }
            }
        }
        
        private MoveDirection GetMoveDirection(Vector2 direction)
        {
            var absX = Mathf.Abs(direction.x);
            var absY = Mathf.Abs(direction.y);
            
            if (absX > absY)
            {
                return direction.x > 0 ? MoveDirection.Right : MoveDirection.Left;
            }
            else
            {
                return direction.y > 0 ? MoveDirection.Up : MoveDirection.Down;
            }
        }
    }
}