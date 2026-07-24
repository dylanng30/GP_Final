using System.Collections.Generic;

namespace Game.Gameplay.AI.GOAP
{
    /// <summary>
    /// Object Pool thuần C# dành riêng cho GOAPNode nhằm tối ưu tốc độ và loại bỏ 100% GC Allocation
    /// khi thuật toán A* sinh ra các nút tìm kiếm.
    /// </summary>
    public static class GOAPNodePool
    {
        private static readonly Stack<GOAPNode> _pool = new Stack<GOAPNode>(128);

        public static GOAPNode Spawn()
        {
            GOAPNode node = _pool.Count > 0 ? _pool.Pop() : new GOAPNode();
            node.OnSpawn();
            return node;
        }

        public static void Release(GOAPNode node)
        {
            if (node == null) return;
            node.OnDespawn();
            _pool.Push(node);
        }

        public static void ReleaseAll(List<GOAPNode> nodes)
        {
            if (nodes == null) return;
            int count = nodes.Count;
            for (int i = 0; i < count; i++)
            {
                Release(nodes[i]);
            }
            nodes.Clear();
        }
    }
}
