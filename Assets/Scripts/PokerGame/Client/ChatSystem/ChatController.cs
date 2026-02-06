////////////////////
//       RECK       //
////////////////////


using Com.poker.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class ChatController : MonoBehaviour
    {
        [SerializeField] ChatMessageUI _ownerMessagePrefab;
        [SerializeField] ChatMessageUI _messagePrefab;
        [SerializeField] Transform _messageContainer;
        [SerializeField] TMP_InputField _chatIF;
        [SerializeField] Button _chatSendButton;
        [SerializeField] UnityEvent _onMessageRecievedEvent;

        private void Start() => _chatSendButton.onClick.AddListener(SendChant);

        private void OnEnable() => GameServerClient.ChatBroadcastReceivedStatic += OnChat;
        private void OnDisable() => GameServerClient.ChatBroadcastReceivedStatic -= OnChat;

        void OnChat(ChatBroadcast msg)
        {
            // msg.TableId
            // msg.FromPlayerId
            // msg.FromDisplayName
            // msg.Seat
            // msg.Message
            // msg.TimestampUnixMs
            // msg.FromSpectator
            //Debug.Log($"[CHAT] {msg.FromDisplayName}({msg.FromPlayerId}) seat={msg.Seat} => {msg.Message}");

            CreateMessageInstance(msg);
            _onMessageRecievedEvent?.Invoke();
        }

        void SendChant()
        {
            if(string.IsNullOrEmpty(_chatIF.text)) return;
            GameServerClient.SendChatStatic(_chatIF.text);
            _chatIF.text = string.Empty;
        }

        void CreateMessageInstance(ChatBroadcast msg)
        {
            var msgIns = Instantiate(msg.FromPlayerId == PokerNetConnect.OwnerPlayerID ? _ownerMessagePrefab : _messagePrefab, _messageContainer);
            msgIns.SetMessage(msg.FromPlayerId, msg.FromDisplayName, msg.Message);
        }
    }
}
