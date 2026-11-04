using Game.Data.Hero;

namespace Game.Combat.Domain
{
    public interface IElementChart
    {
        ElementRelation GetRelation(GameElement attacker, GameElement defender);
        float GetDamageMultiplier(ElementRelation relation);
    }
}
