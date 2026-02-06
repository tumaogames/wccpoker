////////////////////
//       RECK       //
////////////////////


using Com.poker.Core;
using Google.Protobuf;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;


namespace WCC.Poker.Client
{
    public class PlayerActionController : MonoBehaviour
    {
        [SerializeField] BaseAnimation _userButtonActions;
        [SerializeField] ChipsUIVolume _raise_chipsValueVolume;
        [SerializeField] ChipsUIVolume _bet_chipsValueVolume;

        [Header("[ACTION-BUTTONS]")]
        [SerializeField] GameObject _foldButton;
        [SerializeField] GameObject _checkButton;
        [SerializeField] GameObject _callButton;
        [SerializeField] GameObject _betButton;
        [SerializeField] GameObject _raiseButton;
        [SerializeField] GameObject _allInButton;

        GameServerClient client;
        int _currentBet;
        int _currentStack;
        int _minRaise;
        int _maxRaise;

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

                        _currentBet = (int)m.CurrentBet;
                        var snapshotMinRaise = (int)m.CurrentBet + (int)m.MinRaise;
                        ApplyChipsLimits(snapshotMinRaise, _maxRaise);

                        break;
                    }
                case MsgType.TurnUpdate:
                    {
                        var m = (TurnUpdate)msg;
                        _currentStack = (int)m.Stack;
                        _minRaise = (int)m.MinRaise;
                        _maxRaise = (int)m.MaxRaise;
                        ApplyChipsLimits(_minRaise, _maxRaise);
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
        public void SetCheckAction_Button() => CloseActionButtons(() => GameServerClient.SendCheckStatic());

        [Button]
        public void SetRaiseAction_Button() => CloseActionButtons(() => GameServerClient.SendRaiseStatic(_raise_chipsValueVolume.ChipsValue));

        [Button]
        public void SetCallAction_Button() => CloseActionButtons(()=> GameServerClient.SendCallStatic(_currentBet));

        [Button]
        public void SetFoldAction_Button() => CloseActionButtons(()=> GameServerClient.SendFoldStatic());

        [Button]
        public void SetAllInAction_Button() => CloseActionButtons(()=> GameServerClient.SendAllInStatic(_currentStack));

        [Button]
        public void SetBetAction_Button() => CloseActionButtons(() => GameServerClient.SendBetStatic(_bet_chipsValueVolume.ChipsValue));

        void CloseActionButtons(UnityAction callback)
        {
            _userButtonActions.PlayAnimation("PlayActionButtonGoDown");
            callback();
        }

        void ApplyChipsLimits(int minRaise, int maxRaise)
        {
            var stack = Mathf.Max(0, _currentStack);

            var min = Mathf.Clamp(minRaise, 0, stack);
            var max = maxRaise > 0 ? Mathf.Clamp(maxRaise, min, stack) : stack;

            _raise_chipsValueVolume.SetMinMaxChips(min, max);

            var betMin = Mathf.Clamp(_currentBet, 0, stack);
            _bet_chipsValueVolume.SetMinMaxChips(betMin, stack);
        }
    }
}
