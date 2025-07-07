// =============================================================================
// Two Sleepy Cats Studio - Input System Module
// 
// Author: Two Sleepy Cats Development Team
// Created: 2025
// 
// Sweet dreams and happy coding! 😸💤
// =============================================================================

using OctoberStudio.Input;
using TwoSleepyCats.CSVReader.Core;
using UnityEngine;
using Zenject;

namespace TwoSleepyCats.TSCInputSystem
{
    public class InputSystemInstaller : MonoInstaller
    {
        [Header("Input System Setup")]
        [SerializeField] private InputAsset _inputAsset;
        [SerializeField] private bool preloadConfiguration = true;
        [SerializeField] private HighlightRenderer highlightRendererPrefab;
        
        public override void InstallBindings()
        {
            // Load configuration from CSV
            InstallConfiguration();
            
            // Core services
            InstallCoreServices();
            
            // Input strategies
            InstallStrategies();
            
            // UI integration
            InstallUIServices();
            
            // Initialization
            Container.BindInterfacesTo<InputSystemInitializer>()
                    .AsSingle()
                    .NonLazy();
        }
        
        private void InstallConfiguration()
        {
            if (preloadConfiguration)
            {
                // Preload CSV configuration
                Debug.Log("[InputSystemInstaller] Preloading CSV configuration...");
                var configTask = CsvDataManager.Instance.LoadAsync<InputConfiguration>();
                configTask.Wait(); // Wait for configuration to load
            }
            
            Container.Bind<InputSystemConfig>()
                    .FromMethod(InputSystemConfig.LoadFromCsv)
                    .AsSingle();
        }
        
        private void InstallCoreServices()
        {
            Container.Bind<IInputEventBus>()
                    .To<InputEventBus>()
                    .AsSingle();
                    
            Container.Bind<IInputDetector>()
                    .To<InputDetector>()
                    .AsSingle();
                    
            Container.Bind<IInputProcessor>()
                    .To<InputProcessor>()
                    .AsSingle();
        }
        
        private void InstallStrategies()
        {
            // Bind InputAsset instance
            Container.Bind<InputAsset>()
                    .FromInstance(_inputAsset)
                    .AsSingle();
            
            // Keyboard strategy
            Container.Bind<IInputStrategy>()
                    .To<KeyboardInputStrategy>()
                    .AsSingle()
                    .WithArguments(_inputAsset);
                    
            // Gamepad strategy
            Container.Bind<IInputStrategy>()
                    .To<GamepadInputStrategy>()
                    .AsSingle()
                    .WithArguments(_inputAsset);
                    
            // Touch strategy
            Container.Bind<IInputStrategy>()
                    .To<TouchInputStrategy>()
                    .AsSingle()
                    .WithArguments(_inputAsset)
                    .When(context => {
                        var config = context.Container.Resolve<InputSystemConfig>();
                        return config.SupportTouch;
                    });
        }
        
        private void InstallUIServices()
        {
            Container.Bind<IUIInputHandler>()
                    .To<UIInputHandler>()
                    .AsSingle();
                    
            Container.Bind<IHighlightController>()
                    .To<HighlightController>()
                    .AsSingle();
                    
            // Highlight renderer
            if (highlightRendererPrefab != null)
            {
                Container.Bind<IHighlightRenderer>()
                        .FromComponentInNewPrefab(highlightRendererPrefab)
                        .AsSingle();
            }
            else
            {
                Container.Bind<IHighlightRenderer>()
                        .To<HighlightRenderer>()
                        .FromNewComponentOnNewGameObject()
                        .AsSingle();
            }
        }
    }
    
    
}