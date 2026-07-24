using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dylanng
{
    public enum ServiceScope
    {
        Global,
        Local
    }

    public static class ServiceLocator
    {
        private static readonly object _lock = new object();
        
        private static readonly Dictionary<Type, IService> _globalServices = new Dictionary<Type, IService>(32);
        private static readonly Dictionary<Scene, Dictionary<Type, IService>> _localServicesByScene = new Dictionary<Scene, Dictionary<Type, IService>>(4);

        static ServiceLocator()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
        
        public static void Register<T>(T service, ServiceScope scope = ServiceScope.Global) where T : IService
        {
            if (service == null)
            {
                GameLogger.LogError($"[ServiceLocator] Cannot register null service for type {typeof(T).Name}.");
                return;
            }

            if (scope == ServiceScope.Global)
            {
                RegisterGlobal<T>(service);
            }
            else
            {
                RegisterLocal<T>(service);
            }
        }

        public static void RegisterGlobal<T>(T service) where T : IService
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (!_globalServices.ContainsKey(type))
                {
                    _globalServices.Add(type, service);
                    GameLogger.Log($"[ServiceLocator] Registered Global Service: {type.Name}");
                }
                else
                {
                    GameLogger.LogError($"[ServiceLocator] Global Service {type.Name} is already registered.");
                }
            }
        }
        
        public static void RegisterLocal<T>(T service, Scene targetScene = default) where T : IService
        {
            lock (_lock)
            {
                var type = typeof(T);
                Scene scene = targetScene.IsValid() ? targetScene : ResolveSceneForService(service);

                if (!scene.IsValid())
                {
                    GameLogger.LogError($"[ServiceLocator] Cannot register Local Service {type.Name}: Invalid Target Scene.");
                    return;
                }

                if (!_localServicesByScene.TryGetValue(scene, out var sceneServices))
                {
                    sceneServices = new Dictionary<Type, IService>(8);
                    _localServicesByScene[scene] = sceneServices;
                }

                if (!sceneServices.ContainsKey(type))
                {
                    sceneServices.Add(type, service);
                    GameLogger.Log($"[ServiceLocator] Registered Local Service: {type.Name} in Scene '{scene.name}'");
                }
                else
                {
                    GameLogger.LogError($"[ServiceLocator] Local Service {type.Name} is already registered in Scene '{scene.name}'.");
                }
            }
        }

        public static void Unregister<T>() where T : IService
        {
            lock (_lock)
            {
                var type = typeof(T);

                if (_globalServices.Remove(type))
                {
                    GameLogger.Log($"[ServiceLocator] Unregistered Global Service: {type.Name}");
                    return;
                }
                
                var activeScene = SceneManager.GetActiveScene();
                if (_localServicesByScene.TryGetValue(activeScene, out var sceneServices) && sceneServices.Remove(type))
                {
                    GameLogger.Log($"[ServiceLocator] Unregistered Local Service: {type.Name} from Scene '{activeScene.name}'");
                    if (sceneServices.Count == 0)
                    {
                        _localServicesByScene.Remove(activeScene);
                    }
                }
            }
        }
        
        public static T Get<T>() where T : IService
        {
            lock (_lock)
            {
                var type = typeof(T);
                
                var activeScene = SceneManager.GetActiveScene();
                if (_localServicesByScene.TryGetValue(activeScene, out var sceneServices) &&
                    sceneServices.TryGetValue(type, out var localService))
                {
                    return (T)localService;
                }
                
                if (_globalServices.TryGetValue(type, out var globalService))
                {
                    return (T)globalService;
                }

                foreach (var dict in _localServicesByScene.Values)
                {
                    if (dict.TryGetValue(type, out var service))
                    {
                        return (T)service;
                    }
                }

                GameLogger.LogError($"[ServiceLocator] Service {type.Name} not found in Local (Scene '{activeScene.name}') or Global scope. Are you sure it's registered?");
                return default;
            }
        }

        private static void OnSceneUnloaded(Scene unloadedScene)
        {
            lock (_lock)
            {
                if (_localServicesByScene.TryGetValue(unloadedScene, out var sceneServices))
                {
                    foreach (var kvp in sceneServices)
                    {
                        if (kvp.Value is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }

                    int removedCount = sceneServices.Count;
                    sceneServices.Clear();
                    _localServicesByScene.Remove(unloadedScene);

                    GameLogger.Log($"[ServiceLocator] Auto-cleaned {removedCount} Local Service(s) for unloaded Scene '{unloadedScene.name}'.");
                }
            }
        }

        public static void ClearAllServices()
        {
            lock (_lock)
            {
                foreach (var sceneServices in _localServicesByScene.Values)
                {
                    foreach (var kvp in sceneServices)
                    {
                        if (kvp.Value is IDisposable disposable) disposable.Dispose();
                    }
                    sceneServices.Clear();
                }
                _localServicesByScene.Clear();

                foreach (var kvp in _globalServices)
                {
                    if (kvp.Value is IDisposable disposable) disposable.Dispose();
                }
                _globalServices.Clear();

                GameLogger.Log("[ServiceLocator] Cleared all Global and Local services.");
            }
        }

        private static Scene ResolveSceneForService<T>(T service)
        {
            if (service is Component component && component != null && component.gameObject.scene.IsValid())
            {
                return component.gameObject.scene;
            }
            return SceneManager.GetActiveScene();
        }
    }
}

