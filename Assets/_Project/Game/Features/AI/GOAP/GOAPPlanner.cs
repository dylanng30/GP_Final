using System;
using System.Collections.Generic;
using Game.Gameplay.AI.Core;
using UnityEngine;

namespace Game.Gameplay.AI.GOAP
{
    /// <summary>
    /// Thuật toán tìm kiếm A* cho hệ thống GOAP.
    /// Sử dụng Non-alloc lists và bộ đệm bộ nhớ GOAPNodePool để bảo đảm 100% Zero-Allocation trong quá trình chạy.
    /// </summary>
    public class GOAPPlanner
    {
        private const int MAX_SEARCH_DEPTH = 32;

        private readonly List<GOAPNode> _openSet = new List<GOAPNode>(64);
        private readonly HashSet<ulong> _closedStates = new HashSet<ulong>();
        private readonly List<GOAPNode> _spawnedNodes = new List<GOAPNode>(128);
        private readonly List<IGOAPAction> _validActionsCache = new List<IGOAPAction>(32);

        /// <summary>
        /// Lập kế hoạch hành vi (Action Plan) từ trạng thái hiện tại đến mục tiêu mong muốn.
        /// Trả về danh sách hành động cần làm theo thứ tự thực hiện vào parameter resultPlan.
        /// </summary>
        public bool Plan(IAIContext context, List<IGOAPAction> availableActions, GOAPGoalSO goal, List<IGOAPAction> resultPlan)
        {
            if (context == null || availableActions == null || goal == null || resultPlan == null) return false;

            resultPlan.Clear();
            _openSet.Clear();
            _closedStates.Clear();
            GOAPNodePool.ReleaseAll(_spawnedNodes);

            AIWorldState startState = context.WorldState;
            AIWorldState desiredState = goal.GetDesiredWorldState();
            AIWorldState desiredStateMask = goal.GetDesiredWorldStateMask();

            // Lọc ra các action hợp lệ (Procedural Precondition)
            _validActionsCache.Clear();
            int actionCount = availableActions.Count;
            for (int i = 0; i < actionCount; i++)
            {
                var action = availableActions[i];
                if (action != null && action.CheckProceduralPrecondition(context))
                {
                    _validActionsCache.Add(action);
                }
            }

            // Tạo nút gốc
            GOAPNode rootNode = GOAPNodePool.Spawn();
            rootNode.Initialize(null, 0f, CalculateHeuristic(startState, desiredState), startState, null);
            _spawnedNodes.Add(rootNode);
            _openSet.Add(rootNode);

            int iterations = 0;
            while (_openSet.Count > 0 && iterations < MAX_SEARCH_DEPTH)
            {
                iterations++;
                
                // Pop node có TotalCost nhỏ nhất từ OpenSet
                int bestIndex = 0;
                float lowestCost = _openSet[0].TotalCost;
                int openCount = _openSet.Count;
                for (int i = 1; i < openCount; i++)
                {
                    if (_openSet[i].TotalCost < lowestCost)
                    {
                        lowestCost = _openSet[i].TotalCost;
                        bestIndex = i;
                    }
                }

                GOAPNode currentNode = _openSet[bestIndex];
                _openSet.RemoveAt(bestIndex);

                // Kiểm tra xem đã đạt mục tiêu chưa
                if (currentNode.State.Satisfies(desiredStateMask, desiredState))
                {
                    BuildPlanResult(currentNode, resultPlan);
                    GOAPNodePool.ReleaseAll(_spawnedNodes);
                    return true;
                }

                _closedStates.Add(currentNode.State.StateMask);

                // Duyệt qua các hành vi hợp lệ để sinh các nút con
                int validCount = _validActionsCache.Count;
                for (int i = 0; i < validCount; i++)
                {
                    var action = _validActionsCache[i];
                    
                    // Kiểm tra xem Preconditions của action có thỏa mãn trong currentNode.State không
                    if (!currentNode.State.Satisfies(action.PreconditionsMask, action.PreconditionsValues))
                    {
                        continue;
                    }

                    // Sinh ra trạng thái mới sau khi áp dụng Effects
                    AIWorldState nextState = currentNode.State;
                    nextState.ApplyEffects(action.EffectsMask, action.EffectsValues);

                    if (_closedStates.Contains(nextState.StateMask))
                    {
                        continue;
                    }

                    float newCost = currentNode.CostSoFar + action.CalculateDynamicCost(context);
                    
                    // Kiểm tra xem trạng thái nextState đã có trong OpenSet với chi phí thấp hơn chưa
                    bool inOpenSet = false;
                    for (int j = 0; j < _openSet.Count; j++)
                    {
                        if (_openSet[j].State == nextState)
                        {
                            inOpenSet = true;
                            break;
                        }
                    }

                    if (!inOpenSet)
                    {
                        GOAPNode childNode = GOAPNodePool.Spawn();
                        childNode.Initialize(currentNode, newCost, CalculateHeuristic(nextState, desiredState), nextState, action);
                        _spawnedNodes.Add(childNode);
                        _openSet.Add(childNode);
                    }
                }
            }

            GOAPNodePool.ReleaseAll(_spawnedNodes);
            return false;
        }

        private float CalculateHeuristic(AIWorldState current, AIWorldState desired)
        {
            // Hàm heuristic đơn giản: đếm số lượng bit khác biệt giữa current và desired
            ulong diff = (current.StateMask ^ desired.StateMask) & desired.StateMask;
            return CountSetBits(diff);
        }

        private int CountSetBits(ulong n)
        {
            int count = 0;
            while (n > 0)
            {
                n &= (n - 1);
                count++;
            }
            return count;
        }

        private void BuildPlanResult(GOAPNode targetNode, List<IGOAPAction> resultPlan)
        {
            GOAPNode curr = targetNode;
            while (curr != null && curr.Action != null)
            {
                resultPlan.Add(curr.Action);
                curr = curr.Parent;
            }
            resultPlan.Reverse(); // Đảo ngược để có thứ tự từ hành động đầu tiên đến hành động cuối cùng
        }
    }
}
