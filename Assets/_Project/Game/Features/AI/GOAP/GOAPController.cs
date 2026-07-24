using System.Collections.Generic;
using Game.Gameplay.AI.Core;

namespace Game.Gameplay.AI.GOAP
{
    /// <summary>
    /// Controller quản lý việc thực thi một chuỗi hành vi (Action Chain) được lập kế hoạch bởi GOAPPlanner.
    /// </summary>
    public class GOAPController
    {
        private readonly List<IGOAPAction> _currentPlan = new List<IGOAPAction>(16);
        private int _currentActionIndex = -1;

        public bool HasActivePlan => _currentPlan.Count > 0 && _currentActionIndex >= 0 && _currentActionIndex < _currentPlan.Count;
        public IGOAPAction CurrentAction => HasActivePlan ? _currentPlan[_currentActionIndex] : null;

        public void SetPlan(List<IGOAPAction> newPlan)
        {
            StopCurrentPlan(null);
            _currentPlan.Clear();
            if (newPlan != null && newPlan.Count > 0)
            {
                _currentPlan.AddRange(newPlan);
                _currentActionIndex = 0;
            }
        }

        public void StopCurrentPlan(IAIContext context)
        {
            if (HasActivePlan && context != null)
            {
                _currentPlan[_currentActionIndex].Exit(context);
            }
            _currentPlan.Clear();
            _currentActionIndex = -1;
        }

        public ActionStatus UpdatePlan(IAIContext context, float deltaTime)
        {
            if (!HasActivePlan || context == null)
            {
                return ActionStatus.Failure;
            }

            var action = _currentPlan[_currentActionIndex];
            if (action == null)
            {
                StopCurrentPlan(context);
                return ActionStatus.Failure;
            }

            // Nếu đây là frame đầu tiên thực hiện action (hoặc vừa chuyển action)
            ActionStatus status = action.Update(context, deltaTime);

            if (status == ActionStatus.Success)
            {
                action.Exit(context);
                _currentActionIndex++;

                if (_currentActionIndex >= _currentPlan.Count)
                {
                    // Hoàn thành toàn bộ plan
                    _currentPlan.Clear();
                    _currentActionIndex = -1;
                    return ActionStatus.Success;
                }
                else
                {
                    // Bước sang action tiếp theo trong kế hoạch
                    _currentPlan[_currentActionIndex].Enter(context);
                    return ActionStatus.Running;
                }
            }
            else if (status == ActionStatus.Failure)
            {
                action.Exit(context);
                _currentPlan.Clear();
                _currentActionIndex = -1;
                return ActionStatus.Failure;
            }

            return ActionStatus.Running;
        }

        public void StartFirstAction(IAIContext context)
        {
            if (HasActivePlan && context != null)
            {
                _currentPlan[_currentActionIndex].Enter(context);
            }
        }
    }
}
