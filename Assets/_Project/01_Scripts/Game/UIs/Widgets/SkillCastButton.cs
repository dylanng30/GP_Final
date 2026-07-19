using Dylanng.Core;
using Dylanng.Gameplay.RPG.Abilities;
using Game.Gameplay.Entities;
using Dylanng.Gameplay.RPG.Abilities.Managers;
using Game.Managers.Party;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Game.UIs.Widgets
{
    public class SkillCastButton : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("--- SKILL SETTINGS ---")]
        [SerializeField] private int skillIndex = 0;
        [SerializeField] private float maxDragRadius = 80f;
        [SerializeField] private float autoCastThreshold = 20f;

        [Header("--- VISUAL JOYSTICK COMPONENTS ---")]
        [SerializeField] private RectTransform joystickBackground;
        [SerializeField] private RectTransform joystickHandle;
        [SerializeField] private GameObject cancelCastZoneVisual;
        [SerializeField] private Image skillIconImage;
        
        [Header("--- COOLDOWN & ENERGY UI ---")]
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private TextMeshProUGUI cooldownText;
        [SerializeField] private TextMeshProUGUI energyText;

        [Header("--- INDICATOR COLORS ---")]
        [SerializeField] private Color normalColor = new Color(0.12f, 0.64f, 1f, 0.8f);
        [SerializeField] private Color cancelColor = new Color(1f, 0.2f, 0.2f, 0.8f);

        public void SetupSkill(HeroInstance hero)
        {
            if (hero == null)
            {
                gameObject.SetActive(false);
                return;
            }

            AbilitySO ability = hero.GetAbility(skillIndex);
            if (ability == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (skillIconImage != null)
            {
                skillIconImage.sprite = ability.Icon;
                skillIconImage.enabled = ability.Icon != null;
            }
        }

        private Vector2 _pointerDownPos;
        private bool _isDragging;


        [Header("--- INDICATOR ---")]
        [SerializeField] private SkillIndicator _indicator;

        private HeroInstance GetActiveHero()
        {
            var partyManager = ServiceLocator.Get<PartyCombatManager>();
            return partyManager?.GetActiveHero();
        }

        private void Update()
        {
            HeroInstance activeHero = GetActiveHero();
            if (activeHero == null) return;

            AbilitySO ability = activeHero.GetAbility(skillIndex);
            if (ability == null) return;

            // Update Energy Text
            if (energyText != null)
            {
                energyText.text = ability.EnergyCost.ToString();
                energyText.color = activeHero.CurrentEnergy >= ability.EnergyCost ? Color.green : Color.red;
            }

            // Update Cooldown Overlay and Text
            float remainingCD = activeHero.GetSkillRemainingCooldown(skillIndex);
            if (remainingCD > 0f)
            {
                if (cooldownOverlay != null)
                {
                    cooldownOverlay.fillAmount = remainingCD / ability.Cooldown;
                }
                if (cooldownText != null)
                {
                    cooldownText.text = remainingCD.ToString("F1");
                }
            }
            else
            {
                if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
                if (cooldownText != null) cooldownText.text = "";
            }
        }

        private bool IsPointerOverCancelZone(PointerEventData eventData)
        {
            if (CancelCastZone.Instance == null || CancelCastZone.Instance.RectTransform == null) return false;
            
            return RectTransformUtility.RectangleContainsScreenPoint(
                CancelCastZone.Instance.RectTransform, 
                eventData.position, 
                eventData.pressEventCamera
            );
        }

        private void Start()
        {
            if (joystickHandle != null)
            {
                joystickHandle.anchoredPosition = Vector2.zero;
            }
            if (cancelCastZoneVisual != null)
            {
                cancelCastZoneVisual.SetActive(false);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            HeroInstance activeHero = GetActiveHero();
            if (activeHero == null || !activeHero.CanUseSkill(skillIndex))
            {
                eventData.pointerDrag = null; // Prevent drag
                return;
            }

            _pointerDownPos = eventData.position;
            _isDragging = true;

            if (cancelCastZoneVisual != null)
            {
                cancelCastZoneVisual.SetActive(true);
            }

            // Use pre-assigned indicator
            // _indicator should be assigned in Inspector

            UpdateAiming(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            UpdateAiming(eventData);
        }

        private GameObject FindTargetNearPoint(HeroInstance hero, Vector3 point, float radius)
        {
            Collider[] colliders = Physics.OverlapSphere(point, radius);
            GameObject bestTarget = null;
            float minSqrDist = float.MaxValue;
            foreach (var col in colliders)
            {
                if (col.TryGetComponent<ITargetable>(out var targetable))
                {
                    if (targetable.IsDead || targetable.Transform == hero.transform) continue;
                    float sqrDist = (targetable.Transform.position - point).sqrMagnitude;
                    if (sqrDist < minSqrDist)
                    {
                        minSqrDist = sqrDist;
                        bestTarget = targetable.Transform.gameObject;
                    }
                }
            }
            return bestTarget;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;

            if (joystickHandle != null)
            {
                joystickHandle.anchoredPosition = Vector2.zero;
            }

            if (cancelCastZoneVisual != null)
            {
                cancelCastZoneVisual.SetActive(false);
            }

            HeroInstance activeHero = GetActiveHero();
            if (activeHero != null)
            {
                AbilitySO ability = activeHero.GetAbility(skillIndex);
                if (ability != null)
                {
                    bool isCancelled = IsPointerOverCancelZone(eventData);
                    
                    if (CancelCastZone.Instance != null)
                    {
                        CancelCastZone.Instance.SetHoverState(false);
                    }

                    if (!isCancelled)
                    {
                        Vector2 dragDelta = eventData.position - _pointerDownPos;
                        if (dragDelta.magnitude < autoCastThreshold)
                        {
                            // Auto cast
                            GameObject targetObj = null;
                            if (activeHero.TryGetComponent<Game.Gameplay.Controllers.PlayerTargetingSystem>(out var pts) && pts.CurrentTarget != null && !pts.CurrentTarget.IsDead)
                            {
                                float dist = Vector3.Distance(activeHero.transform.position, pts.CurrentTarget.Transform.position);
                                if (dist <= ability.Range)
                                {
                                    targetObj = pts.CurrentTarget.Transform.gameObject;
                                }
                            }

                            if (ability.RequiresTarget && targetObj == null)
                            {
                                Debug.Log($"[SkillCastButton] {ability.AbilityName} cần mục tiêu nhưng không có ai trong tầm!");
                            }
                            else
                            {
                                activeHero.UseSkill(skillIndex, targetObj);
                            }
                        }
                        else
                        {
                            // Manual cast
                            Vector2 dragDir = dragDelta.normalized;
                            float dragRatio = Mathf.Min(dragDelta.magnitude / maxDragRadius, 1f);

                            Transform cameraTrans = Camera.main != null ? Camera.main.transform : null;
                            Vector3 worldCastDir = activeHero.transform.forward;

                            if (cameraTrans != null)
                            {
                                Vector3 camForward = cameraTrans.forward;
                                Vector3 camRight = cameraTrans.right;
                                camForward.y = 0;
                                camRight.y = 0;
                                camForward.Normalize();
                                camRight.Normalize();

                                worldCastDir = (camRight * dragDir.x + camForward * dragDir.y).normalized;
                            }

                            Vector3 targetPoint = activeHero.transform.position + worldCastDir * (dragRatio * ability.Range);

                            if (ability.RequiresTarget)
                            {
                                GameObject targetObj = FindTargetNearPoint(activeHero, targetPoint, ability.IndicatorRadius);
                                if (targetObj == null)
                                {
                                    Debug.Log($"[SkillCastButton] {ability.AbilityName} cần mục tiêu nhưng không có ai ở khu vực chỉ định!");
                                }
                                else
                                {
                                    activeHero.UseSkill(skillIndex, targetObj, targetPoint);
                                }
                            }
                            else
                            {
                                activeHero.UseSkill(skillIndex, null, targetPoint);
                            }
                        }
                    }
                }
            }

            if (_indicator != null)
            {
                _indicator.HideAll();
            }
        }

        private void UpdateAiming(PointerEventData eventData)
        {
            HeroInstance activeHero = GetActiveHero();
            if (activeHero == null) return;

            AbilitySO ability = activeHero.GetAbility(skillIndex);
            if (ability == null) return;

            Vector2 dragDelta = eventData.position - _pointerDownPos;
            Vector2 dragDir = dragDelta.normalized;
            float dragDistance = Mathf.Min(dragDelta.magnitude, maxDragRadius);

            if (joystickHandle != null)
            {
                joystickHandle.anchoredPosition = dragDir * dragDistance;
            }

            float dragRatio = dragDistance / maxDragRadius;

            bool isCancelled = IsPointerOverCancelZone(eventData);
            
            if (CancelCastZone.Instance != null)
            {
                CancelCastZone.Instance.SetHoverState(isCancelled);
            }

            Color indicatorColor = isCancelled ? cancelColor : normalColor;

            if (_indicator != null)
            {
                _indicator.HideAll();

                if (ability.IndicatorType != SkillIndicatorType.None)
                {
                    _indicator.UpdateRangeIndicator(activeHero.transform.position, ability.Range, indicatorColor);

                    Transform cameraTrans = Camera.main != null ? Camera.main.transform : null;
                    Vector3 worldCastDir = activeHero.transform.forward;

                    if (cameraTrans != null)
                    {
                        Vector3 camForward = cameraTrans.forward;
                        Vector3 camRight = cameraTrans.right;
                        camForward.y = 0;
                        camRight.y = 0;
                        camForward.Normalize();
                        camRight.Normalize();

                        worldCastDir = (camRight * dragDir.x + camForward * dragDir.y).normalized;
                    }

                    if (ability.IndicatorType == SkillIndicatorType.AreaTarget)
                    {
                        Vector3 targetPoint = activeHero.transform.position + worldCastDir * (dragRatio * ability.Range);
                        _indicator.UpdateAreaIndicator(targetPoint, ability.IndicatorRadius, indicatorColor);
                    }
                    else if (ability.IndicatorType == SkillIndicatorType.Directional)
                    {
                        _indicator.UpdateDirectionalIndicator(activeHero.transform.position, worldCastDir, ability.Range, indicatorColor);
                    }
                }
            }
        }

        private void OnDisable()
        {
            _isDragging = false;
            if (joystickHandle != null)
            {
                joystickHandle.anchoredPosition = Vector2.zero;
            }
            if (cancelCastZoneVisual != null)
            {
                cancelCastZoneVisual.SetActive(false);
            }
            if (_indicator != null)
            {
                _indicator.HideAll();
            }
        }
    }
}
