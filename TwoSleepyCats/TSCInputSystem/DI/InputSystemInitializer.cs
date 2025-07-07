using System;
using System.Collections.Generic;
using Zenject;

namespace TwoSleepyCats.TSCInputSystem
{
    /// <summary>
    /// Initializes the input system after DI setup
    /// </summary>
    public class InputSystemInitializer : IInitializable, ITickable, IDisposable
    {
        private readonly IInputDetector _inputDetector;
        private readonly IInputProcessor _inputProcessor;
        private readonly IInputEventBus eventBus;
        private readonly List<IInputStrategy> strategies;
        private readonly InputSystemConfig config;
        
        public InputSystemInitializer(
            IInputDetector detector,
            IInputProcessor processor,
            IInputEventBus bus,
            List<IInputStrategy> inputStrategies,
            InputSystemConfig configuration)
        {
            _inputDetector = detector;
            _inputProcessor = processor;
            eventBus = bus;
            strategies = inputStrategies;
            config = configuration;
        }
        
        public void Initialize()
        {
            _inputDetector.OnDeviceChanged += OnDeviceChanged;
            _inputDetector.StartDetection();
            var initialStrategy = strategies.Find(s => s.DeviceType == config.DefaultDevice);
            if (initialStrategy != null)
            {
                _inputProcessor.SetActiveStrategy(initialStrategy);
            }
        }
        
        public void Tick()
        {
            _inputDetector.Update();
            _inputProcessor.Update();
            
            // Publish input events
            PublishInputEvents();
        }
        
        private void PublishInputEvents()
        {
            var movement = _inputProcessor.GetMovementInput();
            if (movement.magnitude > 0.1f)
            {
                eventBus.PublishMovement(movement);
            }
            
            // Check for common actions
            CheckAndPublishAction("Submit");
            CheckAndPublishAction("Cancel");
            CheckAndPublishAction("Back");
            CheckAndPublishAction("Navigate");
        }
        
        private void CheckAndPublishAction(string actionName)
        {
            if (_inputProcessor.GetActionInputDown(actionName))
            {
                eventBus.PublishAction(actionName, InputActionType.Started);
            }
            else if (_inputProcessor.GetActionInput(actionName))
            {
                eventBus.PublishAction(actionName, InputActionType.Performed);
            }
            else if (_inputProcessor.GetActionInputUp(actionName))
            {
                eventBus.PublishAction(actionName, InputActionType.Canceled);
            }
        }
        
        private void OnDeviceChanged(InputDeviceType deviceType)
        {
            var strategy = strategies.Find(s => s.DeviceType == deviceType);
            if (strategy != null)
            {
                _inputProcessor.SetActiveStrategy(strategy);
            }
            
            eventBus.PublishDeviceChange(deviceType);
        }
        
        public void Dispose()
        {
            _inputDetector.OnDeviceChanged -= OnDeviceChanged;
            _inputDetector.StopDetection();
        }
    }
}