using System.Collections.Generic;
using UnityEngine;

namespace GAME.Data.Gacha
{
    public class GachaDataManager : MonoBehaviour
    {
        #region ---SINGLETON---
        private static GachaDataManager _instance;

        public static GachaDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GachaDataManager>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject(nameof(GachaDataManager));
                        _instance = go.AddComponent<GachaDataManager>();
                    }
                }

                return _instance;
            }
        }
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }
        #endregion
        
        public void Initialize()
        {
            //Load remote
        }

        #region ---LOCAL DATA---
        [field: SerializeField] public GachaLocalData LocalData { get; private set; }
        
        public List<WeaponLocalData> LocalWeapons => LocalData?.Weapons;
        public List<ArmorLocalData> LocalArmors => LocalData?.Armors;
        public List<AccessoryLocalData> LocalAccessories => LocalData?.Accessories;
        public List<ConsumableLocalData> LocalConsumables => LocalData?.Consumables;
        public List<CharacterLocalData> LocalCharacters => LocalData?.Characters;
        public List<ShopThemeLocalData> LocalShopThemes => LocalData?.ShopThemes;

        // Find function
        public WeaponLocalData GetLocalWeapon(string id) => LocalData?.Weapons?.Find(x => x.Id == id);
        public ArmorLocalData GetLocalArmor(string id) => LocalData?.Armors?.Find(x => x.Id == id);
        public AccessoryLocalData GetLocalAccessory(string id) => LocalData?.Accessories?.Find(x => x.Id == id);
        public ConsumableLocalData GetLocalConsumable(string id) => LocalData?.Consumables?.Find(x => x.Id == id);
        public MagicSkillLocalData GetLocalMagicSkill(string id) => LocalData?.MagicAndSkills?.Find(x => x.Id == id);
        public CharacterLocalData GetLocalCharacter(string id) => LocalData?.Characters?.Find(x => x.Id == id);
        public CosmeticLocalData GetLocalCosmetic(string id) => LocalData?.Cosmetics?.Find(x => x.Id == id);
        public MountPetLocalData GetLocalMountPet(string id) => LocalData?.MountsAndPets?.Find(x => x.Id == id);
        public FurnitureLocalData GetLocalFurniture(string id) => LocalData?.Furnitures?.Find(x => x.Id == id);
        public MaterialUpgradeLocalData GetLocalMaterial(string id) => LocalData?.MaterialsAndUpgrades?.Find(x => x.Id == id);
        public MysteryItemLocalData GetLocalMysteryItem(string id) => LocalData?.MysteryItems?.Find(x => x.Id == id);
        public ShopThemeLocalData GetLocalShopTheme(string shopId) => LocalData?.ShopThemes?.Find(x => x.ShopId == shopId);

        #endregion
        
        #region ---REMOTE DATA---
        [field: Header("--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------")]
        [field: SerializeField] public GachaRemoteData RemoteData { get; private set; }
        
        public List<WeaponRemoteData> RemoteWeapons => RemoteData?.Weapons;
        public List<ArmorRemoteData> RemoteArmors => RemoteData?.Armors;
        public List<AccessoryRemoteData> RemoteAccessories => RemoteData?.Accessories;
        public List<ConsumableRemoteData> RemoteConsumables => RemoteData?.Consumables;
        public List<CharacterRemoteData> RemoteCharacters => RemoteData?.Characters;
        public List<ShopInstanceRemoteData> ActiveShops => RemoteData?.ActiveShops;

        // Find function
        public WeaponRemoteData GetRemoteWeapon(string id) => RemoteData?.Weapons?.Find(x => x.Id == id);
        public ArmorRemoteData GetRemoteArmor(string id) => RemoteData?.Armors?.Find(x => x.Id == id);
        public AccessoryRemoteData GetRemoteAccessory(string id) => RemoteData?.Accessories?.Find(x => x.Id == id);
        public ConsumableRemoteData GetRemoteConsumable(string id) => RemoteData?.Consumables?.Find(x => x.Id == id);
        public MagicSkillRemoteData GetRemoteMagicSkill(string id) => RemoteData?.MagicAndSkills?.Find(x => x.Id == id);
        public CharacterRemoteData GetRemoteCharacter(string id) => RemoteData?.Characters?.Find(x => x.Id == id);
        public CosmeticRemoteData GetRemoteCosmetic(string id) => RemoteData?.Cosmetics?.Find(x => x.Id == id);
        public MountPetRemoteData GetRemoteMountPet(string id) => RemoteData?.MountsAndPets?.Find(x => x.Id == id);
        public FurnitureRemoteData GetRemoteFurniture(string id) => RemoteData?.Furnitures?.Find(x => x.Id == id);
        public MaterialUpgradeRemoteData GetRemoteMaterial(string id) => RemoteData?.MaterialsAndUpgrades?.Find(x => x.Id == id);
        public ShopInstanceRemoteData GetActiveShop(string shopId) => RemoteData?.ActiveShops?.Find(x => x.ShopId == shopId);

        #endregion
    }
}