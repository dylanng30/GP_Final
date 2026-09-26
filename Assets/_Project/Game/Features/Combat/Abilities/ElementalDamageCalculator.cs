using Game.Data.Hero;

namespace Game.Gameplay.Combat
{
    public static class ElementalDamageCalculator
    {
        // Khắc chế: Thủy > Hỏa > Kim > Mộc > Thổ > Thủy
        // Light và Darkness khắc lẫn nhau.
        public static float GetMultiplier(GameElement attackerElement, GameElement defenderElement)
        {
            if (attackerElement == GameElement.Physical || defenderElement == GameElement.Physical)
                return 1.0f;
            
            // Khắc hệ (Strong against) -> x1.2 damage
            if (attackerElement == GameElement.Water && defenderElement == GameElement.Fire) return 1.2f;
            if (attackerElement == GameElement.Fire && defenderElement == GameElement.Metal) return 1.2f;
            if (attackerElement == GameElement.Metal && defenderElement == GameElement.Wood) return 1.2f;
            if (attackerElement == GameElement.Wood && defenderElement == GameElement.Earth) return 1.2f;
            if (attackerElement == GameElement.Earth && defenderElement == GameElement.Water) return 1.2f;
            
            if (attackerElement == GameElement.Light && defenderElement == GameElement.Darkness) return 1.2f;
            if (attackerElement == GameElement.Darkness && defenderElement == GameElement.Light) return 1.2f;

            // Bị khắc (Weak against) -> x0.8 damage
            if (defenderElement == GameElement.Water && attackerElement == GameElement.Fire) return 0.8f;
            if (defenderElement == GameElement.Fire && attackerElement == GameElement.Metal) return 0.8f;
            if (defenderElement == GameElement.Metal && attackerElement == GameElement.Wood) return 0.8f;
            if (defenderElement == GameElement.Wood && attackerElement == GameElement.Earth) return 0.8f;
            if (defenderElement == GameElement.Earth && attackerElement == GameElement.Water) return 0.8f;

            return 1.0f; // Trung bình
        }
    }
}
