using System.Collections;
using DG.Tweening;
using Dylanng;
using Game.Combat.Abilities.Events;
using Game.Combat.Stats;
using Game.Gameplay.AI.Controllers;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Monster
{
    /// <summary>
    /// Vòng đời của một con quái thường: chỉ số khởi tạo, hồi đầy máu khi bỏ đuổi về lại điểm spawn,
    /// lật ngửa rồi lún xuống đất khi chết và tái xuất hiện ở điểm spawn sau một khoảng thời gian.
    /// AI đuổi đánh và giới hạn vùng hoạt động nằm ở EnemyBotController.
    /// </summary>
    [RequireComponent(typeof(HeroInstance))]
    public class MonsterInstance : MonoBehaviour
    {
        [Header("--- STATS ---")]
        [SerializeField] private float maxHp = 1500f;
        [SerializeField] private float attack = 80f;
        [SerializeField] private float defense = 40f;
        [SerializeField] private float critRate = 0.05f;
        [SerializeField] private float critDamage = 1.5f;

        [Header("--- RESET ---")]
        [Tooltip("Hồi đầy máu khi đã mất mục tiêu và về tới gần điểm spawn")]
        [SerializeField] private bool healOnReturn = true;
        [SerializeField] private float returnedDistance = 1.2f;

        [Header("--- DEATH & RESPAWN ---")]
        [Tooltip("Phần hình ảnh để lật và lún khi chết. Để trống sẽ dùng con đầu tiên")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float flipDuration = 0.35f;
        [Tooltip("Nâng model lên khi lật ngửa để thân không chìm xuống đất (pivot ở chân). Gần bằng chiều cao model")]
        [SerializeField] private float flipLift = 1.15f;
        [SerializeField] private float corpseDuration = 1.5f;
        [SerializeField] private float sinkDuration = 0.8f;
        [SerializeField] private float sinkDepth = 1.6f;
        [Tooltip("Số giây sau khi chết thì quái xuất hiện lại. 0 = không hồi sinh")]
        [SerializeField] private float respawnDelay = 10f;
        [SerializeField] private float respawnGrowDuration = 0.45f;

        private HeroInstance _actor;
        private EnemyBotController _bot;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private Vector3 _visualPosition;
        private Quaternion _visualRotation;
        private Vector3 _visualScale;
        private bool _isReturning;
        private Coroutine _deathRoutine;

        public Vector3 SpawnPosition => _spawnPosition;

        private void Awake()
        {
            _actor = GetComponent<HeroInstance>();
            _bot = GetComponent<EnemyBotController>();
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;

            if (visualRoot == null && transform.childCount > 0) visualRoot = transform.GetChild(0);
            if (visualRoot != null)
            {
                _visualPosition = visualRoot.localPosition;
                _visualRotation = visualRoot.localRotation;
                _visualScale = visualRoot.localScale;
            }

            // Khởi tạo trước Start của HeroInstance để nó không dùng chỉ số mặc định.
            var stats = GetComponent<StatController>();
            if (stats == null) return;

            stats.InitializeStat(StatType.MaxHP, maxHp);
            stats.InitializeStat(StatType.ATK, attack);
            stats.InitializeStat(StatType.DEF, defense);
            stats.InitializeStat(StatType.CritRate, critRate);
            stats.InitializeStat(StatType.CritDamage, critDamage);
        }

        private void OnEnable()
        {
            _actor.Health.Died += OnDied;
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
        }

        private void OnDisable()
        {
            _actor.Health.Died -= OnDied;
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            if (visualRoot != null) visualRoot.DOKill();
        }

        /// <summary>Bị đánh từ ngoài tầm phát hiện thì vẫn quay sang đuổi kẻ tấn công.</summary>
        private void OnDamageDealt(DamageDealtEvent evt)
        {
            if (evt.Target != gameObject || evt.Caster == null || _bot == null || !_bot.enabled) return;
            if (_actor.IsDead || _bot.CurrentTarget != null) return;

            ITargetable attacker = evt.Caster.GetComponentInParent<ITargetable>();
            if (attacker != null && attacker != (ITargetable)_actor) _bot.TrySetTarget(attacker);
        }

        private void Update()
        {
            if (!healOnReturn || _bot == null || _actor.IsDead) return;

            if (_bot.CurrentTarget != null)
            {
                _isReturning = false;
                return;
            }

            if (_actor.Health.Ratio < 1f && _bot.DistanceToSpawn > returnedDistance) _isReturning = true;

            if (_isReturning && _bot.DistanceToSpawn <= returnedDistance)
            {
                _isReturning = false;
                _actor.Health.Restore(_actor.Health.Max);
            }
        }

        private void OnDied()
        {
            if (_deathRoutine != null) StopCoroutine(_deathRoutine);
            _deathRoutine = StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            if (_bot != null)
            {
                _bot.ClearTarget();
                _bot.enabled = false;
            }
            _isReturning = false;

            if (visualRoot != null)
            {
                visualRoot.DOKill();
                Sequence flip = DOTween.Sequence().SetLink(visualRoot.gameObject);
                flip.Append(visualRoot.DOLocalJump(_visualPosition + Vector3.up * flipLift, 0.5f, 1, flipDuration));
                flip.Join(visualRoot.DOLocalRotateQuaternion(_visualRotation * Quaternion.Euler(0f, 0f, 175f), flipDuration).SetEase(Ease.OutQuad));
            }

            yield return new WaitForSeconds(flipDuration + corpseDuration);

            if (visualRoot != null)
            {
                visualRoot.DOLocalMoveY(_visualPosition.y + flipLift - sinkDepth, sinkDuration).SetEase(Ease.InQuad);
                visualRoot.DOScale(_visualScale * 0.6f, sinkDuration).SetEase(Ease.InQuad);
            }

            yield return new WaitForSeconds(sinkDuration);

            if (visualRoot != null) visualRoot.gameObject.SetActive(false);
            if (respawnDelay <= 0f) yield break;

            yield return new WaitForSeconds(Mathf.Max(0f, respawnDelay - flipDuration - corpseDuration - sinkDuration));
            Respawn();
        }

        private void Respawn()
        {
            _deathRoutine = null;

            if (visualRoot != null)
            {
                visualRoot.DOKill();
                visualRoot.localPosition = _visualPosition;
                visualRoot.localRotation = _visualRotation;
                visualRoot.localScale = _visualScale * 0.05f;
                visualRoot.gameObject.SetActive(true);
            }

            // Bật lại hình ảnh trước để Animator được rebind khi hồi sinh.
            _actor.Revive(_spawnPosition, _spawnRotation);
            if (visualRoot != null) visualRoot.DOScale(_visualScale, respawnGrowDuration).SetEase(Ease.OutBack);

            if (_bot != null) _bot.enabled = true;
        }
    }
}
