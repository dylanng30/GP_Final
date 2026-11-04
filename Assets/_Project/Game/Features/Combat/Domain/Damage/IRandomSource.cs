namespace Game.Combat.Domain
{
    public interface IRandomSource
    {
        /// <summary>Trả về số ngẫu nhiên trong khoảng 0 đến 1.</summary>
        float NextFloat();
    }
}
