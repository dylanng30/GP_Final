using System.Linq;
using System.Text;
using Dylanng;
using Game.Combat;
using Game.Combat.Domain;
using Game.Gameplay.Controllers;
using Game.Gameplay.Entities;
using Game.Managers.Party;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UIs.Screens.CombatHUD
{
    /// <summary>
    /// Bảng thông tin trận đấu dựng bằng code: Nộ, liên kết tương sinh, mục tiêu và tâm ngắm.
    /// Có thể thay bằng widget do UI artist làm mà không đổi dữ liệu.
    /// </summary>
    public class CombatInfoPanel : MonoBehaviour
    {
        private const float RefreshInterval = 0.2f;
        private const float FontSize = 26f;
        private const float CrosshairSize = 8f;
        private static readonly Vector2 PanelSize = new Vector2(760f, 150f);
        private static readonly Vector2 PanelPosition = new Vector2(0f, -24f);

        private TMP_Text _label;
        private Image _crosshair;
        private float _refreshTimer;

        private void Awake()
        {
            _label = CreateLabel();
            _crosshair = CreateCrosshair();
        }

        private void Update()
        {
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f) return;

            _refreshTimer = RefreshInterval;
            Refresh();
        }

        private void Refresh()
        {
            var party = ServiceLocator.Get<PartyCombatManager>();
            HeroInstance hero = party != null ? party.GetActiveHero() : null;
            if (hero == null) return;

            var text = new StringBuilder();
            text.AppendLine($"{hero.HeroId}  |  Nộ {hero.CurrentEnergy:0}/{hero.MaxEnergyLimit:0}");

            AppendLinks(text);
            AppendTarget(text, hero);

            _label.text = text.ToString();

            bool isThirdPerson = CameraController.Instance != null && CameraController.Instance.Mode == CameraMode.ThirdPerson;
            _crosshair.enabled = isThirdPerson;
        }

        private static void AppendLinks(StringBuilder text)
        {
            var synergy = ServiceLocator.Get<PartySynergyService>();
            if (synergy == null || synergy.ActiveLinks.Count == 0) return;

            text.AppendLine(string.Join("  •  ", synergy.ActiveLinks.Select(link => link.DisplayName)));
        }

        private static void AppendTarget(StringBuilder text, HeroInstance hero)
        {
            ITargetable target = hero.TryGetComponent<PlayerTargetingSystem>(out var targeting)
                ? targeting.CurrentTarget
                : null;
            if (target == null || target.Transform == null) return;

            string lockMark = targeting.IsHardLocked ? "[Khóa] " : string.Empty;
            string statuses = string.Join(", ", target.Status.Set.Active.Select(status => status.Definition.DisplayName));

            text.Append($"{lockMark}{target.Transform.name} ({target.Element}) {target.Health.Ratio:P0}");
            if (statuses.Length > 0) text.Append($"  -  {statuses}");
        }

        private TMP_Text CreateLabel()
        {
            var labelObject = new GameObject("CombatInfoLabel", typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);

            var rect = (RectTransform)labelObject.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = PanelPosition;
            rect.sizeDelta = PanelSize;

            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = FontSize;
            label.alignment = TextAlignmentOptions.Top;
            label.raycastTarget = false;
            return label;
        }

        private Image CreateCrosshair()
        {
            var crosshairObject = new GameObject("Crosshair", typeof(RectTransform));
            crosshairObject.transform.SetParent(transform, false);

            var rect = (RectTransform)crosshairObject.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.one * CrosshairSize;

            var image = crosshairObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }
    }
}
