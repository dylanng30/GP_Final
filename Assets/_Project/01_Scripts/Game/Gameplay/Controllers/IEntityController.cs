using UnityEngine;

namespace Game.Gameplay.Controllers
{
    public interface IEntityController
    {
        void Move(Vector2 direction);
        void StopMove();
        void Attack();
        void UseSkill(int skillIndex);
        void Jump();
        void Dash();
    }

    public static class EntityControllerExtensions
    {
        public static void MoveInWorldSpace(this IEntityController controller, Vector3 worldDirection)
        {
            if (controller == null) return;
            if (worldDirection == Vector3.zero)
            {
                controller.StopMove();
                return;
            }

            if (Camera.main != null)
            {
                Transform camTransform = Camera.main.transform;
                Vector3 cameraForward = camTransform.forward;
                Vector3 cameraRight = camTransform.right;

                cameraForward.y = 0f;
                cameraRight.y = 0f;
                if (cameraForward.sqrMagnitude > 0.001f) cameraForward.Normalize();
                if (cameraRight.sqrMagnitude > 0.001f) cameraRight.Normalize();

                float moveX = Vector3.Dot(worldDirection, cameraRight);
                float moveY = Vector3.Dot(worldDirection, cameraForward);
                controller.Move(new Vector2(moveX, moveY));
            }
            else
            {
                controller.Move(new Vector2(worldDirection.x, worldDirection.z));
            }
        }
    }
}
