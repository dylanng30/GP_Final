using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dylanng;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Game.Combat;
using Game.Data.Hero;
using Game.Events;
using Game.Gameplay.Entities;
using Game.Managers.Party;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.Editor.Combat
{
    /// <summary>
    /// Chạy tự động các mục chính của checklist trong Combat Sandbox, không cần người chơi.
    /// Dòng lệnh: Unity -batchmode -projectPath [path] -executeMethod Game.Editor.Combat.CombatSandboxSmokeTest.Run
    /// Mã thoát: 0 = đạt, 1 = có mục lỗi, 2 = quá thời gian.
    /// </summary>
    public static class CombatSandboxSmokeTest
    {
        private const string RunningKey = "CombatSandboxSmokeTest.Running";
        private const string ReportPath = "Logs/CombatSandboxSmoke.txt";
        private const string SandboxSceneName = "Combat_Sandbox";

        private const double GlobalTimeoutSeconds = 240d;
        private const int MinimumDummyCount = 5;
        private const float FullEnergy = 200f;
        private const float ProbeDamage = 1000f;
        private const float OvercomesMultiplier = 1.2f;
        private const float OvercomedByMultiplier = 0.8f;

        private sealed class Step
        {
            public string Name;
            public double Timeout;
            public Action Begin;
            public Func<string> Verify; // Trả về null khi đạt, ngược lại là lý do chưa đạt.
            public Action End;
        }

        private static readonly List<string> Results = new List<string>();
        private static readonly List<string> ConsoleErrors = new List<string>();

        private static List<Step> _steps;
        private static int _stepIndex;
        private static bool _hasBegunStep;
        private static double _startTime;
        private static double _stepStartTime;
        private static int _failedCount;

        private static PartyCombatManager _party;
        private static InputManager _input;
        private static int _damageCount;
        private static int _damageBaseline;
        private static bool _hasSeenFireDamage;
        private static bool _hasCastSkill;
        private static Vector3 _positionBaseline;

        private static HeroInstance ActiveHero => _party != null ? _party.GetActiveHero() : null;
        private static double StepElapsed => EditorApplication.timeSinceStartup - _stepStartTime;

        [MenuItem("Tools/Combat/Run Smoke Test")]
        public static void Run()
        {
            SessionState.SetBool(RunningKey, true);
            CombatSandboxTools.PlaySandbox();
        }

        [InitializeOnLoadMethod]
        private static void ResumeAfterDomainReload()
        {
            if (!SessionState.GetBool(RunningKey, false)) return;

            _startTime = EditorApplication.timeSinceStartup;
            Application.logMessageReceived += OnLogMessage;
            EditorApplication.update += Tick;
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                ConsoleErrors.Add(message);
            }
        }

        private static void OnDamageDealt(DamageDealtEvent evt)
        {
            _damageCount++;
            if (evt.Element == GameElement.Fire) _hasSeenFireDamage = true;
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup - _startTime > GlobalTimeoutSeconds)
            {
                Results.Add("FAIL Quá thời gian chạy toàn bộ");
                Finish(2);
                return;
            }

            if (!EditorApplication.isPlaying) return;

            if (_steps == null)
            {
                if (SceneManager.GetActiveScene().name != SandboxSceneName) return;

                _party = ServiceLocator.Get<PartyCombatManager>();
                if (_party == null || _party.ActiveHeroes.Count == 0) return;

                _input = ServiceLocator.Get<InputManager>();
                EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
                _steps = BuildSteps();
            }

            if (_stepIndex >= _steps.Count)
            {
                ReportConsoleErrors();
                Finish(_failedCount == 0 ? 0 : 1);
                return;
            }

            RunCurrentStep();
        }

        private static void RunCurrentStep()
        {
            Step step = _steps[_stepIndex];

            if (!_hasBegunStep)
            {
                _hasBegunStep = true;
                _stepStartTime = EditorApplication.timeSinceStartup;
                step.Begin?.Invoke();
                return;
            }

            string failure = step.Verify();
            bool isPassed = failure == null;
            if (!isPassed && StepElapsed < step.Timeout) return;

            Results.Add(isPassed ? $"PASS {step.Name}" : $"FAIL {step.Name}: {failure}");
            if (!isPassed) _failedCount++;

            step.End?.Invoke();
            _stepIndex++;
            _hasBegunStep = false;
        }

        private static List<Step> BuildSteps()
        {
            return new List<Step>
            {
                new Step
                {
                    Name = "Sandbox đã tải đủ đối tượng",
                    Timeout = 5d,
                    Verify = VerifySceneContent
                },
                new Step
                {
                    Name = "Hero nhận hệ từ dữ liệu",
                    Timeout = 1d,
                    Verify = VerifyHeroElements
                },
                new Step
                {
                    Name = "Khắc hệ thay đổi sát thương",
                    Timeout = 1d,
                    Verify = VerifyElementMultipliers
                },
                new Step
                {
                    Name = "Auto combat gây sát thương",
                    Timeout = 30d,
                    Begin = MarkDamageBaseline,
                    Verify = VerifyDamageIncreased,
                    End = () => CaptureCamera("sandbox_auto.png")
                },
                new Step
                {
                    Name = "Tắt auto chuyển sang góc nhìn thứ 3",
                    Timeout = 6d,
                    Begin = () => EventBus.Publish(new ToggleFollowModeEvent(false)),
                    Verify = VerifyManualMode,
                    End = () => CaptureCamera("sandbox_third_person.png")
                },
                new Step
                {
                    Name = "Joystick di chuyển hero",
                    Timeout = 4d,
                    Begin = StartMoving,
                    Verify = () => VerifyMovedAtLeast(0.5f),
                    End = StopMoving
                },
                new Step
                {
                    Name = "Nhảy rời mặt đất",
                    Timeout = 3d,
                    Begin = () => ActiveHero.Jump(),
                    Verify = () => ActiveHero.IsGrounded ? "Hero vẫn chạm đất" : null
                },
                new Step
                {
                    Name = "Dash di chuyển hero",
                    Timeout = 3d,
                    Begin = StartDash,
                    Verify = () => VerifyMovedAtLeast(2f)
                },
                new Step
                {
                    Name = "Đánh thường gây sát thương",
                    Timeout = 6d,
                    Begin = StartBasicAttack,
                    Verify = VerifyDamageIncreased
                },
                new Step
                {
                    Name = "Skill vào cooldown và gây sát thương",
                    Timeout = 8d,
                    Begin = StartSkill,
                    Verify = VerifySkillResult
                },
                new Step
                {
                    Name = "Đổi hero điều khiển",
                    Timeout = 3d,
                    Begin = SwitchToNextHero,
                    Verify = VerifyHeroSwitched
                },
                new Step
                {
                    Name = "Chạy ổn định thêm 3 giây",
                    Timeout = 4d,
                    Verify = () => StepElapsed >= 3d ? null : "Đang chờ"
                }
            };
        }

        private static string VerifySceneContent()
        {
            int dummyCount = Object.FindObjectsOfType<DummyBotInstance>().Length;
            if (dummyCount < MinimumDummyCount) return $"Chỉ có {dummyCount} DummyBot";
            if (GameObject.Find("Portal_A_Out") != null) return "Portal chưa bị gỡ";
            if (Camera.main == null) return "Không có Main Camera";
            return null;
        }

        private static string VerifyHeroElements()
        {
            IReadOnlyList<HeroInstance> heroes = _party.ActiveHeroes;
            if (heroes[0].Element != GameElement.Fire) return $"Hero đầu có hệ {heroes[0].Element}";
            if (heroes.Count > 1 && heroes[1].Element != GameElement.Wood) return $"Hero thứ hai có hệ {heroes[1].Element}";
            return null;
        }

        private static string VerifyElementMultipliers()
        {
            float neutral = DealProbe(GameElement.Earth);
            float overcomes = DealProbe(GameElement.Metal);
            float overcomedBy = DealProbe(GameElement.Water);

            if (neutral <= 0f) return "Không gây được sát thương thử";
            if (!Mathf.Approximately(overcomes / neutral, OvercomesMultiplier)) return $"Khắc = {overcomes / neutral}";
            if (!Mathf.Approximately(overcomedBy / neutral, OvercomedByMultiplier)) return $"Bị khắc = {overcomedBy / neutral}";
            return null;
        }

        /// <summary>Gây sát thương hệ Hỏa lên DummyBot mang hệ chỉ định, không có nguồn nên không chí mạng.</summary>
        private static float DealProbe(GameElement targetElement)
        {
            DummyBotInstance dummy = Object.FindObjectsOfType<DummyBotInstance>()
                .FirstOrDefault(candidate => candidate.Element == targetElement);
            if (dummy == null) return 0f;

            return ServiceLocator.Get<CombatResolver>()
                .Deal(null, dummy.gameObject, ProbeDamage, GameElement.Fire, dummy.transform.position, canCrit: false)
                .Amount;
        }

        private static void MarkDamageBaseline() => _damageBaseline = _damageCount;

        private static string VerifyDamageIncreased()
        {
            return _damageCount > _damageBaseline ? null : "Chưa có sự kiện sát thương";
        }

        private static string VerifyManualMode()
        {
            if (_party.IsFollowModeEnabled) return "Vẫn ở chế độ auto";
            if (StepElapsed < 1.5d) return "Đang chờ camera chuyển";
            return Mathf.Approximately(Time.timeScale, 1f) ? null : $"timeScale = {Time.timeScale}";
        }

        private static void StartMoving()
        {
            _positionBaseline = ActiveHero.transform.position;
            _input.SetJoystickActive(true);
            _input.SetMovementInput(Vector2.up);
        }

        private static void StopMoving()
        {
            _input.SetMovementInput(Vector2.zero);
            _input.SetJoystickActive(false);
        }

        private static string VerifyMovedAtLeast(float distance)
        {
            float moved = Vector3.Distance(_positionBaseline, ActiveHero.transform.position);
            return moved >= distance ? null : $"Mới di chuyển {moved:F2}m";
        }

        private static void StartDash()
        {
            _positionBaseline = ActiveHero.transform.position;
            ActiveHero.Dash();
        }

        private static DummyBotInstance FindNearestDummy()
        {
            Vector3 heroPosition = ActiveHero.transform.position;
            return Object.FindObjectsOfType<DummyBotInstance>()
                .Where(dummy => !dummy.IsDead)
                .OrderBy(dummy => (dummy.transform.position - heroPosition).sqrMagnitude)
                .FirstOrDefault();
        }

        private static void StartBasicAttack()
        {
            MarkDamageBaseline();
            ActiveHero.Attack(FindNearestDummy());
        }

        private static void StartSkill()
        {
            MarkDamageBaseline();
            _hasCastSkill = false;
            ActiveHero.GainEnergy(FullEnergy);
        }

        private static string VerifySkillResult()
        {
            if (ActiveHero.GetAbility(0) == null) return "Hero không có skill ở slot 0";
            if (_hasCastSkill) return _hasSeenFireDamage ? null : "Chưa có sát thương hệ Hỏa";

            // AI có thể vừa dùng skill trước khi tắt auto, nên chờ hết cooldown rồi mới cast.
            if (!ActiveHero.CanUseSkill(0)) return "Đang chờ hết cooldown";

            _hasSeenFireDamage = false;
            ActiveHero.UseSkill(0, FindNearestDummy()?.gameObject);
            _hasCastSkill = true;

            return ActiveHero.GetSkillRemainingCooldown(0) > 0f ? "Đang chờ skill trúng" : "Skill không vào cooldown";
        }

        private static void SwitchToNextHero()
        {
            if (_party.ActiveHeroes.Count > 1) _party.SwitchActiveHero(1);
        }

        private static string VerifyHeroSwitched()
        {
            IReadOnlyList<HeroInstance> heroes = _party.ActiveHeroes;
            if (heroes.Count < 2) return null;
            return ActiveHero == heroes[1] ? null : "Hero điều khiển chưa đổi";
        }

        private static void ReportConsoleErrors()
        {
            if (ConsoleErrors.Count == 0)
            {
                Results.Add("PASS Không có lỗi trong Console");
                return;
            }

            _failedCount++;
            Results.Add($"FAIL Console có {ConsoleErrors.Count} lỗi");
            Results.AddRange(ConsoleErrors.Distinct().Take(10).Select(error => $"  - {error}"));
        }

        private static void CaptureCamera(string fileName)
        {
            Camera camera = Camera.main;
            if (camera == null) return;

            const int width = 1280;
            const int height = 720;
            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24);
            RenderTexture previousTarget = camera.targetTexture;

            camera.targetTexture = renderTexture;
            camera.Render();
            camera.targetTexture = previousTarget;

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previousActive;

            File.WriteAllBytes(Path.Combine("Logs", fileName), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            RenderTexture.ReleaseTemporary(renderTexture);
        }

        private static void Finish(int exitCode)
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLogMessage;
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            SessionState.EraseBool(RunningKey);

            var report = new StringBuilder();
            foreach (string result in Results) report.AppendLine(result);
            report.AppendLine($"Mã thoát: {exitCode}");
            File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
            Debug.Log($"[CombatSandboxSmokeTest]\n{report}");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
            else
            {
                EditorApplication.isPlaying = false;
            }
        }
    }
}
