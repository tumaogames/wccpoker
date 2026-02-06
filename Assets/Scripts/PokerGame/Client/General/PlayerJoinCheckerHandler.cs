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

        private void Awake() => PokerNetConnect.OnMessageEvent += OnMessage;

        void OnMessage(MsgType type, IMessage msg)
        {
            switch (type)
            {

                case MsgType.TableSnapshot:
                    {
                        var m = (TableSnapshot)msg;
                        //m.CurrentBet
                        //m.MinRaise
                        //r = CurrentBet + MinRaise
                        var isTurnStarted = m.State != TableState.Reset && m.State != TableState.Waiting;

                        _onJoinTableBoolEvent?.Invoke(isTurnStarted);

                        var fEvent = isTurnStarted ? _onJoinTableEvent : _onNotYetJoinTableEvent;
                        fEvent?.Invoke();

                        break;
                    }

            }
        }
    }
}
