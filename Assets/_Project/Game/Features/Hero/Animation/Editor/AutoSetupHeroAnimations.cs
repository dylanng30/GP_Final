#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Game.Animation.Hero;
using Game.Data.Hero;

namespace Game.Editor.Hero
{
    public class AutoSetupHeroAnimations : EditorWindow
    {
        [MenuItem("Tools/Hero/Auto Setup ALL Hero Animations (One-Click)")]
        public static void SetupAll()
        {
            Debug.Log("[AutoSetup] Bắt đầu tự động setup animations...");

            string[] guids = AssetDatabase.FindAssets("t:HeroLocalDataSO");
            if (guids.Length == 0)
            {
                Debug.LogError("[AutoSetup] Không tìm thấy HeroLocalDataSO!");
                return;
            }
            
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            HeroLocalDataSO localData = AssetDatabase.LoadAssetAtPath<HeroLocalDataSO>(path);
            
            AnimationClip idleClip = FindClip("Idle");
            AnimationClip walkClip = FindClip("Walk");
            AnimationClip runClip = FindClip("Run");
            AnimationClip jumpClip = FindClip("Jumping");
            AnimationClip fallClip = FindClip("Falling Idle");
            AnimationClip landClip = FindClip("Landing");
            AnimationClip rollClip = FindClip("Rolling");
            AnimationClip attackClip = FindClip("Hook");
            AnimationClip deathClip = FindClip("Hit");

            if (idleClip == null) Debug.LogWarning("[AutoSetup] Cảnh báo: Không tìm thấy Idle.anim trong project.");

            string generatedFolder = "Assets/_Project/Game/Features/Hero/Animation/Generated";
            if (!AssetDatabase.IsValidFolder(generatedFolder))
            {
                System.IO.Directory.CreateDirectory(generatedFolder);
                AssetDatabase.Refresh();
            }

            bool isDataChanged = false;
            for (int i = 0; i < localData.HeroAssets.Count; i++)
            {
                var heroAsset = localData.HeroAssets[i];
                string heroId = heroAsset.HeroId;
                
                if (heroAsset.HeroPrefab == null) continue;

                string configPath = $"{generatedFolder}/{heroId}_AnimConfig.asset";
                HeroAnimationConfig config = AssetDatabase.LoadAssetAtPath<HeroAnimationConfig>(configPath);
                if (config == null)
                {
                    config = ScriptableObject.CreateInstance<HeroAnimationConfig>();
                    AssetDatabase.CreateAsset(config, configPath);
                }
                
                if (heroAsset.AnimationConfig != config)
                {
                    heroAsset.AnimationConfig = config;
                    isDataChanged = true;
                }

                string controllerPath = $"{generatedFolder}/{heroId}_Animator.controller";
                AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var rootSM = controller.layers[0].stateMachine;

                controller.AddParameter(config.SpeedParam, AnimatorControllerParameterType.Float);
                controller.AddParameter(config.IsGroundedParam, AnimatorControllerParameterType.Bool);
                controller.AddParameter(config.IsFallingParam, AnimatorControllerParameterType.Bool);
                controller.AddParameter(config.JumpAnim, AnimatorControllerParameterType.Trigger);
                controller.AddParameter(config.LandAnim, AnimatorControllerParameterType.Trigger);
                controller.AddParameter(config.RollAnim, AnimatorControllerParameterType.Trigger);
                controller.AddParameter(config.BasicAttackAnim, AnimatorControllerParameterType.Trigger);
                controller.AddParameter(config.DeathAnim, AnimatorControllerParameterType.Trigger);

                var locoState = rootSM.AddState("Locomotion", new Vector3(300, 0));
                var blendTree = new BlendTree
                {
                    name = "Locomotion_Blend",
                    blendType = BlendTreeType.Simple1D,
                    blendParameter = config.SpeedParam,
                    useAutomaticThresholds = false
                };
                if (idleClip) blendTree.AddChild(idleClip, 0f);
                if (walkClip) blendTree.AddChild(walkClip, config.WalkThreshold);
                if (runClip) blendTree.AddChild(runClip, config.RunThreshold);
                AssetDatabase.AddObjectToAsset(blendTree, controller);
                controller.SetStateEffectiveMotion(locoState, blendTree);
                rootSM.defaultState = locoState;

                var jumpState = rootSM.AddState(config.JumpAnim, new Vector3(300, -100));
                if (jumpClip) jumpState.motion = jumpClip;

                var fallState = rootSM.AddState("Fall", new Vector3(300, 100));
                if (fallClip) fallState.motion = fallClip;

                var landState = rootSM.AddState(config.LandAnim, new Vector3(600, 0));
                if (landClip) landState.motion = landClip;

                var rollState = rootSM.AddState(config.RollAnim, new Vector3(300, -200));
                if (rollClip) rollState.motion = rollClip;

                var attackState = rootSM.AddState(config.BasicAttackAnim, new Vector3(600, -100));
                if (attackClip) attackState.motion = attackClip;

                var deathState = rootSM.AddState(config.DeathAnim, new Vector3(600, 200));
                if (deathClip) deathState.motion = deathClip;

                var t = locoState.AddTransition(jumpState);
                t.AddCondition(AnimatorConditionMode.If, 0, config.JumpAnim);
                t.duration = 0.1f; t.hasExitTime = false;

                t = jumpState.AddTransition(landState);
                t.AddCondition(AnimatorConditionMode.If, 0, config.IsGroundedParam);
                t.duration = 0.1f; t.hasExitTime = false;

                t = locoState.AddTransition(fallState);
                t.AddCondition(AnimatorConditionMode.If, 0, config.IsFallingParam);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, config.IsGroundedParam);
                t.duration = 0.1f; t.hasExitTime = false;

                t = fallState.AddTransition(landState);
                t.AddCondition(AnimatorConditionMode.If, 0, config.IsGroundedParam);
                t.duration = 0.1f; t.hasExitTime = false;

                t = landState.AddTransition(locoState);
                t.hasExitTime = true; t.exitTime = 0.8f; t.duration = 0.15f;

                t = locoState.AddTransition(rollState);
                t.AddCondition(AnimatorConditionMode.If, 0, config.RollAnim);
                t.duration = 0.05f; t.hasExitTime = false;
                t = rollState.AddTransition(locoState);
                t.hasExitTime = true; t.exitTime = 0.85f; t.duration = 0.15f;

                t = locoState.AddTransition(attackState);
                t.AddCondition(AnimatorConditionMode.If, 0, config.BasicAttackAnim);
                t.duration = 0.05f; t.hasExitTime = false;
                t = attackState.AddTransition(locoState);
                t.hasExitTime = true; t.exitTime = 0.75f; t.duration = 0.2f;

                t = rootSM.AddAnyStateTransition(deathState);
                t.AddCondition(AnimatorConditionMode.If, 0, config.DeathAnim);
                t.hasExitTime = false; t.duration = 0.1f; t.canTransitionToSelf = false;

                HeroAnimatorTransitionFixer.Apply(controller);
                EditorUtility.SetDirty(controller);

                string prefabPath = AssetDatabase.GetAssetPath(heroAsset.HeroPrefab);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
                    {
                        var prefabRoot = editingScope.prefabContentsRoot;
                        var animator = prefabRoot.GetComponent<Animator>();
                        if (animator != null)
                        {
                            animator.runtimeAnimatorController = controller;
                            
                            var heroAnimCtrl = prefabRoot.GetComponent<HeroAnimatorController>();
                            if (heroAnimCtrl != null)
                            {
                                var configField = typeof(HeroAnimatorController).GetField("_config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (configField != null)
                                {
                                    configField.SetValue(heroAnimCtrl, config);
                                }
                            }
                        }
                    }
                }
            }

            if (isDataChanged) EditorUtility.SetDirty(localData);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AutoSetup] HOÀN TẤT! Toàn bộ Hero đã có Animation Controller với Clip thật. Bạn có thể Play ngay.");
        }

        private static AnimationClip FindClip(string clipName)
        {
            string[] guids = AssetDatabase.FindAssets($"{clipName} t:AnimationClip");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith($"{clipName}.anim"))
                {
                    return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                }
            }
            return null;
        }
    }
}
#endif
