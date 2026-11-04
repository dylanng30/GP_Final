using System.Collections.Generic;
using Dylanng;
using Dylanng.Gameplay.RPG.Abilities;
using Dylanng.Gameplay.RPG.Abilities.Effects;
using Game.Data.Hero;
using UnityEditor;

namespace Game.Editor.Combat
{
    public static partial class CombatContentBuilder
    {
        private const string AbilityFolder = SoRoot + "/Abilities";
        private const string FireballPrefabPath = "Assets/_Project/02_Prefabs/Projectiles/FireBall_Demo.prefab";

        private static void BuildAbilities(StatusSet statuses)
        {
            BuildFireball(statuses);
        }

        private static void BuildFireball(StatusSet statuses)
        {
            var ability = AssetDatabase.LoadAssetAtPath<AbilitySO>($"{AbilityFolder}/Skill_FireBall.asset");
            ability.AbilityName = "Fireball";
            ability.Element = GameElement.Fire;
            ability.Effects = new List<IAbilityEffect>
            {
                new ProjectileEffect
                {
                    ProjectilePrefab = AssetDatabase.LoadAssetAtPath<PoolableObject>(FireballPrefabPath),
                    CollisionEffects = new List<IAbilityEffect>
                    {
                        new DamageEffect { BaseDamage = 100f, AtkMultiplier = 1.5f, UseFalloff = true },
                        new ApplyStatusEffect { Status = statuses.Burn }
                    }
                }
            };
            EditorUtility.SetDirty(ability);
        }
    }
}
