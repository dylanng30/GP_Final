using EpicToonFX;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Tạo VFX từ gói Epic Toon FX. Mặc định ETFXLightFade xóa Light khi mờ hết, nhưng URP không cho xóa
    /// Light có UniversalAdditionalLightData, nên đổi sang tắt Light. Light sẽ hủy cùng VFX.
    /// </summary>
    public static class VfxSpawner
    {
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject vfx = Object.Instantiate(prefab, position, rotation, parent);

            foreach (ETFXLightFade lightFade in vfx.GetComponentsInChildren<ETFXLightFade>(true))
            {
                lightFade.onLifeEnd = ETFXLightFade.OnLifeEnd.Disable;
            }

            return vfx;
        }
    }
}
