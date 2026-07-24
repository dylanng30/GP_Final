namespace Dylanng
{
    public abstract class ServiceBase : IService
    {
        public abstract void Initialize();
        public virtual void Dispose() { }
    }
}
