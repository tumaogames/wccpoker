 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;


namespace WCC.Poker.Client
{
    public class EmojiController : MonoBehaviour
    {
        [SerializeField] PlayerHUDController _HUDController;
        [SerializeField] EmojiData _emojiData;
        [SerializeField] EmojiItemUI _emojiPrefab;
        [SerializeField] Transform _emojiGroupContainer;

        private void Start() => CreateAvailableEmojis();

        void OnEnable() => GameServerClient.EmojiBroadcastReceivedStatic += OnEmoji;

        void OnDisable() => GameServerClient.EmojiBroadcastReceivedStatic -= OnEmoji;

        void OnEmoji(Com.poker.Core.EmojiBroadcast msg)
        {
            //Debug.Log($"Emoji {msg.EmojiType} from {msg.FromPlayerId} to {msg.ToPlayerId}");

            _HUDController.SendEmoji(msg.ToPlayerId, _emojiData.Emojis[msg.EmojiType]);
        }


        void CreateAvailableEmojis()
        {
            for (int i = 0; i < _emojiData.Emojis.Length; i++)
            {
                var emojiSprite = _emojiData.Emojis[i];
                var emj = Instantiate(_emojiPrefab, _emojiGroupContainer);
                emj.InitializeEmoji(i, emojiSprite, OnSelectEmoji);
            }
        }

        void OnSelectEmoji(int i) => GameServerClient.SendEmojiStatic(i, PokerNetConnect.OwnerPlayerID);
    }
}
