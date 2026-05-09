using UnityEngine;

namespace Dylanng.Gameplay.RPG.Combat.Weapons
{
    public abstract class BowState
    {
        protected BowWeapon bow;
        public BowState(BowWeapon bow) => this.bow = bow;

        public virtual void Enter() { }
        public virtual void Update(float deltaTime) { }
        public virtual void Exit() { }
        public virtual void HandleInputRelease() { }
    }

    public class DrawState : BowState
    {
        private float _drawTimer;

        public DrawState(BowWeapon bow) : base(bow) { }

        public override void Enter()
        {
            _drawTimer = 0;
            Debug.Log("[Bow] Stage: DRAWING");
            bow.SetAnimatorTrigger("Draw");
        }

        public override void Update(float deltaTime)
        {
            _drawTimer += deltaTime;
            float power = Mathf.Clamp01(_drawTimer / bow.DrawTime);
            bow.SetCurrentPower(power);

            if (_drawTimer >= bow.DrawTime)
            {
                bow.TransitionTo(bow.HoldState);
            }
        }

        public override void HandleInputRelease()
        {
            bow.TransitionTo(bow.ShootState);
        }
    }

    public class HoldState : BowState
    {
        public HoldState(BowWeapon bow) : base(bow) { }

        public override void Enter()
        {
            Debug.Log("[Bow] Stage: HOLDING (Full Power)");
            bow.SetCurrentPower(1.0f);
            bow.SetAnimatorTrigger("Hold");
        }

        public override void HandleInputRelease()
        {
            bow.TransitionTo(bow.ShootState);
        }
    }

    public class ShootState : BowState
    {
        public ShootState(BowWeapon bow) : base(bow) { }

        public override void Enter()
        {
            Debug.Log($"[Bow] Stage: SHOOTING (Power: {bow.CurrentPower})");
            bow.FireProjectile();
            bow.SetAnimatorTrigger("Shoot");
            
            // Auto transition back to idle/draw readiness
            bow.TransitionTo(null); 
        }
    }
}
