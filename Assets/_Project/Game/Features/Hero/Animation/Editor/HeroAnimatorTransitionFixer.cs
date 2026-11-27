#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.Editor.Hero
{
    /// <summary>
    /// Nối lại transition của Animator Controller hero, giữ nguyên clip đã gán.
    /// Menu: Tools > Hero > Fix Animator Transitions (áp cho mọi controller trong thư mục Generated).
    ///
    ///   AnyState ──(Attack | Roll | Skill_N)──► state đó, chen ngang được mọi state trừ Death
    ///   Locomotion ──(Jump)──► Jump,  Locomotion ──(IsFalling)──► Fall
    ///   Jump/Fall ──(IsGrounded + Land)──► Land, ──(IsGrounded)──► Locomotion
    ///   Jump ──(hết clip, còn trên không)──► Fall
    ///   Land ──(ExitTime)──► Locomotion
    ///   Skill_N chạy theo tốc độ của parameter ActionSpeed
    ///
    /// Chạy lại bao nhiêu lần cũng được.
    /// </summary>
    public static class HeroAnimatorTransitionFixer
    {
        private const string GeneratedFolder = "Assets/_Project/Game/Features/Hero/Animation/Generated";

        private const string Locomotion = "Locomotion";
        private const string AttackState = "Attack";
        private const string RollState = "Roll";
        private const string JumpState = "Jump";
        private const string FallState = "Fall";
        private const string LandState = "Land";
        private const string SkillPrefix = "Skill_";

        private const string SpeedParam = "Speed";
        private const string IsGroundedParam = "IsGrounded";
        private const string IsFallingParam = "IsFalling";
        private const string ActionSpeedParam = "ActionSpeed";

        // Thời gian blend (giây, cố định).
        private const float ActionBlend = 0.08f;
        private const float RollBlend = 0.05f;
        private const float JumpBlend = 0.08f;
        private const float FallBlend = 0.15f;
        private const float LandingBlend = 0.12f;
        private const float LandExitTime = 0.55f;
        private const float LandExitBlend = 0.2f;
        private const float JumpToFallExitTime = 0.9f;
        // Đang di chuyển thì thoát Land sớm để không trượt trong tư thế tiếp đất.
        private const float LandMoveExitTime = 0.2f;
        private const float LandMoveSpeed = 0.3f;

        // Đường ra mặc định khi state hành động chưa có.
        private const float AttackExitTime = 0.75f;
        private const float AttackExitBlend = 0.15f;
        private const float SkillExitTime = 0.9f;
        private const float SkillExitBlend = 0.15f;
        private const float RollExitTime = 0.85f;
        private const float RollExitBlend = 0.15f;

        [MenuItem("Tools/Hero/Fix Animator Transitions")]
        public static void FixAllMenu()
        {
            Debug.Log(FixAll());
        }

        public static string FixAll()
        {
            var report = new StringBuilder("[HeroAnimatorTransitionFixer]\n");
            foreach (string guid in AssetDatabase.FindAssets("t:AnimatorController", new[] { GeneratedFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                report.AppendLine(path + ": " + Apply(controller));
            }
            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        /// <summary>Sửa một controller. Trả về mô tả ngắn, hoặc lý do bỏ qua.</summary>
        public static string Apply(AnimatorController controller)
        {
            if (controller == null || controller.layers.Length == 0) return "bỏ qua (không có layer)";

            AnimatorStateMachine root = controller.layers[0].stateMachine;
            Dictionary<string, AnimatorState> states = root.states.ToDictionary(s => s.state.name, s => s.state);
            if (!states.TryGetValue(Locomotion, out AnimatorState locomotion)) return "bỏ qua (không có state Locomotion)";

            EnsureFloatParam(controller, ActionSpeedParam, 1f);

            var actionStates = new List<AnimatorState>();
            if (states.TryGetValue(AttackState, out AnimatorState attack)) actionStates.Add(attack);
            if (states.TryGetValue(RollState, out AnimatorState roll)) actionStates.Add(roll);
            List<AnimatorState> skills = states.Values.Where(s => s.name.StartsWith(SkillPrefix)).OrderBy(s => s.name).ToList();
            actionStates.AddRange(skills);

            // 1. Hành động vào từ AnyState để chen ngang được Land, Jump, Fall và hành động khác.
            foreach (AnimatorState action in actionStates)
            {
                RemoveTransitionsInto(root, action);
                if (!HasParam(controller, action.name)) continue;

                AnimatorStateTransition t = root.AddAnyStateTransition(action);
                t.AddCondition(AnimatorConditionMode.If, 0f, action.name);
                Configure(t, action == roll ? RollBlend : ActionBlend);
                t.canTransitionToSelf = true;
            }

            // 2. Đường ra của state hành động: giữ cái đã có, thêm nếu thiếu.
            if (attack != null) EnsureExit(attack, locomotion, AttackExitTime, AttackExitBlend);
            if (roll != null) EnsureExit(roll, locomotion, RollExitTime, RollExitBlend);
            foreach (AnimatorState skill in skills)
            {
                EnsureExit(skill, locomotion, SkillExitTime, SkillExitBlend);
                skill.speedParameter = ActionSpeedParam;
                skill.speedParameterActive = true;
            }

            // 3. Nhảy, rơi, tiếp đất.
            states.TryGetValue(JumpState, out AnimatorState jump);
            states.TryGetValue(FallState, out AnimatorState fall);
            states.TryGetValue(LandState, out AnimatorState land);

            RemoveTransitions(locomotion, t => t.destinationState == jump || t.destinationState == fall);
            if (jump != null && HasParam(controller, JumpState))
            {
                AnimatorStateTransition t = locomotion.AddTransition(jump);
                t.AddCondition(AnimatorConditionMode.If, 0f, JumpState);
                Configure(t, JumpBlend);
            }
            if (fall != null)
            {
                AnimatorStateTransition t = locomotion.AddTransition(fall);
                t.AddCondition(AnimatorConditionMode.If, 0f, IsFallingParam);
                Configure(t, FallBlend);
            }

            foreach (AnimatorState air in new[] { jump, fall })
            {
                if (air == null) continue;

                RemoveTransitions(air, _ => true);
                if (land != null && HasParam(controller, LandState))
                {
                    AnimatorStateTransition hard = air.AddTransition(land);
                    hard.AddCondition(AnimatorConditionMode.If, 0f, IsGroundedParam);
                    hard.AddCondition(AnimatorConditionMode.If, 0f, LandState);
                    Configure(hard, LandingBlend);
                }

                AnimatorStateTransition soft = air.AddTransition(locomotion);
                soft.AddCondition(AnimatorConditionMode.If, 0f, IsGroundedParam);
                Configure(soft, LandingBlend);
            }

            if (jump != null && fall != null)
            {
                // Clip nhảy không lặp: hết clip mà vẫn trên không thì sang Fall, không đứng ở frame cuối.
                AnimatorStateTransition t = jump.AddTransition(fall);
                t.AddCondition(AnimatorConditionMode.IfNot, 0f, IsGroundedParam);
                Configure(t, FallBlend);
                t.hasExitTime = true;
                t.exitTime = JumpToFallExitTime;
            }

            if (land != null)
            {
                RemoveTransitions(land, _ => true);
                if (HasParam(controller, SpeedParam))
                {
                    AnimatorStateTransition moving = land.AddTransition(locomotion);
                    moving.AddCondition(AnimatorConditionMode.Greater, LandMoveSpeed, SpeedParam);
                    Configure(moving, LandExitBlend);
                    moving.hasExitTime = true;
                    moving.exitTime = LandMoveExitTime;
                }

                AnimatorStateTransition t = land.AddTransition(locomotion);
                Configure(t, LandExitBlend);
                t.hasExitTime = true;
                t.exitTime = LandExitTime;
            }

            EditorUtility.SetDirty(controller);
            return $"{actionStates.Count} hành động vào từ AnyState, nhảy {(jump != null ? "có" : "không")}, rơi {(fall != null ? "có" : "không")}, tiếp đất {(land != null ? "có" : "không")}";
        }

        private static void Configure(AnimatorStateTransition t, float duration)
        {
            t.hasExitTime = false;
            t.hasFixedDuration = true;
            t.duration = duration;
            t.offset = 0f;
            t.interruptionSource = TransitionInterruptionSource.None;
        }

        private static void EnsureExit(AnimatorState from, AnimatorState to, float exitTime, float duration)
        {
            if (from.transitions.Any(t => t.destinationState == to && t.hasExitTime)) return;

            AnimatorStateTransition exit = from.AddTransition(to);
            Configure(exit, duration);
            exit.hasExitTime = true;
            exit.exitTime = exitTime;
        }

        private static void RemoveTransitionsInto(AnimatorStateMachine root, AnimatorState target)
        {
            foreach (AnimatorStateTransition t in root.anyStateTransitions.Where(t => t.destinationState == target).ToList())
            {
                root.RemoveAnyStateTransition(t);
            }
            foreach (ChildAnimatorState child in root.states)
            {
                RemoveTransitions(child.state, t => t.destinationState == target);
            }
        }

        private static void RemoveTransitions(AnimatorState state, System.Func<AnimatorStateTransition, bool> match)
        {
            foreach (AnimatorStateTransition t in state.transitions.Where(match).ToList())
            {
                state.RemoveTransition(t);
            }
        }

        private static bool HasParam(AnimatorController controller, string name)
        {
            return controller.parameters.Any(p => p.name == name);
        }

        private static void EnsureFloatParam(AnimatorController controller, string name, float defaultValue)
        {
            if (HasParam(controller, name)) return;

            controller.AddParameter(new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = defaultValue
            });
        }
    }
}
#endif
