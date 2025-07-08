// =============================================================================
// Two Sleepy Cats Studio - Professional CSV Reader Module
// 
// Author: Two Sleepy Cats Development Team
// Created: 2025
// 
// Sweet dreams and happy coding! 😸💤
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwoSleepyCats.CSVReader.Models;
using TwoSleepyCats.CSVReader.Utils;
using TwoSleepyCats.Patterns.Singleton;
using UnityEngine;

namespace TwoSleepyCats.CSVReader.Core
{
    public class CsvDataManager : Singleton<CsvDataManager>
    {
        private readonly Dictionary<Type, object> _dataCache = new Dictionary<Type, object>();
        private readonly Dictionary<Type, CsvErrorCollection> _errorCache = new Dictionary<Type, CsvErrorCollection>();
        private readonly object _cacheLock = new object();
        private readonly AdvancedCacheManager _advancedCache = new AdvancedCacheManager();
        private readonly Dictionary<Type, CsvLoadingProgress> _loadingProgress = new Dictionary<Type, CsvLoadingProgress>();
        
        public event Action<CsvLoadingProgress> OnLoadingProgress;
        public event Action<Type, List<object>, CsvErrorCollection> OnDataLoaded;
        protected override void Initialize()
        {
            
        }
        // ReSharper disable Unity.PerformanceAnalysis
        public Task<List<T>> LoadAsync<T>() where T : ICsvModel, new()
        {
            var type = typeof(T);
            
            lock (_cacheLock)
            {
                if (_dataCache.TryGetValue(type, out var value))
                {
                    return Task.FromResult(value as List<T>);
                }
            }

            // Load data
            var (data, errors) = CsvReader<T>.LoadSync();

            lock (_cacheLock)
            {
                _dataCache[type] = data;
                _errorCache[type] = errors;
            }

            // Log errors
            if (errors.Errors.Count > 0)
            {
                Debug.LogWarning($"[CSVDataManager] Loaded {typeof(T).Name} with {errors.Errors.Count} issues:");
                errors.LogToConsole();
            }
            else
            {
                Debug.Log($"[CSVDataManager] Successfully loaded {data.Count} {typeof(T).Name} records");
            }

            return Task.FromResult(data);
        }

        public List<T> Get<T>() where T : ICsvModel, new()
        {
            var type = typeof(T);
            
            lock (_cacheLock)
            {
                if (_dataCache.TryGetValue(type, out var value))
                {
                    return value as List<T>;
                }
            }
            Debug.LogWarning($"[CSVDataManager] {typeof(T).Name} not loaded yet. Consider using LoadAsync first.");
            var task = LoadAsync<T>();
            task.Wait();
            return task.Result;
        }
        public bool IsLoaded<T>() where T : ICsvModel
        {
            lock (_cacheLock)
            {
                return _dataCache.ContainsKey(typeof(T));
            }
        }

        public CsvError[] GetErrors<T>() where T : ICsvModel
        {
            lock (_cacheLock)
            {
                var type = typeof(T);
                if (_errorCache.TryGetValue(type, out var value))
                {
                    return value.Errors.ToArray();
                }
                return Array.Empty<CsvError>();
            }
        }

        /// <summary>
        /// Preload all known CSV model types (Phase 1 method)
        /// </summary>
        public async Task PreloadAllAsync()
        {
            var csvModelTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(ICsvModel).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                .ToArray();

            Debug.Log($"[CSVDataManager] Preloading {csvModelTypes.Length} CSV model types...");

            var tasks = new List<Task>();
            foreach (var type in csvModelTypes)
            {
                var method = typeof(CsvDataManager).GetMethod(nameof(LoadAsync))?.MakeGenericMethod(type);
                if (method == null) continue;
                var task = (Task)method.Invoke(this, null);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
            Debug.Log("[CSVDataManager] Preloading completed");
        }

        public async Task<List<T>> LoadWithRelationshipsAsync<T>(IProgress<CsvLoadingProgress> progress = null) 
            where T : ICsvModel, new()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var type = typeof(T);
            if (_advancedCache.Contains(type.Name))
            {
                stopwatch.Stop();
                return _advancedCache.Get<List<T>>(type.Name);
            }
            
            try
            {
                var data = await LoadAsync<T>();
                var progressInfo = new CsvLoadingProgress
                {
                    FileName = new T().GetCsvFileName(),
                    TotalRows = data.Count,
                    ProcessedRows = 0,
                    Status = "Resolving relationships..."
                };
                
                progress?.Report(progressInfo);
                OnLoadingProgress?.Invoke(progressInfo);
                var context = new CsvRelationshipContext();
                await CsvRelationshipResolver.ResolveRelationshipsAsync(data, context);
                
                progressInfo.ProcessedRows = data.Count;
                progressInfo.Status = "Complete";
                progressInfo.ElapsedTime = stopwatch.Elapsed;
                
                progress?.Report(progressInfo);
                OnLoadingProgress?.Invoke(progressInfo);
                stopwatch.Stop();
                _advancedCache.Set(type.Name, data, stopwatch.Elapsed);
                OnDataLoaded?.Invoke(type, data.Cast<object>().ToList(), GetErrorCollection<T>());
                
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CSVDataManager] Failed to load {typeof(T).Name} with relationships: {ex.Message}");
                throw;
            }
        }
        
        public async Task PreloadWithProgressAsync(IProgress<string> progress = null, params Type[] types)
        {
            if (types == null || types.Length == 0)
            {
                types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(type => typeof(ICsvModel).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    .ToArray();
            }
            
            progress?.Report($"Preloading {types.Length} CSV types...");
            
            var tasks = new List<Task>();
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                progress?.Report($"Loading {type.Name} ({i + 1}/{types.Length})...");
                
                var method = typeof(CsvDataManager).GetMethod(nameof(LoadWithRelationshipsAsync))?.MakeGenericMethod(type);
                if (method != null)
                {
                    var task = (Task)method.Invoke(this, new object[] { null });
                    tasks.Add(task);
                }
                await Task.Delay(10);
            }
            
            await Task.WhenAll(tasks);
            progress?.Report("Preloading completed!");
        }
        public CsvCacheStats GetCacheStats()
        {
            return _advancedCache.GetStats();
        }
        public CsvLoadingProgress GetLoadingProgress<T>() where T : ICsvModel
        {
            var type = typeof(T);
            return _loadingProgress.TryGetValue(type, out var value) ? value : null;
        }
        
        private CsvErrorCollection GetErrorCollection<T>() where T : ICsvModel
        {
            var errors = GetErrors<T>();
            var collection = new CsvErrorCollection();
            foreach (var error in errors)
            {
                collection.AddError(error);
            }
            return collection;
        }
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _dataCache.Clear();
                _errorCache.Clear();
            }
            _advancedCache.Clear();
            _loadingProgress.Clear();
            
            Debug.Log("[CSVDataManager] All caches cleared (basic + advanced)");
        }

        public override void Dispose()
        {
            ClearCache();
            base.Dispose();
        }
        public string GetCacheInfo()
        {
            var info = new System.Text.StringBuilder();
            
            lock (_cacheLock)
            {
                info.AppendLine($"Basic Cache Entries: {_dataCache.Count}");
                info.AppendLine($"Error Cache Entries: {_errorCache.Count}");
            }
            
            var advancedStats = _advancedCache.GetStats();
            info.AppendLine($"Advanced Cache Entries: {advancedStats.TotalEntries}");
            info.AppendLine($"Cache Hit Rate: {advancedStats.HitRate:P1}");
            info.AppendLine($"Memory Usage: {advancedStats.MemoryUsageBytes / 1024 / 1024:F2} MB");
            
            return info.ToString();
        }

        /// <summary>
        /// Check if any data is loaded
        /// </summary>
        public bool HasAnyData()
        {
            lock (_cacheLock)
            {
                return _dataCache.Count > 0;
            }
        }

        /// <summary>
        /// Get all loaded type names
        /// </summary>
        public string[] GetLoadedTypeNames()
        {
            lock (_cacheLock)
            {
                return _dataCache.Keys.Select(t => t.Name).ToArray();
            }
        }

        /// <summary>
        /// Force reload specific type (clears cache and reloads)
        /// </summary>
        public async Task<List<T>> ForceReloadAsync<T>() where T : ICsvModel, new()
        {
            var type = typeof(T);
            
            // Clear from all caches
            lock (_cacheLock)
            {
                _dataCache.Remove(type);
                _errorCache.Remove(type);
            }
            _advancedCache.Remove(type.Name);
            _loadingProgress.Remove(type);
            
            // Reload
            return await LoadAsync<T>();
        }
    }
}