////////////////////
//       RECK       //
////////////////////


using TMPro;
using UnityEngine;


namespace WCC.Poker.Client
{
    public class ChatMessageUI : MonoBehaviour
    {
        [SerializeField] TMP_Text _playerNameText;
        [SerializeField] TMP_Text _messageText;

        string _playerID;

        public void SetMessage(string playerID, string playerName, string message)
        {
            _playerID = playerID;
            _playerNameText.text = playerName;
            _messageText.text = message;
        }

    }
}
