using Dylanng;
using Dylanng.Gameplay.RPG.Stats;
using Game.Combat.Domain;
using Game.Data.Hero;
using UnityEngine;

namespace Game.Gameplay.Entities
{
    [RequireComponent(typeof(Collider))]
    public class DummyBotInstance : MonoBehaviourBase, ITargetable
    {
        [Header("--- DUMMY BOT CONFIG ---")]
        [SerializeField] private float initialMaxHp = 10000f;
        [SerializeField] private float initialDef = 100f;
        [SerializeField] private GameElement element = GameElement.Physical;
        [SerializeField] private GameObject selectionIndicator;

        public Transform Transform => TransformCache;
        public StatController Stats { get; private set; }
        public GameElement Element => element;
        public HealthPool Health { get; } = new HealthPool();
        public bool IsDead => Health.IsDead;
        public bool IsInvulnerable => false;

        protected override void Awake()
        {
            base.Awake();
            selectionIndicator.SetActive(false);

            Stats = GetComponent<StatController>();
            Stats.InitializeStat(StatType.MaxHP, initialMaxHp);
            Stats.InitializeStat(StatType.DEF, initialDef);
            Health.Restore(initialMaxHp);
        }

        public void OnTargeted(bool isTargeted)
        {
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(isTargeted);
            }
        }

        public void ResetDummy()
        {
            Health.Restore(initialMaxHp);
        }
    }
}
