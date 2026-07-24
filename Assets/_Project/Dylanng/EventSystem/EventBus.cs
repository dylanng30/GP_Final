using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dylanng
{
    public interface IEvent { }
    
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> subs = new ();

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        public static void InitializeEditor()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                ClearAll();
            }
        }
#endif

        public static void ClearAll()
        {
            subs.Clear();
            Debug.Log("Dylanng_EventBus: Cleared all subscriptions.");
        }

        public static void Subscribe<T>(Action<T> handler) where T : IEvent
        {
            Type t = typeof(T);
            if (!subs.TryGetValue(t, out List<Delegate> list))
            {
                list = new List<Delegate>();
                subs[t] = list;
            }
            list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : IEvent
        {
            Type t = typeof(T);
            if (subs.TryGetValue(t, out List<Delegate> list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                    subs.Remove(t);
            }
        }


        public static void Publish<T>(T eventData) where T : IEvent
        {
            Type t = typeof(T);
            if (subs.TryGetValue(t, out List<Delegate> list))
            {
                Delegate[] delegates = list.ToArray();
                foreach (Delegate d in delegates)
                {
                    ((Action<T>)d)(eventData);
                }
            }
        }
        
    }
}