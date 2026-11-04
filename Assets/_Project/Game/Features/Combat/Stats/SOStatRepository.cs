using Dylanng;
using System.Collections.Generic;
using System.Linq;
using Game.Data.Hero;
using UnityEngine;

namespace Game.Combat.Stats
{
    [CreateAssetMenu(fileName = "SOStatRepository", menuName = "RPG/SO Stat Repository")]
    public class SOStatRepository : ScriptableObject, IStatRepository
    {
        [SerializeField] private List<RemoteBaseHeroData> _heroStats = new List<RemoteBaseHeroData>();

        public void Initialize()
        {
            
        }

        public RemoteBaseHeroData GetHeroStatData(string heroId)
        {
            return _heroStats.FirstOrDefault(x => x.HeroId == heroId);
        }
    }
}
