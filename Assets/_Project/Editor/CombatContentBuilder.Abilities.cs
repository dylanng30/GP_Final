using System.Collections.Generic;
using Dylanng;
using Dylanng.Gameplay.RPG.Abilities;
using Dylanng.Gameplay.RPG.Abilities.BasicAttack;
using Dylanng.Gameplay.RPG.Abilities.Effects;
using Game.Combat.Domain;
using Game.Data.Hero;
using Game.Gameplay.Controllers;
using Game.Gameplay.Entities;
using Game.Gameplay.RPG.Abilities.Effects;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Combat
{
    public static partial class CombatContentBuilder
    {
        private const string AbilityFolder = SoRoot + "/Abilities";
        private const string KitFolder = AbilityFolder + "/Heroes";
        private const string HeroPrefabFormat = "Assets/_Project/02_Prefabs/Hero/Hero_{0:D6}.prefab";
        private const string CombatPrefabFolder = "Assets/_Project/02_Prefabs/Combat";
        private const string FireballPrefabPath = "Assets/_Project/02_Prefabs/Projectiles/FireBall_Demo.prefab";
        private const string StoneMaterialPath = "Assets/_Project/02_Art/Materials/Blockout/BLK_Stone.mat";
        private const string RangedBasicAttackPath = AbilityFolder + "/Fire_Default_BasicAttack_Demo.asset";

        private const float UltimateEnergy = 100f;
        private const float MeleeAttackRange = 2.2f;
        private const float RangedAttackRange = 7f;
        private const string SkillTrigger1 = "Skill_0";
        private const string SkillTrigger2 = "Skill_1";

        private sealed class AbilitySpec
        {
            public string AssetName;
            public string DisplayName;
            public string Description;
            public AbilityType Type = AbilityType.ElementalSkill;
            public GameElement Element;
            public float Cooldown;
            public float EnergyCost;
            public float Range = 8f;
            public float Radius = 2f;
            public float CastDuration = 0.6f;
            public bool LocksMovement = true;
            public SkillIndicatorType Indicator = SkillIndicatorType.AreaTarget;
            public string Trigger = SkillTrigger1;
            public List<IAbilityEffect> Effects;
        }

        private static void BuildAbilities(StatusCatalog statuses)
        {
            ZoneDamageInstance zone = BuildZonePrefab();
            StoneWallStructure wall = BuildWallPrefab();

            AssignHero(0, GameElement.Fire, isMelee: false, BuildFireKit(statuses));
            AssignHero(1, GameElement.Wood, isMelee: true, BuildWoodKit(statuses));
            AssignHero(2, GameElement.Earth, isMelee: true, BuildEarthKit(statuses, wall));
            AssignHero(3, GameElement.Metal, isMelee: true, BuildMetalKit(statuses, zone));
            AssignHero(4, GameElement.Water, isMelee: false, BuildWaterKit(statuses, zone));
        }

        private static AbilitySO[] BuildFireKit(StatusCatalog statuses)
        {
            AbilitySO skill1 = BuildFireball(statuses);

            AbilitySO skill2 = CreateAbility(new AbilitySpec
            {
                AssetName = "Fire_Skill2_Eruption",
                DisplayName = "Địa Hỏa Bùng Phát",
                Description = "Báo hiệu một vùng rồi phát nổ, gây Thiêu Đốt.",
                Element = GameElement.Fire,
                Cooldown = 9f,
                Range = 9f,
                Radius = 3.5f,
                Trigger = SkillTrigger2,
                Effects = new List<IAbilityEffect>
                {
                    Vfx("FireFieldRed", EffectOrigin.Point, 1f, 1.2f),
                    Delayed(0.8f,
                        Vfx("ExplosionFireballFire", EffectOrigin.Point, 2f, 2f),
                        Area(EffectOrigin.Point, 3.5f, Damage(150f, 2.2f), Status(statuses.Burn)))
                }
            });

            AbilitySO ultimate = CreateAbility(new AbilitySpec
            {
                AssetName = "Fire_Ultimate_MeteorRain",
                DisplayName = "Mưa Hỏa Thạch",
                Description = "Ba đợt nổ liên tiếp trên vùng rộng.",
                Type = AbilityType.ElementalBurst,
                Element = GameElement.Fire,
                Cooldown = 18f,
                EnergyCost = UltimateEnergy,
                Range = 10f,
                Radius = 5f,
                CastDuration = 1f,
                Trigger = SkillTrigger2,
                Effects = new List<IAbilityEffect>
                {
                    Delayed(0.3f, MeteorWave(statuses)),
                    Delayed(0.8f, MeteorWave(statuses)),
                    Delayed(1.3f, MeteorWave(statuses))
                }
            });

            return new[] { skill1, skill2, ultimate };
        }

        private static IAbilityEffect MeteorWave(StatusCatalog statuses)
        {
            return Group(
                Vfx("ExplosionFireballFire", EffectOrigin.Point, 2.5f, 2f),
                Area(EffectOrigin.Point, 5f, Damage(90f, 1.2f), Status(statuses.Burn)));
        }

        private static AbilitySO BuildFireball(StatusCatalog statuses)
        {
            var ability = AssetDatabase.LoadAssetAtPath<AbilitySO>($"{AbilityFolder}/Skill_FireBall.asset");
            ability.AbilityName = "Cầu Lửa";
            ability.Description = "Bắn cầu lửa nổ khi trúng đích, gây Thiêu Đốt.";
            ability.Element = GameElement.Fire;
            ability.Cooldown = 5f;
            ability.AnimationTriggerName = SkillTrigger1;
            ability.Effects = new List<IAbilityEffect>
            {
                new ProjectileEffect
                {
                    ProjectilePrefab = AssetDatabase.LoadAssetAtPath<PoolableObject>(FireballPrefabPath),
                    CollisionEffects = new List<IAbilityEffect>
                    {
                        new DamageEffect { BaseDamage = 100f, AtkMultiplier = 1.5f, UseFalloff = true },
                        Status(statuses.Burn)
                    }
                }
            };
            EditorUtility.SetDirty(ability);
            return ability;
        }

        private static AbilitySO[] BuildWoodKit(StatusCatalog statuses)
        {
            AbilitySO skill1 = CreateAbility(new AbilitySpec
            {
                AssetName = "Wood_Skill1_Shockwave",
                DisplayName = "Sóng Chấn Động",
                Description = "Đẩy lùi kẻ địch phía trước bằng sóng chấn động.",
                Element = GameElement.Wood,
                Cooldown = 6f,
                Range = 5f,
                Indicator = SkillIndicatorType.Directional,
                Effects = new List<IAbilityEffect>
                {
                    Vfx("ExplosionNovaGreen", EffectOrigin.Caster, 1.2f, 2f),
                    Area(EffectOrigin.Caster, 5f, 120f, Damage(60f, 1.2f), Knockback(3f))
                }
            });

            AbilitySO skill2 = CreateAbility(new AbilitySpec
            {
                AssetName = "Wood_Skill2_Entangle",
                DisplayName = "Quấn Rễ",
                Description = "Rễ cây trói kẻ địch trong vùng.",
                Element = GameElement.Wood,
                Cooldown = 10f,
                Range = 8f,
                Radius = 3f,
                Trigger = SkillTrigger2,
                Effects = new List<IAbilityEffect>
                {
                    Vfx("MagicPillarBlastGreen", EffectOrigin.Point, 1.5f, 2f),
                    Area(EffectOrigin.Point, 3f, Damage(40f, 0.8f), Status(statuses.Root))
                }
            });

            AbilitySO ultimate = CreateAbility(new AbilitySpec
            {
                AssetName = "Wood_Ultimate_LivingForest",
                DisplayName = "Rừng Sống",
                Description = "Trói toàn bộ kẻ địch trong vùng rộng.",
                Type = AbilityType.ElementalBurst,
                Element = GameElement.Wood,
                Cooldown = 20f,
                EnergyCost = UltimateEnergy,
                Range = 9f,
                Radius = 6f,
                CastDuration = 0.8f,
                Trigger = SkillTrigger2,
                Effects = new List<IAbilityEffect>
                {
                    Vfx("ExplosionNovaGreen", EffectOrigin.Point, 2.5f, 2f),
                    Area(EffectOrigin.Point, 6f, Damage(120f, 2f), Status(statuses.Root))
                }
            });

            return new[] { skill1, skill2, ultimate };
        }

        private static AbilitySO[] BuildEarthKit(StatusCatalog statuses, StoneWallStructure wall)
        {
            AbilitySO skill1 = CreateAbility(new AbilitySpec
            {
                AssetName = "Earth_Skill1_StoneWall",
                DisplayName = "Tường Đá",
                Description = "Dựng tường chặn đường và chặn đạn.",
                Element = GameElement.Earth,
                Cooldown = 10f,
                Range = 3f,
                Indicator = SkillIndicatorType.Directional,
                Effects = new List<IAbilityEffect>
                {
                    new SpawnStructureEffect { Prefab = wall, ForwardDistance = 3f, Lifetime = 5f }
                }
            });

            AbilitySO skill2 = CreateAbility(new AbilitySpec
            {
                AssetName = "Earth_Skill2_RockPillar",
                DisplayName = "Cột Đá",
                Description = "Cột đá trồi lên hất tung và hóa đá kẻ địch.",
                Element = GameElement.Earth,
                Cooldown = 9f,
                Range = 7f,
                Radius = 2.5f,
                Trigger = SkillTrigger2,
                Effects = new List<IAbilityEffect>
                {
                    Vfx("MagicPillarBlastYellow", EffectOrigin.Point, 1.5f, 2f),
                    Delayed(0.5f, Area(EffectOrigin.Point, 2.5f,
                        Damage(90f, 1.5f), Knockup(2.5f), Status(statuses.Petrify)))
                }
            });

            AbilitySO ultimate = CreateAbility(new AbilitySpec
            {
                AssetName = "Earth_Ultimate_StoneGuard",
                DisplayName = "Giáp Đại Địa",
                Description = "Tạo khiên lớn cho cả đội.",
                Type = AbilityType.ElementalBurst,
                Element = GameElement.Earth,
                Cooldown = 20f,
                EnergyCost = UltimateEnergy,
                Indicator = SkillIndicatorType.None,
                Effects = new List<IAbilityEffect>
                {
                    Vfx("MagicShieldYellow", EffectOrigin.Caster, 2f, 3f),
                    AllyArea(8f, new ShieldEffect { ShieldAmount = 300f, Duration = 8f })
                }
            });

            return new[] { skill1, skill2, ultimate };
        }

        private static AbilitySO[] BuildMetalKit(StatusCatalog statuses, ZoneDamageInstance zone)
        {
            AbilitySO skill1 = CreateAbility(new AbilitySpec
            {
                AssetName = "Metal_Skill1_Lance",
                DisplayName = "Trường Thương",
                Description = "Đâm thẳng, phá khiên và phá giáp.",
                Element = GameElement.Metal,
                Cooldown = 6f,
                Range = 6f,
                Indicator = SkillIndicatorType.Directional,
                Effects = new List<IAbilityEffect>
                {
                    Vfx("SwordSlashThickRed", EffectOrigin.Caster, 1.5f, 1.5f),
                    Area(EffectOrigin.Caster, 6f, 50f,
                        Damage(120f, 1.8f), new BreakShieldEffect(), Status(statuses.ArmorBreak))
                }
            });

            AbilitySO skill2 = CreateAbility(new AbilitySpec
            {
                AssetName = "Metal_Skill2_Whirlwind",
                DisplayName = "Lốc Thép",
                Description = "Xoay quanh người, gây sát thương liên tục.",
                Element = GameElement.Metal,
                Cooldown = 9f,
                Range = 3f,
                Radius = 3f,
                LocksMovement = false,
                Indicator = SkillIndicatorType.None,
                Trigger = SkillTrigger2,
                Effects = new List<IAbilityEffect>
                {
                    new VfxCueEffect
                    {
                        Prefab = LoadVfx("SwordWhirlwindYellow"),
                        Origin = EffectOrigin.Caster,
                        Lifetime = 3f,
                        FollowCaster = true
                    },
                    new ZoneDamageEffect
                    {
                        Radius = 3f,
                        Duration = 3f,
                        TickInterval = 0.5f,
                        DamagePerTickMultiplier = 0.5f,
                        ZoneVFXPrefab = zone,
                        IsAura = true
                    }
                }
            });

            AbilitySO ultimate = CreateAbility(new AbilitySpec
            {
                AssetName = "Metal_Ultimate_TitanForm",
                DisplayName = "Cự Nhân Thép",
                Description = "Biến thành dạng khổng lồ, tăng mạnh công và thủ.",
                Type = AbilityType.ElementalBurst,
                Element = GameElement.Metal,
                Cooldown = 25f,
                EnergyCost = UltimateEnergy,
                Indicator = SkillIndicatorType.None,
                Effects = new List<IAbilityEffect>
                {
                    Vfx("MagicBuffYellow", EffectOrigin.Caster, 2f, 2f),
                    new ApplyStatusEffect { Status = statuses.TitanForm, ApplyToCaster = true }
                }
            });

            return new[] { skill1, skill2, ultimate };
        }

        private static AbilitySO[] BuildWaterKit(StatusCatalog statuses, ZoneDamageInstance zone)
        {
            AbilitySO skill1 = CreateAbility(new AbilitySpec
            {
                AssetName = "Water_Skill1_AquaShield",
                DisplayName = "Giáp Nước",
                Description = "Tạo lớp giáp cho đồng minh xung quanh.",
                Element = GameElement.Water,
                Cooldown = 10f,
                Indicator = SkillIndicatorType.None,
                Effects = new List<IAbilityEffect>
                {
                    Vfx("MagicShieldBlue", EffectOrigin.Caster, 1.5f, 3f),
                    AllyArea(8f, new ShieldEffect { ShieldAmount = 150f, Duration = 6f })
                }
            });

            AbilitySO skill2 = CreateAbility(new AbilitySpec
            {
                AssetName = "Water_Skill2_Rain",
                DisplayName = "Mưa Lạnh",
                Description = "Tạo vùng mưa làm ướt và làm chậm kẻ địch.",
                Element = GameElement.Water,
                Cooldown = 10f,
                Range = 9f,
                Radius = 4f,
                Trigger = SkillTrigger2,
                Effects = new List<IAbilityEffect>
                {
                    Vfx("WaterRainRipples", EffectOrigin.Point, 1.5f, 5f),
                    new ZoneDamageEffect
                    {
                        Radius = 4f,
                        Duration = 5f,
                        TickInterval = 1f,
                        DamagePerTickMultiplier = 0.25f,
                        ZoneVFXPrefab = zone,
                        AppliedStatus = statuses.Wet
                    }
                }
            });

            AbilitySO ultimate = CreateAbility(new AbilitySpec
            {
                AssetName = "Water_Ultimate_Tide",
                DisplayName = "Thủy Triều Hồi Sinh",
                Description = "Hồi máu và xóa trạng thái xấu cho cả đội.",
                Type = AbilityType.ElementalBurst,
                Element = GameElement.Water,
                Cooldown = 20f,
                EnergyCost = UltimateEnergy,
                Indicator = SkillIndicatorType.None,
                Effects = new List<IAbilityEffect>
                {
                    Vfx("MagicBuffBlue", EffectOrigin.Caster, 2f, 2f),
                    AllyArea(10f, new HealEffect { BaseHeal = 200f, HealScalingMultiplier = 2f }, new CleanseEffect())
                }
            });

            return new[] { skill1, skill2, ultimate };
        }

        private static AbilitySO CreateAbility(AbilitySpec spec)
        {
            var ability = GetOrCreate<AbilitySO>($"{KitFolder}/{spec.AssetName}.asset");

            ability.AbilityID = spec.AssetName;
            ability.AbilityName = spec.DisplayName;
            ability.Description = spec.Description;
            ability.Type = spec.Type;
            ability.Element = spec.Element;
            ability.Cooldown = spec.Cooldown;
            ability.EnergyCost = spec.EnergyCost;
            ability.Range = spec.Range;
            ability.IndicatorRadius = spec.Radius;
            ability.IndicatorType = spec.Indicator;
            ability.TotalCastDuration = spec.CastDuration;
            ability.LocksMovement = spec.LocksMovement;
            ability.AnimationTriggerName = spec.Trigger;
            ability.Effects = spec.Effects;

            EditorUtility.SetDirty(ability);
            return ability;
        }

        private static IAbilityEffect Damage(float baseDamage, float attackMultiplier)
        {
            return new DamageEffect { BaseDamage = baseDamage, AtkMultiplier = attackMultiplier };
        }

        private static IAbilityEffect Status(StatusDefinitionSO status)
        {
            return new ApplyStatusEffect { Status = status };
        }

        private static IAbilityEffect Knockback(float distance)
        {
            return new DisplacementEffect { Type = DisplacementType.Knockback, Distance = distance };
        }

        private static IAbilityEffect Knockup(float height)
        {
            return new DisplacementEffect { Type = DisplacementType.Knockup, Distance = height, Duration = 0.6f };
        }

        private static IAbilityEffect Vfx(string prefabName, EffectOrigin origin, float scale, float lifetime)
        {
            return new VfxCueEffect { Prefab = LoadVfx(prefabName), Origin = origin, Scale = scale, Lifetime = lifetime };
        }

        private static IAbilityEffect Delayed(float delay, params IAbilityEffect[] effects)
        {
            return new DelayedEffect { Delay = delay, Effects = new List<IAbilityEffect>(effects) };
        }

        /// <summary>Chạy nhiều effect cùng lúc bằng một DelayedEffect không trễ.</summary>
        private static IAbilityEffect Group(params IAbilityEffect[] effects)
        {
            return Delayed(0f, effects);
        }

        private static IAbilityEffect Area(EffectOrigin origin, float radius, params IAbilityEffect[] hitEffects)
        {
            return Area(origin, radius, 360f, hitEffects);
        }

        private static IAbilityEffect Area(EffectOrigin origin, float radius, float arcAngle, params IAbilityEffect[] hitEffects)
        {
            return new AreaEffect
            {
                Origin = origin,
                Radius = radius,
                ArcAngle = arcAngle,
                HitEffects = new List<IAbilityEffect>(hitEffects)
            };
        }

        private static IAbilityEffect AllyArea(float radius, params IAbilityEffect[] hitEffects)
        {
            return new AreaEffect
            {
                Origin = EffectOrigin.Caster,
                Team = TargetTeam.Allies,
                Radius = radius,
                HitEffects = new List<IAbilityEffect>(hitEffects)
            };
        }

        private static BasicAttackSO BuildBasicAttack(GameElement element, bool isMelee)
        {
            string path = $"{KitFolder}/Basic_{element}.asset";

            if (!isMelee && AssetDatabase.LoadAssetAtPath<BasicAttackSO>(path) == null)
            {
                EnsureFolder(KitFolder);
                AssetDatabase.CopyAsset(RangedBasicAttackPath, path);
            }

            var attack = GetOrCreate<BasicAttackSO>(path);
            attack.Element = element;
            attack.IsMelee = isMelee;

            if (isMelee)
            {
                attack.AttackCooldown = 0.6f;
                attack.DamageMultiplier = 1f;
                attack.Combo = new List<ComboStep>
                {
                    new ComboStep { DamageMultiplier = 1f, Range = 2.6f, ArcAngle = 120f, LungeDistance = 0.4f },
                    new ComboStep { DamageMultiplier = 1.1f, Range = 2.6f, ArcAngle = 120f, LungeDistance = 0.4f },
                    new ComboStep { DamageMultiplier = 1.6f, Range = 3f, ArcAngle = 180f, LungeDistance = 0.8f }
                };
            }
            else
            {
                attack.ProjectileSpeed = 20f;
                attack.AttackCooldown = 0.8f;
                attack.MaxRange = 12f;
                attack.Combo = new List<ComboStep>();
            }

            EditorUtility.SetDirty(attack);
            return attack;
        }

        private static void AssignHero(int heroIndex, GameElement element, bool isMelee, AbilitySO[] kit)
        {
            BasicAttackSO basicAttack = BuildBasicAttack(element, isMelee);
            string prefabPath = string.Format(HeroPrefabFormat, heroIndex);

            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var hero = new SerializedObject(scope.prefabContentsRoot.GetComponent<HeroInstance>());
                hero.FindProperty("basicAttackConfig").objectReferenceValue = basicAttack;
                AssignList(hero.FindProperty("abilities"), kit);
                hero.ApplyModifiedPropertiesWithoutUndo();

                var ai = new SerializedObject(scope.prefabContentsRoot.GetComponent<HeroAIController>());
                ai.FindProperty("attackRange").floatValue = isMelee ? MeleeAttackRange : RangedAttackRange;
                ai.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static ZoneDamageInstance BuildZonePrefab()
        {
            string path = $"{CombatPrefabFolder}/Zone_Generic.prefab";

            var existing = AssetDatabase.LoadAssetAtPath<ZoneDamageInstance>(path);
            if (existing != null) return existing;

            EnsureFolder(CombatPrefabFolder);

            var zoneObject = new GameObject("Zone_Generic");
            zoneObject.AddComponent<ZoneDamageInstance>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(zoneObject, path);
            Object.DestroyImmediate(zoneObject);

            return prefab.GetComponent<ZoneDamageInstance>();
        }

        private static StoneWallStructure BuildWallPrefab()
        {
            string path = $"{CombatPrefabFolder}/StoneWall.prefab";

            var existing = AssetDatabase.LoadAssetAtPath<StoneWallStructure>(path);
            if (existing != null) return existing;

            EnsureFolder(CombatPrefabFolder);

            var wallObject = new GameObject("StoneWall");
            wallObject.AddComponent<StoneWallStructure>();

            var body = wallObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var trigger = wallObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = Vector3.up * 1.5f;
            trigger.size = new Vector3(5.4f, 3.4f, 1.4f);

            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "Mesh";
            mesh.transform.SetParent(wallObject.transform);
            mesh.transform.localPosition = Vector3.up * 1.5f;
            mesh.transform.localScale = new Vector3(5f, 3f, 0.8f);
            mesh.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(StoneMaterialPath);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wallObject, path);
            Object.DestroyImmediate(wallObject);

            return prefab.GetComponent<StoneWallStructure>();
        }
    }
}
