using System;
using System.Collections.Generic;
using UnityEngine;
using Dylanng.Core.Base;
using Dylanng.Core.UI;
using Dylanng.Dylanng_EventBus;
using Dylanng.Events.SystemEvents;
using Game.UIs.Screens.Explore;

namespace Dylanng.Core.Managers
{
    public class UIManager : ManagerBase
    {
        [Header("--- LAYER MANAGER ----")]
        [SerializeField] protected Transform screenLayer;
        [SerializeField] protected Transform popupLayer;

        [Header("--- UI DATABASE ---")]
        [SerializeField] private UiDatabaseSO uiDB;
        
        [Space (10)]
        [Header("--- EDITOR SETTINGS ---")]
        [SerializeField] protected bool enableLog;
        
        //------------------------------------------------------------------------------------------------------------//
        // Screen Stack
        protected Stack<UIScreen> _screenStack;
        protected Stack<UIScreen> ScreenStack => _screenStack ??= new Stack<UIScreen>();

        // Popup Stack
        protected Stack<UIPopup> _popupStack;
        protected Stack<UIPopup> PopupStack => _popupStack ??= new Stack<UIPopup>();

        // Popup Queue
        protected Queue<Action> _popupQueue;
        protected Queue<Action> PopupQueue => _popupQueue ??= new Queue<Action>();

        // UI Prefabs
        protected Dictionary<Type, UIBase> _uiPrefabs;
        protected Dictionary<Type, UIBase> UIPrefabs => _uiPrefabs ??= BuildPrefabDictionary();

        // UI Instances
        protected Dictionary<Type, UIBase> _uiInstances;
        protected Dictionary<Type, UIBase> UIInstances => _uiInstances ??= new Dictionary<Type, UIBase>();

        //------------------------------------------------------------------------------------------------------------//
        
        public override void Initialize()
        {
            ServiceLocator.Register<UIManager>(this);
            EventBus.Subscribe<GameBootedEvent>(OnGameBooted);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventBus.Unsubscribe<GameBootedEvent>(OnGameBooted);
        }

        private void OnGameBooted(GameBootedEvent evt)
        {
            OpenScreen<Screen_ExploreHUD>();
        }
        
        protected Dictionary<Type, UIBase> BuildPrefabDictionary()
        {
            var dict = new Dictionary<Type, UIBase>();
            if (uiDB != null && uiDB.ListUI != null)
            {
                foreach (UIBase ui in uiDB.ListUI)
                {
                    dict[ui.GetType()] = ui;
                }
            }
            return dict;
        }
        
        protected T GetOrInstantiateUI<T>(Transform parent) where T : UIBase
        {
            var type = typeof(T);
            if (UIInstances.TryGetValue(type, out var instance))
            {
                return instance as T;
            }

            if (UIPrefabs.TryGetValue(type, out var prefab))
            {
                try
                {
                    T newInstance = Instantiate(prefab as T, parent);
                    newInstance.Initialize();
                    UIInstances.Add(type, newInstance);
                    return newInstance;
                }
                catch (Exception ex)
                {
                    if (enableLog)
                    {
                        GameLogger.LogError($"[UIManager] Lỗi khi Instantiate {type.Name}: {ex.Message}");
                    }                    
                    return null;
                }
            }

            if (enableLog)
            {
                GameLogger.LogError($"[UIManager] Không tìm thấy Prefab của {type.Name} trong UiDatabaseSO!");
            }            
            return null;
        }
        
        // Screen
        
        public T OpenScreen<T>(object data = null) where T : UIScreen
        {
            T screen = GetOrInstantiateUI<T>(screenLayer);
            
            if (screen != null)
            {
                if (ScreenStack.Count > 0)
                {
                    ScreenStack.Peek().Hide();
                }

                screen.Setup(data);
                screen.Show();
                ScreenStack.Push(screen);
            }
            
            return screen;
        }

        public void CloseCurrentScreen()
        {
            if (_screenStack != null && _screenStack.Count > 1)
            {
                var screen = _screenStack.Pop();
                screen.Hide();

                _screenStack.Peek().Show();
            }
        }
        
        // Popup
        
        public T OpenPopup<T>(object data = null, bool isImmediate = false) where T : UIPopup
        {
            if (!isImmediate && _popupStack != null && _popupStack.Count > 0)
            {
                PopupQueue.Enqueue(() => LoadAndShowPopup<T>(data));
                return null; 
            }
            
            return LoadAndShowPopup<T>(data);
        }

        private T LoadAndShowPopup<T>(object data) where T : UIPopup
        {
            T popup = GetOrInstantiateUI<T>(popupLayer);
            
            if (popup != null)
            {
                popup.Setup(data);
                popup.Show();
                PopupStack.Push(popup);
            }

            return popup;
        }
        
        public void CloseCurrentPopup()
        {
            if (_popupStack == null || _popupStack.Count == 0) return;

            var popup = _popupStack.Pop();
            popup.Hide();
            
            if (_popupStack.Count == 0 && _popupQueue != null && _popupQueue.Count > 0)
            {
                var nextPopupAction = _popupQueue.Dequeue();
                nextPopupAction?.Invoke();
            }
        }

        public void CloseAllPopups()
        {
            if (_popupStack == null) return;
            
            while (_popupStack.Count > 0)
            {
                var popup = _popupStack.Pop();
                popup.Hide();
            }
        }
        
        public void ClearPopupQueue()
        {
            _popupQueue?.Clear();
        }
        
    }
}