using UnityEngine;

namespace Dylanng.Gameplay.RPG.Inventory
{
    public interface IItemEffect
    {
        void ExecuteEffect(GameObject target);
    }
}
