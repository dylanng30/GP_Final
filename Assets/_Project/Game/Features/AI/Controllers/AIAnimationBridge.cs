using UnityEngine;

namespace Game.Gameplay.AI.Controllers
{
    /// <summary>
    /// Presentation layer bridge: Nhận lệnh hoặc state từ AI Brain/Controller và chuyển thành anim triggers/VFX.
    /// Đảm bảo Business Logic hoàn toàn tách rời khỏi Visual/Animator (Clean Architecture).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AIAnimationBridge : MonoBehaviour
    {
        private Animator _animator;
        
        // Cache lại hash để không gọi string trong hot loops
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
        private static readonly int CastSkillHash = Animator.StringToHash("CastSkillTrigger");
        private static readonly int SkillIndexHash = Animator.StringToHash("SkillIndex");
        private static readonly int HitTriggerHash = Animator.StringToHash("HitTrigger");
        private static readonly int DeadBoolHash = Animator.StringToHash("IsDead");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void SetMoveSpeed(float speed)
        {
            if (_animator != null) _animator.SetFloat(MoveSpeedHash, speed);
        }

        public void TriggerAttack()
        {
            if (_animator != null) _animator.SetTrigger(AttackTriggerHash);
        }

        public void TriggerCastSkill(int skillIndex)
        {
            if (_animator != null)
            {
                _animator.SetInteger(SkillIndexHash, skillIndex);
                _animator.SetTrigger(CastSkillHash);
            }
        }

        public void TriggerHit()
        {
            if (_animator != null) _animator.SetTrigger(HitTriggerHash);
        }

        public void SetDead(bool isDead)
        {
            if (_animator != null) _animator.SetBool(DeadBoolHash, isDead);
        }
    }
}
