 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class EmojiItemUI : MonoBehaviour
    {
        [SerializeField] Image _image;
        UnityAction<int> _onClickCallback;
        int _emojiIndex;

        public void InitializeEmoji(int i, Sprite emojiSprite, UnityAction<int> callback)
        {
            _image.sprite = emojiSprite;
            _emojiIndex = i;
            _onClickCallback = callback;
        }
        //
        public void OnClickSendEmoji() => _onClickCallback?.Invoke(_emojiIndex);
    }
}
