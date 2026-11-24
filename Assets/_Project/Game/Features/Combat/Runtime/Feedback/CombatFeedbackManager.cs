using System.Collections.Generic;
using Dylanng;
using Game.Combat.Abilities;
using Game.Combat.Abilities.Events;
using Game.Combat.Domain;
using Game.Data.Hero;
using Game.Events;
using Game.Gameplay.AI.Controllers;
using Game.Gameplay.Controllers;
using Game.Gameplay.Entities;
using Game.Managers.Party;
using UnityEngine;

namespace Game.Combat.Feedback
{
    /// <summary>
    /// Cảm giác đánh: hitstop, chớp sáng, nảy model, đẩy lùi, rung camera, VFX va chạm theo hệ,
    /// bụi khi dash và tiếp đất, vệt dash, hiệu ứng chết, vòng cảnh báo skill diện rộng,
    /// âm thanh, VFX tụ lực khi cast, slow-motion của Ultimate, bóng mờ và hiển thị trạng thái.
    /// GameManager gắn component này sau khi các manager chiến đấu đã khởi tạo.
    /// </summary>
    public class CombatFeedbackManager : MonoBehaviour
    {
        private const float MinHitPointOffset = 0.05f;

        private CombatFeedbackSO _config;
        private Material _flashMaterial;
        private Material _trailMaterial;
        private Material _telegraphMaterial;
        private Material _ghostMaterial;
        private Material _rimMaterial;
        private AfterimageRenderer _afterimages;
        private ScreenFlash _screenFlash;
        private PauseSystem _pauseSystem;
        private Transform _poolRoot;

        // Hitstop và slow-motion dùng chung một cơ chế giảm timeScale.
        private bool _isHitStopped;
        private float _hitStopStart;
        private float _hitStopEnd;
        private float _hitStopReadyAt;
        private float _savedTimeScale = 1f;
        private float _hitStopCap;

        // Pool VFX: mỗi prefab một chồng instance đã tắt.
        private readonly Dictionary<GameObject, Stack<GameObject>> _vfxPool = new Dictionary<GameObject, Stack<GameObject>>();
        private readonly List<ActiveVfx> _activeVfx = new List<ActiveVfx>();
        private readonly Stack<GroundTelegraph> _telegraphPool = new Stack<GroundTelegraph>();
        // Actor đã kiểm tra mà không có lưỡi vũ khí, để không phải tìm lại mỗi đòn.
        private readonly HashSet<GameObject> _noWeaponActors = new HashSet<GameObject>();

        private struct ActiveVfx
        {
            public GameObject Prefab;
            public GameObject Instance;
            public float ReleaseTime;
            public bool IsAttached;
        }

        private void Awake()
        {
            _config = Resources.Load<CombatFeedbackSO>(CombatFeedbackSO.ResourcePath);
            if (_config == null)
            {
                GameLogger.LogWarning($"[CombatFeedbackManager] Không tìm thấy Resources/{CombatFeedbackSO.ResourcePath}, dùng thông số mặc định và không có VFX.");
                _config = ScriptableObject.CreateInstance<CombatFeedbackSO>();
            }

            _flashMaterial = _config.FlashMaterial != null ? _config.FlashMaterial : CreateMaterial("Game/Feedback/HitFlash");
            _trailMaterial = _config.TrailMaterial != null ? _config.TrailMaterial : CreateMaterial("Game/Feedback/AdditiveTrail");
            _telegraphMaterial = _config.TelegraphMaterial != null ? _config.TelegraphMaterial : CreateMaterial("Game/Feedback/GroundTelegraph");

            _ghostMaterial = _config.GhostMaterial != null ? _config.GhostMaterial : CreateMaterial("Game/Feedback/FresnelGlow");
            _rimMaterial = _config.RimMaterial != null ? _config.RimMaterial : _ghostMaterial;

            _poolRoot = new GameObject("CombatFeedbackPool").transform;
            _poolRoot.SetParent(transform, false);

            _afterimages = _poolRoot.gameObject.AddComponent<AfterimageRenderer>();
            _afterimages.Initialize(_ghostMaterial);
            _screenFlash = ScreenFlash.Create(transform);

            if (GetComponent<CombatAudioPlayer>() == null) gameObject.AddComponent<CombatAudioPlayer>();
        }

        private void Start()
        {
            _pauseSystem = ServiceLocator.Get<PauseSystem>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<ActorDiedEvent>(OnActorDied);
            EventBus.Subscribe<HeroDashedEvent>(OnHeroDashed);
            EventBus.Subscribe<HeroLandedEvent>(OnHeroLanded);
            EventBus.Subscribe<AbilityTelegraphEvent>(OnAbilityTelegraph);
            EventBus.Subscribe<AbilityExecutedEvent>(OnAbilityExecuted);
            EventBus.Subscribe<MeleeSwingEvent>(OnMeleeSwing);
            EventBus.Subscribe<MeleeAttackStartedEvent>(OnMeleeAttackStarted);
            EventBus.Subscribe<RangedShotEvent>(OnRangedShot);
            EventBus.Subscribe<AbilityCastStartedEvent>(OnAbilityCastStarted);
            EventBus.Subscribe<StatusAppliedEvent>(OnStatusApplied);
            EventBus.Subscribe<StatusRemovedEvent>(OnStatusRemoved);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<ActorDiedEvent>(OnActorDied);
            EventBus.Unsubscribe<HeroDashedEvent>(OnHeroDashed);
            EventBus.Unsubscribe<HeroLandedEvent>(OnHeroLanded);
            EventBus.Unsubscribe<AbilityTelegraphEvent>(OnAbilityTelegraph);
            EventBus.Unsubscribe<AbilityExecutedEvent>(OnAbilityExecuted);
            EventBus.Unsubscribe<MeleeSwingEvent>(OnMeleeSwing);
            EventBus.Unsubscribe<MeleeAttackStartedEvent>(OnMeleeAttackStarted);
            EventBus.Unsubscribe<RangedShotEvent>(OnRangedShot);
            EventBus.Unsubscribe<AbilityCastStartedEvent>(OnAbilityCastStarted);
            EventBus.Unsubscribe<StatusAppliedEvent>(OnStatusApplied);
            EventBus.Unsubscribe<StatusRemovedEvent>(OnStatusRemoved);

            if (_isHitStopped) EndHitStop();
        }

        private void Update()
        {
            if (_isHitStopped && (Time.unscaledTime >= _hitStopEnd || IsPaused)) EndHitStop();

            ReleaseFinishedVfx();
        }

        #region Damage

        private void OnDamageDealt(DamageDealtEvent evt)
        {
            if (evt.Target == null) return;

            ITargetable target = evt.Target.GetComponentInParent<ITargetable>();
            if (target == null) return;

            GameObject activeHero = GetActiveHeroObject();
            bool casterIsPlayer = activeHero != null && evt.Caster == activeHero;
            bool targetIsPlayer = activeHero != null && evt.Target == activeHero;
            bool isKill = target.IsDead;
            Color elementColor = _config.GetColor(evt.Element);

            HitReaction reaction = GetOrAdd<HitReaction>(target.Transform.gameObject);

            if (evt.IsPeriodic)
            {
                reaction.Flash(_flashMaterial, elementColor, _config.PeriodicFlashDuration);
                return;
            }

            reaction.Flash(_flashMaterial, evt.IsCritical ? Color.Lerp(_config.FlashColor, elementColor, 0.35f) : _config.FlashColor,
                evt.IsCritical ? _config.CritFlashDuration : _config.FlashDuration);
            reaction.Squash(evt.IsCritical ? _config.CritSquashAmount : _config.SquashAmount, _config.SquashDuration);

            SpawnImpact(evt, target);
            PlaySound(evt.IsCritical ? _config.CritSoundKey : HitSoundKey(evt.Element), evt.Target.transform.position);

            if (!targetIsPlayer) ApplyPush(evt, target);

            if (casterIsPlayer)
            {
                RequestHitStop(isKill ? _config.KillHitStop : evt.IsCritical ? _config.CritHitStop : _config.NormalHitStop);
                if (isKill) Shake(_config.KillShake);
            }
            else if (targetIsPlayer)
            {
                RequestHitStop(_config.PlayerHurtHitStop);
                Shake(_config.PlayerHurtShake);
            }
        }

        private void SpawnImpact(DamageDealtEvent evt, ITargetable target)
        {
            CombatFeedbackSO.ElementFeedback entry = _config.Get(evt.Element);
            GameObject prefab = evt.IsCritical
                ? FirstNonNull(entry != null ? entry.CritImpactPrefab : null, _config.DefaultCritImpactPrefab, entry != null ? entry.ImpactPrefab : null, _config.DefaultImpactPrefab)
                : FirstNonNull(entry != null ? entry.ImpactPrefab : null, _config.DefaultImpactPrefab);
            if (prefab == null) return;

            Vector3 position = ResolveHitPosition(evt, target);
            Vector3 fromCaster = evt.Caster != null ? position - evt.Caster.transform.position : -target.Transform.forward;
            fromCaster.y = 0f;
            Quaternion rotation = fromCaster.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(-fromCaster) : Quaternion.identity;

            float scale = _config.ImpactScale * (evt.IsCritical ? 1.25f : 1f);
            SpawnVfx(prefab, position, rotation, scale, _config.ImpactLifetime);
        }

        private void ApplyPush(DamageDealtEvent evt, ITargetable target)
        {
            if (evt.Caster == null || !(target is HeroInstance hero)) return;

            Vector3 direction = target.Transform.position - evt.Caster.transform.position;
            float distance = evt.IsCritical ? _config.CritPushDistance : _config.PushDistance;
            hero.ApplyHitPush(direction, distance, _config.PushDuration);
        }

        /// <summary>Đòn cận chiến truyền điểm trúng ở chân mục tiêu, nâng lên giữa thân cho VFX.</summary>
        private static Vector3 ResolveHitPosition(DamageDealtEvent evt, ITargetable target)
        {
            Vector3 root = target.Transform.position;
            Collider body = target.Transform.GetComponentInChildren<Collider>();
            Vector3 center = body != null ? body.bounds.center : root + Vector3.up;

            if ((evt.HitPoint - root).sqrMagnitude < MinHitPointOffset * MinHitPointOffset || evt.HitPoint == Vector3.zero)
            {
                return center;
            }

            // Giữ hướng ngang của điểm trúng nhưng đặt chiều cao giữa thân.
            return new Vector3(evt.HitPoint.x, Mathf.Max(evt.HitPoint.y, center.y), evt.HitPoint.z);
        }

        private void OnActorDied(ActorDiedEvent evt)
        {
            if (evt.Actor == null) return;

            ITargetable actor = evt.Actor.GetComponentInParent<ITargetable>();
            GameElement element = actor != null ? actor.Element : GameElement.Physical;
            CombatFeedbackSO.ElementFeedback entry = _config.Get(element);
            GameObject prefab = FirstNonNull(entry != null ? entry.DeathPrefab : null, _config.DefaultDeathPrefab);
            if (prefab == null) return;

            Collider body = evt.Actor.GetComponentInChildren<Collider>();
            Vector3 position = body != null ? body.bounds.center : evt.Actor.transform.position + Vector3.up;
            SpawnVfx(prefab, position, Quaternion.identity, 1f, _config.DeathLifetime);
            PlaySound(_config.DeathSoundKey, position);
        }

        private string HitSoundKey(GameElement element)
        {
            CombatFeedbackSO.ElementFeedback entry = _config.Get(element);
            return entry != null && !string.IsNullOrEmpty(entry.HitSoundKey) ? entry.HitSoundKey : _config.DefaultHitSoundKey;
        }

        #endregion

        #region Melee

        /// <summary>Vệt chém theo hệ ở trước mặt, nghiêng theo từng đòn combo.</summary>
        private void OnMeleeSwing(MeleeSwingEvent evt)
        {
            if (evt.Attacker == null) return;

            CombatFeedbackSO.ElementFeedback entry = _config.Get(evt.Element);
            float range = evt.Step.Range > 0f ? evt.Step.Range : 2.5f;

            // Hero có lưỡi vũ khí: vệt sáng đã bám theo kiếm trong animation, không đặt thêm vệt chém cố định.
            if (GetWeaponTrail(evt.Attacker) != null)
            {
                PlaySound(evt.Step.IsFinisher ? _config.FinisherSwingSoundKey : _config.SwingSoundKey, evt.Attacker.transform.position);
                if (evt.Step.IsFinisher && evt.HasHit) PlayFinisherBurst(evt, entry, range);
                return;
            }

            GameObject prefab = evt.Step.IsFinisher
                ? FirstNonNull(entry != null ? entry.FinisherSlashPrefab : null, _config.DefaultFinisherSlashPrefab, entry != null ? entry.SlashPrefab : null, _config.DefaultSlashPrefab)
                : FirstNonNull(entry != null ? entry.SlashPrefab : null, _config.DefaultSlashPrefab);
            if (prefab == null) return;

            Transform attacker = evt.Attacker.transform;
            var controller = evt.Attacker.GetComponent<CharacterController>();
            float height = controller != null ? controller.center.y : 1f;
            Vector3 position = attacker.position + Vector3.up * height + attacker.forward * _config.SlashForwardOffset;

            Quaternion rotation = attacker.rotation
                * Quaternion.Euler(_config.SlashPitch, 0f, evt.Step.SlashRoll)
                * prefab.transform.rotation;

            float scale = range * _config.SlashScalePerRange * (evt.Step.IsFinisher ? _config.FinisherSlashScale : 1f);
            var scale3 = new Vector3(evt.Step.MirrorSlash ? -scale : scale, scale, scale);

            SpawnVfx(prefab, position, rotation, scale3, _config.SlashLifetime);
            PlaySound(evt.Step.IsFinisher ? _config.FinisherSwingSoundKey : _config.SwingSoundKey, position);

            if (evt.Step.IsFinisher && evt.HasHit) PlayFinisherBurst(evt, entry, range);
        }

        /// <summary>Lửa đầu nòng bám theo súng, âm thanh và giật camera nhẹ cho hero đang điều khiển.</summary>
        private void OnRangedShot(RangedShotEvent evt)
        {
            if (evt.Config == null) return;

            GameObject flash = evt.Config.MuzzleFlashPrefab;
            if (flash != null)
            {
                const float flashLifetime = 0.6f;
                if (evt.Muzzle != null)
                {
                    SpawnAttachedVfx(flash, evt.Muzzle, evt.Config.MuzzleFlashScale, flashLifetime);
                    // Lửa phụt theo hướng đạn bay, không theo trục xương có thể lệch vài độ.
                    _activeVfx[_activeVfx.Count - 1].Instance.transform.rotation = evt.Rotation;
                }
                else
                {
                    SpawnVfx(flash, evt.Position, evt.Rotation, evt.Config.MuzzleFlashScale, flashLifetime);
                }
            }

            PlaySound(evt.Config.ShootSoundKey, evt.Position);
            if (evt.Attacker == GetActiveHeroObject()) Shake(evt.Config.ShootShake);
        }

        private void OnMeleeAttackStarted(MeleeAttackStartedEvent evt)
        {
            EmitWeaponTrail(evt.Attacker, evt.Element, _config.WeaponTrailAttackDuration);
        }

        private void EmitWeaponTrail(GameObject actor, GameElement element, float duration)
        {
            WeaponTrail trail = GetWeaponTrail(actor);
            if (trail == null) return;

            Color color = Color.Lerp(Color.white, _config.GetAfterimageColor(element), _config.WeaponTrailTint);
            trail.Emit(duration, color, _config.WeaponTrailLifetime, _config.WeaponTrailFullSpeed);
        }

        /// <summary>Lấy hoặc dựng WeaponTrail cho actor. Null nếu actor không có renderer lưỡi vũ khí.</summary>
        private WeaponTrail GetWeaponTrail(GameObject actor)
        {
            if (actor == null || _noWeaponActors.Contains(actor)) return null;
            if (actor.TryGetComponent(out WeaponTrail existing)) return existing.HasBlades ? existing : null;

            var trail = actor.AddComponent<WeaponTrail>();
            if (trail.Initialize(_trailMaterial, _config.WeaponKeywords, _poolRoot)) return trail;

            Destroy(trail);
            _noWeaponActors.Add(actor);
            return null;
        }

        /// <summary>Đòn kết trúng: nổ dưới đất trước mặt và giật camera nếu là người chơi.</summary>
        private void PlayFinisherBurst(MeleeSwingEvent evt, CombatFeedbackSO.ElementFeedback entry, float range)
        {
            Transform attacker = evt.Attacker.transform;
            if (entry != null && entry.FinisherBurstPrefab != null)
            {
                Vector3 ground = attacker.position + attacker.forward * (range * 0.55f) + Vector3.up * 0.1f;
                SpawnVfx(entry.FinisherBurstPrefab, ground, Quaternion.LookRotation(attacker.forward), _config.FinisherBurstScale, 1.5f);
            }

            if (evt.Attacker == GetActiveHeroObject()) Shake(_config.FinisherShake);
        }

        #endregion

        #region Movement

        private void OnHeroDashed(HeroDashedEvent evt)
        {
            if (evt.Hero == null) return;

            Transform hero = evt.Hero.transform;
            if (_config.DashPuffPrefab != null)
            {
                SpawnVfx(_config.DashPuffPrefab, hero.position, Quaternion.LookRotation(-hero.forward), _config.DustScale, 1.5f);
            }

            ITargetable actor = evt.Hero.GetComponent<ITargetable>();
            GameElement element = actor != null ? actor.Element : GameElement.Physical;
            Color color = _config.GetColor(element);
            PlaySound(_config.DashSoundKey, hero.position);

            CombatFeedbackSO.ElementFeedback entry = _config.Get(element);
            if (entry != null && entry.UseAfterimageDash)
            {
                StartCoroutine(DashAfterimages(evt.Hero, _config.GetAfterimageColor(element), evt.Duration));
                return;
            }

            GetOrAdd<DashTrail>(evt.Hero).Play(_trailMaterial, color, evt.Duration, _config.TrailTime, _config.TrailWidth);
        }

        private System.Collections.IEnumerator DashAfterimages(GameObject hero, Color color, float duration)
        {
            int count = Mathf.Max(1, _config.DashAfterimageCount);
            float interval = duration / count;
            for (int i = 0; i < count && hero != null; i++)
            {
                _afterimages.Capture(hero, color, _config.AfterimageLifetime);
                yield return new WaitForSeconds(interval);
            }
        }

        private void OnHeroLanded(HeroLandedEvent evt)
        {
            if (evt.Hero == null || _config.LandDustPrefab == null) return;

            SpawnVfx(_config.LandDustPrefab, evt.Hero.transform.position, Quaternion.identity, _config.DustScale, 1.5f);
            PlaySound(_config.LandSoundKey, evt.Hero.transform.position);
        }

        #endregion

        #region Abilities

        private void OnAbilityTelegraph(AbilityTelegraphEvent evt)
        {
            if (evt.Caster == null || _telegraphMaterial == null) return;

            Color color;
            if (IsEnemy(evt.Caster))
            {
                color = _config.EnemyTelegraphColor;
            }
            else
            {
                color = _config.GetColor(evt.Ability != null ? evt.Ability.Element : GameElement.Physical);
                color.a = _config.AllyTelegraphAlpha;
            }

            GroundTelegraph telegraph = _telegraphPool.Count > 0 ? _telegraphPool.Pop() : GroundTelegraph.Create(_telegraphMaterial, _poolRoot);
            telegraph.Show(evt.Point, evt.Radius, evt.Duration, color, finished => _telegraphPool.Push(finished));
        }

        private void OnAbilityCastStarted(AbilityCastStartedEvent evt)
        {
            if (evt.Caster == null || evt.Ability == null) return;

            AbilitySO ability = evt.Ability;
            Transform caster = evt.Caster.transform;
            PlaySound(ability.CastSoundKey, caster.position);

            if (ability.CastVfxPrefab != null)
            {
                Transform attach = null;
                if (!string.IsNullOrEmpty(ability.CastVfxAttachPoint) && evt.Caster.TryGetComponent(out HeroInstance hero))
                {
                    attach = hero.GetBone(ability.CastVfxAttachPoint);
                }

                float lifetime = ability.CastVfxLifetime > 0f ? ability.CastVfxLifetime : Mathf.Max(0.2f, ability.TotalCastDuration + 0.1f);
                if (attach != null)
                {
                    SpawnAttachedVfx(ability.CastVfxPrefab, attach, ability.CastVfxScale, lifetime);
                }
                else
                {
                    SpawnVfx(ability.CastVfxPrefab, caster.position + Vector3.up, caster.rotation, ability.CastVfxScale, lifetime);
                }
            }

            EmitWeaponTrail(evt.Caster, ability.Element, ability.TotalCastDuration + 0.3f);

            Color color = _config.GetColor(ability.Element);
            if (ability.CastAfterimage) _afterimages.Capture(evt.Caster, _config.GetAfterimageColor(ability.Element), _config.AfterimageLifetime * 1.5f);

            if (ability.Type != AbilityType.ElementalBurst || evt.Caster != GetActiveHeroObject()) return;

            // Ultimate của người chơi: chậm thời gian, chớp màn hình, rung mạnh.
            RequestTimeWarp(_config.UltimateSlowMoScale, _config.UltimateSlowMoDuration, true);
            Color flash = Color.Lerp(_config.ScreenFlashColor, color, 0.25f);
            flash.a = _config.ScreenFlashColor.a;
            _screenFlash.Play(flash, _config.ScreenFlashDuration);
            Shake(_config.UltimateShake * 0.6f);
        }

        private void OnAbilityExecuted(AbilityExecutedEvent evt)
        {
            if (evt.Ability == null || evt.Caster == null) return;

            PlaySound(evt.Ability.ExecuteSoundKey, evt.Caster.transform.position);

            if (evt.Ability.Type != AbilityType.ElementalBurst || evt.Caster != GetActiveHeroObject()) return;
            Shake(_config.UltimateShake);
        }

        #endregion

        #region Status

        private void OnStatusApplied(StatusAppliedEvent evt)
        {
            if (evt.Target == null || evt.Status == null || !NeedsPresentation(evt.Status)) return;

            StatusPresentation presentation = GetOrAddPresentation(evt.Target);
            if (!presentation.Add(evt.Status)) return;

            Vector3 feet = evt.Target.transform.position;
            if (evt.Status.ApplyVfxPrefab != null)
            {
                SpawnVfx(evt.Status.ApplyVfxPrefab, feet + Vector3.up * 0.1f, Quaternion.identity, evt.Status.ApplyVfxScale, 2.5f);
            }
            PlaySound(evt.Status.ApplySoundKey, feet);
        }

        private void OnStatusRemoved(StatusRemovedEvent evt)
        {
            if (evt.Target == null || evt.Status == null || !NeedsPresentation(evt.Status)) return;
            if (!evt.Target.TryGetComponent(out StatusPresentation presentation) || !presentation.Remove(evt.Status)) return;

            Vector3 feet = evt.Target.transform.position;
            if (evt.Status.RemoveVfxPrefab != null)
            {
                SpawnVfx(evt.Status.RemoveVfxPrefab, feet + Vector3.up * 0.1f, Quaternion.identity, evt.Status.RemoveVfxScale, 2.5f);
            }
            PlaySound(evt.Status.RemoveSoundKey, feet);
        }

        private static bool NeedsPresentation(StatusDefinitionSO status)
        {
            return status.RimColor.a > 0f || status.FootstepShake > 0f || status.ApplyVfxPrefab != null
                || status.RemoveVfxPrefab != null || !string.IsNullOrEmpty(status.ApplySoundKey) || !string.IsNullOrEmpty(status.RemoveSoundKey);
        }

        private StatusPresentation GetOrAddPresentation(GameObject target)
        {
            if (target.TryGetComponent(out StatusPresentation existing)) return existing;

            var presentation = target.AddComponent<StatusPresentation>();
            presentation.Initialize(_rimMaterial);
            presentation.IsControlledByPlayer = () => target == GetActiveHeroObject();
            presentation.OnFootstep = OnHeavyFootstep;
            return presentation;
        }

        private void OnHeavyFootstep(Vector3 position, float shake)
        {
            Shake(shake);
            if (_config.LandDustPrefab != null)
            {
                SpawnVfx(_config.LandDustPrefab, position, Quaternion.identity, _config.FootstepDustScale, 1.2f);
            }
            PlaySound(_config.LandSoundKey, position);
        }

        #endregion

        #region Hitstop

        private bool IsPaused => _pauseSystem != null && _pauseSystem.IsPaused;

        private void RequestHitStop(float duration)
        {
            RequestTimeWarp(_config.HitStopTimeScale, duration, false);
        }

        /// <summary>
        /// Giảm timeScale trong một khoảng thời gian thực. Hitstop tôn trọng khoảng nghỉ,
        /// slow-motion của Ultimate thì luôn chạy. Nhiều yêu cầu chồng nhau lấy scale nhỏ nhất và kết thúc muộn nhất.
        /// </summary>
        private void RequestTimeWarp(float scale, float duration, bool ignoreCooldown)
        {
            if (duration <= 0f || IsPaused) return;

            float now = Time.unscaledTime;
            if (!_isHitStopped)
            {
                if (!ignoreCooldown && now < _hitStopReadyAt) return;

                _isHitStopped = true;
                _hitStopStart = now;
                _hitStopEnd = now;
                _hitStopCap = _config.MaxHitStop;
                _savedTimeScale = Time.timeScale;
            }

            _hitStopCap = Mathf.Max(_hitStopCap, duration);
            Time.timeScale = Mathf.Min(Time.timeScale, scale);
            _hitStopEnd = Mathf.Min(Mathf.Max(_hitStopEnd, now + duration), _hitStopStart + _hitStopCap);
        }

        private void EndHitStop()
        {
            _isHitStopped = false;
            _hitStopReadyAt = Time.unscaledTime + _config.HitStopCooldown;

            // PauseSystem đang giữ timeScale = 0 thì để nó tự trả lại khi bỏ tạm dừng.
            if (!IsPaused) Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
        }

        #endregion

        #region VFX pool

        private void SpawnVfx(GameObject prefab, Vector3 position, Quaternion rotation, float scale, float lifetime)
        {
            SpawnVfx(prefab, position, rotation, Vector3.one * scale, lifetime);
        }

        private void SpawnVfx(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale, float lifetime)
        {
            if (!_vfxPool.TryGetValue(prefab, out Stack<GameObject> pool))
            {
                pool = new Stack<GameObject>();
                _vfxPool.Add(prefab, pool);
            }

            GameObject instance = null;
            while (pool.Count > 0 && instance == null) instance = pool.Pop();

            if (instance == null)
            {
                instance = VfxSpawner.Spawn(prefab, position, rotation, _poolRoot);
                // Đèn điểm cho mỗi cú đánh quá tốn trên mobile và dễ vượt giới hạn đèn của URP.
                foreach (Light light in instance.GetComponentsInChildren<Light>(true)) light.enabled = false;
            }
            else
            {
                instance.transform.SetPositionAndRotation(position, rotation);
            }

            instance.transform.localScale = Vector3.Scale(prefab.transform.localScale, scale);
            instance.SetActive(true);
            foreach (ParticleSystem particles in instance.GetComponentsInChildren<ParticleSystem>())
            {
                if (particles.transform.parent != null && particles.transform.parent.GetComponent<ParticleSystem>() != null) continue;
                particles.Clear(true);
                particles.Play(true);
            }

            _activeVfx.Add(new ActiveVfx { Prefab = prefab, Instance = instance, ReleaseTime = Time.time + lifetime });
        }

        /// <summary>VFX đi theo một xương (ví dụ mũi kiếm). Trả về pool khi hết thời gian.</summary>
        private void SpawnAttachedVfx(GameObject prefab, Transform attach, float scale, float lifetime)
        {
            SpawnVfx(prefab, attach.position, attach.rotation, scale, lifetime);
            ActiveVfx active = _activeVfx[_activeVfx.Count - 1];
            active.Instance.transform.SetParent(attach, true);
            active.IsAttached = true;
            _activeVfx[_activeVfx.Count - 1] = active;
        }

        private void ReleaseFinishedVfx()
        {
            for (int i = _activeVfx.Count - 1; i >= 0; i--)
            {
                ActiveVfx active = _activeVfx[i];
                if (active.Instance != null && Time.time < active.ReleaseTime) continue;

                _activeVfx.RemoveAt(i);
                if (active.Instance == null) continue;

                if (active.IsAttached) active.Instance.transform.SetParent(_poolRoot, false);
                active.Instance.SetActive(false);
                _vfxPool[active.Prefab].Push(active.Instance);
            }
        }

        #endregion

        #region Helpers

        private GameObject GetActiveHeroObject()
        {
            var party = ServiceLocator.Get<PartyCombatManager>();
            HeroInstance hero = party != null ? party.GetActiveHero() : null;
            return hero != null ? hero.gameObject : null;
        }

        private static bool IsEnemy(GameObject caster)
        {
            return caster.GetComponentInParent<EnemyBotController>() != null;
        }

        private static void PlaySound(string key, Vector3 position)
        {
            if (!string.IsNullOrEmpty(key)) EventBus.Publish(new PlayAudioEvent(key, position));
        }

        private void Shake(float strength)
        {
            if (strength > 0f && CameraController.Instance != null) CameraController.Instance.AddShake(strength);
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent(out T component) ? component : target.AddComponent<T>();
        }

        private static GameObject FirstNonNull(params GameObject[] candidates)
        {
            foreach (GameObject candidate in candidates)
            {
                if (candidate != null) return candidate;
            }
            return null;
        }

        private static Material CreateMaterial(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            return shader != null ? new Material(shader) : null;
        }

        #endregion
    }
}
