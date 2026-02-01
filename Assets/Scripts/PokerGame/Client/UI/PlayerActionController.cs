////////////////////
//       RECK       //
////////////////////


using Com.poker.Core;
using Google.Protobuf;
using NaughtyAttributes;
using UnityEngine;


namespace WCC.Poker.Client
{
    public class PlayerActionController : MonoBehaviour
    {
        [SerializeField] BaseAnimation _userButtonActions;
        [SerializeField] ChipsUIVolume _chipsValueVolume;

        [Header("[ACTION-BUTTONS]")]
        [SerializeField] GameObject _foldButton;
        [SerializeField] GameObject _checkButton;
        [SerializeField] GameObject _callButton;
        [SerializeField] GameObject _betButton;
        [SerializeField] GameObject _raiseButton;
        [SerializeField] GameObject _allInButton;

        GameServerClient client;

        void OnEnable()
        {
            client = GameServerClient.Instance;
            if (client == null)
                return;

            client.TurnUpdateReceived += OnTurnUpdate;

            PokerNetConnect.OnMessageEvent += OnMessage;
        }

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

                        _chipsValueVolume.SetMinMaxChips((int)m.CurrentBet + (int)m.MinRaise);

                        break;
                    }

            }
        }

        void OnTurnUpdate(TurnUpdate turn)
        {
            _userButtonActions.PlayAnimation(turn.PlayerId == PokerNetConnect.OwnerPlayerID ? "PlayActionButtonGoUp" : "PlayActionButtonGoDown");

            if (turn.PlayerId != PokerNetConnect.OwnerPlayerID) return;

            var isAllIn = turn.AllowedActions.Contains(PokerActionType.Raise);

            _foldButton.SetActive(turn.AllowedActions.Contains(PokerActionType.Fold));
            _checkButton.SetActive(turn.AllowedActions.Contains(PokerActionType.Check));
            _callButton.SetActive(turn.AllowedActions.Contains(PokerActionType.Call));
            _betButton.SetActive(turn.AllowedActions.Contains(PokerActionType.Bet));
            _raiseButton.SetActive(isAllIn);
            _allInButton.SetActive(turn.AllowedActions.Contains(PokerActionType.AllIn));
        }

        [Button]
        public void SetCheckAction_Button() => GameServerClient.SendCheckStatic();

        [Button]
        public void SetRaiseAction_Button()
        {
            GameServerClient.SendRaiseStatic(_chipsValueVolume.ChipsValue);
            print($"<color=green>SendRaiseStatic: {_chipsValueVolume.ChipsValue}</color>");
        }

        [Button]
        public void SetCallAction_Button() => GameServerClient.SendCallStatic(50);

        [Button]
        public void SetFoldAction_Button() => GameServerClient.SendFoldStatic();

        [Button]
        public void SetAllInAction_Button() => GameServerClient.SendAllInStatic(200);

        [Button]
        public void SetBetAction_Button() => GameServerClient.SendBetStatic(60);
    }
}
