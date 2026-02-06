 ////////////////////
//       RECK       //
 ////////////////////


using TMPro;
using UnityEngine;


namespace WCC.Poker.Client
{
    [RequireComponent(typeof(TMP_InputField))]
    [RequireComponent(typeof(RectTransform))]
    public class ChatInputfield : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] TMP_InputField _inputField;
        [SerializeField] RectTransform _targetRect;

        [Header("Sizing")]
        [SerializeField] float _minHeight = 72f;
        [SerializeField] float _maxHeight = 183f;
        [SerializeField] float _verticalPadding = 20f;
        [SerializeField] bool _keepBottomAligned = true;
        TMP_Text _textComponent;
        float _lastHeight = -1f;

        void Awake()
        {
            if (_inputField == null)
                TryGetComponent(out _inputField);
            if (_targetRect == null)
                TryGetComponent(out _targetRect);

            _textComponent = _inputField != null ? _inputField.textComponent : null;
            if (_targetRect != null)
                _lastHeight = _targetRect.sizeDelta.y;
        }

        void OnEnable()
        {
            if (_inputField != null)
                _inputField.onValueChanged.AddListener(OnValueChanged);
            RefreshSize();
        }

        void OnDisable()
        {
            if (_inputField != null)
                _inputField.onValueChanged.RemoveListener(OnValueChanged);
        }

        void OnValidate()
        {
            if (_minHeight < 0f) _minHeight = 0f;
            if (_maxHeight < _minHeight) _maxHeight = _minHeight;
            if (_verticalPadding < 0f) _verticalPadding = 0f;
        }

        void OnValueChanged(string _)
        {
            RefreshSize();
        }

        void RefreshSize()
        {
            if (_targetRect == null || _textComponent == null)
                return;

            float targetHeight = Mathf.Clamp(_textComponent.preferredHeight + _verticalPadding, _minHeight, _maxHeight);

            var size = _targetRect.sizeDelta;
            float prevHeight = _lastHeight < 0f ? size.y : _lastHeight;
            size.y = targetHeight;
            _targetRect.sizeDelta = size;

            if (_keepBottomAligned)
            {
                float delta = targetHeight - prevHeight;
                var anchored = _targetRect.anchoredPosition;
                anchored.y += delta * _targetRect.pivot.y;
                _targetRect.anchoredPosition = anchored;
            }

            _lastHeight = targetHeight;
        }

    }
}
