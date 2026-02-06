////////////////////
//       RECK       //
////////////////////


using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class ChatMessageUI : MonoBehaviour
    {
        [SerializeField] TMP_Text _playerNameText;
        [SerializeField] TMP_Text _messageText;
        [SerializeField] RectTransform _targetRect;
        [SerializeField] float _minHeight = 72f;
        [SerializeField] float _maxHeight = 220f;
        [SerializeField] float _verticalPadding = 20f;
        [SerializeField] bool _keepBottomAligned = true;

        string _playerID;
        float _lastHeight = -1f;

        void Awake()
        {
            if (_targetRect == null)
                _targetRect = GetComponent<RectTransform>();
            if (_targetRect != null)
                _lastHeight = _targetRect.sizeDelta.y;
        }

        public void SetMessage(string playerID, string playerName, string message)
        {
            _playerID = playerID;
            _playerNameText.text = playerName;
            _messageText.text = message;
            RefreshSize();
        }

        void RefreshSize()
        {
            if (_targetRect == null || _messageText == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(_messageText.rectTransform);
            float targetHeight = Mathf.Clamp(_messageText.preferredHeight + _verticalPadding, _minHeight, _maxHeight);

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
