#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Linq;
using Dylanng;
using Game.Combat;
using Game.Data.Hero;
using Game.Gameplay.Entities;
using Game.Managers.Hero;
using UnityEngine;

namespace Game.Managers.Party
{
    /// <summary>
    /// Công cụ test, không có trong bản phát hành: đổi nhanh hero đang điều khiển sang bất kỳ hero nào có model.
    /// Tab thay bằng hero kế tiếp chưa có trong đội. Bảng ở góc phải bấm được bằng chuột hoặc chạm.
    /// Hero mới đứng đúng chỗ hero cũ, hai hero còn lại giữ nguyên.
    /// </summary>
    public sealed class HeroSwapDebugPanel : MonoBehaviour
    {
        private const KeyCode CycleKey = KeyCode.Tab;
        private const float ReferenceHeight = 1080f;
        private const float Margin = 16f;
        private const float ButtonWidth = 300f;
        private const float ButtonHeight = 56f;
        private const float Spacing = 6f;
        private const float TopOffset = 120f;
        private const int FontSize = 24;
        private const string ToggleLabelClosed = "Đổi hero (Tab)  ▼";
        private const string ToggleLabelOpen = "Đổi hero (Tab)  ▲";
        private const string ControlledMark = "▶ ";
        private const string InPartyMark = "● ";

        private bool _isOpen;
        private GUIStyle _buttonStyle;

        private void Update()
        {
            if (Input.GetKeyDown(CycleKey)) CycleControlledHero();
        }

        /// <summary>Thay hero đang điều khiển bằng hero kế tiếp theo id, bỏ qua hero đã có trong đội.</summary>
        public void CycleControlledHero()
        {
            HeroInstance controlled = ServiceLocator.Get<PartyCombatManager>()?.GetActiveHero();
            IReadOnlyList<string> roster = ServiceLocator.Get<HeroDataManager>()?.GetPlayableHeroIds();
            string[] party = ServiceLocator.Get<PartyManager>()?.HeroIds;
            if (controlled == null || roster == null || party == null || roster.Count == 0) return;

            int start = Mathf.Max(0, roster.ToList().IndexOf(controlled.HeroId));
            for (int step = 1; step < roster.Count; step++)
            {
                string candidate = roster[(start + step) % roster.Count];
                if (!party.Contains(candidate))
                {
                    SelectHero(candidate);
                    return;
                }
            }
        }

        /// <summary>Hero đã trong đội thì chuyển quyền điều khiển, chưa thì thay vào chỗ hero đang điều khiển.</summary>
        public void SelectHero(string heroId)
        {
            var partyCombat = ServiceLocator.Get<PartyCombatManager>();
            var party = ServiceLocator.Get<PartyManager>();
            HeroInstance controlled = partyCombat?.GetActiveHero();
            if (controlled == null || party == null) return;

            int memberIndex = partyCombat.ActiveHeroes.ToList().FindIndex(hero => hero != null && hero.HeroId == heroId);
            if (memberIndex >= 0) partyCombat.SwitchActiveHero(memberIndex);
            else party.TrySwapHero(controlled.HeroId, heroId);
        }

        private void OnGUI()
        {
            var heroData = ServiceLocator.Get<HeroDataManager>();
            var partyCombat = ServiceLocator.Get<PartyCombatManager>();
            if (heroData == null || partyCombat == null || partyCombat.GetActiveHero() == null) return;

            float scale = Screen.height / ReferenceHeight;
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            _buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = FontSize, alignment = TextAnchor.MiddleLeft };

            float x = Screen.width / scale - ButtonWidth - Margin;
            float y = TopOffset;

            if (DrawButton(x, ref y, scale, _isOpen ? ToggleLabelOpen : ToggleLabelClosed)) _isOpen = !_isOpen;

            if (_isOpen)
            {
                string controlledId = partyCombat.GetActiveHero().HeroId;
                string[] party = ServiceLocator.Get<PartyManager>()?.HeroIds ?? new string[0];

                foreach (string heroId in heroData.GetPlayableHeroIds())
                {
                    if (DrawButton(x, ref y, scale, BuildLabel(heroData.GetHeroData(heroId), heroId, controlledId, party)))
                    {
                        SelectHero(heroId);
                    }
                }
            }

            GUI.matrix = previousMatrix;
        }

        private bool DrawButton(float x, ref float y, float scale, string label)
        {
            var rect = new Rect(x, y, ButtonWidth, ButtonHeight);
            y += ButtonHeight + Spacing;

            DebugUiHitArea.Register(new Rect(rect.x * scale, rect.y * scale, rect.width * scale, rect.height * scale));
            return GUI.Button(rect, label, _buttonStyle);
        }

        private static string BuildLabel(BaseHeroData data, string heroId, string controlledId, string[] party)
        {
            string mark = heroId == controlledId ? ControlledMark : party.Contains(heroId) ? InPartyMark : "    ";
            string name = data != null && !string.IsNullOrEmpty(data.HeroName) ? data.HeroName : heroId;
            string element = data != null ? data.Faction.ToString() : "?";
            return $"{mark}{name}  ({element})";
        }
    }
}
#endif
