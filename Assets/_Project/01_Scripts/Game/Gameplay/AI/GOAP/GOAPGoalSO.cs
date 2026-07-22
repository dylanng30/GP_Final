using Game.Gameplay.AI.Core;
using UnityEngine;

namespace Game.Gameplay.AI.GOAP
{
    [CreateAssetMenu(fileName = "NewGOAPGoal", menuName = "Dylanng/AI/GOAP/GOAP Goal")]
    public class GOAPGoalSO : ScriptableObject
    {
        [Header("--- GOAL METADATA ---")]
        [SerializeField] protected string goalName = "Attack Target Goal";
        [SerializeField] protected float basePriority = 50f;

        [Header("--- DESIRED WORLD STATE ---")]
        [Tooltip("The desired world state flags achieved upon completing this goal.")]
        [SerializeField] protected AIWorldStateKey[] desiredStateKeys;
        [SerializeField] protected bool[] desiredStateValues;

        public string GoalName => goalName;
        public float BasePriority => basePriority;

        private void Reset()
        {
            desiredStateKeys = new[] { AIWorldStateKey.TargetIsDead };
            desiredStateValues = new[] { true };
        }

        public AIWorldState GetDesiredWorldState()
        {
            var state = new AIWorldState(0);
            if (desiredStateKeys == null || desiredStateKeys.Length == 0 || desiredStateValues == null)
            {
                state.SetState(AIWorldStateKey.TargetIsDead, true);
                return state;
            }

            int count = Mathf.Min(desiredStateKeys.Length, desiredStateValues.Length);
            for (int i = 0; i < count; i++)
            {
                state.SetState(desiredStateKeys[i], desiredStateValues[i]);
            }
            return state;
        }

        public AIWorldState GetDesiredWorldStateMask()
        {
            var mask = new AIWorldState(0);
            if (desiredStateKeys == null || desiredStateKeys.Length == 0 || desiredStateValues == null)
            {
                mask.SetState(AIWorldStateKey.TargetIsDead, true);
                return mask;
            }

            int count = Mathf.Min(desiredStateKeys.Length, desiredStateValues.Length);
            for (int i = 0; i < count; i++)
            {
                mask.SetState(desiredStateKeys[i], true);
            }
            return mask;
        }

        public virtual float CalculateDynamicPriority(IAIContext context)
        {
            if (context == null) return 0f;

            // Nếu goal mong muốn tiêu diệt mục tiêu (TargetIsDead) nhưng hiện tại không có mục tiêu, trả về 0 để nhường cho ReturnToSpawn hoặc Idle
            if (desiredStateKeys != null)
            {
                for (int i = 0; i < desiredStateKeys.Length; i++)
                {
                    if (desiredStateKeys[i] == AIWorldStateKey.TargetIsDead && context.CurrentTarget == null)
                    {
                        return 0f;
                    }
                }
            }

            return basePriority;
        }
    }
}
