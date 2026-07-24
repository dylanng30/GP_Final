using System;
using System.Collections.Generic;
using Game.Data.Hero;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Stats
{
    public class FirebaseStatRepository : IStatRepository
    {
        // ================= CONFIGURATION =================
        // Change these constants to match your Firebase database structure
        private const string FIREBASE_STATS_PATH = "character_stats";
        // =================================================

        private Dictionary<string, RemoteBaseHeroData> _cachedStats = new Dictionary<string, RemoteBaseHeroData>();

        public void Initialize()
        {
            FetchStatsFromFirebase();
        }

        private void FetchStatsFromFirebase()
        {
            Debug.Log($"[FirebaseStatRepository] Initializing fetch request to Firebase path: '{FIREBASE_STATS_PATH}'");
            
            // TODO: Implement Firebase database retrieval logic
            // Example:
            /*
            FirebaseDatabase.DefaultInstance
                .GetReference(FIREBASE_STATS_PATH)
                .GetValueAsync().ContinueWithOnMainThread(task => 
                {
                    if (task.IsFaulted || task.IsCanceled) 
                    {
                        Debug.LogError($"[FirebaseStatRepository] Failed to fetch stats from path: {FIREBASE_STATS_PATH}. Error: {task.Exception}");
                        return;
                    }
                    
                    if (task.IsCompleted) 
                    {
                        DataSnapshot snapshot = task.Result;
                        // Iterate and deserialize into _cachedStats
                        // foreach (var child in snapshot.Children) {
                        //     var data = JsonUtility.FromJson<RemoteBaseHeroData>(child.GetRawJsonValue());
                        //     _cachedStats[data.HeroId] = data;
                        // }
                    }
                });
            */
        }

        public RemoteBaseHeroData GetHeroStatData(string heroId)
        {
            if (_cachedStats.TryGetValue(heroId, out var data))
            {
                return data;
            }
            
            Debug.LogWarning($"[FirebaseStatRepository] Hero stats cache miss for ID: {heroId}");
            return null;
        }
        
        public void UpdateCachedStat(string heroId, RemoteBaseHeroData data)
        {
            _cachedStats[heroId] = data;
        }
    }
}
