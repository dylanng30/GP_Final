using System;
using Dylanng;
using UnityEngine;

namespace Game.VFXs
{
    public abstract class BaseVFXView : PoolableObject
    {
        protected Action<BaseVFXView> _onCompleteCallback;
        
        public virtual void Setup(IVFXPayload payload)
        {
            
        }

        public virtual void Play(Vector3 position, Action<BaseVFXView> onComplete)
        {
            transform.position = position;
            _onCompleteCallback = onComplete;
            gameObject.SetActive(true);
        }
        
        public virtual void Stop()
        {
            ReturnToPool();
        }

        protected void ReturnToPool()
        {
            gameObject.SetActive(false);
            _onCompleteCallback?.Invoke(this);
        }
    }
}