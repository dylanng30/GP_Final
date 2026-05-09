using UnityEngine;
using UnityEngine.UI;
using Dylanng.Core.UI;
using TMPro; // Giả sử dùng TextMeshPro

public class GameOverPopup : UIPopup
{
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private Button _homeButton;

    public override void Initialize()
    {
        base.Initialize();
        _homeButton.onClick.AddListener(Close);
    }

    // Nhận dữ liệu (Data) khi được UIManager gọi OpenPopup
    public override void Setup(object data = null)
    {
        if (data is int finalScore)
        {
            _scoreText.text = $"Score: {finalScore}";
        }
    }
}