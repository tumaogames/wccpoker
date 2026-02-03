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
        [SerializeField] GameObject _actionsRoot;
        [SerializeField] GameObject _foldButton;
        [SerializeField] GameObject _checkButton;
        [SerializeField] GameObject _callButton;
        [SerializeField] GameObject _betButton;
        [SerializeField] GameObject _raiseButton;
        [SerializeField] GameObject _allInButton;
        [SerializeField] bool _logTurnDebug = false;
        [SerializeField] bool _debugShowAllWhenEmpty = false;

        int _currentBet;

        GameServerClient _client;

        void OnEnable()
        {
            _client = GameServerClient.Instance;
            if (_client != null)
                _client.TurnUpdateReceived += OnTurnUpdate;

            PokerNetConnect.OnMessageEvent += OnMessage;
        }

        void OnDisable()
        {
            if (_client != null)
            {
                _client.TurnUpdateReceived -= OnTurnUpdate;
                _client = null;
            }

            PokerNetConnect.OnMessageEvent -= OnMessage;
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
                        _raise_chipsValueVolume.SetMinMaxChips((int)m.CurrentBet + (int)m.MinRaise);
                        _bet_chipsValueVolume.SetMinMaxChips(1);

                        break;
                    }
                case MsgType.TurnUpdate:
                    OnTurnUpdate((TurnUpdate)msg);
                    break;

            }
        }

        void OnTurnUpdate(TurnUpdate turn)
        {
            var ownerId = PokerNetConnect.OwnerPlayerID;
            if (string.IsNullOrEmpty(ownerId) && _client != null)
                ownerId = _client.PlayerId;
            var isOwnerTurn = turn.PlayerId == ownerId;
            if (_userButtonActions != null)
                _userButtonActions.PlayAnimation(isOwnerTurn ? "PlayActionButtonGoUp" : "PlayActionButtonGoDown");

            if (_actionsRoot != null)
                _actionsRoot.SetActive(isOwnerTurn);

            if (!isOwnerTurn)
            {
                SetAllButtonsActive(false);
                return;
            }

            if (_logTurnDebug)
            {
                var allowed = turn.AllowedActions == null ? "null" : string.Join(",", turn.AllowedActions);
                Debug.Log($"[ActionButtons] turn player={turn.PlayerId} owner={PokerNetConnect.OwnerPlayerID} allowed=[{allowed}]");
            }

            if (turn.AllowedActions == null || turn.AllowedActions.Count == 0)
            {
                if (_debugShowAllWhenEmpty)
                    SetAllButtonsActive(true);
                else
                    SetAllButtonsActive(false);
                return;
            }

            var isAllIn = turn.AllowedActions.Contains(PokerActionType.Raise);

            if (_foldButton != null) _foldButton.SetActive(turn.AllowedActions.Contains(PokerActionType.Fold));
            if (_checkButton != null) _checkButton.SetActive(turn.AllowedActions.Contains(PokerActionType.Check));
            if (_callButton != null) _callButton.SetActive(turn.AllowedActions.Contains(PokerActionType.Call));
            if (_betButton != null) _betButton.SetActive(turn.AllowedActions.Contains(PokerActionType.Bet));
            if (_raiseButton != null) _raiseButton.SetActive(isAllIn);
            if (_allInButton != null) _allInButton.SetActive(turn.AllowedActions.Contains(PokerActionType.AllIn));
        }

        void SetAllButtonsActive(bool state)
        {
            if (_foldButton != null) _foldButton.SetActive(state);
            if (_checkButton != null) _checkButton.SetActive(state);
            if (_callButton != null) _callButton.SetActive(state);
            if (_betButton != null) _betButton.SetActive(state);
            if (_raiseButton != null) _raiseButton.SetActive(state);
            if (_allInButton != null) _allInButton.SetActive(state);
        }

        [Button]
        public void SetCheckAction_Button()
        {
            NetworkDebugLogger.LogSend("Action", "Check");
            GameServerClient.SendCheckStatic();
        }

        [Button]
        public void SetRaiseAction_Button() => CloseActionButtons(() => GameServerClient.SendRaiseStatic(_raise_chipsValueVolume.ChipsValue));

        [Button]
        public void SetCallAction_Button() => CloseActionButtons(() => GameServerClient.SendCallStatic(_currentBet));

        [Button]
        public void SetFoldAction_Button() => CloseActionButtons(() => GameServerClient.SendFoldStatic());

        [Button]
        public void SetAllInAction_Button() => CloseActionButtons(() => GameServerClient.SendAllInStatic(200));

        [Button]
        public void SetBetAction_Button() => CloseActionButtons(() => GameServerClient.SendBetStatic(_bet_chipsValueVolume.ChipsValue));

        void CloseActionButtons(UnityAction callback)
        {
            _userButtonActions.PlayAnimation("PlayActionButtonGoDown");
            callback?.Invoke();
        }
    }
}
