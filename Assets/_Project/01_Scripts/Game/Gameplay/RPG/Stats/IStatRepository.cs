using Dylanng.Core;
using Game.Data.Hero;

namespace Dylanng.Gameplay.RPG.Stats
{
    public interface IStatRepository : IService
    {
        RemoteBaseHeroData GetHeroStatData(string heroId);
    }
}
