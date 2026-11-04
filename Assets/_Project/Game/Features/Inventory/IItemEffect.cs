using Dylanng;
using UnityEngine;

namespace Game.Inventory
{
    public interface IItemEffect
    {
        void ExecuteEffect(GameObject target);
    }
}
