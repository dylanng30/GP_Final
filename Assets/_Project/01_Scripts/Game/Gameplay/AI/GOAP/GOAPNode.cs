using System;
using Dylanng.Core.Pooling;
using Game.Gameplay.AI.Core;

namespace Game.Gameplay.AI.GOAP
{
    /// <summary>
    /// Node trong cây tìm kiếm A* của GOAP.
    /// Tích hợp IPoolable để tái sử dụng trong Object Pool, đảm bảo 0 bytes GC allocation trong chu kỳ lập kế hoạch.
    /// </summary>
    public class GOAPNode : IPoolable, IComparable<GOAPNode>
    {
        public GOAPNode Parent { get; private set; }
        public float CostSoFar { get; private set; } // G Cost
        public float HeuristicCost { get; private set; } // H Cost
        public float TotalCost => CostSoFar + HeuristicCost; // F Cost

        public AIWorldState State { get; private set; }
        public IGOAPAction Action { get; private set; }

        public void Initialize(GOAPNode parent, float costSoFar, float heuristicCost, AIWorldState state, IGOAPAction action)
        {
            Parent = parent;
            CostSoFar = costSoFar;
            HeuristicCost = heuristicCost;
            State = state;
            Action = action;
        }

        public void OnSpawn()
        {
            Parent = null;
            CostSoFar = 0f;
            HeuristicCost = 0f;
            Action = null;
            State = new AIWorldState(0);
        }

        public void OnDespawn()
        {
            Parent = null;
            Action = null;
        }

        public int CompareTo(GOAPNode other)
        {
            if (other == null) return -1;
            int costCompare = TotalCost.CompareTo(other.TotalCost);
            if (costCompare != 0) return costCompare;
            return HeuristicCost.CompareTo(other.HeuristicCost);
        }
    }
}
