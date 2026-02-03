////////////////////
//       RECK       //
////////////////////


using Com.poker.Core;
using Google.Protobuf;
using UnityEngine;
using UnityEngine.Events;


namespace WCC.Poker.Client
{
    public class PlayerJoinCheckerHandler : MonoBehaviour
    {
        [SerializeField] UnityEvent<bool> _onJoinTableBoolEvent;
        [SerializeField] UnityEvent _onJoinTableEvent;
        [SerializeField] UnityEvent _onNotYetJoinTableEvent;

        private void OnEnable() => GameServerClient.JoinTableResponseReceivedStatic += OnJoinTableResponse;

        private void OnDisable() => GameServerClient.JoinTableResponseReceivedStatic -= OnJoinTableResponse;

        void OnJoinTableResponse(JoinTableResponse r)
        {
            var isOnTable = r.Success;

            _onJoinTableBoolEvent?.Invoke(isOnTable);

            var fEvent = isOnTable ? _onJoinTableEvent : _onNotYetJoinTableEvent;
            fEvent?.Invoke();

        }

    }
}
