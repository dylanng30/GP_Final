using Dylanng;
using Dylanng.Gameplay.RPG.Stats;
using Dylanng.Gameplay.RPG.Abilities.Events;
using UnityEngine;

namespace Game.Gameplay.Entities
{
    [RequireComponent(typeof(Collider))]
    public class DummyBotInstance : MonoBehaviourBase, ITargetable
    {
        [Header("--- DUMMY BOT CONFIG ---")]
        [SerializeField] private float initialMaxHp = 10000f;
        [SerializeField] private float initialDef = 100f;
        [SerializeField] private GameObject selectionIndicator;

        public Transform Transform => TransformCache;
        public StatController Stats { get; private set; }
        public bool IsDead { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            selectionIndicator.SetActive(false);
            SetupStats();
        }

        private void SetupStats()
        {
            Stats = GetComponent<StatController>();
            if (Stats == null)
            {
                Stats = gameObject.GetComponent<StatController>();
            }

            Stats.InitializeStat(StatType.MaxHP, initialMaxHp);
            Stats.InitializeStat(StatType.DEF, initialDef);
            IsDead = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EventBus.Subscribe<DamageDealtEvent>(OnDamageReceived);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageReceived);
        }

        private void OnDamageReceived(DamageDealtEvent evt)
        {
            if (evt.Target == GameObjectCache)
            {
                float currentHp = Stats.GetStatValue(StatType.MaxHP);
                float newHp = Mathf.Max(0f, currentHp - evt.Damage);
                Stats.InitializeStat(StatType.MaxHP, newHp);

                //Debug.Log($"[DummyBot] Nhận {evt.Damage} sát thương hệ {evt.Element} từ {evt.Caster?.name}. HP còn lại: {newHp}/{initialMaxHp}");

                if (newHp <= 0f && !IsDead)
                {
                    IsDead = true;
                    Debug.Log($"[DummyBot] {GameObjectCache.name} đã bị tiêu diệt!");
                }
            }
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
            IsDead = false;
            Stats.InitializeStat(StatType.MaxHP, initialMaxHp);
            Debug.Log("[DummyBot] Đã khôi phục đầy máu.");
        }
    }
}
