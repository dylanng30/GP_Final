using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dylanng;
using Game.Combat.Abilities.Events;
using Game.Combat;
using Game.Combat.Domain;
using Game.Data.Hero;
using Game.Events;
using Game.Gameplay.Controllers;
using Game.Gameplay.Entities;
using Game.Managers.Party;
using Game.UIs.FloatingText;
using Game.UIs.Widgets;
using Game.UIs.Screens;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
        private const float DamageTextSearchRadius = 4f;
        private const double DamageTextSettleSeconds = 0.35d;
        private const float HealthBarProbeDamage = 12000f;
        private const float HealthBarDroppedRatio = 0.5f;
        private const float MinimumCameraTurnDegrees = 20f;
        private const float HealthBarFacingToleranceDegrees = 2f;
        private const int MinimumChainTargets = 3;
        private const float DummyRowMinimumZ = 6f;
        private const float BladeWaveHalfAngle = 30f;
        private const float BladeWaveAngleTolerance = 8f;
        private const float BladeWaveRange = 9f;
        private const float BladeWaveRangeTolerance = 1f;
        private const double MetalCaptureAfterImpactSeconds = 0.3d;
        private const double MetalObserveSeconds = 1.5d;
        private const float SwapPositionTolerance = 0.5f;
        private const float MetalCameraBack = 5f;
        private const float MetalCameraHeight = 4f;
        private const float MetalCameraLookAhead = 6f;
        private static readonly Vector3 MetalStandOffset = new Vector3(0f, 0f, -5f);
        private const float MinimumSwipeTurnDegrees = 5f;
        private const float LookProbeX = 0.55f;
        private const float LookProbeY = 0.6f;
        private const int FirstFingerId = 1;
        private const int SecondFingerId = 2;
        private static readonly Vector2 SwipeDelta = new Vector2(200f, 0f);
        private static readonly Vector2 PinchStart = new Vector2(100f, 0f);
        private static readonly Vector2 PinchEnd = new Vector2(300f, 0f);

        /// <summary>Nguồn input giả: Move giữ nguyên, Pending chỉ có tác dụng trong một lần đọc.</summary>
        private sealed class ScriptedIntentSource : IPlayerIntentSource
        {
            public Vector2 Move;
            public PlayerIntent Pending = NewIntent();

            public PlayerIntent Read()
            {
                PlayerIntent intent = Pending;
                intent.Move = Move;
                Pending = NewIntent();
                return intent;
            }

            private static PlayerIntent NewIntent()
            {
                return new PlayerIntent { SwitchHeroIndex = PlayerIntent.NoHeroSelected };
            }
        }

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
        private static float _energyBaseline;
        private static bool _hasToggledViewBack;
        private static int _castCursor;
        private static DummyBotInstance _hitTarget;
        private static Quaternion _thirdPersonCameraRotation;
        private static int _swapSlot;
        private static string _swapOldHeroId;
        private static Vector3 _swapOldPosition;
        private static List<HeroInstance> _swapPartyBefore;
        private static double _metalCastTime = -1d;
        private static List<GameObject> _metalVictims;
        private static Vector3 _metalCastOrigin;
        private static Vector3 _metalCastDirection;
        private static bool _hasCapturedMetalSkill;
        private static Quaternion _cameraRotationBaseline;
        private static float _cameraDistanceBaseline;
        private static bool _isCursorFreeAtStart;
        private static bool _isCursorRequestFlowCorrect;
        private static HeroInstance _fallenHero;
        private static readonly ScriptedIntentSource _scripted = new ScriptedIntentSource();
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
            if (_metalVictims != null && evt.Caster == MetalHero.gameObject) _metalVictims.Add(evt.Target);
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
                    Name = "Chỉ còn một EventSystem",
                    Timeout = 3d,
                    Verify = () => Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length == 1
                        ? null
                        : "Có nhiều EventSystem"
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
                    Name = "Khiên chặn sát thương và bị trừ",
                    Timeout = 1d,
                    Verify = VerifyShieldAbsorbs
                },
                new Step
                {
                    Name = "Hồi máu không vượt quá máu tối đa",
                    Timeout = 1d,
                    Verify = VerifyHealClamped
                },
                new Step
                {
                    Name = "Tương sinh kích hoạt đúng liên kết",
                    Timeout = 2d,
                    Verify = VerifySynergy
                },
                new Step
                {
                    Name = "Thủy dập Thiêu Đốt trên mục tiêu",
                    Timeout = 1d,
                    Verify = VerifyWaterExtinguishesBurn
                },
                new Step
                {
                    Name = "Choáng khóa hành động và tự hết",
                    Timeout = 1d,
                    Verify = VerifyStunBlocksActions
                },
                new Step
                {
                    Name = "Khống chế không bật input của hero khác",
                    Timeout = 1d,
                    Verify = VerifyControlKeepsInputOff
                },
                new Step
                {
                    Name = "Auto combat gây sát thương",
                    Timeout = 30d,
                    Begin = StartAutoCombat,
                    Verify = VerifyDamageIncreased,
                    End = () => CaptureCamera("sandbox_auto.png")
                },
                new Step
                {
                    Name = "Tắt auto, giữ góc nhìn thứ 3",
                    Timeout = 6d,
                    Begin = StartManualMode,
                    Verify = VerifyManualMode,
                    End = () => CaptureCamera("sandbox_third_person.png")
                },
                new Step
                {
                    Name = "Phím đổi góc nhìn sang top-down và ngược lại",
                    Timeout = 5d,
                    Begin = () => _scripted.Pending.ToggleView = true,
                    Verify = VerifyViewToggled
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
                    Verify = VerifyBasicAttackResult
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
                    Name = "Phím skill 2 dùng đúng slot",
                    Timeout = 5d,
                    Begin = () => _scripted.Pending.Skill2 = true,
                    Verify = () => ActiveHero.GetSkillRemainingCooldown(AbilitySlot.Skill2) > 0f ? null : "Skill 2 chưa được dùng"
                },
                new Step
                {
                    Name = "Lock-on giữ mục tiêu",
                    Timeout = 3d,
                    Begin = StartLockOn,
                    Verify = VerifyLockOn
                },
                new Step
                {
                    Name = "Số sát thương hiện trên đầu mục tiêu",
                    Timeout = 2d,
                    Begin = HitLockedTarget,
                    Verify = VerifyDamageTextShown,
                    End = () => CaptureCamera("sandbox_hit.png")
                },
                new Step
                {
                    Name = "Thanh máu giảm theo máu và không dùng Canvas",
                    Timeout = 2d,
                    Begin = HitTargetHard,
                    Verify = VerifyHealthBarMatchesHealth,
                    End = () =>
                    {
                        CaptureCamera("sandbox_health_bar.png");
                        _thirdPersonCameraRotation = Camera.main.transform.rotation;
                        _scripted.Pending.LockOn = true;
                    }
                },
                new Step
                {
                    Name = "Thanh máu quay theo camera khi đổi góc nhìn",
                    Timeout = 4d,
                    Begin = () => _scripted.Pending.ToggleView = true,
                    Verify = VerifyHealthBarFacesCamera,
                    End = () =>
                    {
                        CaptureCamera("sandbox_health_bar_top_down.png");
                        _scripted.Pending.ToggleView = true;
                    }
                },
                new Step
                {
                    Name = "Lệnh tập hợp kéo đồng đội về",
                    Timeout = 2d,
                    Begin = StartRegroup,
                    Verify = VerifyRegrouped
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
                    Name = "Đổi đội để có đủ hệ Kim và Thủy",
                    Timeout = 5d,
                    Begin = SwapPartyToMetalAndWater,
                    Verify = VerifyPartySwapped
                },
                new Step
                {
                    Name = "Đội mới có đủ 3 skill và đúng liên kết",
                    Timeout = 2d,
                    Verify = VerifyHeroKits
                },
                new Step
                {
                    Name = "Toàn bộ skill của đội dùng được",
                    Timeout = 45d,
                    Begin = () => _castCursor = 0,
                    Verify = CastNextPartySkill
                },
                new Step
                {
                    Name = "Kim skill 1: tia sét nảy qua nhiều mục tiêu",
                    Timeout = 15d,
                    Begin = BeginMetalSkillTest,
                    Verify = VerifyChainLightning
                },
                new Step
                {
                    Name = "Kim skill 2: sóng quạt chỉ trúng phía trước",
                    Timeout = 15d,
                    Begin = () => _metalCastTime = -1d,
                    Verify = VerifyBladeWave,
                    End = EndMetalSkillTest
                },
                new Step
                {
                    Name = "Hero ngã thì chuyển sang hero còn sống",
                    Timeout = 3d,
                    Begin = KillActiveHero,
                    Verify = VerifyActiveHeroReplaced
                },
                new Step
                {
                    Name = "HUD chiến đấu mở được và có nút Ultimate",
                    Timeout = 4d,
                    Begin = OpenCombatHud,
                    Verify = VerifyCombatHud,
                    End = () => CaptureCamera("sandbox_end.png")
                },
                new Step
                {
                    Name = "Mobile: không khóa chuột và vùng vuốt nhận được chạm",
                    Timeout = 2d,
                    Begin = () => _input.SetUsePCInput(false),
                    Verify = VerifyMobileInputActive
                },
                new Step
                {
                    Name = "Mobile: vuốt màn hình xoay camera",
                    Timeout = 2d,
                    Begin = SwipeLookArea,
                    Verify = () => Quaternion.Angle(Camera.main.transform.rotation, _cameraRotationBaseline) >= MinimumSwipeTurnDegrees
                        ? null
                        : "Camera chưa xoay"
                },
                new Step
                {
                    Name = "Mobile: hai ngón đổi độ zoom",
                    Timeout = 1d,
                    Begin = PinchLookArea,
                    Verify = () => Mathf.Approximately(ReadCameraDistance(), _cameraDistanceBaseline) ? "Zoom chưa đổi" : null
                },
                new Step
                {
                    Name = "Mobile: nút trên màn hình đổi được camera",
                    Timeout = 2d,
                    Begin = ClickViewToggleButton,
                    Verify = () => CameraController.Instance.Mode == CameraMode.TopDown ? null : "Camera chưa sang top-down",
                    End = ClickViewToggleButton
                },
                new Step
                {
                    Name = "Mobile: giữ nút đánh để tấn công",
                    Timeout = 3d,
                    Begin = PressAttackButtonWithDeviceInput,
                    Verify = () => !ActiveHero.CanAttack() ? null : "Hero chưa đánh",
                    End = () => SendPointer(Object.FindAnyObjectByType<AttackCastButton>().gameObject, ExecuteEvents.pointerUpHandler)
                },
                new Step
                {
                    Name = "Mobile: nút khóa mục tiêu, nhảy và tập hợp",
                    Timeout = 3d,
                    Begin = PressMobileActionButtons,
                    Verify = () => ActiveHero.GetComponent<PlayerTargetingSystem>().IsHardLocked ? null : "Chưa khóa mục tiêu",
                    End = () =>
                    {
                        ClickHudButton("lockOnBtn");
                        CaptureHud("sandbox_mobile_hud.png");
                    }
                },
                new Step
                {
                    Name = "Quay lại PC: tắt vùng vuốt",
                    Timeout = 2d,
                    Begin = () => _input.SetUsePCInput(true),
                    Verify = () => Object.FindAnyObjectByType<LookSwipeArea>().GetComponent<Graphic>().raycastTarget ? "Vùng vuốt vẫn bật" : null
                },
                new Step
                {
                    Name = "PC: vào game chuột vẫn hiện, khóa và thả được",
                    Timeout = 2d,
                    Begin = RunCursorCaptureRequests,
                    Verify = VerifyCursorCaptureRequests
                },
                new Step
                {
                    Name = "Tab đổi hero đang điều khiển, giữ vị trí và hai hero còn lại",
                    Timeout = 3d,
                    Begin = CycleControlledHero,
                    Verify = VerifyControlledHeroCycled,
                    End = () => CaptureHud("sandbox_hero_swap.png")
                },
                new Step
                {
                    Name = "Chọn hero đã trong đội thì chỉ chuyển quyền điều khiển",
                    Timeout = 2d,
                    Begin = SelectExistingPartyMember,
                    Verify = VerifyControlMovedWithoutRespawn
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
            int dummyCount = Object.FindObjectsByType<DummyBotInstance>().Length;
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

        private static string VerifySynergy()
        {
            IReadOnlyList<HeroInstance> heroes = _party.ActiveHeroes;
            int links = ServiceLocator.Get<PartySynergyService>().ActiveLinks.Count;
            if (heroes.Count >= 3 && links != 2) return $"Có {links} liên kết thay vì 2";

            float fireBonus = heroes[0].Stats.GetStatValue(Game.Combat.Stats.StatType.DamageBonus);
            return fireBonus > 0f ? null : "Hero Hỏa chưa có DamageBonus";
        }

        private static StatusDefinitionSO LoadStatus(string assetName)
        {
            return AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(
                $"Assets/_Project/Game/Features/Combat/SOs/Status/{assetName}.asset");
        }

        private static string VerifyWaterExtinguishesBurn()
        {
            DummyBotInstance dummy = Object.FindObjectsByType<DummyBotInstance>()
                .First(candidate => candidate.Element == GameElement.Earth);
            StatusDefinitionSO burn = LoadStatus("Status_Burn");
            StatusDefinitionSO wet = LoadStatus("Status_Wet");
            var resolver = ServiceLocator.Get<CombatResolver>();

            resolver.ApplyStatus(null, dummy.gameObject, burn);
            if (!dummy.Status.Set.Has(burn)) return "Không áp được Thiêu Đốt";

            resolver.ApplyStatus(null, dummy.gameObject, wet);
            bool isExtinguished = !dummy.Status.Set.Has(burn) && dummy.Status.Set.Has(wet);
            dummy.Status.Set.Clear();

            return isExtinguished ? null : "Thiêu Đốt chưa bị dập";
        }

        private static string VerifyStunBlocksActions()
        {
            HeroInstance hero = ActiveHero;
            StatusDefinitionSO stun = LoadStatus("Status_Stun");

            ServiceLocator.Get<CombatResolver>().ApplyStatus(null, hero.gameObject, stun);
            bool isBlocked = !hero.Status.CanAct && !hero.CanAttack() && !hero.CanUseSkill();
            hero.Status.Set.Remove(stun);

            if (!isBlocked) return "Hero vẫn hành động được khi choáng";
            return hero.Status.CanAct ? null : "Hero vẫn bị khóa sau khi hết choáng";
        }

        private static string VerifyControlKeepsInputOff()
        {
            if (_party.ActiveHeroes.Count < 2) return null;

            HeroInstance follower = _party.ActiveHeroes[1];
            var input = follower.GetComponent<Game.Gameplay.Controllers.PlayerInputController>();
            object controlLock = follower.Status.AddLock(ControlFlags.CannotAct);
            follower.Status.RemoveLock(controlLock);

            return input.enabled ? "Input của hero không điều khiển bị bật" : null;
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
            DummyBotInstance dummy = Object.FindObjectsByType<DummyBotInstance>()
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

        private static void StartAutoCombat()
        {
            MarkDamageBaseline();
            EventBus.Publish(new ToggleFollowModeEvent(true));
        }

        private static void StartManualMode()
        {
            EventBus.Publish(new ToggleFollowModeEvent(false));
            ActiveHero.GetComponent<PlayerInputController>().SetIntentSource(_scripted);
        }

        private static string VerifyManualMode()
        {
            if (_party.IsFollowModeEnabled) return "Vẫn ở chế độ auto";
            return CameraController.Instance.Mode == CameraMode.ThirdPerson ? null : "Camera không ở góc nhìn thứ 3";
        }

        private static string VerifyViewToggled()
        {
            if (CameraController.Instance.Mode == CameraMode.TopDown && !_hasToggledViewBack)
            {
                _hasToggledViewBack = true;
                _scripted.Pending.ToggleView = true;
                return "Đang đổi lại";
            }

            bool isBack = _hasToggledViewBack && CameraController.Instance.Mode == CameraMode.ThirdPerson;
            return isBack ? null : "Góc nhìn chưa đổi";
        }

        private static void StartMoving()
        {
            _positionBaseline = ActiveHero.transform.position;
            _scripted.Move = Vector2.up;
        }

        private static void StopMoving()
        {
            _scripted.Move = Vector2.zero;
        }

        private static void StartLockOn()
        {
            ActiveHero.GetComponent<PlayerTargetingSystem>().SetTarget(FindNearestDummy());
            _scripted.Pending.LockOn = true;
        }

        private static string VerifyLockOn()
        {
            var targeting = ActiveHero.GetComponent<PlayerTargetingSystem>();
            return targeting.IsHardLocked && targeting.CurrentTarget != null ? null : "Chưa hard-lock";
        }

        private static void HitLockedTarget()
        {
            _hitTarget = (DummyBotInstance)ActiveHero.GetComponent<PlayerTargetingSystem>().CurrentTarget;
            ServiceLocator.Get<CombatResolver>().Deal(
                null, _hitTarget.gameObject, ProbeDamage, GameElement.Fire, _hitTarget.transform.position, canCrit: false);
        }

        private static string VerifyDamageTextShown()
        {
            Vector3 targetPosition = _hitTarget.transform.position;
            float targetTop = _hitTarget.GetComponent<Collider>().bounds.max.y;

            DamageText text = Object.FindObjectsByType<DamageText>()
                .FirstOrDefault(candidate => Vector3.Distance(candidate.transform.position, targetPosition) < DamageTextSearchRadius);

            if (text == null) return "Không có DamageText nào gần mục tiêu";
            if (!text.GetComponent<TMP_Text>().text.StartsWith("-")) return "DamageText không hiện số";
            if (text.transform.position.y < targetTop) return $"DamageText bị che trong model, y = {text.transform.position.y}";

            // Chờ hiệu ứng phóng to xong để ảnh chụp thấy rõ chữ.
            return StepElapsed >= DamageTextSettleSeconds ? null : "Đang chờ";
        }

        private static void HitTargetHard()
        {
            ServiceLocator.Get<CombatResolver>().Deal(
                null, _hitTarget.gameObject, HealthBarProbeDamage, GameElement.Fire, _hitTarget.transform.position, canCrit: false);
        }

        private static string VerifyHealthBarMatchesHealth()
        {
            ActorHealthBar bar = _hitTarget.GetComponentInChildren<ActorHealthBar>();
            if (bar == null) return "DummyBot chưa có thanh máu";
            if (bar.GetComponentInParent<Canvas>() != null) return "Thanh máu đang dùng Canvas";

            var barRenderer = bar.GetComponent<MeshRenderer>();
            if (!barRenderer.enabled) return "Thanh máu đang ẩn";

            var block = new MaterialPropertyBlock();
            barRenderer.GetPropertyBlock(block);
            float displayed = -block.GetVector("_ProgressPercentage").x;
            float ratio = _hitTarget.Health.Ratio;

            if (ratio >= HealthBarDroppedRatio) return $"Máu chưa giảm đủ, còn {ratio}";
            return Mathf.Abs(displayed - ratio) < 0.001f ? null : $"Thanh hiện {displayed}, máu thật {ratio}";
        }

        private static string VerifyHealthBarFacesCamera()
        {
            Transform cameraTransform = Camera.main.transform;
            float cameraTurn = Quaternion.Angle(cameraTransform.rotation, _thirdPersonCameraRotation);
            if (cameraTurn < MinimumCameraTurnDegrees) return $"Camera mới xoay {cameraTurn} độ";

            Transform bar = _hitTarget.GetComponentInChildren<ActorHealthBar>().transform;
            float offset = Quaternion.Angle(bar.rotation, cameraTransform.rotation);
            return offset <= HealthBarFacingToleranceDegrees ? null : $"Thanh máu lệch camera {offset} độ";
        }

        private static void StartRegroup()
        {
            HeroInstance follower = _party.ActiveHeroes[_party.ActiveHeroes.Count - 1];
            follower.GetComponent<HeroAIController>().RegroupTo(ActiveHero.transform.position + Vector3.right * 12f);
            _scripted.Pending.Regroup = true;
        }

        private static string VerifyRegrouped()
        {
            HeroInstance follower = _party.ActiveHeroes[_party.ActiveHeroes.Count - 1];
            float distance = Vector3.Distance(follower.transform.position, ActiveHero.transform.position);
            return distance <= 6f ? null : $"Đồng đội còn cách {distance:F1}m";
        }

        private static string VerifyMovedAtLeast(float distance)
        {
            float moved = Vector3.Distance(_positionBaseline, ActiveHero.transform.position);
            return moved >= distance ? null : $"Mới di chuyển {moved:F2}m";
        }

        private static void StartDash()
        {
            _positionBaseline = ActiveHero.transform.position;

            // Lùi về sau để không bị DummyBot phía trước chặn đường.
            ActiveHero.Move(Vector2.down);
            ActiveHero.Dash();
        }

        private static DummyBotInstance FindNearestDummy()
        {
            Vector3 heroPosition = ActiveHero.transform.position;
            return Object.FindObjectsByType<DummyBotInstance>()
                .Where(dummy => !dummy.IsDead)
                .OrderBy(dummy => (dummy.transform.position - heroPosition).sqrMagnitude)
                .FirstOrDefault();
        }

        private static void StartBasicAttack()
        {
            MarkDamageBaseline();
            _energyBaseline = ActiveHero.CurrentEnergy;
            ActiveHero.Attack(FindNearestDummy());
        }

        private static string VerifyBasicAttackResult()
        {
            string damageFailure = VerifyDamageIncreased();
            if (damageFailure != null) return damageFailure;

            return ActiveHero.CurrentEnergy > _energyBaseline ? null : "Đánh trúng nhưng không tăng Nộ";
        }

        private static string VerifyShieldAbsorbs()
        {
            HeroInstance hero = ActiveHero;
            float health = hero.Health.Current;
            object source = new object();
            hero.Health.Shields.Add(source, ProbeDamage * 10f);

            ServiceLocator.Get<CombatResolver>()
                .Deal(null, hero.gameObject, ProbeDamage, GameElement.Physical, hero.transform.position, canCrit: false);

            float shieldLeft = hero.Health.Shields.Total;
            hero.Health.Shields.RemoveSource(source);

            if (hero.Health.Current < health) return "Máu vẫn bị trừ";
            return shieldLeft < ProbeDamage * 10f ? null : "Khiên không bị trừ";
        }

        private static string VerifyHealClamped()
        {
            HeroInstance hero = ActiveHero;
            ServiceLocator.Get<CombatResolver>().Heal(null, hero.gameObject, hero.Health.Max * 10f);
            return Mathf.Approximately(hero.Health.Current, hero.Health.Max) ? null : "Máu vượt giới hạn";
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
            _scripted.Pending.SwitchHeroIndex = 1;
        }

        private static string VerifyHeroSwitched()
        {
            IReadOnlyList<HeroInstance> heroes = _party.ActiveHeroes;
            if (heroes.Count < 2) return null;
            return ActiveHero == heroes[1] ? null : "Hero điều khiển chưa đổi";
        }

        private static void SwapPartyToMetalAndWater()
        {
            var partyManager = ServiceLocator.Get<PartyManager>();
            partyManager.TrySwapHero("hero_000001", "hero_000003");
            partyManager.TrySwapHero("hero_000002", "hero_000004");
        }

        private static string VerifyPartySwapped()
        {
            IReadOnlyList<HeroInstance> heroes = _party.ActiveHeroes;
            if (heroes.Count != 3) return $"Đội có {heroes.Count} hero";

            bool isExpected = heroes[1] != null && heroes[1].Element == GameElement.Metal
                              && heroes[2] != null && heroes[2].Element == GameElement.Water;
            return isExpected ? null : "Hệ của đội chưa đúng";
        }

        private static string VerifyHeroKits()
        {
            foreach (HeroInstance hero in _party.ActiveHeroes)
            {
                for (int slot = 0; slot < AbilitySlot.Count; slot++)
                {
                    if (hero.GetAbility(slot) == null) return $"{hero.HeroId} thiếu skill slot {slot}";
                }
            }

            int links = ServiceLocator.Get<PartySynergyService>().ActiveLinks.Count;
            return links == 1 ? null : $"Có {links} liên kết thay vì 1";
        }

        private static HeroInstance MetalHero => _party.ActiveHeroes.First(hero => hero.Element == GameElement.Metal);

        /// <summary>Dummy giữa hàng sau, hai bên cách 6m nên tia sét nảy tiếp được.</summary>
        private static DummyBotInstance RowCenterDummy => Object.FindObjectsByType<DummyBotInstance>()
            .Where(dummy => !dummy.IsDead && dummy.transform.position.z > DummyRowMinimumZ)
            .OrderBy(dummy => Mathf.Abs(dummy.transform.position.x))
            .First();

        /// <summary>
        /// Đưa hero Kim đứng trước hàng dummy và tắt AI, để đòn đánh thường không làm lẫn số mục tiêu trúng skill.
        /// Không đổi hero đang điều khiển để các bước sau chạy như cũ.
        /// </summary>
        private static void BeginMetalSkillTest()
        {
            HeroInstance metal = MetalHero;
            metal.GetComponent<HeroAIController>().enabled = false;

            Vector3 rowCenter = RowCenterDummy.transform.position;
            var characterController = metal.GetComponent<CharacterController>();
            characterController.enabled = false;
            metal.transform.SetPositionAndRotation(rowCenter + MetalStandOffset, Quaternion.LookRotation(-MetalStandOffset));
            characterController.enabled = true;
            metal.StopMove();

            _metalCastTime = -1d;
        }

        private static void EndMetalSkillTest()
        {
            MetalHero.GetComponent<HeroAIController>().enabled = true;
            _metalVictims = null;
        }

        /// <summary>Chờ skill hồi xong rồi dùng, trả về true khi đã dùng và đủ thời gian quan sát.</summary>
        private static bool TryCastMetalSkill(int slot, GameObject target, Vector3 point, out string waiting)
        {
            waiting = null;
            HeroInstance metal = MetalHero;

            if (_metalCastTime < 0d)
            {
                if (!metal.CanUseSkill(slot))
                {
                    waiting = "Đang chờ skill hồi";
                    return false;
                }

                _metalVictims = new List<GameObject>();
                _metalCastOrigin = metal.transform.position;
                _metalCastDirection = Vector3.ProjectOnPlane(point - _metalCastOrigin, Vector3.up).normalized;
                metal.UseSkill(slot, target, point);
                _metalCastTime = EditorApplication.timeSinceStartup;
                _hasCapturedMetalSkill = false;
            }

            double elapsed = EditorApplication.timeSinceStartup - _metalCastTime;
            // Effect chỉ chạy khi hết thời gian cast, nên chụp sau mốc đó.
            double impactTime = metal.GetAbility(slot).TotalCastDuration;
            if (!_hasCapturedMetalSkill && elapsed >= impactTime + MetalCaptureAfterImpactSeconds)
            {
                _hasCapturedMetalSkill = true;
                Vector3 eye = _metalCastOrigin - _metalCastDirection * MetalCameraBack + Vector3.up * MetalCameraHeight;
                CaptureFromViewpoint(slot == AbilitySlot.Skill1 ? "metal_chain_lightning.png" : "metal_blade_wave.png",
                    eye, _metalCastOrigin + _metalCastDirection * MetalCameraLookAhead);
            }

            if (elapsed < MetalObserveSeconds)
            {
                waiting = "Đang quan sát";
                return false;
            }
            return true;
        }

        private static string VerifyChainLightning()
        {
            DummyBotInstance first = RowCenterDummy;

            if (!TryCastMetalSkill(AbilitySlot.Skill1, first.gameObject, first.transform.position, out string waiting)) return waiting;

            int distinctTargets = _metalVictims.Distinct().Count();
            if (!_metalVictims.Contains(first.gameObject)) return "Tia sét không trúng mục tiêu đầu";
            return distinctTargets >= MinimumChainTargets ? null : $"Tia sét chỉ trúng {distinctTargets} mục tiêu";
        }

        private static string VerifyBladeWave()
        {
            DummyBotInstance aimed = RowCenterDummy;

            if (!TryCastMetalSkill(AbilitySlot.Skill2, null, aimed.transform.position, out string waiting)) return waiting;
            if (!_metalVictims.Contains(aimed.gameObject)) return "Sóng kiếm không trúng dummy ngay phía trước";

            foreach (GameObject victim in _metalVictims.Distinct())
            {
                Vector3 toVictim = Vector3.ProjectOnPlane(victim.transform.position - _metalCastOrigin, Vector3.up);
                float angle = Vector3.Angle(_metalCastDirection, toVictim);
                if (angle > BladeWaveHalfAngle + BladeWaveAngleTolerance) return $"Trúng {victim.name} lệch {angle:0} độ, ngoài vùng quạt";
                if (toVictim.magnitude > BladeWaveRange + BladeWaveRangeTolerance) return $"Trúng {victim.name} xa {toVictim.magnitude:0.0}m, quá tầm";
            }
            return null;
        }

        private static void CycleControlledHero()
        {
            HeroInstance controlled = ActiveHero;
            _swapSlot = _party.ActiveHeroes.ToList().IndexOf(controlled);
            _swapOldHeroId = controlled.HeroId;
            _swapOldPosition = controlled.transform.position;
            _swapPartyBefore = _party.ActiveHeroes.ToList();

            Object.FindAnyObjectByType<HeroSwapDebugPanel>().CycleControlledHero();
        }

        private static string VerifyControlledHeroCycled()
        {
            HeroInstance controlled = ActiveHero;
            if (controlled == null) return "Không có hero đang điều khiển";
            if (controlled.HeroId == _swapOldHeroId) return "Hero đang điều khiển chưa đổi";
            if (_swapPartyBefore.Any(hero => hero != null && hero.HeroId == controlled.HeroId)) return "Hero mới vốn đã trong đội";
            if (_party.ActiveHeroes.ToList().IndexOf(controlled) != _swapSlot) return "Hero mới không nằm đúng slot cũ";

            float moved = Vector3.Distance(controlled.transform.position, _swapOldPosition);
            if (moved > SwapPositionTolerance) return $"Hero mới đứng cách chỗ cũ {moved:0.0}m";

            for (int slot = 0; slot < _swapPartyBefore.Count; slot++)
            {
                if (slot != _swapSlot && _party.ActiveHeroes[slot] != _swapPartyBefore[slot]) return $"Hero slot {slot} bị tạo lại";
            }

            bool isPortraitUpdated = Object.FindObjectsByType<HeroAvatarWidget>().Any(widget => widget.HeroId == controlled.HeroId);
            return isPortraitUpdated ? null : "Chân dung trên HUD chưa đổi";
        }

        private static void SelectExistingPartyMember()
        {
            _swapPartyBefore = _party.ActiveHeroes.ToList();
            HeroInstance target = _swapPartyBefore.First(hero => hero != null && !hero.IsDead && hero != ActiveHero);
            _swapOldHeroId = target.HeroId;

            Object.FindAnyObjectByType<HeroSwapDebugPanel>().SelectHero(target.HeroId);
        }

        private static string VerifyControlMovedWithoutRespawn()
        {
            if (ActiveHero == null || ActiveHero.HeroId != _swapOldHeroId) return "Chưa chuyển sang hero đã chọn";
            return _party.ActiveHeroes.SequenceEqual(_swapPartyBefore) ? null : "Đội bị tạo lại";
        }

        private static string CastNextPartySkill()
        {
            int totalCasts = _party.ActiveHeroes.Count * AbilitySlot.Count;
            if (_castCursor >= totalCasts) return null;

            HeroInstance hero = _party.ActiveHeroes[_castCursor / AbilitySlot.Count];
            int slot = _castCursor % AbilitySlot.Count;

            hero.GainEnergy(FullEnergy);

            // AI của đồng đội có thể đã dùng skill này trước, vẫn tính là skill hoạt động.
            if (hero.GetSkillRemainingCooldown(slot) > 0f)
            {
                _castCursor++;
                return "Đang cast";
            }

            if (!hero.CanUseSkill(slot)) return $"Đang chờ {hero.HeroId} slot {slot}";

            Vector3 point = hero.transform.position + hero.transform.forward * 3f;
            hero.UseSkill(slot, null, point);
            _castCursor++;
            return "Đang cast";
        }

        private static void KillActiveHero()
        {
            _fallenHero = ActiveHero;
            _fallenHero.Health.TakeDamage(_fallenHero.Health.Max * 10f);
        }

        private static string VerifyActiveHeroReplaced()
        {
            if (ActiveHero == _fallenHero || ActiveHero.IsDead) return "Chưa đổi sang hero còn sống";
            return _fallenHero.CanAttack() ? "Hero đã ngã vẫn đánh được" : null;
        }

        private static void OpenCombatHud()
        {
            EventBus.Publish(new SwitchGameModeEvent(Game.Data.GameMode.GameModeType.Combat));
        }

        private static string VerifyCombatHud()
        {
            var hud = Object.FindAnyObjectByType<Game.UIs.Screens.CombatHUD.Screen_CombatHUD>();
            if (hud == null) return "Chưa mở HUD";
            if (hud.GetComponent<Game.UIs.Screens.CombatHUD.CombatInfoPanel>() == null) return "Thiếu CombatInfoPanel";

            bool hasUltimateButton = hud.GetComponentsInChildren<Game.UIs.Widgets.SkillCastButton>(true)
                .Any(button => button.name == "UltimateCastButton");
            return hasUltimateButton ? null : "Thiếu nút Ultimate";
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
            if (Camera.main != null) RenderToFile(Camera.main, fileName);
        }

        /// <summary>Chụp từ một góc cố định bằng camera tạm, để ảnh không phụ thuộc vị trí camera theo người chơi.</summary>
        private static void CaptureFromViewpoint(string fileName, Vector3 eye, Vector3 lookAt)
        {
            if (Camera.main == null) return;

            var cameraObject = new GameObject("SmokeCaptureCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.CopyFrom(Camera.main);
            camera.enabled = false;
            cameraObject.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(lookAt - eye));

            RenderToFile(camera, fileName);
            Object.DestroyImmediate(cameraObject);
        }

        private static void RenderToFile(Camera camera, string fileName)
        {
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

        private static string VerifyMobileInputActive()
        {
            if (Cursor.lockState == CursorLockMode.Locked) return "Con trỏ vẫn bị khóa";

            var area = Object.FindAnyObjectByType<LookSwipeArea>();
            if (area == null) return "Không có vùng vuốt";
            if (!area.GetComponent<Graphic>().raycastTarget) return "Vùng vuốt chưa bật";

            var pointer = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Screen.width * LookProbeX, Screen.height * LookProbeY)
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);

            if (hits.Count == 0) return "Chạm vào màn hình không trúng UI nào";
            return hits[0].gameObject == area.gameObject ? null : $"Vùng vuốt bị {hits[0].gameObject.name} che";
        }

        private static void SwipeLookArea()
        {
            GameObject area = Object.FindAnyObjectByType<LookSwipeArea>().gameObject;
            _cameraRotationBaseline = Camera.main.transform.rotation;

            ExecuteEvents.Execute(area, NewPointer(FirstFingerId, Vector2.zero, Vector2.zero), ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(area, NewPointer(FirstFingerId, SwipeDelta, SwipeDelta), ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(area, NewPointer(FirstFingerId, SwipeDelta, Vector2.zero), ExecuteEvents.pointerUpHandler);
        }

        private static void PinchLookArea()
        {
            GameObject area = Object.FindAnyObjectByType<LookSwipeArea>().gameObject;
            _cameraDistanceBaseline = ReadCameraDistance();

            ExecuteEvents.Execute(area, NewPointer(FirstFingerId, Vector2.zero, Vector2.zero), ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(area, NewPointer(SecondFingerId, PinchStart, Vector2.zero), ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(area, NewPointer(SecondFingerId, PinchEnd, PinchEnd - PinchStart), ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(area, NewPointer(FirstFingerId, Vector2.zero, Vector2.zero), ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(area, NewPointer(SecondFingerId, PinchEnd, Vector2.zero), ExecuteEvents.pointerUpHandler);
        }

        private static float ReadCameraDistance()
        {
            return new SerializedObject(CameraController.Instance).FindProperty("thirdPersonDistance").floatValue;
        }

        private static PointerEventData NewPointer(int fingerId, Vector2 position, Vector2 delta)
        {
            return new PointerEventData(EventSystem.current) { pointerId = fingerId, position = position, delta = delta };
        }

        private static void SendPointer<T>(GameObject target, ExecuteEvents.EventFunction<T> handler) where T : IEventSystemHandler
        {
            ExecuteEvents.Execute(target, NewPointer(FirstFingerId, Vector2.zero, Vector2.zero), handler);
        }

        /// <summary>Hero đầu đội vẫn giữ nguồn input giả từ bước chơi tay, phải trả về input thật để nút trên màn hình có tác dụng.</summary>
        private static void PressAttackButtonWithDeviceInput()
        {
            ActiveHero.GetComponent<PlayerInputController>().SetIntentSource(new DeviceIntentSource(_input));
            SendPointer(Object.FindAnyObjectByType<AttackCastButton>().gameObject, ExecuteEvents.pointerDownHandler);
        }

        private static void ClickViewToggleButton()
        {
            Object.FindAnyObjectByType<CameraViewToggleButton>().GetComponent<Button>().onClick.Invoke();
        }

        private static void ClickHudButton(string fieldName)
        {
            var hud = new SerializedObject(Object.FindAnyObjectByType<Game.UIs.Screens.CombatHUD.Screen_CombatHUD>());
            ((Button)hud.FindProperty(fieldName).objectReferenceValue).onClick.Invoke();
        }

        private static void PressMobileActionButtons()
        {
            ActiveHero.GetComponent<PlayerTargetingSystem>().SetTarget(FindNearestDummy());

            ClickHudButton("lockOnBtn");
            ClickHudButton("jumpBtn");
            ClickHudButton("regroupBtn");
        }

        private static void RunCursorCaptureRequests()
        {
            var cursor = Object.FindAnyObjectByType<CursorCaptureController>();

            _isCursorFreeAtStart = !cursor.WantsCapture;
            cursor.CaptureFromWorldClick();
            bool capturedByClick = cursor.WantsCapture;
            cursor.ToggleCapture();
            bool releasedByToggle = !cursor.WantsCapture;
            cursor.ToggleCapture();
            bool capturedByToggle = cursor.WantsCapture;
            cursor.ReleaseCapture();

            _isCursorRequestFlowCorrect = capturedByClick && releasedByToggle && capturedByToggle && !cursor.WantsCapture;
        }

        private static string VerifyCursorCaptureRequests()
        {
            if (!_isCursorFreeAtStart) return "Vào game con trỏ đã bị khóa";
            if (!_isCursorRequestFlowCorrect) return "Khóa và thả con trỏ không đúng thứ tự";
            return Cursor.lockState == CursorLockMode.None && Cursor.visible ? null : "Con trỏ chưa hiện lại";
        }

        /// <summary>Canvas đang giữ camera của Bootstrapper đã bị hủy nên phải gán camera hiện tại để chụp được HUD.</summary>
        private static void CaptureHud(string fileName)
        {
            Canvas canvas = Object.FindAnyObjectByType<LookSwipeArea>().GetComponentInParent<Canvas>();
            Camera previousCamera = canvas.worldCamera;

            canvas.worldCamera = Camera.main;
            Canvas.ForceUpdateCanvases();
            CaptureCamera(fileName);
            canvas.worldCamera = previousCamera;
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
