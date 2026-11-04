using System;
using Game.Data.Hero;

namespace Game.Combat.Domain
{
    [Serializable]
    public struct ElementPair
    {
        public GameElement First;
        public GameElement Second;

        public ElementPair(GameElement first, GameElement second)
        {
            First = first;
            Second = second;
        }
    }
}
