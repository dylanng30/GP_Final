using System.Collections.Generic;
using System.Linq;
using Dylanng;
using Game.Combat;
using Game.Combat.Domain;
using Game.Data.Hero;
using Game.Managers.Hero;
using Game.Managers.Party;
using TMPro;
using UnityEngine;

namespace Game.UIs.Party
{
    /// <summary>Hiện các liên kết tương sinh của đội ngay trên màn xếp đội.</summary>
    public class SynergyPreviewLabel : MonoBehaviour
    {
        private const float FontSize = 24f;
        private const string NoLinkText = "Chưa có liên kết tương sinh";
        private static readonly Vector2 LabelSize = new Vector2(900f, 80f);
        private static readonly Vector2 LabelPosition = new Vector2(0f, 24f);

        private readonly List<GameElement> _elements = new List<GameElement>();
        private readonly List<ElementSynergySO> _links = new List<ElementSynergySO>();

        private TMP_Text _label;

        private void Awake()
        {
            _label = CreateLabel();
        }

        private void OnEnable()
        {
            var partyManager = ServiceLocator.Get<PartyManager>();
            if (partyManager != null) partyManager.OnPartyChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            var partyManager = ServiceLocator.Get<PartyManager>();
            if (partyManager != null) partyManager.OnPartyChanged -= Refresh;
        }

        public void Refresh()
        {
            var partyManager = ServiceLocator.Get<PartyManager>();
            var heroData = ServiceLocator.Get<HeroDataManager>();
            var synergy = ServiceLocator.Get<PartySynergyService>();
            if (partyManager == null || heroData == null || synergy == null) return;

            _elements.Clear();
            foreach (string heroId in partyManager.HeroIds)
            {
                BaseHeroData hero = string.IsNullOrEmpty(heroId) ? null : heroData.GetHeroData(heroId);
                if (hero != null) _elements.Add(hero.Faction);
            }

            synergy.Preview(_elements, _links);

            _label.text = _links.Count > 0
                ? string.Join("\n", _links.Select(link => $"{link.DisplayName}: {link.Description}"))
                : NoLinkText;
        }

        private TMP_Text CreateLabel()
        {
            var labelObject = new GameObject("SynergyPreview", typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);

            var rect = (RectTransform)labelObject.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = LabelPosition;
            rect.sizeDelta = LabelSize;

            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = FontSize;
            label.alignment = TextAlignmentOptions.Bottom;
            label.raycastTarget = false;
            return label;
        }
    }
}
