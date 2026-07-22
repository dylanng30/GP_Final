using Dylanng.Core.Pooling;
using Dylanng.Core;
using Dylanng.Dylanng_EventBus;
using Dylanng.Events.SystemEvents;
using Dylanng.Gameplay.RPG.Abilities;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Game.Gameplay.Controllers;
using Game.Gameplay.Entities;
using Game.Gameplay.AI.Controllers;
using TMPro;
using UnityEngine;

namespace Game.UIs.FloatingText
{
    public class FloatingTextManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private PoolableObject damageTextPrefab;

        [Header("--- Physical & Critical Gradients (SO) ---")]
        [SerializeField] private DamageGradientSO normalGradient;
        [SerializeField] private DamageGradientSO critGradient;
        [SerializeField] private DamageGradientSO playerTakenDamageGradient;

        [Header("--- Elemental & DoT Gradients (SO) ---")]
        [SerializeField] private DamageGradientSO fireGradient;
        [SerializeField] private DamageGradientSO waterGradient;
        [SerializeField] private DamageGradientSO earthGradient;
        [SerializeField] private DamageGradientSO woodGradient;
        [SerializeField] private DamageGradientSO mentalGradient;

        [Header("--- Global Batch Override ---")]
        [SerializeField] private bool overrideGlobalDirection = false;
        [SerializeField] private GradientDirection globalDirection = GradientDirection.Vertical;
        [SerializeField] private TextureMappingOptions globalHorizontalMapping = TextureMappingOptions.Line;
        [SerializeField] private TextureMappingOptions globalVerticalMapping = TextureMappingOptions.Line;

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
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameBootedEvent>(OnGameBooted);
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
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
            if (mentalGradient == null) mentalGradient = CreateRuntimeGradient(new Color(0.95f, 0.6f, 1f, 1f), new Color(0.6f, 0.1f, 0.85f, 1f));
        }

        private DamageGradientSO CreateRuntimeGradient(Color topOrLeft, Color bottomOrRight)
        {
            var inst = ScriptableObject.CreateInstance<DamageGradientSO>();
            inst.SetColors(topOrLeft, bottomOrRight, GradientDirection.Vertical);
            return inst;
        }

        private void OnGameBooted(GameBootedEvent evt)
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

            // Randomize spawn position of damageText around target's position (offset upward slightly for better visibility)
            Vector3 spawnPos = evt.Target.transform.position + Random.insideUnitSphere * 0.3f + Vector3.up * 1.5f;

            DamageGradientSO chosenGradient = normalGradient;

            // 1. Phân loại theo nguyên tố trước (đặc biệt là Fire / Burn thiêu đốt)
            if (evt.Element != ElementType.Physical)
            {
                switch (evt.Element)
                {
                    case ElementType.Fire:
                        chosenGradient = fireGradient;
                        break;
                    case ElementType.Water:
                        chosenGradient = waterGradient;
                        break;
                    case ElementType.Earth:
                        chosenGradient = earthGradient;
                        break;
                    case ElementType.Wood:
                        chosenGradient = woodGradient;
                        break;
                    case ElementType.Mental:
                        chosenGradient = mentalGradient;
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

            SpawnText(spawnPos, evt.Damage, chosenGradient);
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

        private void SpawnText(Vector3 position, float damage, DamageGradientSO gradientSO)
        {
            if (!isPoolInitialized) return;

            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager != null)
            {
                var damageTextObj = poolManager.Spawn<DamageText>(poolKey, position, Quaternion.identity);
                if (damageTextObj != null)
                {
                    damageTextObj.Setup(damage, gradientSO, overrideGlobalDirection, globalDirection, globalHorizontalMapping, globalVerticalMapping);
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
            mentalGradient = CreateOrLoadAsset("DamageGradient_Mental.asset", new Color(0.95f, 0.6f, 1f, 1f), new Color(0.6f, 0.1f, 0.85f, 1f));

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("Successfully created and assigned 8 DamageGradientSO assets!");
        }

        [ContextMenu("2. Batch Set ALL SOs to Horizontal Mode (Line Mapping)")]
        public void BatchSetToHorizontal()
        {
            var all = new[] { normalGradient, critGradient, playerTakenDamageGradient, fireGradient, waterGradient, earthGradient, woodGradient, mentalGradient };
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
            var all = new[] { normalGradient, critGradient, playerTakenDamageGradient, fireGradient, waterGradient, earthGradient, woodGradient, mentalGradient };
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
