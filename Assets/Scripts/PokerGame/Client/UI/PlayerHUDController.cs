////////////////////
//       RECK       //
////////////////////


using Com.poker.Core;
using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WCC.Core.Audio;
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

        readonly List<PlayerHUD_UI> _playersHUDList = new();

        PlayerHUD_UI _currentPlayerHUD;

        public event Action<ConcurrentDictionary<string, PlayerHUD_UI>> PlayerHUDListListenerEvent;

        int _playerIndex = 4;

        readonly ConcurrentDictionary<string, PlayerHUD_UI> _inGamePlayersRecords = new();
        readonly ConcurrentDictionary<string, int> _displayStacks = new();
        readonly ConcurrentDictionary<string, int> _betThisRound = new();
        readonly ConcurrentDictionary<string, int> _pendingWinAmounts = new();

        TableState _currentTableState;

        public enum TagType { Dealer, SmallBlind, BigBlind }

        string _last_dealerId = "?";
        string _last_sbId = "?";
        string _last_bbId = "?";

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
                Debug.Log($"<color=green>WINNER-ID: {playerID} </color>");
                if (!_inGamePlayersRecords.ContainsKey(playerID)) return;
                var p = _inGamePlayersRecords[playerID];

                p.SetEnableWinner(true);
                AudioManager.main.PlayRandomAudio("Winner", Vector2.zero);

                await Task.Delay(3000);

                p.SetEnableWinner(false);
            };

            TableBetController.OnPotChipsMovedToWinner += OnPotChipsMovedToWinner;
        }

        #region DEBUG
        [Space(20)]
        public int Debug_inGamePlayersRecords;
        public int Debug_playersHUDList;

        private void Update()
        {
            Debug_inGamePlayersRecords = _inGamePlayersRecords.Count;
            Debug_playersHUDList = _playersHUDList.Count;
        }
        #endregion DEBUG

        void OnActionBroadcast(ActionBroadcast m)
        {
            if (_inGamePlayersRecords.ContainsKey(m.PlayerId))
            {
                _inGamePlayersRecords[m.PlayerId].SetCancelTurnTime();
                _inGamePlayersRecords[m.PlayerId].SetEnableActionHolder(true);
                _inGamePlayersRecords[m.PlayerId].SetActionBroadcast($"{m.Action}");

                if (m.Action == PokerActionType.Check) AudioManager.main.PlayAudio("Actions", 0);
            }

            if (!_inGamePlayersRecords.ContainsKey(m.PlayerId)) return;

            if (m.Action == PokerActionType.Bet ||
                m.Action == PokerActionType.Call ||
                m.Action == PokerActionType.Raise ||
                m.Action == PokerActionType.AllIn)
            {
                var currentBet = _betThisRound.TryGetValue(m.PlayerId, out var b) ? b : 0;
                var delta = m.Amount > 0 ? (int)m.Amount : Math.Max(0, (int)m.CurrentBet - currentBet);
                if (delta > 0)
                {
                    var stack = _displayStacks.TryGetValue(m.PlayerId, out var s) ? s : 0;
                    stack = Math.Max(0, stack - delta);
                    _displayStacks[m.PlayerId] = stack;
                    _betThisRound[m.PlayerId] = currentBet + delta;
                    _inGamePlayersRecords[m.PlayerId].UpdateChipsAmount(stack);
                }
            }
        }

        void OnTableSnapshot(TableSnapshot m)
        {
            var dealerId = "?";
            var sbId = "?";
            var bbId = "?";

            foreach (var p in m.Players)
            {
                if (p.Seat == m.DealerSeat)
                {
                    if (_inGamePlayersRecords.ContainsKey(_last_dealerId))
                        _inGamePlayersRecords[_last_dealerId].SetTag(TagType.Dealer, false);
                    dealerId = p.PlayerId;
                }
                if (p.Seat == m.SmallBlindSeat)
                {
                    if (_inGamePlayersRecords.ContainsKey(_last_sbId))
                        _inGamePlayersRecords[_last_sbId].SetTag(TagType.Dealer, false);
                    sbId = p.PlayerId;
                }
                if (p.Seat == m.BigBlindSeat)
                {
                    if (_inGamePlayersRecords.ContainsKey(_last_bbId))
                        _inGamePlayersRecords[_last_bbId].SetTag(TagType.Dealer, false);
                    bbId = p.PlayerId;
                }
            }

            if (_inGamePlayersRecords.Count != 0)
            {
                if (_inGamePlayersRecords.ContainsKey(dealerId))
                {
                    _inGamePlayersRecords[dealerId].SetTag(TagType.Dealer, true);
                    _last_dealerId = dealerId;
                }
                if (_inGamePlayersRecords.ContainsKey(sbId))
                {
                    _inGamePlayersRecords[sbId].SetTag(TagType.SmallBlind, true);
                    _last_sbId = sbId;
                }
                if (_inGamePlayersRecords.ContainsKey(bbId))
                {
                    _inGamePlayersRecords[bbId].SetTag(TagType.BigBlind, true);
                    _last_bbId = bbId;
                }
            }


            if (_currentTableState != m.State)
            {
                foreach (var p in _inGamePlayersRecords)
                    p.Value.SetEnableActionHolder(false);

                _currentTableState = m.State;
            }

            foreach (var p in m.Players)
            {
                if (!_inGamePlayersRecords.ContainsKey(p.PlayerId))
                    SummonPlayerHUDUI(p.Seat - 1, $"P-{p.PlayerId}-{p.Seat}", p.PlayerId, (int)p.Stack);
                _displayStacks[p.PlayerId] = (int)p.Stack;
                _betThisRound[p.PlayerId] = (int)p.BetThisRound;
            }

            PlayerHUDListListenerEvent?.Invoke(_inGamePlayersRecords);

            if (m.State == TableState.Reset)
            {
                foreach (var p in _inGamePlayersRecords)
                    p.Value.SetEnableActionHolder(false);
            }
            if (m.State == TableState.Waiting)
            {
                foreach (var p in _inGamePlayersRecords)
                    p.Value.SetEnableActionHolder(false);
            }
        }

        void OnTurnUpdate(TurnUpdate m)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var remainingMs = m.DeadlineUnixMs > 0 ? Math.Max(0, (long)m.DeadlineUnixMs - nowMs) : 0;

            _inGamePlayersRecords[m.PlayerId].UpdateChipsAmount((int)m.Stack);
            _displayStacks[m.PlayerId] = (int)m.Stack;
            _inGamePlayersRecords[m.PlayerId].SetTurn();
        }

        void OnStackUpdate(StackUpdate m)
        {
            _inGamePlayersRecords[m.PlayerId].UpdateChipsAmount((int)m.Stack);
            _displayStacks[m.PlayerId] = (int)m.Stack;
        }

        void OnHandResult(HandResult m)
        {
            foreach (var winner in m.Winners)
            {
                if (!_inGamePlayersRecords.ContainsKey(winner.PlayerId)) continue;
                _pendingWinAmounts.AddOrUpdate(winner.PlayerId, (int)winner.Amount, (_, v) => v + (int)winner.Amount);
                _betThisRound[winner.PlayerId] = 0;
            }
        }

        void OnPotChipsMovedToWinner(string playerId)
        {
            if (!_pendingWinAmounts.TryRemove(playerId, out var winAmount)) return;
            if (!_inGamePlayersRecords.ContainsKey(playerId)) return;

            var current = _displayStacks.TryGetValue(playerId, out var s) ? s : 0;
            var updated = current + winAmount;
            _displayStacks[playerId] = updated;
            _inGamePlayersRecords[playerId].UpdateChipsAmount(updated);
        }

        void OnMessage(MsgType type, IMessage msg)
        {
            switch (type)
            {
                case MsgType.ActionBroadcast: OnActionBroadcast((ActionBroadcast)msg); break;
                case MsgType.TableSnapshot: OnTableSnapshot((TableSnapshot)msg); break;
                case MsgType.TurnUpdate: OnTurnUpdate((TurnUpdate)msg); break;
                case MsgType.StackUpdate: OnStackUpdate((StackUpdate)msg); break;
                case MsgType.HandResult: OnHandResult((HandResult)msg); break;
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
            _inGamePlayersRecords.TryAdd(playerID, p);

            p.transform.localPosition = seat == _playerIndex ? _playersTablePositions[_playerIndex].localPosition : _playersTablePositions[seat].localPosition;

            p.InititalizePlayerHUDUI(playerID, playerName, seat == _playerIndex, 1, _sampleAvatars[UnityEngine.Random.Range(1, _sampleAvatars.Length)], stackAmount);
            p.SetSeatIndex(seat);
            
            if(seat == _playerIndex && _currentPlayerHUD == null) _currentPlayerHUD = p;
        }
    }
}
