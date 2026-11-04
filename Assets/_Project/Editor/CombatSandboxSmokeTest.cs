using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dylanng;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Game.Combat;
using Game.Combat.Domain;
using Game.Data.Hero;
using Game.Events;
using Game.Gameplay.Controllers;
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
                    Verify = () => Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>().Length == 1
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
                    Verify = VerifyLockOn,
                    End = () => _scripted.Pending.LockOn = true
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
                    Name = "HUD chiến đấu mở được và có nút Ultimate",
                    Timeout = 4d,
                    Begin = OpenCombatHud,
                    Verify = VerifyCombatHud,
                    End = () => CaptureCamera("sandbox_end.png")
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

        private static string VerifySynergy()
        {
            IReadOnlyList<HeroInstance> heroes = _party.ActiveHeroes;
            int links = ServiceLocator.Get<PartySynergyService>().ActiveLinks.Count;
            if (heroes.Count >= 3 && links != 2) return $"Có {links} liên kết thay vì 2";

            float fireBonus = heroes[0].Stats.GetStatValue(Dylanng.Gameplay.RPG.Stats.StatType.DamageBonus);
            return fireBonus > 0f ? null : "Hero Hỏa chưa có DamageBonus";
        }

        private static StatusDefinitionSO LoadStatus(string assetName)
        {
            return AssetDatabase.LoadAssetAtPath<StatusDefinitionSO>(
                $"Assets/_Project/Game/Features/Combat/SOs/Status/{assetName}.asset");
        }

        private static string VerifyWaterExtinguishesBurn()
        {
            DummyBotInstance dummy = Object.FindObjectsOfType<DummyBotInstance>()
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
            return Object.FindObjectsOfType<DummyBotInstance>()
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

        private static void OpenCombatHud()
        {
            EventBus.Publish(new SwitchGameModeEvent(Game.Data.GameMode.GameModeType.Combat));
        }

        private static string VerifyCombatHud()
        {
            var hud = Object.FindObjectOfType<Game.UIs.Screens.CombatHUD.Screen_CombatHUD>();
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
