 ////////////////////
//       RECK       //
 ////////////////////


using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class ChatViewUpdater : MonoBehaviour
    {
        [SerializeField] ScrollRect _scrollRect;
        [SerializeField] RectTransform _content;

        void Awake()
        {
            if (_scrollRect == null)
                _scrollRect = GetComponentInParent<ScrollRect>();
            if (_content == null && _scrollRect != null)
                _content = _scrollRect.content;
        }

        [Button]
        public void ForceScrollToBottom()
        {
            if (_scrollRect == null)
                return;

            if (_content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_content);

            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

    }
}
