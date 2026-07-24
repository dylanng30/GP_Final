using Dylanng;

namespace Game.Events
{
    public struct ActiveHeroChangedEvent : IEvent
    {
        public int NewActiveIndex;
        public string HeroId;

        public ActiveHeroChangedEvent(int newActiveIndex, string heroId)
        {
            NewActiveIndex = newActiveIndex;
            HeroId = heroId;
        }
    }

    public struct EntityDamagedEvent : IEvent
    {
        public string EntityId;
        public float CurrentHealth;
        public float MaxHealth;
        public float DamageAmount;

        public EntityDamagedEvent(string entityId, float currentHealth, float maxHealth, float damageAmount)
        {
            EntityId = entityId;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            DamageAmount = damageAmount;
        }
    }
}
