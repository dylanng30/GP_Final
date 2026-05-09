using UnityEngine;
using UnityEngine.UI;
using Dylanng.Managers;

namespace Dylanng.Core.UI.Screens
{
    public class MainMenuScreen : UIScreen
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;

        public override void Initialize()
        {
            base.Initialize();

            // Đăng ký sự kiện nút bấm
            _playButton.onClick.AddListener(OnPlayClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        private void OnPlayClicked()
        {
            // Yêu cầu SceneManager load màn chơi
            ServiceLocator.Get<SceneLoadManager>().LoadSceneAsync("GameplayScene");

            // Hoặc mở thẳng màn GameplayScreen nếu không cần đổi Scene
            // ServiceLocator.Get<UIManager>().OpenScreen<GameplayScreen>();
        }

        private void OnSettingsClicked()
        {
            // Mở Popup Settings đè lên MainMenu
            //ServiceLocator.Get<UIManager>().OpenPopup<SettingsPopup>();
        }
    }
}