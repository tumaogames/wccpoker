////////////////////
//       RECK       //
////////////////////


using Com.poker.Core;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WCC.Core.Exposed;

namespace WCC.Poker.Client
{
    public class PlayerHUDController : Exposing<PlayerHUDController>
    {
       
        [SerializeField] PlayerHUD_UI _playerHUDPrefab;
        [SerializeField] Transform[] _playersTablePositions;
        [SerializeField] Transform _playersContainer;

        [Space]

        [SerializeField] int _maxPlayers = 3;

        [Space]

        [SerializeField] Sprite[] _sampleAvatars;

        [Space]

        [SerializeField] BaseAnimation _userButtonActions;

        readonly List<PlayerHUD_UI> _playersHUDList = new();

        PlayerHUD_UI _currentPlayerHUD;

        public event Action<Dictionary<string, PlayerHUD_UI>> PlayerHUDListListenerEvent;

        int _playerIndex = 4;

        readonly Dictionary<string, PlayerHUD_UI> _inGamePlayersRecords = new();

        TableState _currentTableState;

        public enum TagType { Dealer, SmallBlind, BigBlind }

        #region EDITOR
        private void OnValidate()
        {
            if(_maxPlayers > _playersTablePositions.Length) _maxPlayers = _playersTablePositions.Length;
        }
        #endregion EDITOR

        protected override void Awake()
        {
            base.Awake();
            PokerNetConnect.OnMessageEvent += OnMessage;
        }

        private async void Start()
        {
            DeckCardsController.OnWinnerEvent += async playerID =>
            {
                var p = _inGamePlayersRecords[playerID];

                p.SetEnableWinner(true);

                await Task.Delay(1000);

                p.SetEnableWinner(false);
            };
        }

        void OnMessage(MsgType type, IMessage msg)
        {
            if (type == MsgType.ActionBroadcast)
            {
                // Broadcast of OTHER players' actions.
                var m = (ActionBroadcast)msg;
                // Data you can use:
                // - m.PlayerId
                // - m.Action, m.Amount
                // - m.CurrentBet, m.PotTotal
                Debug.Log($"<color=green>[ActionBroadcast] player={m.PlayerId} action={m.Action} amount={m.Amount} pot={m.PotTotal} | {_inGamePlayersRecords.Count}</color>");

                //if (string.IsNullOrEmpty(_currentPlayerIdTurn))
                //{
                //    _inGamePlayersRecords[_currentPlayerIdTurn].SeCancelTurnTime();
                //}

                if (_inGamePlayersRecords.ContainsKey(m.PlayerId))
                {
                    _inGamePlayersRecords[m.PlayerId].SetCancelTurnTime();
                    _inGamePlayersRecords[m.PlayerId].SetEnableActionHolder(true);
                    _inGamePlayersRecords[m.PlayerId].SetActionBroadcast($"{m.Action}");
                }


            }
            else if (type == MsgType.TableSnapshot)
            {
                //Debug.Log($"<color=green> TableSnapshot </color>");
                // Full table state (seats, stacks, pot, board).
                var m = (TableSnapshot)msg;
                // Data you can use:
                // - m.State (waiting/preflop/flop/turn/river/showdown/reset)
                // - m.CommunityCards (board cards if already revealed)
                // - m.PotTotal, m.CurrentBet, m.MinRaise
                // - m.CurrentTurnSeat


                // Who is dealer / blinds (seat -> playerId)
                string dealerId = "?";
                string sbId = "?";
                string bbId = "?";

                foreach (var p in m.Players)
                {
                    if (p.Seat == m.DealerSeat) dealerId = p.PlayerId;
                    if (p.Seat == m.SmallBlindSeat) sbId = p.PlayerId;
                    if (p.Seat == m.BigBlindSeat) bbId = p.PlayerId;
                }

                if (_inGamePlayersRecords.Count != 0)
                {
                    if(_inGamePlayersRecords.ContainsKey(dealerId)) _inGamePlayersRecords[dealerId].SetTag(TagType.Dealer, true);
                    if (_inGamePlayersRecords.ContainsKey(sbId)) _inGamePlayersRecords[sbId].SetTag(TagType.SmallBlind, true);
                    if (_inGamePlayersRecords.ContainsKey(bbId)) _inGamePlayersRecords[bbId].SetTag(TagType.BigBlind, true);
                }


                if (_currentTableState != m.State)
                {
                    foreach (var p in _inGamePlayersRecords)
                    {
                        p.Value.SetEnableActionHolder(false);
                    }
                    _currentTableState = m.State;
                }

                if (_inGamePlayersRecords.Count == 0)
                {
                    foreach (var p in m.Players)
                    {
                        if (_inGamePlayersRecords.ContainsKey(p.PlayerId)) continue;

                        SummonPlayerHUDUI(p.Seat - 1, $"P-{p.PlayerId}-{p.Seat}", p.PlayerId, (int)p.Stack);
                    }

                    PlayerHUDListListenerEvent?.Invoke(_inGamePlayersRecords);
                }

                if (m.State == TableState.Reset)
                {
                    foreach (var p in _inGamePlayersRecords)
                    {
                        p.Value.SetEnableActionHolder(false);
                    }
                    //foreach (var p in _inGamePlayersRecords)
                    //{
                    //    Destroy(p.Value.gameObject);
                    //}

                    //_inGamePlayersRecords.Clear();

                }
                //Debug.Log($"[Snapshot] table={m.TableId} state={m.State} pot={m.PotTotal} currentBet={m.CurrentBet}");
                //foreach (var p in m.Players)
                //{
                //    Debug.Log($"<color=blue>  seat={p.Seat} player={p.PlayerId} stack={p.Stack} bet={p.BetThisRound} status={p.Status} </color>");
                //}
                // Community cards (if any are already revealed)
                // You can read them like:
                // - m.CommunityCards[0] -> first board card
                // - m.CommunityCards[1] -> second board card
                // If no board yet (pre-flop), count = 0.
                //if (m.CommunityCards != null && m.CommunityCards.Count > 0)
                //    Debug.Log($"  board={PokerNetConnect.FormatCards(m.CommunityCards)}");
            }
            else if (type == MsgType.TurnUpdate)
            {
                // Whose turn + allowed actions.
                var m = (TurnUpdate)msg;
                // Data you can use:
                // - m.PlayerId, m.Seat (current turn)
                // - Seat number starts at 1 (not 0)
                // - m.AllowedActions (Fold/Check/Call/Bet/Raise/AllIn)
                // - m.CallAmount, m.MinRaise, m.MaxRaise
                // - m.Stack (current player stack)
                // - m.DeadlineUnixMs (absolute server time when turn expires)
                //   Use this to build a countdown/progress bar:
                //   remainingMs = max(0, DeadlineUnixMs - nowUtcMs)
                var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var remainingMs = m.DeadlineUnixMs > 0 ? Math.Max(0, (long)m.DeadlineUnixMs - nowMs) : 0;
                Debug.Log($"[Turn] player={m.PlayerId} seat={m.Seat} allowed={m.AllowedActions.Count} remainingMs={remainingMs}");

                _inGamePlayersRecords[m.PlayerId].UpdateChipsAmount((int)m.Stack);

                _inGamePlayersRecords[m.PlayerId].SetTurn();
            }
            else if (type == MsgType.StackUpdate)
            {
                // Single player's stack update.
                var m = (StackUpdate)msg;
                // Data you can use:
                // - m.PlayerId
                // - m.Stack (stack = player's current chips in this match)
                Debug.Log($"[StackUpdate] player={m.PlayerId} stack={m.Stack}");

                _inGamePlayersRecords[m.PlayerId].UpdateChipsAmount((int)m.Stack);

            }
        }

        /// <summary>
        /// This function ay para mag instantiate ng player UIs
        /// </summary>
        /// <param name="i"></param>
        void SummonPlayerHUDUI(int seat, string playerName, string playerID, int stackAmount)
        {
            var p = Instantiate(_playerHUDPrefab, _playersContainer);
            _playersHUDList.Add(p);
            _inGamePlayersRecords.Add(playerID, p);

            p.transform.localPosition = seat == _playerIndex ? _playersTablePositions[_playerIndex].localPosition : _playersTablePositions[seat].localPosition;

            p.InititalizePlayerHUDUI(playerID, playerName, seat == _playerIndex, 1, _sampleAvatars[UnityEngine.Random.Range(1, _sampleAvatars.Length)], stackAmount);
            
            if(seat == _playerIndex && _currentPlayerHUD == null) _currentPlayerHUD = p;
        }

        async void TestRoundTurn()
        {
            foreach (var p in _playersHUDList)
            {
                var tcs = new TaskCompletionSource<bool>();

                if (p == _currentPlayerHUD) _userButtonActions.PlayAnimation("PlayActionButtonGoUp");

                p.SetTurn(() =>
                {
                    tcs.SetResult(true);
                    if (p == _currentPlayerHUD) _userButtonActions.PlayAnimation("PlayActionButtonGoDown");
                });

                await tcs.Task; 
            }
        }
    }
}
