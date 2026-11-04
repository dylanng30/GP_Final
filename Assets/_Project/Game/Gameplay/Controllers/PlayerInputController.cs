using Dylanng;
using Game.Combat;
using Game.Gameplay.Entities;
using Game.Managers.Party;
using UnityEngine;

namespace Game.Gameplay.Controllers
{
    /// <summary>Biến PlayerIntent thành hành động của hero đang điều khiển.</summary>
    public class PlayerInputController : MonoBehaviour
    {
        private const float MoveDeadZone = 0.01f;
        private const float SkillBufferWindow = 0.8f;
        private const int NoBufferedSkill = -1;
        private static readonly int[] SkillSlots = { AbilitySlot.Skill1, AbilitySlot.Skill2, AbilitySlot.Ultimate };

        private HeroInstance _hero;
        private HeroAIController _aiController;
        private PlayerTargetingSystem _targetingSystem;
        private IPlayerIntentSource _intentSource;
        private int _bufferedSkillSlot = NoBufferedSkill;
        private float _skillBufferExpireTime;

        public void Initialize(HeroInstance hero)
        {
            _hero = hero;
            _aiController = GetComponent<HeroAIController>();
            _targetingSystem = GetComponent<PlayerTargetingSystem>();
            _intentSource = new DeviceIntentSource(ServiceLocator.Get<InputManager>());
        }

        /// <summary>Thay nguồn input, ví dụ khi chạy kiểm thử tự động.</summary>
        public void SetIntentSource(IPlayerIntentSource intentSource)
        {
            _intentSource = intentSource;
        }

        private void OnEnable()
        {
            if (_targetingSystem == null) _targetingSystem = GetComponent<PlayerTargetingSystem>();
            if (_targetingSystem != null) _targetingSystem.enabled = true;
        }

        private void OnDisable()
        {
            if (_targetingSystem == null) return;

            _targetingSystem.ClearTarget();
            _targetingSystem.enabled = false;
        }

        private void Update()
        {
            if (_hero == null || _intentSource == null) return;

            PlayerIntent intent = _intentSource.Read();

            ApplyMovement(intent.Move);
            ApplyActions(intent);
            ApplySkills(intent);
            ApplySystemCommands(intent);
        }

        private void ApplyMovement(Vector2 move)
        {
            if (move.sqrMagnitude > MoveDeadZone)
            {
                _hero.Move(move);
            }
            else if (_aiController == null || !_aiController.IsInCombatState)
            {
                _hero.StopMove();
            }
        }

        private void ApplyActions(in PlayerIntent intent)
        {
            if (intent.Jump) _hero.Jump();
            if (intent.Dash) _hero.Dash();
            if (intent.Attack) Attack();
        }

        private void Attack()
        {
            ITargetable target = _targetingSystem != null ? _targetingSystem.CurrentTarget : null;
            if (target == null)
            {
                _hero.FaceTowards(AimResolver.Resolve(_hero, null, null).Point);
            }

            _hero.Attack(target);
        }

        private void ApplySkills(in PlayerIntent intent)
        {
            ConsumeBufferedSkill();

            if (intent.Skill1) UseSkill(SkillSlots[0]);
            if (intent.Skill2) UseSkill(SkillSlots[1]);
            if (intent.Ultimate) UseSkill(SkillSlots[2]);
        }

        private void ConsumeBufferedSkill()
        {
            if (_bufferedSkillSlot == NoBufferedSkill || _hero.IsCasting) return;

            int slot = _bufferedSkillSlot;
            _bufferedSkillSlot = NoBufferedSkill;

            if (Time.time <= _skillBufferExpireTime) UseSkill(slot);
        }

        private void UseSkill(int slot)
        {
            // Bấm trong lúc đang cast thì giữ lại để dùng ngay khi cast xong.
            if (_hero.IsCasting)
            {
                _bufferedSkillSlot = slot;
                _skillBufferExpireTime = Time.time + SkillBufferWindow;
                return;
            }

            ITargetable selected = _targetingSystem != null ? _targetingSystem.CurrentTarget : null;
            AimResult aim = AimResolver.Resolve(_hero, selected, _hero.GetAbility(slot));

            var ability = _hero.GetAbility(slot);
            if (ability != null && ability.RequiresTarget && aim.Target == null) return;

            _hero.UseSkill(slot, aim.Target, aim.Point);
        }

        private void ApplySystemCommands(in PlayerIntent intent)
        {
            if (intent.LockOn && _targetingSystem != null) _targetingSystem.ToggleHardLock();
            if (intent.ToggleView && CameraController.Instance != null) CameraController.Instance.ToggleMode();

            var party = (intent.Regroup || intent.SwitchHeroIndex != PlayerIntent.NoHeroSelected)
                ? ServiceLocator.Get<PartyCombatManager>()
                : null;
            if (party == null) return;

            if (intent.Regroup) party.RegroupFollowers();
            if (intent.SwitchHeroIndex != PlayerIntent.NoHeroSelected) party.SwitchActiveHero(intent.SwitchHeroIndex);
        }
    }
}
