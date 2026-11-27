using System.Collections;
using System.Collections.Generic;
using Dylanng;
using Game.Gameplay.Entities;
using Game.Managers.Party;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// Giữ đội trong map mở: hero rơi xuống dưới <see cref="killY"/> được đưa về checkpoint gần nhất đã đi qua,
    /// cả đội ngã thì hồi sinh tất cả ở checkpoint đó sau một khoảng chờ.
    /// Checkpoint mặc định là <see cref="PlayerSpawnPoint"/> của scene.
    /// </summary>
    public sealed class WorldRespawnService : MonoBehaviour
    {
        [Tooltip("Hero có vị trí Y thấp hơn giá trị này coi như đã rơi khỏi map")]
        [SerializeField] private float killY = -40f;

        [Tooltip("Số giây chờ trước khi hồi sinh cả đội")]
        [SerializeField] private float partyWipeReviveDelay = 3f;

        [SerializeField] private float formationSpacing = 2f;

        [Tooltip("Kích thước thân hero dùng để tìm chỗ đứng không vướng vật cản")]
        [SerializeField] private float actorRadius = 0.6f;
        [SerializeField] private float actorHeight = 2.6f;

        private readonly List<Checkpoint> _checkpoints = new List<Checkpoint>();
        private Vector3 _respawnPosition;
        private Quaternion _respawnRotation = Quaternion.identity;
        private Checkpoint _currentCheckpoint;
        private Coroutine _wipeRoutine;

        public float KillY => killY;
        public Checkpoint CurrentCheckpoint => _currentCheckpoint;

        private void Start()
        {
            _checkpoints.AddRange(FindObjectsByType<Checkpoint>());

            PlayerSpawnPoint spawn = PlayerSpawnPoint.Current;
            Transform origin = spawn != null ? spawn.transform : transform;
            _respawnPosition = origin.position;
            _respawnRotation = Quaternion.Euler(0f, origin.eulerAngles.y, 0f);
        }

        private void Update()
        {
            PartyCombatManager party = ServiceLocator.Get<PartyCombatManager>();
            if (party == null || party.ActiveHeroes.Count == 0) return;

            HeroInstance leader = party.GetActiveHero();
            if (leader != null && !leader.IsDead) TryActivateCheckpoint(leader.transform.position);

            IReadOnlyList<HeroInstance> heroes = party.ActiveHeroes;
            bool anyAlive = false;
            for (int i = 0; i < heroes.Count; i++)
            {
                HeroInstance hero = heroes[i];
                if (hero == null) continue;
                if (!hero.IsDead) anyAlive = true;
                if (hero.transform.position.y < killY) Teleport(hero, GetFormationPosition(i, heroes.Count));
            }

            if (!anyAlive && _wipeRoutine == null) _wipeRoutine = StartCoroutine(ReviveParty(party));
        }

        private void TryActivateCheckpoint(Vector3 leaderPosition)
        {
            for (int i = 0; i < _checkpoints.Count; i++)
            {
                Checkpoint checkpoint = _checkpoints[i];
                if (checkpoint == null || checkpoint == _currentCheckpoint || !checkpoint.Contains(leaderPosition)) continue;

                _currentCheckpoint = checkpoint;
                _respawnPosition = checkpoint.Position;
                _respawnRotation = checkpoint.Rotation;
                GameLogger.Log($"[WorldRespawnService] Checkpoint: {checkpoint.name}");
                return;
            }
        }

        private IEnumerator ReviveParty(PartyCombatManager party)
        {
            yield return new WaitForSeconds(partyWipeReviveDelay);

            IReadOnlyList<HeroInstance> heroes = party.ActiveHeroes;
            for (int i = 0; i < heroes.Count; i++)
            {
                if (heroes[i] != null) heroes[i].Revive(GetFormationPosition(i, heroes.Count), _respawnRotation);
            }

            party.SwitchActiveHero(0);
            _wipeRoutine = null;
        }

        private Vector3 GetFormationPosition(int index, int count)
        {
            return PlayerSpawnPoint.GetFormationPosition(_respawnPosition, _respawnRotation, index, count,
                formationSpacing, actorRadius, actorHeight);
        }

        private void Teleport(HeroInstance hero, Vector3 position)
        {
            var controller = hero.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            hero.transform.SetPositionAndRotation(position, _respawnRotation);
            if (controller != null) controller.enabled = wasEnabled;
            hero.StopMove();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
            Vector3 center = transform.position;
            center.y = killY;
            Gizmos.DrawCube(center, new Vector3(600f, 0.1f, 600f));
        }
    }
}
