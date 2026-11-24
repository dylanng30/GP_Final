using Dylanng;
using Game.Gameplay.Controllers;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>Ý định của người chơi trong một frame, không phụ thuộc thiết bị nhập.</summary>
    public struct PlayerIntent
    {
        public const int NoHeroSelected = -1;

        public Vector2 Move;
        public bool Attack;
        public bool Skill1;
        public bool Skill2;
        public bool Ultimate;
        public bool Dash;
        public bool Jump;
        public bool LockOn;
        public bool ToggleView;
        public bool Regroup;
        public int SwitchHeroIndex;
    }

    public interface IPlayerIntentSource
    {
        PlayerIntent Read();
    }

    /// <summary>Đọc input từ InputManager và bàn phím, chuột. Nút trên màn hình cùng đi qua InputManager.</summary>
    public sealed class DeviceIntentSource : IPlayerIntentSource
    {
        private static readonly KeyCode[] HeroKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5 };

        private readonly InputManager _input;

        public DeviceIntentSource(InputManager input)
        {
            _input = input;
        }

        public PlayerIntent Read()
        {
            var intent = new PlayerIntent
            {
                Move = _input.MovementInput,
                Attack = _input.AttackInput,
                Jump = _input.JumpInput,
                SwitchHeroIndex = PlayerIntent.NoHeroSelected
            };

            if (_input.UsePCInput) ReadKeyboardAndMouse(ref intent);
            return intent;
        }

        private static void ReadKeyboardAndMouse(ref PlayerIntent intent)
        {
            intent.Attack |= CanClickToAttack() && Input.GetMouseButton(0);
            intent.LockOn = Input.GetMouseButtonDown(2);
            intent.Skill1 = Input.GetKeyDown(KeyCode.E);
            intent.Skill2 = Input.GetKeyDown(KeyCode.R);
            intent.Ultimate = Input.GetKeyDown(KeyCode.Q);
            intent.Dash = Input.GetKeyDown(KeyCode.LeftShift);
            intent.ToggleView = Input.GetKeyDown(KeyCode.V);
            intent.Regroup = Input.GetKeyDown(KeyCode.G);

            for (int i = 0; i < HeroKeys.Length; i++)
            {
                if (Input.GetKeyDown(HeroKeys[i])) intent.SwitchHeroIndex = i;
            }
        }

        /// <summary>Góc nhìn thứ 3 phải khóa chuột trước, top-down thì bấm vào đâu ngoài UI cũng được.</summary>
        private static bool CanClickToAttack()
        {
            if (Cursor.lockState == CursorLockMode.Locked) return true;

            CameraController camera = CameraController.Instance;
            return camera != null && camera.Mode == CameraMode.TopDown && !CursorCaptureController.IsPointerOverUi;
        }
    }
}
