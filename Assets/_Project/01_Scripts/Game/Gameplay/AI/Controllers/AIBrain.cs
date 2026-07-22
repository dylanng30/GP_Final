using System;
using System.Collections.Generic;
using Dylanng.Core;
using Dylanng.Core.Base;
using Dylanng.Core.Systems.TickSystem;
using Game.Gameplay.AI.Core;
using Game.Gameplay.AI.Gambit;
using Game.Gameplay.AI.GOAP;
using UnityEngine;

namespace Game.Gameplay.AI.Controllers
{
    /// <summary>
    /// Bộ não trung tâm (Core AI Brain Coordinator).
    /// Tích hợp với ITickSystem theo nguyên tắc Time-Slicing để tối ưu hiệu năng, không dùng Update() mặc định của Unity.
    /// Điều phối hài hòa giữa phản xạ khẩn cấp Gambit System và lập kế hoạch dài hạn GOAP.
    /// </summary>
    public class AIBrain : IUpdatable
    {
        private IAIContext _context;
        private readonly GambitEvaluator _gambitEvaluator = new GambitEvaluator();
        private readonly GOAPPlanner _goapPlanner = new GOAPPlanner();
        private readonly GOAPController _goapController = new GOAPController();

        private readonly List<GambitRuleSO> _gambitRules = new List<GambitRuleSO>();
        private readonly List<GOAPGoalSO> _goapGoals = new List<GOAPGoalSO>();
        private readonly List<IGOAPAction> _availableActions = new List<IGOAPAction>();

        private readonly List<IGOAPAction> _planBuffer = new List<IGOAPAction>(16);

        // Time-slicing configuration
        private float _gambitTickInterval = 0.1f;
        private float _goapTickInterval = 0.35f;
        private float _gambitTimer;
        private float _goapTimer;

        private GambitRuleSO _activeGambitRule;
        private GOAPGoalSO _activeGoal;
        private bool _isRegisteredToTickSystem;

        public IAIContext Context => _context;
        public GambitRuleSO ActiveGambitRule => _activeGambitRule;
        public GOAPGoalSO ActiveGoal => _activeGoal;
        public IGOAPAction CurrentGOAPAction => _goapController.CurrentAction;
        public bool IsRegisteredToTickSystem => _isRegisteredToTickSystem;

        public void Initialize(IAIContext context, IEnumerable<GambitRuleSO> gambitRules, IEnumerable<GOAPGoalSO> goapGoals, IEnumerable<IGOAPAction> availableActions)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            _gambitRules.Clear();
            if (gambitRules != null) _gambitRules.AddRange(gambitRules);
            _gambitEvaluator.SetRules(_gambitRules);

            _goapGoals.Clear();
            if (goapGoals != null) _goapGoals.AddRange(goapGoals);

            _availableActions.Clear();
            if (availableActions != null) _availableActions.AddRange(availableActions);

            RegisterToTickSystem();
        }

        public void RegisterToTickSystem()
        {
            if (_isRegisteredToTickSystem) return;
            var tickSystem = ServiceLocator.Get<ITickSystem>();
            if (tickSystem != null)
            {
                tickSystem.Register(this);
                _isRegisteredToTickSystem = true;
            }
        }

        public void UnregisterFromTickSystem()
        {
            if (!_isRegisteredToTickSystem) return;
            var tickSystem = ServiceLocator.Get<ITickSystem>();
            if (tickSystem != null)
            {
                tickSystem.Unregister(this);
                _isRegisteredToTickSystem = false;
            }
        }

        public void OnUpdate(float deltaTime)
        {
            if (_context == null || _context.EntityInstance == null) return;

            _context.ScanAndValidateTarget(deltaTime);

            // 1. Đánh giá Gambit theo định kỳ hoặc khi cần (Time-Slicing)
            _gambitTimer += deltaTime;
            if (_gambitTimer >= _gambitTickInterval)
            {
                _gambitTimer = 0f;
                EvaluateGambitSystem();
            }

            // Nếu đang bị chiếm quyền điều khiển bởi phản xạ Gambit, không chạy GOAP
            if (_activeGambitRule != null)
            {
                // Kiểm tra xem điều kiện Gambit còn thỏa mãn không
                if (!_activeGambitRule.Evaluate(_context))
                {
                    _activeGambitRule = null;
                    _goapTimer = _goapTickInterval; // Kích hoạt tính toán lại GOAP ngay ở bước tiếp theo
                }
                else
                {
                    return;
                }
            }

            // 2. Cập nhật Action Chain hiện tại của GOAP
            if (_goapController.HasActivePlan)
            {
                ActionStatus status = _goapController.UpdatePlan(_context, deltaTime);
                if (status != ActionStatus.Running)
                {
                    _goapTimer = _goapTickInterval; // Plan xong hoặc thất bại -> tính lại ngay
                }
            }

            // 3. Lập kế hoạch GOAP định kỳ hoặc khi không có Action Chain nào
            _goapTimer += deltaTime;
            if (_goapTimer >= _goapTickInterval || !_goapController.HasActivePlan)
            {
                _goapTimer = 0f;
                EvaluateAndPlanGOAP();

                if (!_goapController.HasActivePlan && _activeGambitRule == null)
                {
                    _context.EntityController?.StopMove();
                }
            }
        }

        private void EvaluateGambitSystem()
        {
            GambitRuleSO triggeredRule = _gambitEvaluator.EvaluatePriorityRule(_context);
            if (triggeredRule != null && triggeredRule != _activeGambitRule)
            {
                // Ngắt kế hoạch GOAP hiện tại
                _goapController.StopCurrentPlan(_context);
                _activeGambitRule = triggeredRule;
                _activeGambitRule.Action?.Execute(_context);
            }
        }

        private void EvaluateAndPlanGOAP()
        {
            if (_goapGoals.Count == 0 || _availableActions.Count == 0) return;

            // Tìm Goal có priority động cao nhất (> 0)
            GOAPGoalSO bestGoal = null;
            float highestPriority = 0f; // Chỉ chọn Goal có priority > 0 để tránh lập kế hoạch cho goal inactive

            int count = _goapGoals.Count;
            for (int i = 0; i < count; i++)
            {
                var goal = _goapGoals[i];
                if (goal == null) continue;

                float priority = goal.CalculateDynamicPriority(_context);
                if (priority > highestPriority)
                {
                    highestPriority = priority;
                    bestGoal = goal;
                }
            }

            if (bestGoal != null)
            {
                bool success = _goapPlanner.Plan(_context, _availableActions, bestGoal, _planBuffer);
                if (success)
                {
                    _activeGoal = bestGoal;
                    _goapController.SetPlan(_planBuffer);
                    _goapController.StartFirstAction(_context);
                }
            }
        }
    }
}
