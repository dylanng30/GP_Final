using System.Collections.Generic;
using Game.Gameplay.AI.Core;
using Game.Gameplay.AI.Gambit;
using Game.Gameplay.AI.GOAP;
using UnityEngine;

namespace Game.Gameplay.AI.Controllers
{
    /// <summary>
    /// MonoBehaviour Controller mở rộng dành cho Boss (Boss Bot Controller).
    /// Hỗ trợ Multi-Phase Engine: Tự động chuyển đổi bộ Gambit Rules và GOAP Goals khi máu xuống ngưỡng (Ví dụ Phase 2 dưới 50% HP).
    /// </summary>
    public class BossBotController : EnemyBotController
    {
        [Header("--- BOSS MULTI-PHASE CONFIG ---")]
        [Tooltip("Gambit Rules configuration used for Phase 2 of the Boss.")]
        [SerializeField] private List<GambitRuleSO> phase2GambitRules;
        [Tooltip("GOAP Goals configuration used for Phase 2 of the Boss.")]
        [SerializeField] private List<GOAPGoalSO> phase2GoapGoals;
        [Tooltip("Additional GOAP Actions available in Phase 2.")]
        [SerializeField] private List<GOAPActionBase> phase2AvailableActions;
        [Range(0f, 1f)]
        [SerializeField] private float phase2HealthThreshold = 0.5f;

        private bool _isPhase2Active;

        protected override void OnEnable()
        {
            base.OnEnable();
            _isPhase2Active = false;
        }

        protected override void Update()
        {
            base.Update();
            CheckAndTransitionPhase();
        }

        public void CheckAndTransitionPhase()
        {
            if (!_isPhase2Active && HealthPercentage <= phase2HealthThreshold)
            {
                _isPhase2Active = true;
                Debug.Log($"[BossBotController] {gameObject.name} transitioned to Phase 2!");

                // Cập nhật World State để kích hoạt các kỹ năng Phase 2
                var ws = WorldState;
                ws.SetState(AIWorldStateKey.IsBossPhase2, true);
                WorldState = ws;

                // Tái khởi tạo lại AIBrain với bộ luật và mục tiêu mới của Phase 2
                var aiBrainField = typeof(EnemyBotController).GetField("_aiBrain", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (aiBrainField?.GetValue(this) is AIBrain brain)
                {
                    brain.Initialize(this, phase2GambitRules, phase2GoapGoals, phase2AvailableActions);
                }
            }
        }
    }
}
