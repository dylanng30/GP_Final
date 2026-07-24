using System;
using Dylanng;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GAME.Utilities
{
    [RequireComponent(typeof(Image))]
    public class CustomToggle : UIBase, IPointerClickHandler
    {
        [SerializeField] private Image main;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private Sprite onSprite;

        private bool _isOn;
        private Action<bool> _switchAction;

        private void OnValidate()
        {
            if (main == null)
            {
                main = GetComponent<Image>();
            }
        }

        public CustomToggle SetIsOn(bool isOn)
        {
            _isOn = isOn;
            main.sprite = _isOn ? onSprite : offSprite;
            return this;
        }

        public CustomToggle SetAction(Action<bool> switchAcion)
        {
            _switchAction = switchAcion;
            return this;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SetIsOn(!_isOn);
            _switchAction?.Invoke(_isOn);
        }
    }
}