using Dylanng;
using Game.Data.Hero;

namespace Game.Combat.Stats
{
    public interface IStatRepository : IService
    {
        RemoteBaseHeroData GetHeroStatData(string heroId);
    }
}
