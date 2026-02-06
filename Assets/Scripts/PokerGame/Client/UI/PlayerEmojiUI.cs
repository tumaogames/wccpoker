////////////////////
//       RECK       //
////////////////////


using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class PlayerEmojiUI : MonoBehaviour
    {
        [SerializeField] Image _emojiImage;

        public async void SetPlayerEmoji(Sprite emojiIcon)
        {
            _emojiImage.gameObject.SetActive(true);
            _emojiImage.sprite = emojiIcon;

            await Task.Delay(3000);

            _emojiImage.gameObject.SetActive(false);
        }
    }
}
