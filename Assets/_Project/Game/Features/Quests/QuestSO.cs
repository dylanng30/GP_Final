using Dylanng;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Quests
{
    public enum QuestStatus { Inactive, Active, Completed, Failed }

    [CreateAssetMenu(fileName = "NewQuest", menuName = "RPG/Quests/Quest")]
    public class QuestSO : ScriptableData
    {
        public string QuestID;
        public string Title;
        [TextArea] public string Description;
        public List<QuestObjective> Objectives;
        public QuestStatus Status = QuestStatus.Inactive;

        public bool IsComplete()
        {
            foreach (var obj in Objectives)
            {
                if (!obj.IsReached) return false;
            }
            return true;
        }
    }

    [Serializable]
    public abstract class QuestObjective
    {
        public string Description;
        public int RequiredAmount;
        public int CurrentAmount;
        public bool IsReached => CurrentAmount >= RequiredAmount;

        public abstract void OnNotify(object data);
    }

    [Serializable]
    public class KillObjective : QuestObjective
    {
        public string TargetEnemyID;

        public override void OnNotify(object data)
        {
            if (data is string enemyID && enemyID == TargetEnemyID)
            {
                CurrentAmount = Mathf.Min(RequiredAmount, CurrentAmount + 1);
            }
        }
    }

    [Serializable]
    public class LootObjective : QuestObjective
    {
        public string TargetItemID;

        public override void OnNotify(object data)
        {
            if (data is KeyValuePair<string, int> lootData && lootData.Key == TargetItemID)
            {
                CurrentAmount = Mathf.Min(RequiredAmount, CurrentAmount + lootData.Value);
            }
        }
    }
}
