using Game.Data.Hero;
using Dylanng;
using Game.Combat.Abilities;
using Game.Combat.Abilities.Events;
using Game.Gameplay.Controllers;
using Game.Gameplay.Entities;
using Game.Gameplay.AI.Controllers;
using Game.Combat.Domain;
using Game.Events;
using TMPro;
using UnityEngine;

namespace Game.UIs.FloatingText
{
    public class FloatingTextManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private PoolableObject damageTextPrefab;

        [Header("Vị trí chữ so với đỉnh collider của mục tiêu")]
        [SerializeField] private float damageTextHeadPadding = 0.6f;
        [SerializeField] private float labelHeadPadding = 1.1f;
        [SerializeField] private float textScatterRadius = 0.3f;
        [SerializeField] private float fallbackHeadHeight = 2f;

        [Header("--- Physical & Critical Gradients (SO) ---")]
        [SerializeField] private DamageGradientSO normalGradient;
        [SerializeField] private DamageGradientSO critGradient;
        [SerializeField] private DamageGradientSO playerTakenDamageGradient;

        [Header("--- EleMetal & DoT Gradients (SO) ---")]
        [SerializeField] private DamageGradientSO fireGradient;
        [SerializeField] private DamageGradientSO waterGradient;
        [SerializeField] private DamageGradientSO earthGradient;
        [SerializeField] private DamageGradientSO woodGradient;
        [SerializeField] private DamageGradientSO MetalGradient;

        [Header("--- Global Batch Override ---")]
        [SerializeField] private bool overrideGlobalDirection = false;
        [SerializeField] private GradientDirection globalDirection = GradientDirection.Vertical;
        [SerializeField] private TextureMappingOptions globalHorizontalMapping = TextureMappingOptions.Line;
        [SerializeField] private TextureMappingOptions globalVerticalMapping = TextureMappingOptions.Line;

        private const string OvercomesText = "Khắc chế";
        private const string OvercomedByText = "Bị khắc";
        private static readonly Color OvercomesColor = new Color(1f, 0.85f, 0.2f);
        private static readonly Color OvercomedByColor = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color HealColor = new Color(0.35f, 1f, 0.45f);
        private static readonly Color ReactionColor = new Color(1f, 0.5f, 0.9f);

        private readonly string poolKey = "DamageText";
        private bool isPoolInitialized = false;

        private void Awake()
        {
            InitializeDefaultGradientsIfNull();
        }

        private void Start()
        {
            EventBus.Subscribe<GameBootedEvent>(OnGameBooted);
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<HealAppliedEvent>(OnHealApplied);
            EventBus.Subscribe<ReactionTriggeredEvent>(OnReactionTriggered);

            // GameManager phát GameBootedEvent trước khi load scene gameplay, nên manager nằm trong scene sẽ không nhận được.
            InitializePool();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameBootedEvent>(OnGameBooted);
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<HealAppliedEvent>(OnHealApplied);
            EventBus.Unsubscribe<ReactionTriggeredEvent>(OnReactionTriggered);
        }

        private void InitializeDefaultGradientsIfNull()
        {
            if (normalGradient == null) normalGradient = CreateRuntimeGradient(Color.white, new Color(0.75f, 0.75f, 0.75f, 1f));
            if (critGradient == null) critGradient = CreateRuntimeGradient(new Color(1f, 0.95f, 0.2f, 1f), new Color(1f, 0.1f, 0.1f, 1f));
            if (playerTakenDamageGradient == null) playerTakenDamageGradient = CreateRuntimeGradient(new Color(1f, 0.4f, 0.4f, 1f), new Color(0.7f, 0.05f, 0.15f, 1f));

            if (fireGradient == null) fireGradient = CreateRuntimeGradient(new Color(1f, 0.9f, 0.2f, 1f), new Color(1f, 0.35f, 0f, 1f));
            if (waterGradient == null) waterGradient = CreateRuntimeGradient(Color.white, new Color(0.1f, 0.65f, 1f, 1f));
            if (earthGradient == null) earthGradient = CreateRuntimeGradient(new Color(1f, 0.85f, 0.4f, 1f), new Color(0.75f, 0.45f, 0.1f, 1f));
            if (woodGradient == null) woodGradient = CreateRuntimeGradient(new Color(0.7f, 1f, 0.5f, 1f), new Color(0.15f, 0.75f, 0.2f, 1f));
            if (MetalGradient == null) MetalGradient = CreateRuntimeGradient(new Color(0.95f, 0.6f, 1f, 1f), new Color(0.6f, 0.1f, 0.85f, 1f));
        }

        private DamageGradientSO CreateRuntimeGradient(Color topOrLeft, Color bottomOrRight)
        {
            var inst = ScriptableObject.CreateInstance<DamageGradientSO>();
            inst.SetColors(topOrLeft, bottomOrRight, GradientDirection.Vertical);
            return inst;
        }

        private void OnGameBooted(GameBootedEvent evt) => InitializePool();

        private void InitializePool()
        {
            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager != null && damageTextPrefab != null)
            {
                poolManager.CreatePool(poolKey, damageTextPrefab, 20);
                isPoolInitialized = true;
            }
        }

        private void OnDamageDealt(DamageDealtEvent evt)
        {
            if (evt.Target == null || evt.Damage <= 0) return;

            InitializeDefaultGradientsIfNull();

            Vector3 spawnPos = GetAboveHeadPosition(evt.Target, damageTextHeadPadding);

            DamageGradientSO chosenGradient = normalGradient;

            // 1. Phân loại theo nguyên tố trước (đặc biệt là Fire / Burn thiêu đốt)
            if (evt.Element != GameElement.Physical)
            {
                switch (evt.Element)
                {
                    case GameElement.Fire:
                        chosenGradient = fireGradient;
                        break;
                    case GameElement.Water:
                        chosenGradient = waterGradient;
                        break;
                    case GameElement.Earth:
                        chosenGradient = earthGradient;
                        break;
                    case GameElement.Wood:
                        chosenGradient = woodGradient;
                        break;
                    case GameElement.Metal:
                        chosenGradient = MetalGradient;
                        break;
                }
            }
            // 2. Nếu là sát thương vật lý thông thường
            else
            {
                if (evt.IsCritical)
                {
                    chosenGradient = critGradient;
                }
                else if (IsPlayerTarget(evt.Target))
                {
                    chosenGradient = playerTakenDamageGradient;
                }
                else
                {
                    chosenGradient = normalGradient;
                }
            }

            SpawnText(spawnPos, evt.Damage, chosenGradient, GetEmphasis(evt));
            SpawnRelationLabel(evt);
        }

        private void SpawnRelationLabel(DamageDealtEvent evt)
        {
            switch (evt.Relation)
            {
                case ElementRelation.Overcomes:
                    SpawnLabel(evt.Target, OvercomesText, OvercomesColor);
                    break;
                case ElementRelation.OvercomedBy:
                    SpawnLabel(evt.Target, OvercomedByText, OvercomedByColor);
                    break;
            }
        }

        private void OnHealApplied(HealAppliedEvent evt)
        {
            SpawnLabel(evt.Target, $"+{Mathf.RoundToInt(evt.Amount)}", HealColor);
        }

        private void OnReactionTriggered(ReactionTriggeredEvent evt)
        {
            SpawnLabel(evt.Target, evt.Rule.DisplayName, ReactionColor);
        }

        private void SpawnLabel(GameObject target, string text, Color color)
        {
            if (!isPoolInitialized || target == null) return;

            Vector3 position = GetAboveHeadPosition(target, labelHeadPadding);
            var label = ServiceLocator.Get<PoolManager>()?.Spawn<DamageText>(poolKey, position, Quaternion.identity);
            if (label != null) label.SetupLabel(text, color);
        }

        /// <summary>Lấy điểm trên đỉnh collider của mục tiêu để chữ không bị model che, kèm lệch ngẫu nhiên để các số không chồng nhau.</summary>
        private Vector3 GetAboveHeadPosition(GameObject target, float padding)
        {
            Vector3 position = target.transform.position;
            Collider targetCollider = target.GetComponentInChildren<Collider>();
            float headHeight = targetCollider != null ? targetCollider.bounds.max.y : position.y + fallbackHeadHeight;

            Vector2 scatter = Random.insideUnitCircle * textScatterRadius;
            return new Vector3(position.x + scatter.x, headHeight + padding, position.z + scatter.y);
        }

        private bool IsPlayerTarget(GameObject target)
        {
            if (target == null) return false;
            if (target.TryGetComponent<PlayerTargetingSystem>(out _)) return true;
            if (target.TryGetComponent<HeroInstance>(out _) && !target.TryGetComponent<EnemyBotController>(out _) && !target.TryGetComponent<DummyBotInstance>(out _))
            {
                return true;
            }
            return false;
        }

        /// <summary>Độ lớn của số: chí mạng và khắc chế to hơn, sát thương theo nhịp nhỏ hơn.</summary>
        private static float GetEmphasis(DamageDealtEvent evt)
        {
            if (evt.IsPeriodic) return 0.75f;

            float emphasis = 1f;
            if (evt.IsCritical) emphasis *= 1.5f;
            if (evt.Relation == ElementRelation.Overcomes) emphasis *= 1.15f;
            return emphasis;
        }

        private void SpawnText(Vector3 position, float damage, DamageGradientSO gradientSO, float emphasis = 1f)
        {
            if (!isPoolInitialized) return;

            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager != null)
            {
                var damageTextObj = poolManager.Spawn<DamageText>(poolKey, position, Quaternion.identity);
                if (damageTextObj != null)
                {
                    damageTextObj.Setup(damage, gradientSO, overrideGlobalDirection, globalDirection, globalHorizontalMapping, globalVerticalMapping, emphasis);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (normalGradient == null && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && normalGradient == null)
                    {
                        CreateAndAssignGradientAssets();
                    }
                };
            }
        }

        [ContextMenu("1. Auto Create & Assign Gradient SO Assets")]
        public void CreateAndAssignGradientAssets()
        {
            string folderPath = "Assets/_Project/07_SOs/UI/DamageGradients";
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/_Project/07_SOs/UI"))
            {
                if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/_Project/07_SOs"))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets/_Project", "07_SOs");
                }
                UnityEditor.AssetDatabase.CreateFolder("Assets/_Project/07_SOs", "UI");
            }
            if (!UnityEditor.AssetDatabase.IsValidFolder(folderPath))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets/_Project/07_SOs/UI", "DamageGradients");
            }

            normalGradient = CreateOrLoadAsset("DamageGradient_Normal.asset", Color.white, new Color(0.75f, 0.75f, 0.75f, 1f));
            critGradient = CreateOrLoadAsset("DamageGradient_Crit.asset", new Color(1f, 0.95f, 0.2f, 1f), new Color(1f, 0.1f, 0.1f, 1f));
            playerTakenDamageGradient = CreateOrLoadAsset("DamageGradient_PlayerTaken.asset", new Color(1f, 0.4f, 0.4f, 1f), new Color(0.7f, 0.05f, 0.15f, 1f));

            fireGradient = CreateOrLoadAsset("DamageGradient_Fire.asset", new Color(1f, 0.9f, 0.2f, 1f), new Color(1f, 0.35f, 0f, 1f));
            waterGradient = CreateOrLoadAsset("DamageGradient_Water.asset", Color.white, new Color(0.1f, 0.65f, 1f, 1f));
            earthGradient = CreateOrLoadAsset("DamageGradient_Earth.asset", new Color(1f, 0.85f, 0.4f, 1f), new Color(0.75f, 0.45f, 0.1f, 1f));
            woodGradient = CreateOrLoadAsset("DamageGradient_Wood.asset", new Color(0.7f, 1f, 0.5f, 1f), new Color(0.15f, 0.75f, 0.2f, 1f));
            MetalGradient = CreateOrLoadAsset("DamageGradient_Metal.asset", new Color(0.95f, 0.6f, 1f, 1f), new Color(0.6f, 0.1f, 0.85f, 1f));

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("Successfully created and assigned 8 DamageGradientSO assets!");
        }

        [ContextMenu("2. Batch Set ALL SOs to Horizontal Mode (Line Mapping)")]
        public void BatchSetToHorizontal()
        {
            var all = new[] { normalGradient, critGradient, playerTakenDamageGradient, fireGradient, waterGradient, earthGradient, woodGradient, MetalGradient };
            foreach (var g in all)
            {
                if (g != null)
                {
                    g.Direction = GradientDirection.Horizontal;
                    g.HorizontalMapping = TextureMappingOptions.Line;
                    UnityEditor.EditorUtility.SetDirty(g);
                }
            }
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("Successfully set all 8 DamageGradientSO assets to Horizontal (Line Mapping) mode!");
        }

        [ContextMenu("3. Batch Set ALL SOs to Vertical Mode (Line Mapping)")]
        public void BatchSetToVertical()
        {
            var all = new[] { normalGradient, critGradient, playerTakenDamageGradient, fireGradient, waterGradient, earthGradient, woodGradient, MetalGradient };
            foreach (var g in all)
            {
                if (g != null)
                {
                    g.Direction = GradientDirection.Vertical;
                    g.VerticalMapping = TextureMappingOptions.Line;
                    UnityEditor.EditorUtility.SetDirty(g);
                }
            }
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("Successfully set all 8 DamageGradientSO assets to Vertical mode!");
        }

        private DamageGradientSO CreateOrLoadAsset(string fileName, Color top, Color bottom)
        {
            string assetPath = $"Assets/_Project/07_SOs/UI/DamageGradients/{fileName}";
            var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<DamageGradientSO>(assetPath);
            if (existing != null)
            {
                existing.SetColors(top, bottom, existing.Direction);
                UnityEditor.EditorUtility.SetDirty(existing);
                return existing;
            }

            var inst = ScriptableObject.CreateInstance<DamageGradientSO>();
            inst.SetColors(top, bottom, GradientDirection.Vertical);
            UnityEditor.AssetDatabase.CreateAsset(inst, assetPath);
            return inst;
        }
#endif
    }
}
