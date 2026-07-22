using Game.Gameplay.AI.Core;
using UnityEngine;

namespace Game.Gameplay.AI.GOAP
{
    /// <summary>
    /// Base class cho các hành động GOAP, giảm thiểu boilerplate code cho các lớp con kế thừa.
    /// </summary>
    public abstract class GOAPActionBase : ScriptableObject, IGOAPAction
    {
        [Header("--- ACTION METADATA ---")]
        [SerializeField] private string actionName = "Base Action";
        [SerializeField] private float cost = 1.0f;

        [Header("--- PRECONDITIONS ---")]
        [SerializeField] private AIWorldStateKey[] preconditionKeys;
        [SerializeField] private bool[] preconditionValues;

        [Header("--- EFFECTS ---")]
        [SerializeField] private AIWorldStateKey[] effectKeys;
        [SerializeField] private bool[] effectValues;

        protected AIWorldState _preconditionsMask;
        protected AIWorldState _preconditionsValues;
        protected AIWorldState _effectsMask;
        protected AIWorldState _effectsValues;
        protected bool _isInitialized;

        public string ActionName => actionName;
        public float Cost => cost;

        public AIWorldState PreconditionsMask
        {
            get
            {
                EnsureInitialized();
                return _preconditionsMask;
            }
        }

        public AIWorldState PreconditionsValues
        {
            get
            {
                EnsureInitialized();
                return _preconditionsValues;
            }
        }

        public AIWorldState EffectsMask
        {
            get
            {
                EnsureInitialized();
                return _effectsMask;
            }
        }

        public AIWorldState EffectsValues
        {
            get
            {
                EnsureInitialized();
                return _effectsValues;
            }
        }

        protected virtual void EnsureInitialized()
        {
            if (_isInitialized) return;

            _preconditionsMask = new AIWorldState(0);
            _preconditionsValues = new AIWorldState(0);
            if (preconditionKeys != null && preconditionValues != null)
            {
                int count = Mathf.Min(preconditionKeys.Length, preconditionValues.Length);
                for (int i = 0; i < count; i++)
                {
                    _preconditionsMask.SetState(preconditionKeys[i], true);
                    _preconditionsValues.SetState(preconditionKeys[i], preconditionValues[i]);
                }
            }

            _effectsMask = new AIWorldState(0);
            _effectsValues = new AIWorldState(0);
            if (effectKeys != null && effectValues != null)
            {
                int count = Mathf.Min(effectKeys.Length, effectValues.Length);
                for (int i = 0; i < count; i++)
                {
                    _effectsMask.SetState(effectKeys[i], true);
                    _effectsValues.SetState(effectKeys[i], effectValues[i]);
                }
            }

            _isInitialized = true;
        }

        public virtual bool CheckProceduralPrecondition(IAIContext context)
        {
            return true;
        }

        public virtual float CalculateDynamicCost(IAIContext context)
        {
            return cost;
        }

        public virtual void Enter(IAIContext context) { }
        
        public abstract ActionStatus Update(IAIContext context, float deltaTime);
        
        public virtual void Exit(IAIContext context) { }
    }
}
