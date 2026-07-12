using System;
using System.Collections.Generic;
using Dylanng.Core;
using Dylanng.Core.Base;
using Dylanng.Managers;
using Game.Data.Hero;
using Game.Managers.Hero;
using UnityEngine;

namespace Game.Managers.Party
{
    /// <summary>
    /// Manages the party roster members up to a maximum limit (PartySize = 5).
    /// Synchronizes the selected party heroes from the HeroDataManager.
    /// </summary>
    public class PartyManager : ManagerBase
    {
        [SerializeField] private HeroLocalDataSO hero;
        
        public const int PartySize = 5;
        
        public string[] HeroIds;
        public Action OnPartyChanged;
        
        public override void Initialize()
        {
            ServiceLocator.Register<PartyManager>(this);
            
            HeroIds = new string[PartySize];
            
            // Load the party roster members from HeroDataManager's SaveData
            var heroDataManager = ServiceLocator.Get<HeroDataManager>();
            if (heroDataManager != null && heroDataManager.SaveData != null && heroDataManager.SaveData.Heros != null)
            {
                int slotIndex = 0;
                foreach (var heroInstance in heroDataManager.SaveData.Heros)
                {
                    if (heroInstance != null && heroInstance.IsPartyMemeber && slotIndex < PartySize)
                    {
                        HeroIds[slotIndex] = heroInstance.HeroId;
                        slotIndex++;
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to add a Hero into the first empty slot in the party.
        /// </summary>
        public bool TryAddHero(string addedHeroId)
        {
            for (int i = 0; i < PartySize; i++)
            {
                if (string.IsNullOrEmpty(HeroIds[i]))
                {
                    HeroIds[i] = addedHeroId;
                    OnPartyChanged?.Invoke();
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Swaps an existing hero in the party roster with a new hero.
        /// </summary>
        public bool TrySwapHero(string oldHeroId, string newHeroId)
        {
            for (int i = 0; i < PartySize; i++)
            {
                if (HeroIds[i] == oldHeroId)
                {
                    HeroIds[i] = newHeroId;
                    OnPartyChanged?.Invoke();
                    return true;
                }
            }
            return false;
        }
    }
}