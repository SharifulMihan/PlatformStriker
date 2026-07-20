using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MinimalMenu
{
    [RequireComponent(typeof(RectTransform))]
    public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Image _image;
        private Color _idleColor;
        private Color _hoverColor;
        private RectTransform _rect;
        private Vector3 _baseScale;

        private const float ScaleAmount = 1.1f; 
        private const float TweenSpeed = 15f; 

        private Color _targetColor;
        private Vector3 _targetScale;

        public void Setup(Image image, Color idleColor, Color hoverColor, Color accentColor)
        {
            _image = image;
            _idleColor = idleColor;
            _hoverColor = hoverColor;
            _rect = GetComponent<RectTransform>();
            _baseScale = _rect.localScale;
            _targetColor = idleColor;
            _targetScale = _baseScale;
        }

        private void Update()
        {
            if (_image == null) return;
            _image.color = Color.Lerp(_image.color, _targetColor, Time.unscaledDeltaTime * TweenSpeed);
            _rect.localScale = Vector3.Lerp(_rect.localScale, _targetScale, Time.unscaledDeltaTime * TweenSpeed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _targetColor = _hoverColor;
            _targetScale = _baseScale * ScaleAmount;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _targetColor = _idleColor;
            _targetScale = _baseScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _targetScale = _baseScale * 0.9f; 
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _targetScale = _baseScale * ScaleAmount;
        }
    }
}