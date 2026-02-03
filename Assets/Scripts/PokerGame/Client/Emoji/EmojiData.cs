 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;


namespace WCC.Poker.Client
{
    [CreateAssetMenu(fileName = "Emoji", menuName = "WCC/Emoji/Data")]
    public class EmojiData : ScriptableObject
    {
        public Sprite[] Emojis;
    }
}
