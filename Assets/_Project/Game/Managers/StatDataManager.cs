using Dylanng;
using Dylanng.Gameplay.RPG.Stats;
using UnityEngine;

namespace Game.Managers
{
    public class StatDataManager : ManagerBase
    {
        [SerializeField] private SOStatRepository _statRepository;

        public override void Initialize()
        {
            if (_statRepository != null)
            {
                ServiceLocator.Register<IStatRepository>(_statRepository);
            }
            else
            {
                Debug.LogError("[StatManager] SOStatRepository is missing.");
            }
        }
    }
}
