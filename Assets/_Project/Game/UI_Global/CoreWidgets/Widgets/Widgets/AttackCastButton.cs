using Dylanng;
using Game.Combat.Abilities.Managers;
using Game.Gameplay.Entities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UIs.Widgets
{
    public class AttackCastButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("--- ATTACK SETTINGS ---")]
        [SerializeField] private float holdDurationThreshold = 0.15f;
        [SerializeField] private float defaultAttackRange = 2.5f;

        [Header("--- INDICATOR COLOR ---")]
        [SerializeField] private Color rangeIndicatorColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        private bool _isPointerDown = false;
        private float _pointerDownTime = 0f;
        private bool _isIndicatorShown = false;

        [Header("--- INDICATOR ---")]
        [SerializeField] private SkillIndicator _indicator;

        private HeroInstance GetActiveHero()
        {
            var partyManager = ServiceLocator.Get<Managers.Party.PartyCombatManager>();
            return partyManager?.GetActiveHero();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            HeroInstance activeHero = GetActiveHero();
            if (activeHero == null || !activeHero.CanAttack()) return; // Don't hold or attack if on cooldown

            _isPointerDown = true;
            _pointerDownTime = Time.time;
            _isIndicatorShown = false;
        }

        private void Update()
        {
            if (_isPointerDown && !_isIndicatorShown)
            {
                if (Time.time - _pointerDownTime >= holdDurationThreshold)
                {
                    ShowRangeIndicator();
                }
            }
            
            if (_isIndicatorShown && _indicator != null)
            {
                HeroInstance activeHero = GetActiveHero();
                if (activeHero != null)
                {
                    float range = GetAttackRange(activeHero);
                    _indicator.UpdateRangeIndicator(activeHero.transform.position, range, rangeIndicatorColor);
                }
            }
        }

        private float GetAttackRange(HeroInstance hero)
        {
            float range = defaultAttackRange;
            var aiController = hero.GetComponent<Game.Gameplay.Controllers.HeroAIController>();
            if (aiController != null)
            {
                range = aiController.AttackRange;
            }
            return range;
        }

        private void ShowRangeIndicator()
        {
            HeroInstance activeHero = GetActiveHero();
            if (activeHero == null) return;

            _isIndicatorShown = true;

            if (_indicator != null)
            {
                float range = GetAttackRange(activeHero);
                _indicator.UpdateRangeIndicator(activeHero.transform.position, range, rangeIndicatorColor);
            }
        }

        private void HideIndicator()
        {
            if (_indicator != null)
            {
                _indicator.HideAll();
            }
            _isIndicatorShown = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPointerDown) return;
            
            _isPointerDown = false;
            HideIndicator();

            HeroInstance activeHero = GetActiveHero();
            if (activeHero != null)
            {
                activeHero.Attack();
            }
        }

        private void OnDisable()
        {
            _isPointerDown = false;
            HideIndicator();
        }
    }
}
