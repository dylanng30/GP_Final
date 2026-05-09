using System;
using System.Collections.Generic;
using Dylanng.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UIs.Screens
{
    public class Screen_CombatHUD : UIScreen
    {
        [SerializeField] private List<CharacterAvatarCircleUI> characterAvatarCircles;

        private void RefreshUI()
        {
            
        }
    }

    [Serializable]
    public class CharacterAvatarCircleUI
    {
        public Image Avatar, HealthCircle;
        public GameObject DeathOverlay;
    }
}

