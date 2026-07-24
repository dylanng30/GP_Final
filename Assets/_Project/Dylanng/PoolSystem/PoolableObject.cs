namespace Dylanng
{
    public abstract class PoolableObject : MonoBehaviourBase, IPoolable
    {
        public virtual void OnSpawn() => gameObject.SetActive(true);
        public virtual void OnDespawn() => gameObject.SetActive(false);
    }
}
