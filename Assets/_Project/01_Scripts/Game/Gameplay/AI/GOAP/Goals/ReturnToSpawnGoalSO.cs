using Game.Gameplay.AI.Core;
using UnityEngine;

namespace Game.Gameplay.AI.GOAP.Goals
{
    /// <summary>
    /// Mục tiêu quay trở về điểm Spawn ban đầu khi không có mục tiêu để tấn công hoặc khi bị ra xa khỏi lãnh thổ.
    /// </summary>
    [CreateAssetMenu(fileName = "GOAPGoal_ReturnToSpawn", menuName = "Dylanng/AI/GOAP/Goals/Return To Spawn")]
    public class ReturnToSpawnGoalSO : GOAPGoalSO
    {
        [Tooltip("Khoảng cách tối thiểu tính từ điểm Spawn để bắt đầu kích hoạt Goal này.")]
        [SerializeField] private float minDistanceToTrigger = 1.0f;

        private void Reset()
        {
            goalName = "Return To Spawn Goal";
            basePriority = 30f;
            desiredStateKeys = new[] { AIWorldStateKey.IsAtSpawn };
            desiredStateValues = new[] { true };
        }

        public override float CalculateDynamicPriority(IAIContext context)
        {
            if (context == null) return 0f;

            // Nếu đang có mục tiêu để tấn công (CurrentTarget != null), ưu tiên tấn công trước (trả về 0)
            if (context.CurrentTarget != null)
            {
                return 0f;
            }

            // Nếu không có mục tiêu và đang ở xa điểm Spawn hơn minDistanceToTrigger, kích hoạt Goal quay về
            if (context.DistanceToSpawn > minDistanceToTrigger)
            {
                return basePriority;
            }

            return 0f;
        }
    }
}
