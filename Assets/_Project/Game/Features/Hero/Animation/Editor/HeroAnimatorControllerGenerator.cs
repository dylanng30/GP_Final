// ============================================================================
// HeroAnimatorControllerGenerator.cs
// Editor tool – tự động tạo AnimatorController cho Hero.
//
// Cách dùng:
//   Tools > Hero > Generate Hero Animator Controller
//   Chọn HeroAnimationConfig SO → nhấn Generate
//
// Cấu trúc AnimatorController được tạo ra:
//   Base Layer:
//     [Entry] ──► Locomotion (BlendTree: Idle / Walk / Run)
//
//     Locomotion ──(Trigger: Jump)──► Jump
//     Jump       ──(Bool: IsGrounded)──► Land
//     Locomotion ──(Bool: IsFalling + !IsGrounded)──► Fall
//     Fall       ──(Bool: IsGrounded)──► Land
//     Land       ──(ExitTime 0.8)──► Locomotion
//
//     Locomotion ──(Trigger: Roll)──► Roll ──(ExitTime 0.85)──► Locomotion
//
//     Locomotion ──(Trigger: Attack)──► Attack ──(ExitTime 0.75)──► Locomotion
//     Locomotion ──(Trigger: Skill_N)──► Skill_N ──(ExitTime 0.9)──► Locomotion
//
//     AnyState ──(Trigger: Death)──► Death  [không có exit]
// ============================================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Game.Animation.Hero;

namespace Game.Editor.Hero
{
    public class HeroAnimatorControllerGenerator : EditorWindow
    {
        // ─── GUI State ────────────────────────────────────────────────────────
        private HeroAnimationConfig _config;
        private string _savePath = "Assets/_Project/Game/Features/Hero/Animation/Generated";
        private string _controllerName = "Hero_AnimatorController";

        // ─── Layout Constants ─────────────────────────────────────────────────
        private const float ColSpacing = 280f;
        private const float RowSpacing = 70f;

        // ─── Menu Item ────────────────────────────────────────────────────────
        [MenuItem("Tools/Hero/Generate Hero Animator Controller")]
        public static void OpenWindow()
        {
            var window = GetWindow<HeroAnimatorControllerGenerator>("Hero Animator Generator");
            window.minSize = new Vector2(440f, 310f);
        }

        // ─── GUI ──────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            GUILayout.Space(8f);
            EditorGUILayout.LabelField("Hero Animator Controller Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Tự động tạo AnimatorController với đầy đủ Parameters, States và Transitions " +
                "phù hợp với HeroAnimationConfig được chọn.",
                MessageType.Info);

            GUILayout.Space(8f);

            EditorGUI.BeginChangeCheck();
            _config = (HeroAnimationConfig)EditorGUILayout.ObjectField(
                new GUIContent("Hero Animation Config",
                    "ScriptableObject chứa tên parameter / trigger của hero này."),
                _config, typeof(HeroAnimationConfig), false);

            if (EditorGUI.EndChangeCheck() && _config != null)
                _controllerName = _config.name.Replace("AnimConfig", "AnimatorController");

            GUILayout.Space(6f);
            _savePath       = EditorGUILayout.TextField(
                new GUIContent("Save Folder", "Thư mục lưu file .controller"), _savePath);
            _controllerName = EditorGUILayout.TextField(
                new GUIContent("File Name", "Tên file (không cần .controller)"), _controllerName);

            GUILayout.Space(12f);

            using (new EditorGUI.DisabledScope(_config == null))
            {
                if (GUILayout.Button("✦ Generate AnimatorController", GUILayout.Height(36f)))
                    GenerateController();
            }

            if (_config == null)
                EditorGUILayout.HelpBox("Hãy gán một HeroAnimationConfig để bắt đầu.", MessageType.Warning);
        }

        // ─── Core Generation ──────────────────────────────────────────────────
        private void GenerateController()
        {
            _config.InitializeHashes();

            // Đảm bảo thư mục tồn tại
            EnsureFolderExists(_savePath);

            string fullPath = $"{_savePath}/{_controllerName}.controller";

            // Tạo controller
            var controller = AnimatorController.CreateAnimatorControllerAtPath(fullPath);
            var rootSM     = controller.layers[0].stateMachine;

            // ── Parameters ─────────────────────────────────────────────────
            AddParam(controller, _config.SpeedParam,      AnimatorControllerParameterType.Float);
            AddParam(controller, _config.IsGroundedParam, AnimatorControllerParameterType.Bool);
            AddParam(controller, _config.IsFallingParam,  AnimatorControllerParameterType.Bool);
            AddParam(controller, _config.JumpAnim,        AnimatorControllerParameterType.Trigger);
            AddParam(controller, _config.LandAnim,        AnimatorControllerParameterType.Trigger);
            AddParam(controller, _config.RollAnim,        AnimatorControllerParameterType.Trigger);
            AddParam(controller, _config.BasicAttackAnim, AnimatorControllerParameterType.Trigger);
            AddParam(controller, _config.DeathAnim,       AnimatorControllerParameterType.Trigger);
            foreach (string s in _config.SkillAnims)
                AddParam(controller, s, AnimatorControllerParameterType.Trigger);

            // ── States ─────────────────────────────────────────────────────

            // Cột 0 – Locomotion
            var locomotionState = rootSM.AddState("Locomotion", new Vector3(0f, 0f));
            locomotionState.tag = "Locomotion";
            var blendTree = new BlendTree
            {
                name                   = "Locomotion_BlendTree",
                blendType              = BlendTreeType.Simple1D,
                blendParameter         = _config.SpeedParam,
                useAutomaticThresholds = false
            };
            blendTree.AddChild(EmptyClip(controller, "Idle"), 0f);
            blendTree.AddChild(EmptyClip(controller, "Walk"), _config.WalkThreshold);
            blendTree.AddChild(EmptyClip(controller, "Run"),  _config.RunThreshold);
            controller.SetStateEffectiveMotion(locomotionState, blendTree);
            rootSM.defaultState = locomotionState;

            // Cột 1 – Air states
            var jumpState = rootSM.AddState(_config.JumpAnim, new Vector3(ColSpacing,  -RowSpacing));
            jumpState.motion = EmptyClip(controller, _config.JumpAnim);
            jumpState.tag    = "Jump";

            var fallState = rootSM.AddState("Fall", new Vector3(ColSpacing,  RowSpacing));
            fallState.motion = EmptyClip(controller, "Fall");
            fallState.tag    = "Fall";

            var landState = rootSM.AddState(_config.LandAnim, new Vector3(ColSpacing * 1.6f, 0f));
            landState.motion = EmptyClip(controller, _config.LandAnim);
            landState.tag    = "Land";

            // Cột 2 – Actions
            float actionBaseX = ColSpacing * 2.4f;
            var rollState   = rootSM.AddState(_config.RollAnim,        new Vector3(actionBaseX, -RowSpacing * 2.5f));
            rollState.motion   = EmptyClip(controller, _config.RollAnim);
            rollState.tag      = "Action";

            var attackState = rootSM.AddState(_config.BasicAttackAnim, new Vector3(actionBaseX, 0f));
            attackState.motion = EmptyClip(controller, _config.BasicAttackAnim);
            attackState.tag    = "Attack";

            var skillStates = new List<AnimatorState>();
            for (int i = 0; i < _config.SkillAnims.Count; i++)
            {
                string sn  = _config.SkillAnims[i];
                var sk     = rootSM.AddState(sn, new Vector3(actionBaseX, RowSpacing * (i + 1)));
                sk.motion  = EmptyClip(controller, sn);
                sk.tag     = "Skill";
                skillStates.Add(sk);
            }

            // Cột 3 – Death
            float deathX = ColSpacing * 3.4f;
            float deathY = RowSpacing * (_config.SkillAnims.Count + 2);
            var deathState = rootSM.AddState(_config.DeathAnim, new Vector3(deathX, deathY));
            deathState.motion = EmptyClip(controller, _config.DeathAnim);
            deathState.tag    = "Death";

            // ── Transitions ────────────────────────────────────────────────

            // Locomotion → Jump
            Transition(locomotionState, jumpState,
                condition: (AnimatorConditionMode.If, 0f, _config.JumpAnim),
                duration: 0.1f, hasExit: false);

            // Jump → Land (IsGrounded)
            Transition(jumpState, landState,
                condition: (AnimatorConditionMode.If, 0f, _config.IsGroundedParam),
                duration: 0.1f, hasExit: false);

            // Locomotion → Fall (IsFalling AND !IsGrounded)
            {
                var t = locomotionState.AddTransition(fallState);
                t.AddCondition(AnimatorConditionMode.If,    0f, _config.IsFallingParam);
                t.AddCondition(AnimatorConditionMode.IfNot, 0f, _config.IsGroundedParam);
                t.duration    = 0.15f;
                t.hasExitTime = false;
            }

            // Fall → Land (IsGrounded)
            Transition(fallState, landState,
                condition: (AnimatorConditionMode.If, 0f, _config.IsGroundedParam),
                duration: 0.1f, hasExit: false);

            // Land → Locomotion (exit time)
            ExitTimeTransition(landState, locomotionState, exitTime: 0.80f, duration: 0.15f);

            // Locomotion → Roll
            Transition(locomotionState, rollState,
                condition: (AnimatorConditionMode.If, 0f, _config.RollAnim),
                duration: 0.05f, hasExit: false);

            // Roll → Locomotion (exit time)
            ExitTimeTransition(rollState, locomotionState, exitTime: 0.85f, duration: 0.15f);

            // Locomotion → Attack
            Transition(locomotionState, attackState,
                condition: (AnimatorConditionMode.If, 0f, _config.BasicAttackAnim),
                duration: 0.05f, hasExit: false);

            // Attack → Locomotion (exit time)
            ExitTimeTransition(attackState, locomotionState, exitTime: 0.75f, duration: 0.20f);

            // Locomotion ↔ Skill_N
            for (int i = 0; i < skillStates.Count; i++)
            {
                Transition(locomotionState, skillStates[i],
                    condition: (AnimatorConditionMode.If, 0f, _config.SkillAnims[i]),
                    duration: 0.05f, hasExit: false);

                ExitTimeTransition(skillStates[i], locomotionState, exitTime: 0.90f, duration: 0.15f);
            }

            // AnyState → Death (ưu tiên cao nhất, không tự exit)
            {
                var t = rootSM.AddAnyStateTransition(deathState);
                t.AddCondition(AnimatorConditionMode.If, 0f, _config.DeathAnim);
                t.duration              = 0.1f;
                t.hasExitTime           = false;
                t.canTransitionToSelf   = false;
            }

            // Nối transition hành động qua AnyState, nhảy/rơi/tiếp đất theo cùng quy tắc với các controller cũ.
            HeroAnimatorTransitionFixer.Apply(controller);

            // ── Lưu ──────────────────────────────────────────────────────
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[HeroAnimatorGenerator] ✓ Đã tạo: {fullPath}");
            EditorUtility.DisplayDialog(
                "✓ Thành công",
                $"AnimatorController đã được tạo tại:\n{fullPath}\n\n" +
                "Tiếp theo:\n" +
                "1. Kéo file .controller vào Animator component trên Hero Prefab.\n" +
                "2. Gán animation clip thật vào từng state (thay thế placeholder).\n" +
                "3. Đảm bảo HeroAnimationConfig SO đã được gán vào HeroAsset.",
                "OK");

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimatorController>(fullPath);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private static void AddParam(AnimatorController ctrl, string name, AnimatorControllerParameterType type)
        {
            if (string.IsNullOrEmpty(name)) return;
            foreach (var p in ctrl.parameters)
                if (p.name == name) return;
            ctrl.AddParameter(name, type);
        }

        private static AnimationClip EmptyClip(AnimatorController ctrl, string clipName)
        {
            string path = AssetDatabase.GetAssetPath(ctrl);
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
                if (sub is AnimationClip c && c.name == clipName)
                    return c;

            var clip = new AnimationClip { name = clipName };
            AssetDatabase.AddObjectToAsset(clip, ctrl);
            return clip;
        }

        private static void Transition(
            AnimatorState from, AnimatorState to,
            (AnimatorConditionMode mode, float threshold, string param) condition,
            float duration, bool hasExit)
        {
            var t = from.AddTransition(to);
            t.AddCondition(condition.mode, condition.threshold, condition.param);
            t.duration    = duration;
            t.hasExitTime = hasExit;
        }

        private static void ExitTimeTransition(
            AnimatorState from, AnimatorState to,
            float exitTime, float duration)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = true;
            t.exitTime    = exitTime;
            t.duration    = duration;
        }

        private static void EnsureFolderExists(string path)
        {
            string[] parts   = path.Split('/');
            string   current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
